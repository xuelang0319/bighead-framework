#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine.Networking;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Bighead.BuildSystem.Editor
{
    public static class AssetPackPipeline
    {
        // 我们自定义的 Profile 变量名（避免动到 Unity 内置变量）
        private const string BigheadBuildVar = "Bighead.BuildPath";
        private const string BigheadLoadVar  = "Bighead.LoadPath";

        public static void BuildForPlatform(BuildTarget target)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("打包失败", "AddressableAssetSettings 不存在，请先初始化 Addressables。", "确定");
                return;
            }

            // 1) 切换平台（非常关键）
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

            // 2) 确保 Profile 变量存在并设置为 ServerData/<Target>（不在 Assets 下，避免导入冲突）
            EnsureAndSetProfilePaths(settings, target, out string resolvedBuildPath);

            // 3) 全流程（分段进度条）
            DoBuildInternal(target, settings, resolvedBuildPath);
        }

        private static void EnsureAndSetProfilePaths(AddressableAssetSettings settings, BuildTarget target, out string resolvedBuildPath)
        {
            var profile = settings.profileSettings;
            string profileId = settings.activeProfileId;

            // 如果没这个变量，就创建（默认值给个安全值）
            if (profile.GetValueByName(profileId, BigheadBuildVar) == null)
                profile.CreateValue(BigheadBuildVar, $"ServerData/[BuildTarget]");
            if (profile.GetValueByName(profileId, BigheadLoadVar) == null)
                profile.CreateValue(BigheadLoadVar, $"ServerData/[BuildTarget]");

            // 为当前 Profile 设置我们想要的值（永久生效，按你的要求不恢复）
            string newBuildPath = $"ServerData/{target}";
            string newLoadPath  = newBuildPath;

            profile.SetValue(profileId, BigheadBuildVar, newBuildPath);
            profile.SetValue(profileId, BigheadLoadVar,  newLoadPath);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            // 解析成绝对路径并预创建目录
            string raw = profile.GetValueByName(profileId, BigheadBuildVar);
            string resolved = profile.EvaluateString(profileId, raw);
            resolvedBuildPath = Path.GetFullPath(resolved);
            if (!Directory.Exists(resolvedBuildPath))
            {
                Directory.CreateDirectory(resolvedBuildPath);
                Debug.Log($"[Bighead] 创建输出目录: {resolvedBuildPath}");
            }
        }

        private static void DoBuildInternal(BuildTarget target, AddressableAssetSettings settings, string resolvedBuildPath)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // 阶段 1：清空
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 1/4", "清理 Addressables...", 0);
                ClearAllGroups(settings);
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 1/4", "清理完成", 1);

                // 阶段 2：灌入（返回有效条目数）
                int added = SyncEntries(settings, target);
                if (added <= 0)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("打包中止", "没有可构建的资源条目（条目数为 0）。", "确定");
                    return;
                }

                // 刷新保证新配置生效
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 阶段 3：构建
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 3/4", $"构建 Player Content ({target})...", 0);
                AddressableAssetSettings.BuildPlayerContent();
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 3/4", "构建完成", 1);

                // 阶段 4：清理临时 Group
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 4/4", "清理临时 Group...", 0);
                ClearAllGroups(settings);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 4/4", "清理完成", 1);

                var so = AssetPackProvider.LoadOrCreate();
                if (so.Deploy.SyncUpload)
                {
                    string zipPath = BuildZip(resolvedBuildPath);
                    SyncToServer(zipPath, so.Deploy.ServerAddress, so.Deploy.Port, AssetPackProvider.TempToken, target.ToString());

                    Debug.Log($"[Bighead] 已生成压缩包：{zipPath}");
                }
                
                sw.Stop();
                Debug.Log($"[Bighead] {target} 平台打包完成，用时 {sw.ElapsedMilliseconds / 1000f:F2}s，输出：{resolvedBuildPath}");

                // 打包完成后，直接打开结果目录
                OpenOutputFolder();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ClearAllGroups(AddressableAssetSettings settings)
        {
            var toRemove = settings.groups.Where(g => g != null && g.Name != "Built In Data").ToList();
            foreach (var g in toRemove)
                settings.RemoveGroup(g);

            Debug.Log($"[Bighead] 已清空 Addressables Group，共移除 {toRemove.Count} 个");
        }
        public static async void SyncToServer(string zipPath, string serverIp, int port, string token, string subdir)
        {
            if (string.IsNullOrEmpty(zipPath) || !File.Exists(zipPath))
            {
                Debug.LogError($"[Bighead] SyncToServer 失败：找不到 zip 文件 {zipPath}");
                return;
            }

            string url = $"http://{serverIp}:{port}/deploy?subdir={Uri.EscapeDataString(subdir)}";
            Debug.Log($"[Bighead] 正在上传 {Path.GetFileName(zipPath)} 到 {url} ...");

            try
            {
                using var http = new HttpClient();
                using var content = new StreamContent(File.OpenRead(zipPath));

                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                if (!string.IsNullOrEmpty(token))
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await http.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string respText = await response.Content.ReadAsStringAsync();
                    Debug.Log($"[Bighead] 上传成功：{respText}");
                }
                else
                {
                    string err = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"[Bighead] 上传失败：{response.StatusCode} {err}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bighead] 上传异常：{ex.Message}");
            }
        }
        
        /// <summary>
        /// 将指定目录打包为 Zip 文件，并返回生成的 Zip 文件路径
        /// </summary>
        public static string BuildZip(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                Debug.LogError($"[Bighead] BuildZip 失败：目录不存在 {folderPath}");
                return null;
            }

            // 生成同级目录下的 zip 文件路径
            string zipPath = folderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".zip";

            // 如果已有同名文件，先删除
            if (File.Exists(zipPath))
                File.Delete(zipPath);

            // 使用 .NET 原生压缩功能
            ZipFile.CreateFromDirectory(folderPath, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            Debug.Log($"[Bighead] 目录已压缩为：{zipPath}");
            return zipPath;
        }

        /// <summary>
        /// 将 AssetPackSO 的 Entry 写入 Addressables：
        /// - 每个 Entry = 一个 Group
        /// - 目录则展开为该 Group 的多个条目
        /// - 为每个 Group 绑定 Bighead.BuildPath / Bighead.LoadPath
        /// 返回：新增/更新的总条目数
        /// </summary>
        private static int SyncEntries(AddressableAssetSettings settings, BuildTarget target)
        {
            var so = AssetPackProvider.LoadOrCreate();

            // 汇总总文件数（用于阶段 2 进度条）
            var allFiles = new List<string>();
            foreach (var e in so.Entries)
            {
                if (!string.IsNullOrEmpty(e.Path))
                {
                    if (Directory.Exists(e.Path)) allFiles.AddRange(GetFilesInDirectory(e.Path));
                    else if (File.Exists(e.Path)) allFiles.Add(e.Path);
                }
            }
            int total = allFiles.Count;
            int processed = 0;

            EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 2/4", $"灌入条目 (0/{total})", 0);

            foreach (var entry in so.Entries)
            {
                if (string.IsNullOrEmpty(entry.Path))
                    continue;

                var files = Directory.Exists(entry.Path)
                    ? GetFilesInDirectory(entry.Path)
                    : (File.Exists(entry.Path) ? new List<string> { entry.Path } : new List<string>());

                if (files.Count == 0)
                    continue;

                // 1) 创建/拿到 Group
                string groupName = $"[Bighead] {entry.Path}";
                var group = settings.FindGroup(groupName);
                if (group == null)
                {
                    group = settings.CreateGroup(groupName, false, false, false, null,
                        typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
                }

                // 2) 绑定 Group 的 BuildPath/LoadPath 到我们的 Profile 变量
                var bundled = group.GetSchema<BundledAssetGroupSchema>();
                if (bundled == null) bundled = group.AddSchema<BundledAssetGroupSchema>();

                // 关键：把 schema 的 Profile 引用指向我们创建的变量名
                bundled.BuildPath.SetVariableByName(settings, BigheadBuildVar);
                bundled.LoadPath.SetVariableByName(settings,  BigheadLoadVar);

                // 其它 schema 参数可按需设定（示例：保持默认即可）
                EditorUtility.SetDirty(bundled);

                // 3) 填充条目
                foreach (var file in files)
                {
                    processed++;
                    float p = total > 0 ? processed / (float)total : 1f;
                    EditorUtility.DisplayProgressBar($"Bighead Build ({target}) - 阶段 2/4",
                        $"灌入条目 ({processed}/{total}): {Path.GetFileName(file)}", p);

                    var guid = AssetDatabase.AssetPathToGUID(file);
                    if (string.IsNullOrEmpty(guid)) continue;

                    var addrEntry = settings.CreateOrMoveEntry(guid, group);
                    addrEntry.address = file; // 以资源路径作为 Address
                    addrEntry.labels.Clear();
                    foreach (var l in entry.SelectedLabels)
                        addrEntry.SetLabel(l, true);
                }

                EditorUtility.SetDirty(group);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Bighead] 已灌入 {processed} 个资源");
            return processed;
        }

        private static List<string> GetFilesInDirectory(string dir)
        {
            return Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
                .Where(p => !p.EndsWith(".meta"))
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

        // —— 打开输出目录：使用我们自定义的 Bighead.BuildPath 解析当前 Profile 的真实路径 ——
        public static void OpenOutputFolder()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                EditorUtility.DisplayDialog("打开失败", "AddressableAssetSettings 不存在，请先初始化 Addressables。", "确定");
                return;
            }

            var profile = settings.profileSettings;
            string profileId = settings.activeProfileId;
            string raw = profile.GetValueByName(profileId, BigheadBuildVar);
            if (string.IsNullOrEmpty(raw))
            {
                // 若用户还没运行过我们流程，退回到官方变量名
                raw = profile.GetValueByName(profileId, AddressableAssetSettings.kBuildPath);
            }

            string resolved = profile.EvaluateString(profileId, raw);
            string fullPath = Path.GetFullPath(resolved);

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
