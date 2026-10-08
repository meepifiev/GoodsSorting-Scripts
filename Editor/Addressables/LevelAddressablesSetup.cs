using System.Collections.Generic;
using System.Text;
using _Project.Features.Legacy.Levels;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace _Project.Editor.Addressables
{
    public static class LevelAddressablesSetup
    {
        private const string LevelsFolder = "Assets/_Project/Configs/Level";
        private const string ActiveFolder = LevelsFolder + "/Active";
        private const string DisabledFolder = LevelsFolder + "/Disabled";
        private const string ContainerPath = LevelsFolder + "/LevelContainer.asset";
        private const string ManifestPath = LevelsFolder + "/LevelManifest.asset";

        private const string LevelsGroupName = "Levels";
        private const string MetaGroupName = "LevelsMeta";
        private const string SharedGroupName = "Shared";
        private const string IsolationGroupName = "Duplicate Asset Isolation";

        private const int BatchSize = 100;

        public static string LevelAddress(int levelNumber) => "Level/" + levelNumber;

        [MenuItem("Tools/Addressables/Levels/1. Organize Folders (Active-Disabled)")]
        public static void OrganizeFolders()
        {
            EnsureFolder(ActiveFolder);
            EnsureFolder(DisabledFolder);

            string[] guids = AssetDatabase.FindAssets("t:LegacyLevelConfig", new[] { LevelsFolder });
            int movedActive = 0;
            int movedDisabled = 0;

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    LegacyLevelConfig config = AssetDatabase.LoadAssetAtPath<LegacyLevelConfig>(path);

                    if (config == null)
                        continue;

                    string targetFolder = config.IsDisabled ? DisabledFolder : ActiveFolder;
                    string fileName = System.IO.Path.GetFileName(path);
                    string targetPath = targetFolder + "/" + fileName;

                    if (path == targetPath)
                        continue;

                    string error = AssetDatabase.MoveAsset(path, targetPath);

                    if (string.IsNullOrEmpty(error) == false)
                    {
                        Debug.LogWarning($"[Levels] Move failed for {path}: {error}");
                        continue;
                    }

                    if (config.IsDisabled)
                        movedDisabled++;
                    else
                        movedActive++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[Levels] Folders organized: {movedActive} -> Active, {movedDisabled} -> Disabled (assets already in place were skipped).");
        }

        [MenuItem("Tools/Addressables/Levels/2. Build Manifest")]
        public static void BuildManifest()
        {
            SerializedProperty configs = LoadContainerConfigs(out _);

            if (configs == null)
                return;

            List<LevelManifest.Entry> entries = new List<LevelManifest.Entry>();
            int skipped = 0;

            for (int i = 0; i < configs.arraySize; i++)
            {
                LegacyLevelConfig config = configs.GetArrayElementAtIndex(i).objectReferenceValue as LegacyLevelConfig;

                if (config == null || config.IsDisabled)
                {
                    skipped++;
                    continue;
                }

                int number = entries.Count + 1;
                entries.Add(new LevelManifest.Entry { address = LevelAddress(number), disabled = false });
            }

            LevelManifest manifest = AssetDatabase.LoadAssetAtPath<LevelManifest>(ManifestPath);

            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<LevelManifest>();
                AssetDatabase.CreateAsset(manifest, ManifestPath);
            }

            manifest.EditorSetEntries(entries.ToArray());
            EditorUtility.SetDirty(manifest);

            RegisterManifestEntry(manifest);

            AssetDatabase.SaveAssets();

            Debug.Log($"[Levels] Manifest built: {entries.Count} playable levels ({skipped} disabled/empty slots dropped), address '{AddressableLevelCatalogManifestAddress}'.");
        }

        [MenuItem("Tools/Addressables/Levels/3. Setup Groups")]
        public static void SetupGroups()
        {
            SerializedProperty configs = LoadContainerConfigs(out _);

            if (configs == null)
                return;

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetGroup levelsGroup = FindOrCreateGroup(settings, LevelsGroupName);

            List<AddressableAssetEntry> stale = new List<AddressableAssetEntry>(levelsGroup.entries);

            foreach (AddressableAssetEntry entry in stale)
                settings.RemoveAssetEntry(entry.guid, false);

            int number = 0;
            HashSet<string> labels = new HashSet<string>();

            for (int i = 0; i < configs.arraySize; i++)
            {
                LegacyLevelConfig config = configs.GetArrayElementAtIndex(i).objectReferenceValue as LegacyLevelConfig;

                if (config == null || config.IsDisabled)
                    continue;

                string path = AssetDatabase.GetAssetPath(config);
                string guid = AssetDatabase.AssetPathToGUID(path);

                if (string.IsNullOrEmpty(guid))
                    continue;

                number++;

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, levelsGroup, false, false);
                entry.address = LevelAddress(number);

                string label = "batch" + (number - 1) / BatchSize;

                if (labels.Add(label))
                    settings.AddLabel(label, false);

                entry.SetLabel(label, true, false, false);
            }

            AddressablesRemoteSetup.ApplyRemote(levelsGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Levels] Group '{LevelsGroupName}' ready: {number} active levels, {labels.Count} batches (~{BatchSize}/bundle), remote, PackTogetherByLabel + LZ4.");
        }

        [MenuItem("Tools/Addressables/Levels/4. Fix Duplicate Dependencies")]
        public static void FixDuplicates()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            CheckBundleDupeDependencies rule = new CheckBundleDupeDependencies();
            rule.RefreshAnalysis(settings);
            rule.FixIssues(settings);

            AddressableAssetGroup isolationGroup = settings.FindGroup(IsolationGroupName);
            AddressableAssetGroup sharedGroup = settings.FindGroup(SharedGroupName);

            if (isolationGroup != null)
            {
                if (sharedGroup == null)
                {
                    isolationGroup.Name = SharedGroupName;
                    sharedGroup = isolationGroup;
                }
                else
                {
                    List<AddressableAssetEntry> moved = new List<AddressableAssetEntry>(isolationGroup.entries);

                    foreach (AddressableAssetEntry entry in moved)
                        settings.MoveEntry(entry, sharedGroup, false, false);

                    settings.RemoveGroup(isolationGroup);
                }
            }

            if (sharedGroup != null)
                AddressablesRemoteSetup.ApplyRemote(sharedGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogether);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
            AssetDatabase.SaveAssets();

            int shared = sharedGroup != null ? sharedGroup.entries.Count : 0;
            Debug.Log($"[Levels] Duplicates isolated into '{SharedGroupName}' (remote, PackTogether): {shared} assets. Re-run Analyze to confirm 0.");
        }

        [MenuItem("Tools/Addressables/Levels/5. Analyze Duplicate Dependencies")]
        public static void AnalyzeDuplicates()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);

            CheckBundleDupeDependencies rule = new CheckBundleDupeDependencies();
            List<AnalyzeRule.AnalyzeResult> results = rule.RefreshAnalysis(settings);

            HashSet<string> distinctAssets = new HashSet<string>();

            foreach (AnalyzeRule.AnalyzeResult result in results)
            {
                if (result == null || string.IsNullOrEmpty(result.resultName) || result.severity == MessageType.None)
                    continue;

                int delimiter = result.resultName.LastIndexOf(':');
                string asset = delimiter >= 0 ? result.resultName.Substring(delimiter + 1) : result.resultName;
                distinctAssets.Add(asset);
            }

            if (distinctAssets.Count == 0)
            {
                Debug.Log("[Levels] Duplicate deps: none. Shared assets are isolated.");
                return;
            }

            Debug.Log($"[Levels] Duplicate deps: {distinctAssets.Count} distinct duplicated assets. Run 'Fix Duplicate Dependencies'.");
        }

        private const string AddressableLevelCatalogManifestAddress = AddressableLevelCatalog.ManifestAddress;

        private static void RegisterManifestEntry(LevelManifest manifest)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetGroup metaGroup = FindOrCreateGroup(settings, MetaGroupName);

            string path = AssetDatabase.GetAssetPath(manifest);
            string guid = AssetDatabase.AssetPathToGUID(path);

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, metaGroup, false, false);
            entry.address = AddressableLevelCatalogManifestAddress;

            AddressablesRemoteSetup.ApplyRemote(metaGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogether);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);
        }

        private static AddressableAssetGroup FindOrCreateGroup(AddressableAssetSettings settings, string name)
        {
            AddressableAssetGroup group = settings.FindGroup(name);

            if (group == null)
            {
                group = settings.CreateGroup(
                    name,
                    false,
                    false,
                    true,
                    null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));
            }

            return group;
        }

        private static SerializedProperty LoadContainerConfigs(out SerializedObject serialized)
        {
            serialized = null;

            LevelContainer container = AssetDatabase.LoadAssetAtPath<LevelContainer>(ContainerPath);

            if (container == null)
            {
                Debug.LogError($"[Levels] LevelContainer not found at {ContainerPath}. It stays in the project as the ordering source for the manifest.");
                return null;
            }

            serialized = new SerializedObject(container);
            SerializedProperty configs = serialized.FindProperty("_legacyLevelConfigs");

            if (configs == null || configs.isArray == false)
            {
                Debug.LogError("[Levels] _legacyLevelConfigs not found on LevelContainer.");
                return null;
            }

            return configs;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;

            string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(folder);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
