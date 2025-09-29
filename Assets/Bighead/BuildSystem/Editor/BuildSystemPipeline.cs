#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// Addressables 全量/增量构建管线（队列分帧版：稳定且改动极小）
    /// - 每平台独立 BuildPath / ContentState（.bin 直出到平台/版本目录）
    /// - Remote Catalog 打开并与 Build/LoadPath 变量一致（满足增量校验）
    /// - 不在循环中 Refresh / 多次 SaveAssets / 每平台弹资源管理器
    /// - 用 EditorApplication.delayCall 将“多平台连续构建”拆成多个 Editor 回合，每次只跑一个平台
    /// - 仅当增量命中 “BuildLayout header” 时，下一帧重试一次
    /// - 整轮结束后 SaveAssets 一次并打开根目录
    /// </summary>
    public static class BuildSystemPipeline
    {
        private const string kBuildVar        = "Bighead.BuildPath";
        private const string kLoadVar         = "Bighead.LoadPath";
        private const string kContentStateVar = "Bighead.ContentStatePath";

        // --- 构建队列管理（关键：避免同一 Editor 回合内连续触发 SBP） ---
        private static readonly Queue<BuildPlatformSetting> _queue = new Queue<BuildPlatformSetting>();
        private static bool _running;
        private static BuildSystemSetting _settingSnapshot;
        private static BuildConfigSection _sectionSnapshot;

        // 修改字段

        public static UniTask RunAsync(BuildSystemSetting setting, BuildConfigSection section = null)
        {
            _settingSnapshot = setting;
            _sectionSnapshot = section;

            _queue.Clear();
            if (setting?.BuildPlatformSettings != null)
            {
                foreach (var p in setting.BuildPlatformSettings)
                {
                    if (p != null && p.Platform != BuildTarget.NoTarget)
                        _queue.Enqueue(p);
                }
            }

            if (_queue.Count == 0)
            {
                Debug.LogWarning("[BuildSystemPipeline] 未选择平台，已取消。");
                return UniTask.CompletedTask;
            }

            if (_running)
            {
                Debug.LogWarning("[BuildSystemPipeline] 上一轮构建尚未结束。");
                return UniTask.CompletedTask;
            }

            _running = true;
            EditorApplication.delayCall += ProcessNext;
            return UniTask.CompletedTask;
        }

        private static void ProcessNext()
        {
            if (_queue.Count == 0)
            {
                AssetDatabase.SaveAssets();
                string rootFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", _settingSnapshot.BuildPath));
                if (Directory.Exists(rootFullPath))
                    EditorUtility.RevealInFinder(rootFullPath);

                _running = false;
                Debug.Log("[BuildSystemPipeline] 多平台构建完成。");
                return;
            }

            var platformSetting = _queue.Dequeue();
            BuildOneAsync(_settingSnapshot, platformSetting).Forget();

            // 下一平台同样放到下一次 Editor 回合
            EditorApplication.delayCall += ProcessNext;
        }

        // --- 单个平台构建 ---
        private static async UniTask BuildOneAsync(BuildSystemSetting setting, BuildPlatformSetting platformSetting)
        {
            if (platformSetting == null || platformSetting.Platform == BuildTarget.NoTarget)
            {
                Debug.LogError("[BuildSystemPipeline] 无效的 BuildPlatformSetting");
                return;
            }

            var platform = platformSetting.Platform;

            // 1. 切换平台
            if (EditorUserBuildSettings.activeBuildTarget != platform)
            {
                var group = UnityEditor.BuildPipeline.GetBuildTargetGroup(platform);
                EditorUserBuildSettings.SwitchActiveBuildTarget(group, platform);
                await UniTask.WaitUntil(() => EditorUserBuildSettings.activeBuildTarget == platform);
            }

            // 2. 拼接路径
            string version           = PlayerSettings.bundleVersion;
            string platformBuildPath = Path.Combine(setting.BuildPath, platform.ToString(), version);
            string platformLoadPath  = platformBuildPath; // 不再使用全局 SyncConfig

            // 3. 应用输出配置
            ApplyOutputConfig(platformBuildPath, platformLoadPath);

            // 4. 执行构建
            await ExecuteAsync(setting.Mode, platformBuildPath, platformLoadPath, null, setting.BuildPath, platform);
        }


        /// <summary>
        /// 写入（内存中）Profile 变量并绑定 Group Schema；不在此处调用 Refresh/SaveAssets。
        /// </summary>
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

            EnsureProfileValue(profiles, profileId, kBuildVar,        buildPath);
            EnsureProfileValue(profiles, profileId, kLoadVar,         loadPath);
            EnsureProfileValue(profiles, profileId, kContentStateVar, buildPath); // 让 .bin 直接生成到 BuildPath

            // 绑定所有 Bundled 组的 Build/LoadPath 到变量（仅标脏，不保存）
            foreach (var group in settings.groups)
            {
                if (group == null) continue;
                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null) continue;

                schema.BuildPath.SetVariableByName(settings, kBuildVar);
                schema.LoadPath.SetVariableByName(settings, kLoadVar);
                EditorUtility.SetDirty(schema);
            }

            // Content State 直出到我们指定目录
            settings.ContentStateBuildPath = $"[{kContentStateVar}]";

            // Remote Catalog：增量校验要求一致
            settings.BuildRemoteCatalog = true;
            settings.RemoteCatalogBuildPath.SetVariableByName(settings, kBuildVar);
            settings.RemoteCatalogLoadPath.SetVariableByName(settings, kLoadVar);

            // 标记 settings 脏（不立即保存）
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureProfileValue(AddressableAssetProfileSettings profiles, string profileId, string name, string value)
        {
            var exists = profiles.GetValueByName(profileId, name);
            if (exists == null)
            {
                profiles.CreateValue(name, value);
                Debug.Log($"[BuildSystemPipeline] 创建 Profile 变量: {name} = {value}");
            }
            else
            {
                profiles.SetValue(profileId, name, value);
            }
        }

        /// <summary>
        /// 解析当前 Profile 下 Addressables 实际使用的 BuildPath（最终磁盘路径）
        /// </summary>
        private static string GetEvaluatedBuildPath(AddressableAssetSettings settings)
        {
            var profiles  = settings.profileSettings;
            var profileId = settings.activeProfileId;

            var raw = profiles.GetValueByName(profileId, kBuildVar);
            if (string.IsNullOrEmpty(raw))
            {
                var fallback = AddressableAssetSettingsDefaultObject.kDefaultConfigFolder;
                Debug.LogWarning("[BuildSystemPipeline] 未取到 Bighead.BuildPath 变量，使用默认路径: " + fallback);
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", fallback));
            }
            var evaluated = profiles.EvaluateString(profileId, raw);
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", evaluated));
        }

        private static async UniTask ExecuteAsync(BuildMode mode, string buildPath, string loadPath, BuildConfigSection section, string rootBuildPath, BuildTarget platform)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[BuildSystemPipeline] 找不到 AddressableAssetSettings");
                return;
            }

            // 真实 BuildPath（按当前 Profile 解析）
            string evaluatedBuildPath = GetEvaluatedBuildPath(settings);
            Directory.CreateDirectory(evaluatedBuildPath);

            // Content State 文件实际位置（由 settings.ContentStateBuildPath 决定）
            string contentStatePath = UnityEditor.AddressableAssets.Build.ContentUpdateScript.GetContentStateDataPath(false);

            Debug.Log($"[BuildSystemPipeline] 开始构建: {mode} | 平台={platform}\n" +
                      $" - BuildPath(变量)   = {buildPath}\n" +
                      $" - BuildPath(解析)   = {evaluatedBuildPath}\n" +
                      $" - ContentState(.bin)= {contentStatePath}");

            bool buildSuccess = false;

            switch (mode)
            {
                case BuildMode.FullBuild:
                {
                    AddressableAssetSettings.BuildPlayerContent();

                    if (File.Exists(contentStatePath))
                        Debug.Log($"[BuildSystemPipeline] 平台 {platform} Content State 生成完成");
                    else
                        Debug.LogWarning($"[BuildSystemPipeline] 平台 {platform} 未找到 Content State 文件，增量可能不可用");

                    section?.RefreshIncrementalAvailable();
                    Debug.Log($"[BuildSystemPipeline] 平台 {platform} 全量打包完成");
                    buildSuccess = true;
                    break;
                }

                case BuildMode.Incremental:
                {
                    if (!File.Exists(contentStatePath))
                    {
                        Debug.LogError($"[BuildSystemPipeline] 平台 {platform} 缺少 Content State（{contentStatePath}），请先执行该平台的全量打包");
                        break;
                    }

                    // 第一次尝试
                    var (ok, retryable) = TryBuildContentUpdateOnce(settings, contentStatePath, platform);
                    if (!ok && retryable)
                    {
                        // 仅在命中 BuildLayout header 时，下一帧重试一次（不做额外延时/刷新）
                        await UniTask.NextFrame();
                        (ok, _) = TryBuildContentUpdateOnce(settings, contentStatePath, platform);
                    }

                    buildSuccess = ok;
                    break;
                }
            }

            if (buildSuccess)
            {
                // 保持让步，避免阻塞 UI；不在这里打开资源管理器
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// 执行一次 ContentUpdate；返回 (成功, 是否可重试[仅当 BuildLayout header 异常])
        /// </summary>
        private static (bool success, bool retryable) TryBuildContentUpdateOnce(AddressableAssetSettings settings, string contentStatePath, BuildTarget platform)
        {
            try
            {
                var result = UnityEditor.AddressableAssets.Build.ContentUpdateScript.BuildContentUpdate(settings, contentStatePath);
                if (result != null && string.IsNullOrEmpty(result.Error))
                {
                    Debug.Log($"[BuildSystemPipeline] 平台 {platform} 增量打包完成");
                    return (true, false);
                }

                if (!string.IsNullOrEmpty(result?.Error) &&
                    result.Error.IndexOf("BuildLayout header", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.LogWarning($"[BuildSystemPipeline] 平台 {platform} 增量失败（BuildLayout header），可重试一次。");
                    return (false, true);
                }

                Debug.LogError($"[BuildSystemPipeline] 平台 {platform} 增量打包失败: {result?.Error}");
                return (false, false);
            }
            catch (Exception e)
            {
                var msg = e.Message ?? string.Empty;
                if (msg.IndexOf("BuildLayout header", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Debug.LogWarning($"[BuildSystemPipeline] 平台 {platform} 捕获 BuildLayout 异常，可重试一次：{e.Message}");
                    return (false, true);
                }

                Debug.LogError($"[BuildSystemPipeline] 平台 {platform} 增量异常：{e}");
                return (false, false);
            }
        }
    }
}
#endif
