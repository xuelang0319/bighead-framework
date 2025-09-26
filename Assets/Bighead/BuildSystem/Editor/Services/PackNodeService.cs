#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;

namespace Bighead.BuildSystem.Editor
{
    public class PackNodeService
    {
        private PackNodeRepository _repo = new PackNodeRepository();
        public List<PackGroup> Groups { get; private set; }

        public void Reload() => Groups = _repo.LoadAllGroups();

        public void RemoveGroup(PackGroup group)
        {
            _repo.RemoveGroup(group);
        }

        public void RemoveEntry(PackGroup group, PackEntry entry)
        {
            _repo.RemoveEntry(group, entry);
        }
    }
}
#endif