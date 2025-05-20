using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEngine;

namespace Bighead
{
    public static class AddressableManager
    {
        #region Addressable Build

        public static void BuildAddressableForAllPlatforms()
        {
            BuildAddressable(BuildTarget.StandaloneWindows64);
            BuildAddressable(BuildTarget.Android);
            BuildAddressable(BuildTarget.iOS);
            Debug.Log("Addressable build for all platforms completed.");
        }

        public static void BuildAddressable(BuildTarget target)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable settings not found!");
                return;
            }

            string buildPath = $"AddressableAssets/{target}";
            var builder = settings.ActivePlayerDataBuilder as IDataBuilder;
            if (builder == null)
            {
                Debug.LogError("No active data builder found!");
                return;
            }

            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"Addressable build for {target} failed: {result.Error}");
            }
            else
            {
                Debug.Log($"Addressable build for {target} succeeded. Output path: {buildPath}");
            }
        }

        #endregion
        
        public static List<string> GetAllLabels()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            return settings != null ? new List<string>(settings.GetLabels()) : new List<string>();
        }
    }
}
