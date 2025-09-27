#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using Cysharp.Threading.Tasks;
using System.IO;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 核心打包管线，统一处理全量/增量打包
    /// </summary>
    public static class BuildSystemPipeline
    {
        /// <summary>
        /// 执行多平台构建
        /// </summary>
        public static async UniTask RunAsync(BuildSystemSetting setting, BuildConfigSection section = null)
        {
            foreach (var platform in setting.SelectedPlatforms)
            {
                await SwitchPlatformAsync(platform);

                string buildPath = PathResolver.Resolve(setting.BuildPath, platform);
                string loadPath  = PathResolver.Resolve(setting.SyncConfig.Enable
                    ? setting.SyncConfig.DownloadPath
                    : setting.LocalLoadPath, platform);

                await ExecuteAsync(setting.Mode, buildPath, loadPath, section);
            }
        }

        /// <summary>
        /// 切换 Editor 当前平台
        /// </summary>
        private static async UniTask SwitchPlatformAsync(BuildTarget platform)
        {
            if (EditorUserBuildSettings.activeBuildTarget == platform) return;
            var group = UnityEditor.BuildPipeline.GetBuildTargetGroup(platform);
            EditorUserBuildSettings.SwitchActiveBuildTarget(group, platform);
            await UniTask.WaitUntil(() => EditorUserBuildSettings.activeBuildTarget == platform);
        }

        /// <summary>
        /// 核心执行流程：全量或增量打包
        /// </summary>
        private static async UniTask ExecuteAsync(BuildMode mode, string buildPath, string loadPath, BuildConfigSection section)
        {
            Debug.Log($"[BuildSystemPipeline] 开始构建: {mode} | BuildPath={buildPath}");

            // 1) 配置 Addressables Profile
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            string profileId = settings.activeProfileId;
            settings.profileSettings.SetValue(profileId, "BuildPath", buildPath);
            settings.profileSettings.SetValue(profileId, "LoadPath", loadPath);

            // 2) 执行构建
            switch (mode)
            {
                case BuildMode.FullBuild:
                    await UniTask.Run(() => AddressableAssetSettings.BuildPlayerContent());
                    Debug.Log("[BuildSystemPipeline] 全量打包完成");
                    section?.RefreshIncrementalAvailable(); // 解锁增量打包
                    break;

                case BuildMode.Incremental:
                    string catalogPath = Path.Combine(buildPath, "catalog.json");
                    if (!File.Exists(catalogPath))
                    {
                        Debug.LogError("[BuildSystemPipeline] 找不到上次构建的 catalog.json，无法执行增量打包");
                        return;
                    }

                    AddressablesPlayerBuildResult result = null;
                    await UniTask.Run(() =>
                    {
                        result = ContentUpdateScript.BuildContentUpdate(settings, catalogPath);
                    });

                    if (result != null && string.IsNullOrEmpty(result.Error))
                        Debug.Log("[BuildSystemPipeline] 增量打包完成");
                    else
                        Debug.LogError("[BuildSystemPipeline] 增量打包失败: " + result?.Error);
                    break;
            }

            // 3) 预留后处理步骤，例如上传、签名、生成版本文件
        }
    }
}
#endif
