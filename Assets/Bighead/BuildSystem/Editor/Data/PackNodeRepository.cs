#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

namespace Bighead.BuildSystem.Editor
{
    public class PackNodeRepository
    {
        private readonly AddressableAssetSettings _settings;

        public PackNodeRepository()
        {
            _settings = AddressableAssetSettingsDefaultObject.Settings;
        }

        public List<PackGroupNode> LoadAllGroups()
        {
            var result = new List<PackGroupNode>();
            if (_settings == null) return result;

            foreach (var group in _settings.groups)
            {
                if (group == null) continue;
                // 内置组不纳入可管理范围
                if (group.Name == "Builtin Data") continue;

                var groupNode = new PackGroupNode
                {
                    Name = group.Name,
                    Path = group.Name,
                    GroupRef = group,
                    IsExpanded = true,
                    IsInGroup = true
                };

                // 本组全部 entry 路径（统一分隔符）
                var entryPaths = group.entries
                    .Where(e => !string.IsNullOrEmpty(e.AssetPath))
                    .Select(e => e.AssetPath.Replace('\\', '/'))
                    .ToList();

                var entrySet = new HashSet<string>(entryPaths);

                // 分离“文件夹 entry”与“文件 entry”
                var folderEntries = entryPaths.Where(p => AssetDatabase.IsValidFolder(p))
                                              .OrderBy(p => p.Length).ToList();
                var fileEntries   = entryPaths.Where(p => !AssetDatabase.IsValidFolder(p)).ToList();

                // 仅选择“顶层文件夹 entry”（不被其他文件夹 entry 覆盖）
                var topFolderEntries = new List<string>();
                foreach (var p in folderEntries)
                {
                    bool nested = topFolderEntries.Any(root => IsUnder(p, root));
                    if (!nested) topFolderEntries.Add(p);
                }

                // 先构建每个顶层文件夹 entry 的完整树（递归、无限层级）
                foreach (var rootPath in topFolderEntries)
                {
                    var rootFolder = new PackFolderNode
                    {
                        Name = System.IO.Path.GetFileName(rootPath),
                        Path = rootPath,
                        IsExpanded = false,
                        IsInGroup = true
                    };
                    groupNode.Children.Add(rootFolder);

                    BuildFolderTree(rootFolder, rootPath, entrySet);
                }

                // 再把“不在任何文件夹 entry 覆盖范围内”的文件 entry 放到组根下
                foreach (var fp in fileEntries)
                {
                    bool underAnyRoot = topFolderEntries.Any(root => IsUnder(fp, root));
                    if (!underAnyRoot)
                    {
                        groupNode.Children.Add(new PackFileNode
                        {
                            Name = System.IO.Path.GetFileName(fp),
                            Path = fp,
                            IsInGroup = true
                        });
                    }
                }

                result.Add(groupNode);
            }

            return result;
        }

        /// <summary>
        /// 在 folderEntry 的范围内，按磁盘真实结构递归构建树；
        /// 对于“也是 Addressables entry”的子文件/子文件夹，标记 IsInGroup=true。
        /// </summary>
        private void BuildFolderTree(PackFolderNode rootNode, string rootPath, HashSet<string> entrySet)
        {
            // 路径 → 已建 FolderNode
            var folderMap = new Dictionary<string, PackFolderNode>(StringComparer.OrdinalIgnoreCase)
            {
                [Normalize(rootPath)] = rootNode
            };

            // 收集根下所有资源路径（含子文件夹 & 文件），再按深度排序，保证父在前
            var guids = AssetDatabase.FindAssets(string.Empty, new[] { rootPath });
            var allPaths = new List<string>(guids.Length);
            foreach (var guid in guids)
            {
                var p = Normalize(AssetDatabase.GUIDToAssetPath(guid));
                if (p == rootPath) continue;
                allPaths.Add(p);
            }
            allPaths.Sort((a, b) => Depth(a).CompareTo(Depth(b)));

            foreach (var path in allPaths)
            {
                if (AssetDatabase.IsValidFolder(path))
                {
                    // 确保父链存在后创建该文件夹
                    EnsureFolder(folderMap, rootNode, rootPath, path, entrySet);
                }
                else
                {
                    // 文件：确保父文件夹存在，再挂到父节点下
                    var parentDir = ParentDir(path);
                    var parent = EnsureFolder(folderMap, rootNode, rootPath, parentDir, entrySet);
                    parent.Children.Add(new PackFileNode
                    {
                        Name = System.IO.Path.GetFileName(path),
                        Path = path,
                        IsInGroup = entrySet.Contains(path)
                    });
                }
            }
        }

        private static PackFolderNode EnsureFolder(
            Dictionary<string, PackFolderNode> map,
            PackFolderNode rootNode,
            string rootPath,
            string folderPath,
            HashSet<string> entrySet)
        {
            folderPath = Normalize(folderPath);
            rootPath   = Normalize(rootPath);

            if (map.TryGetValue(folderPath, out var node)) return node;

            // 自 rootPath 起，逐级保证链路存在
            string relative = folderPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)
                ? folderPath.Substring(rootPath.Length).TrimStart('/')
                : folderPath; // 容错

            var parts = relative.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            string currentPath = rootPath;
            PackFolderNode parent = map[rootPath];

            for (int i = 0; i < parts.Length; i++)
            {
                currentPath = string.IsNullOrEmpty(currentPath) ? parts[i] : $"{currentPath}/{parts[i]}";
                if (!map.TryGetValue(currentPath, out var f))
                {
                    f = new PackFolderNode
                    {
                        Name = parts[i],
                        Path = currentPath,
                        IsExpanded = false,
                        IsInGroup = entrySet.Contains(currentPath)
                    };
                    parent.Children.Add(f);
                    map[currentPath] = f;
                }
                parent = f;
            }

            return map[folderPath];
        }

        private static bool IsUnder(string path, string folder)
        {
            path = Normalize(path);
            folder = Normalize(folder).TrimEnd('/');
            return path.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string p) => string.IsNullOrEmpty(p) ? p : p.Replace('\\', '/');
        private static int Depth(string p) => Normalize(p).Count(c => c == '/');
        private static string ParentDir(string p)
        {
            p = Normalize(p);
            int idx = p.LastIndexOf('/');
            return idx > 0 ? p.Substring(0, idx) : string.Empty;
        }

        // ====== 写入 / 移除 ======

        public void AddPathToGroup(string path, AddressableAssetGroup group)
        {
            if (_settings == null || group == null || string.IsNullOrEmpty(path)) return;

            string guid = AssetDatabase.AssetPathToGUID(path.Replace('\\', '/'));
            if (string.IsNullOrEmpty(guid)) return;

            _settings.CreateOrMoveEntry(guid, group);
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
        }

        public void RemoveFile(string path, AddressableAssetGroup expectedGroup)
        {
            if (_settings == null || string.IsNullOrEmpty(path)) return;

            string guid = AssetDatabase.AssetPathToGUID(path.Replace('\\', '/'));
            if (string.IsNullOrEmpty(guid)) return;

            // 全局定位 entry，避免“期望组不一致”导致找不到
            var entry = _settings.FindAssetEntry(guid);
            if (entry == null) return;

            var g = entry.parentGroup;
            g.RemoveAssetEntry(entry);
            EditorUtility.SetDirty(g);
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
        }

        public void RemoveFolder(string folderPath, AddressableAssetGroup group)
        {
            if (_settings == null || group == null || string.IsNullOrEmpty(folderPath)) return;

            string guid = AssetDatabase.AssetPathToGUID(folderPath.Replace('\\', '/'));
            if (string.IsNullOrEmpty(guid)) return;

            var entry = group.GetAssetEntry(guid);
            if (entry == null) return;

            group.RemoveAssetEntry(entry);
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
