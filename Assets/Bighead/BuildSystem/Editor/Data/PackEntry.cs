#if UNITY_EDITOR
using System.Collections.Generic;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 节点
    /// </summary>
    public class PackEntry
    {
        public string Name;
        public List<PackEntry> Children = new List<PackEntry>();
        public UnityEditor.AddressableAssets.Settings.AddressableAssetEntry EntryRef;
        public bool IsExpanded;
    }

    /// <summary>
    /// 容器
    /// </summary>
    public class PackGroup
    {
        public string Name;
        public UnityEditor.AddressableAssets.Settings.AddressableAssetGroup GroupRef;
        public List<PackEntry> Children = new List<PackEntry>();
        public bool IsExpanded;
    }
}
#endif