#if UNITY_EDITOR
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
                if (group == null || group.Name == "Builtin Data") continue;

                var groupNode = new PackGroupNode
                {
                    Name = group.Name,
                    Path = group.Name,
                    GroupRef = group,
                    IsExpanded = true,
                    IsInGroup = true
                };

                // 记录所有 entry 路径，便于判断文件是否已加入
                var entrySet = new HashSet<string>(group.entries.Select(e => e.AssetPath));

                foreach (var entry in group.entries)
                {
                    var assetPath = entry.AssetPath.Replace('\\', '/');
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        var folderNode = new PackFolderNode
                        {
                            Name = System.IO.Path.GetFileName(assetPath),
                            Path = assetPath,
                            IsExpanded = true,
                            IsInGroup = true
                        };

                        // 扫描文件夹内所有文件和子文件夹
                        var guids = AssetDatabase.FindAssets("", new[] { assetPath });
                        foreach (var guid in guids)
                        {
                            var childPath = AssetDatabase.GUIDToAssetPath(guid);
                            if (childPath == assetPath) continue;

                            if (AssetDatabase.IsValidFolder(childPath))
                            {
                                folderNode.Children.Add(new PackFolderNode
                                {
                                    Name = System.IO.Path.GetFileName(childPath),
                                    Path = childPath,
                                    IsExpanded = false,
                                    IsInGroup = entrySet.Contains(childPath)
                                });
                            }
                            else
                            {
                                folderNode.Children.Add(new PackFileNode
                                {
                                    Name = System.IO.Path.GetFileName(childPath),
                                    Path = childPath,
                                    IsInGroup = entrySet.Contains(childPath)
                                });
                            }
                        }

                        groupNode.Children.Add(folderNode);
                    }
                    else
                    {
                        groupNode.Children.Add(new PackFileNode
                        {
                            Name = System.IO.Path.GetFileName(assetPath),
                            Path = assetPath,
                            IsInGroup = true
                        });
                    }
                }

                result.Add(groupNode);
            }

            return result;
        }

        public void AddPathToGroup(string path, AddressableAssetGroup group)
        {
            if (_settings == null || group == null || string.IsNullOrEmpty(path)) return;
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;
            _settings.CreateOrMoveEntry(guid, group);
            EditorUtility.SetDirty(group);
            EditorUtility.SetDirty(_settings);
            AssetDatabase.SaveAssets();
        }

        public void RemoveFile(string path, AddressableAssetGroup group)
        {
            if (_settings == null || group == null) return;
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return;
            var entry = group.GetAssetEntry(guid);
            if (entry != null)
            {
                group.RemoveAssetEntry(entry);
                EditorUtility.SetDirty(group);
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
            }
        }

        public void RemoveFolder(string folderPath, AddressableAssetGroup group)
        {
            if (_settings == null || group == null) return;
            var guid = AssetDatabase.AssetPathToGUID(folderPath);
            if (string.IsNullOrEmpty(guid)) return;
            var entry = group.GetAssetEntry(guid);
            if (entry != null)
            {
                group.RemoveAssetEntry(entry);
                EditorUtility.SetDirty(group);
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
            }
        }
    }
}
#endif
