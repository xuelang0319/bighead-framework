#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>管理 PackNode 树的服务层</summary>
    public class PackNodeService
    {
        private readonly List<PackFolderNode> _roots = new List<PackFolderNode>();

        public IReadOnlyList<PackFolderNode> Roots => _roots;

        /// <summary>添加文件或文件夹路径到根节点</summary>
        public void AddPath(string path)
        {
            if (_roots.Any(r => r.Path == path)) return; // 去重

            if (AssetDatabase.IsValidFolder(path))
            {
                var folderNode = new PackFolderNode
                {
                    Name = System.IO.Path.GetFileName(path),
                    Path = path
                };
                FillFolderChildren(folderNode);
                _roots.Add(folderNode);
            }
            else
            {
                var rootNode = new PackFolderNode
                {
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    Path = path
                };
                rootNode.Children.Add(new PackFileNode
                {
                    Name = System.IO.Path.GetFileName(path),
                    Path = path
                });
                _roots.Add(rootNode);
            }
        }

        /// <summary>递归填充文件夹子节点</summary>
        private void FillFolderChildren(PackFolderNode folderNode)
        {
            var guids = AssetDatabase.FindAssets("", new[] { folderNode.Path });
            foreach (var guid in guids)
            {
                var childPath = AssetDatabase.GUIDToAssetPath(guid);
                if (childPath == folderNode.Path) continue;

                if (AssetDatabase.IsValidFolder(childPath))
                {
                    var childFolder = new PackFolderNode
                    {
                        Name = System.IO.Path.GetFileName(childPath),
                        Path = childPath
                    };
                    FillFolderChildren(childFolder);
                    folderNode.Children.Add(childFolder);
                }
                else
                {
                    folderNode.Children.Add(new PackFileNode
                    {
                        Name = System.IO.Path.GetFileName(childPath),
                        Path = childPath
                    });
                }
            }
        }

        public void RemoveRoot(PackFolderNode node)
        {
            _roots.Remove(node);
        }
    }
}
#endif
