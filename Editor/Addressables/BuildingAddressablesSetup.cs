using _Project.Editor.Building;
using _Project.Features.Building;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace _Project.Editor.Addressables
{
    public static class BuildingAddressablesSetup
    {
        private const string GroupName = "BuildingItems";
        private const string PrefabFolder = "Assets/_Project/Prefabs/Building";
        private const string CatalogPath = "Assets/_Project/Configs/Building/BuildingItemCatalog.asset";
        private const string Label = "buildingitem";

        [MenuItem("Tools/Addressables/Building/Mark Item Prefabs Addressable")]
        public static void MarkItemPrefabs()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetGroup group = FindOrCreateGroup(settings, GroupName);

            settings.AddLabel(Label, false);

            foreach (AddressableAssetEntry stale in new System.Collections.Generic.List<AddressableAssetEntry>(group.entries))
                settings.RemoveAssetEntry(stale.guid, false);

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
            int marked = 0;

            foreach (string g in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                BuildingItemView view = AssetDatabase.LoadAssetAtPath<BuildingItemView>(path);

                if (view == null)
                {
                    continue;
                }

                AddressableAssetEntry entry = settings.CreateOrMoveEntry(g, group, false, false);
                entry.address = "BuildingItem/" + view.ItemId;
                entry.SetLabel(Label, true, false, false);
                marked++;
            }

            AddressablesRemoteSetup.ApplyRemote(group, BundledAssetGroupSchema.BundlePackingMode.PackTogether);

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true, true);

            BuildingItemCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildingItemCatalog>(CatalogPath);
            if (catalog != null)
            {
                EditorUtility.SetDirty(catalog);
            }

            AssetDatabase.SaveAssets();
            BuildingItemPrefabLookup.Invalidate();

            Debug.Log($"[Building] {marked} item prefabs marked Addressable (remote, label '{Label}', address 'BuildingItem/<id>'). Their sprites now ship remotely. Run Content Delivery Build + Upload.");
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
    }
}
