#if UNITY_EDITOR
using System.Collections.Generic;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 节点基类，表示文件或文件夹
    /// </summary>
    public abstract class PackNode
    {
        public string Name;   // 节点名称
        public string Path;   // Asset 相对路径
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