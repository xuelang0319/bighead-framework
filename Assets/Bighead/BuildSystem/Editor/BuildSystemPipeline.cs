#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// Addressables 构建管线：全量/增量打包
    /// 内置 Platform/Version 拼接逻辑，用户只需配置根路径
    /// </summary>
    public static class BuildSystemPipeline
    {
        private const string kBuildVar = "Bighead.BuildPath";
        private const string kLoadVar  = "Bighead.LoadPath";

        public static async UniTask RunAsync(BuildSystemSetting setting, BuildConfigSection section = null)
        {
            if (setting?.SelectedPlatforms == null || setting.SelectedPlatforms.Length == 0)
            {
                Debug.LogWarning("[BuildSystemPipeline] 未选择平台，已取消。");
                return;
            }

            foreach (var platform in setting.SelectedPlatforms)
            {
                await SwitchPlatformAsync(platform);

                // 拼接最终路径：根目录 + 平台 + 版本号
                string version = PlayerSettings.bundleVersion;
                string resolvedBuildPath = Path.Combine(setting.BuildPath, platform.ToString(), version);
                string resolvedLoadPath  = setting.SyncConfig.Enable
                    ? Path.Combine(setting.SyncConfig.DownloadPath, platform.ToString(), version)
                    : resolvedBuildPath;

                ApplyOutputConfig(resolvedBuildPath, resolvedLoadPath);
                await ExecuteAsync(setting.Mode, resolvedBuildPath, resolvedLoadPath, section, setting.BuildPath);
            }
        }

        private static async UniTask SwitchPlatformAsync(BuildTarget platform)
        {
            if (EditorUserBuildSettings.activeBuildTarget == platform) return;
            var group = UnityEditor.BuildPipeline.GetBuildTargetGroup(platform);
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, platform);
            await UniTask.WaitUntil(() => EditorUserBuildSettings.activeBuildTarget == platform);
        }

        private static void ApplyOutputConfig(string buildPath, string loadPath)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[BuildSystemPipeline] 找不到 AddressableAssetSettings");
                return;
            }

            var profiles  = settings.profileSettings;
            var profileId = settings.activeProfileId;

            EnsureProfileVariable(profiles, profileId, kBuildVar, buildPath);
            EnsureProfileVariable(profiles, profileId, kLoadVar,  loadPath);

            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null) continue;

                schema.BuildPath.SetVariableByName(settings, kBuildVar);
                schema.LoadPath.SetVariableByName(settings, kLoadVar);
                EditorUtility.SetDirty(schema);
            }

            AssetDatabase.SaveAssets();
        }

        private static void EnsureProfileVariable(AddressableAssetProfileSettings profiles, string profileId, string name, string value)
        {
            var data = profiles.GetProfileDataByName(name);
            if (data == null)
            {
                profiles.CreateValue(name, value);
                Debug.Log($"[BuildSystemPipeline] 创建 Profile 变量: {name} = {value}");
            }
            else
            {
                profiles.SetValue(profileId, name, value);
                Debug.Log($"[BuildSystemPipeline] 更新 Profile 变量: {name} = {value}");
            }
        }

        private static async UniTask ExecuteAsync(BuildMode mode, string buildPath, string loadPath, BuildConfigSection section, string rootBuildPath)
        {
            Debug.Log($"[BuildSystemPipeline] 开始构建: {mode} | BuildPath={buildPath}");

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            bool buildSuccess = false;

            switch (mode)
            {
                case BuildMode.FullBuild:
                    AddressableAssetSettings.BuildPlayerContent();
                    section?.RefreshIncrementalAvailable();
                    Debug.Log("[BuildSystemPipeline] 全量打包完成");
                    buildSuccess = true;
                    break;

                case BuildMode.Incremental:
                    string catalogPath = Path.Combine(buildPath, "catalog.json");
                    if (!File.Exists(catalogPath))
                    {
                        Debug.LogError("[BuildSystemPipeline] 找不到上次构建的 catalog.json，无法执行增量打包");
                        break;
                    }

                    var result = ContentUpdateScript.BuildContentUpdate(settings, catalogPath);
                    if (result != null && string.IsNullOrEmpty(result.Error))
                    {
                        Debug.Log("[BuildSystemPipeline] 增量打包完成");
                        buildSuccess = true;
                    }
                    else
                    {
                        Debug.LogError("[BuildSystemPipeline] 增量打包失败: " + result?.Error);
                    }
                    break;
            }

            if (buildSuccess)
            {
                string rootFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", rootBuildPath));
                if (Directory.Exists(rootFullPath))
                    EditorUtility.RevealInFinder(rootFullPath);
                else
                    Debug.LogWarning("[BuildSystemPipeline] 输出目录不存在：" + rootFullPath);

                await UniTask.Yield();
            }
        }
    }
}
#endif
