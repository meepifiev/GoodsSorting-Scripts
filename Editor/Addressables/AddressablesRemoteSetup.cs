using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace _Project.Editor.Addressables
{
    public static class AddressablesRemoteSetup
    {
        public const string RemoteBuildPathVariable = AddressableAssetSettings.kRemoteBuildPath;
        public const string RemoteLoadPathVariable = AddressableAssetSettings.kRemoteLoadPath;
        public const string Bucket = "goods-sorting-content";

        private const string RemoteBuildPath = "ServerData/[BuildTarget]";
        private const string RemoteLoadPath = "https://" + YandexObjectStorageClient.Endpoint + "/" + Bucket + "/[BuildTarget]";
        private const string PlayerVersion = "live";

        [MenuItem("Tools/Addressables/Full Setup")]
        public static void FullSetup()
        {
            SetupProfile();
            LevelAddressablesSetup.OrganizeFolders();
            LevelAddressablesSetup.BuildManifest();
            LevelAddressablesSetup.SetupGroups();
            LevelAddressablesSetup.FixDuplicates();
            LevelAddressablesSetup.AnalyzeDuplicates();
        }

        public static void SetupProfile()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            AddressableAssetProfileSettings profiles = settings.profileSettings;
            string profileId = settings.activeProfileId;

            SetVariable(profiles, profileId, RemoteBuildPathVariable, RemoteBuildPath);
            SetVariable(profiles, profileId, RemoteLoadPathVariable, RemoteLoadPath);

            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, RemoteBuildPathVariable);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, RemoteLoadPathVariable);
            settings.OverridePlayerVersion = PlayerVersion;
            settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, null, true, true);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Addressables] Remote profile ready. {RemoteBuildPathVariable} = {RemoteBuildPath}, {RemoteLoadPathVariable} = {profiles.GetValueByName(profileId, RemoteLoadPathVariable)}, remote catalog 'catalog_{PlayerVersion}' enabled.");
        }

        public static void ApplyRemote(AddressableAssetGroup group, BundledAssetGroupSchema.BundlePackingMode packingMode)
        {
            AddressableAssetSettings settings = group.Settings;
            BundledAssetGroupSchema schema = group.GetSchema<BundledAssetGroupSchema>();

            if (schema == null)
                schema = group.AddSchema<BundledAssetGroupSchema>();

            schema.BuildPath.SetVariableByName(settings, RemoteBuildPathVariable);
            schema.LoadPath.SetVariableByName(settings, RemoteLoadPathVariable);
            schema.BundleMode = packingMode;
            schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.AppendHash;
            schema.RetryCount = 3;

            EditorUtility.SetDirty(schema);
        }

        private static void SetVariable(AddressableAssetProfileSettings profiles, string profileId, string name, string value)
        {
            List<string> names = profiles.GetVariableNames();

            if (names.Contains(name) == false)
                profiles.CreateValue(name, value);

            profiles.SetValue(profileId, name, value);
        }
    }
}
