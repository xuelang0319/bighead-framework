#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Bighead.BuildSystem.Editor
{
    public class PackNodeService
    {
        private readonly PackNodeRepository _repo = new PackNodeRepository();
        private readonly List<PackGroupNode> _groups = new List<PackGroupNode>();

        public IReadOnlyList<PackGroupNode> Groups => _groups;

        public event Action OnTreeReloaded;
        public event Action<PackNode, PackGroupNode> OnNodeRemoved;

        public PackNodeService()
        {
            Reload();
        }

        public void Reload()
        {
            _groups.Clear();
            _groups.AddRange(_repo.LoadAllGroups());
            OnTreeReloaded?.Invoke();
        }

        public void AddNode(string path, PackGroupNode targetGroup)
        {
            if (string.IsNullOrEmpty(path) || targetGroup?.GroupRef == null) return;
            _repo.AddPathToGroup(path, targetGroup.GroupRef);
            Reload();
        }

        public void RemoveNode(PackNode node, PackGroupNode fromGroup)
        {
            if (node == null || fromGroup?.GroupRef == null) return;
            if (node is PackFileNode)
                _repo.RemoveFile(node.Path, fromGroup.GroupRef);
            else if (node is PackFolderNode)
                _repo.RemoveFolder(node.Path, fromGroup.GroupRef);

            Reload();
            OnNodeRemoved?.Invoke(node, fromGroup);
        }
    }
}
#endif