using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace PowerMath.Editor
{
    public static class AddressablesSetupTool
    {
        private const string R2BaseUrl = "https://pub-1de297cf85f444a7b4ca56dd0fc5d4e5.r2.dev/Addressables/[BuildTarget]";

        [MenuItem("PowerMath/Addressables/Setup Remote Addressables", priority = 100)]
        public static void SetupAddressables()
        {
            Debug.Log("[AddressablesSetup] Initializing Addressable Asset Settings...");

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            }

            if (settings == null)
            {
                Debug.LogError("[AddressablesSetup] Failed to get or create AddressableAssetSettings.");
                return;
            }

            // Configure Profile with Cloudflare R2 Remote Load Path
            string profileId = settings.profileSettings.GetProfileId("Default");
            if (string.IsNullOrEmpty(profileId))
            {
                profileId = settings.activeProfileId;
            }

            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteLoadPath, R2BaseUrl);
            settings.profileSettings.SetValue(profileId, AddressableAssetSettings.kRemoteBuildPath, "ServerData/[BuildTarget]");
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);

            Debug.Log($"[AddressablesSetup] Configured Remote Catalog LoadPath to {R2BaseUrl}");

            // Setup Music Group
            SetupAssetGroup(
                settings,
                groupName: "Music_Tracks",
                folderPath: "Assets/Project/Art/Music",
                searchPattern: "*.mp3");

            // Setup Biome Backgrounds Group
            SetupAssetGroup(
                settings,
                groupName: "Biome_Backgrounds",
                folderPath: "Assets/Project/Art/Biome",
                searchPattern: "*.png");

            // Setup Voice Group
            SetupAssetGroup(
                settings,
                groupName: "Voice_Clips",
                folderPath: "Assets/Project/Art/Voice",
                searchPattern: "*.mp3");

            // Setup Enemy Sprites Group
            SetupAssetGroup(
                settings,
                groupName: "Enemy_Sprites",
                folderPath: "Assets/Project/Art/Enemy",
                searchPattern: "*.png");

            // Setup Pet Sprites Group (Recursive for R, SR, SSR)
            SetupAssetGroup(
                settings,
                groupName: "Pet_Sprites",
                folderPath: "Assets/Project/Art/Pet",
                searchPattern: "*.png",
                searchOption: SearchOption.AllDirectories);

            // Setup Weapon Sprites Group
            SetupAssetGroup(
                settings,
                groupName: "Weapon_Sprites",
                folderPath: "Assets/Project/Art/Weapon Asset",
                searchPattern: "*.png");

            // Setup Event Sprites Group
            SetupAssetGroup(
                settings,
                groupName: "Event_Sprites",
                folderPath: "Assets/Project/Art/Event",
                searchPattern: "*.png");

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[AddressablesSetup] Addressable Asset Settings successfully configured and saved!");
        }

        [MenuItem("PowerMath/Addressables/Build Addressables Content", priority = 101)]
        public static void BuildAddressables()
        {
            Debug.Log("[AddressablesSetup] Building Addressables Player Content...");
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log($"[AddressablesSetup] Build completed! Check ServerData/{EditorUserBuildSettings.activeBuildTarget} for bundles.");
        }

        private static void SetupAssetGroup(
            AddressableAssetSettings settings,
            string groupName,
            string folderPath,
            string searchPattern,
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    groupName,
                    setAsDefaultGroup: false,
                    readOnly: false,
                    postEvent: true,
                    schemasToCopy: null,
                    typeof(BundledAssetGroupSchema),
                    typeof(ContentUpdateGroupSchema));
            }

            BundledAssetGroupSchema bundleSchema = group.GetSchema<BundledAssetGroupSchema>();
            if (bundleSchema != null)
            {
                bundleSchema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
                bundleSchema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
                bundleSchema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
                bundleSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
                bundleSchema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
            }

            if (!Directory.Exists(folderPath)) return;

            string[] files = Directory.GetFiles(folderPath, searchPattern, searchOption);
            int addedCount = 0;

            foreach (string file in files)
            {
                string relativePath = file.Replace('\\', '/');
                string guid = AssetDatabase.AssetPathToGUID(relativePath);
                if (string.IsNullOrEmpty(guid)) continue;

                string address = Path.GetFileNameWithoutExtension(relativePath);
                AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
                if (entry != null)
                {
                    entry.address = address;
                    addedCount++;
                }
            }

            Debug.Log($"[AddressablesSetup] Group '{groupName}' configured with {addedCount} remote assets.");
        }
    }
}
