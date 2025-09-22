#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace Bighead.BuildSystem.Editor
{
    public static class AssetPackPipeline
    {
        public static void BuildForPlatform(BuildTarget target)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("打包失败", "AddressableAssetSettings 不存在，请先初始化 Addressables。", "确定");
                return;
            }

            try
            {
                // 关键：切换 Unity 活动平台，确保 Addressables 为正确平台构建
                var group = BuildPipeline.GetBuildTargetGroup(target);
                if (EditorUserBuildSettings.activeBuildTarget != target)
                {
                    Debug.Log($"[Bighead] 切换平台到 {target}...");
                    if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
                    {
                        Debug.LogError($"[Bighead] 平台切换失败: {target}，终止打包。");
                        return;
                    }
                }

                // 阶段 1：清空
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 1/4", "清理 Addressables...", 0);
                ClearAllGroups(settings);
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 1/4", "清理完成", 1);

                // 阶段 2：灌入
                SyncEntries(settings, target);

                // 阶段 3：构建
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 3/4", $"构建 Player Content ({target})...", 0);
                AddressableAssetSettings.BuildPlayerContent();
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 3/4", "构建完成", 1);

                // 阶段 4：再次清空
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 4/4", "清理临时 Group...", 0);
                ClearAllGroups(settings);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 4/4", "清理完成", 1);

                sw.Stop();
                Debug.Log($"[Bighead] {target} 平台打包完成，用时 {sw.ElapsedMilliseconds / 1000f:F2}s");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ClearAllGroups(AddressableAssetSettings settings)
        {
            // 只清空 Addressables Group，不碰 AssetPackSO
            var toRemove = settings.groups.Where(g => g != null && g.Name != "Built In Data").ToList();
            foreach (var g in toRemove)
            {
                settings.RemoveGroup(g);
            }
            Debug.Log($"[Bighead] 已清空 Addressables Group，共移除 {toRemove.Count} 个");
        }

        private static void SyncEntries(AddressableAssetSettings settings, BuildTarget target)
        {
            var so = AssetPackProvider.LoadOrCreate();

            // 统计文件总数用于进度条
            var allFiles = new List<string>();
            foreach (var entry in so.Entries)
            {
                if (Directory.Exists(entry.Path))
                    allFiles.AddRange(GetFilesInDirectory(entry.Path));
                else if (File.Exists(entry.Path))
                    allFiles.Add(entry.Path);
            }
            int total = allFiles.Count;
            int processed = 0;

            EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 2/4", $"灌入条目 (0/{total})", 0);

            foreach (var entry in so.Entries)
            {
                var groupName = $"[Bighead] {entry.Path}";
                var group = settings.CreateGroup(groupName, false, false, false, null,
                    typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

                var files = Directory.Exists(entry.Path)
                    ? GetFilesInDirectory(entry.Path)
                    : new List<string> { entry.Path };

                foreach (var file in files)
                {
                    processed++;
                    float p = total > 0 ? processed / (float)total : 1f;
                    EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 2/4",
                        $"灌入条目 ({processed}/{total}): {Path.GetFileName(file)}", p);

                    var guid = AssetDatabase.AssetPathToGUID(file);
                    if (string.IsNullOrEmpty(guid)) continue;

                    var addrEntry = settings.CreateOrMoveEntry(guid, group);
                    addrEntry.address = file;
                    addrEntry.labels.Clear();
                    foreach (var l in entry.SelectedLabels)
                        addrEntry.SetLabel(l, true);
                }
            }

            EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 2/4", "灌入完成", 1);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Bighead] 已灌入 {processed} 个资源");
        }

        private static List<string> GetFilesInDirectory(string dir)
        {
            return Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".meta"))
                .Select(NormalizeAssetPath)
                .ToList();
        }

        private static string NormalizeAssetPath(string fullPath)
        {
            fullPath = fullPath.Replace("\\", "/");
            return fullPath.StartsWith(Application.dataPath)
                ? "Assets" + fullPath.Substring(Application.dataPath.Length)
                : fullPath;
        }

        public static void OpenOutputFolder()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("打开失败", "AddressableAssetSettings 不存在，请先初始化 Addressables。", "确定");
                return;
            }

            string profileId = settings.activeProfileId;
            string rawBuildPath = settings.profileSettings.GetValueByName(profileId, AddressableAssetSettings.kBuildPath);
            string resolvedPath = settings.profileSettings.EvaluateString(profileId, rawBuildPath);
            string fullPath = Path.GetFullPath(resolvedPath);

            if (!Directory.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("未找到目录", $"输出目录不存在：\n{fullPath}\n请先执行一次打包。", "确定");
                return;
            }

            EditorUtility.RevealInFinder(fullPath);
            Debug.Log($"[Bighead] 打开打包结果目录：{fullPath}");
        }

    }
}
#endif
