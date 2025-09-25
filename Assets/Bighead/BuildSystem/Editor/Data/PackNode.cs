#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 节点基类，表示文件或文件夹
    /// </summary>
    public abstract class PackNode
    {
        public string Name;
        public string Path;
        public bool IsInGroup;   // ✅ 表示是否是 Addressables entry
    }
    
    /// <summary>
    /// Addressables 组节点（根级管理单元）
    /// </summary>
    public sealed class PackGroupNode : PackNode
    {
        public AddressableAssetGroup GroupRef;          // 真实 Addressable 组
        public bool IsExpanded = true;                  // UI 展开状态
        public List<PackNode> Children = new();         // 可含 Folder/File
    }

    /// <summary>
    /// 文件夹节点，可包含子节点
    /// </summary>
    public class PackFolderNode : PackNode
    {
        public bool IsExpanded = true;
        public List<PackNode> Children = new List<PackNode>();
    }

    /// <summary>
    /// 文件节点，不可包含子节点
    /// </summary>
    public class PackFileNode : PackNode
    {
    }
}
#endif