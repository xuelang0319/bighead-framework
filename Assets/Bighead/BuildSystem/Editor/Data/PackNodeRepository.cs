#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    public class PackNodeRepository
    {
        private AddressableAssetSettings _settings => AddressableAssetSettingsDefaultObject.Settings;

        public List<PackGroup> LoadAllGroups()
        {
            var result = new List<PackGroup>();
            if (_settings == null) return result;

            foreach (var settingGroup in _settings.groups)
            {
                if (settingGroup == null || settingGroup.name == "Builtin Data") continue;
                var packGroup = new PackGroup
                {
                    Name = settingGroup.Name,
                    GroupRef = settingGroup,
                    IsExpanded = true,
                    Children = new List<PackEntry>()
                };

                foreach (var entry in settingGroup.entries)
                {
                    var packEntry = RecursionEntry(entry);
                    if (packEntry != null)
                        packGroup.Children.Add(packEntry);
                }

                result.Add(packGroup);
            }

            return result;
        }

        private PackEntry RecursionEntry(AddressableAssetEntry assetEntry)
        {
            if (string.IsNullOrEmpty(assetEntry.AssetPath)) return null;

            var packEntry = new PackEntry
            {
                Name = System.IO.Path.GetFileName(assetEntry.AssetPath),
                EntryRef = assetEntry,
                Children = new List<PackEntry>(),
                IsExpanded = false
            };

            if (assetEntry.SubAssets != null)
            {
                foreach (var subEntry in assetEntry.SubAssets)
                {
                    var subPackEntry = RecursionEntry(subEntry);
                    if (subPackEntry != null)
                        packEntry.Children.Add(subPackEntry);
                }
            }

            return packEntry;
        }

        // ========== 新增的接口 ==========

        /// <summary> 新建 Addressable Group </summary>
        public PackGroup AddGroup(string name, bool setAsDefault = false)
        {
            if (_settings == null)
            {
                Debug.LogError("AddressableAssetSettings is null, cannot add group.");
                return null;
            }

            var group = _settings.CreateGroup(name, setAsDefault, false, false, null, typeof(BundledAssetGroupSchema));
            AssetDatabase.SaveAssets();

            return new PackGroup
            {
                Name = group.Name,
                GroupRef = group,
                IsExpanded = true,
                Children = new List<PackEntry>()
            };
        }

        /// <summary> 删除 Addressable Group </summary>
        public bool RemoveGroup(PackGroup group)
        {
            if (_settings == null || group?.GroupRef == null) return false;

            if (!EditorUtility.DisplayDialog("删除分组", $"确定要删除分组 {group.Name} 吗？", "删除", "取消"))
                return false;

            Undo.RegisterCompleteObjectUndo(_settings, "Remove Addressable Group");
            _settings.RemoveGroup(group.GroupRef);
            AssetDatabase.SaveAssets();
            return true;
        }

        /// <summary> 向 Group 添加资源 </summary>
        public PackEntry AddEntry(PackGroup parent, string assetPath)
        {
            if (parent?.GroupRef == null || string.IsNullOrEmpty(assetPath)) return null;

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"无效路径: {assetPath}");
                return null;
            }

            Undo.RegisterCompleteObjectUndo(_settings, "Add Addressable Entry");
            var entryRef = _settings.CreateOrMoveEntry(guid, parent.GroupRef);
            AssetDatabase.SaveAssets();

            var newEntry = new PackEntry
            {
                Name = System.IO.Path.GetFileName(assetPath),
                EntryRef = entryRef,
                Children = new List<PackEntry>(),
                IsExpanded = false
            };
            parent.Children.Add(newEntry);
            return newEntry;
        }

        /// <summary> 删除 Entry </summary>
        public bool RemoveEntry(PackGroup parent, PackEntry entry)
        {
            if (parent?.GroupRef == null || entry?.EntryRef == null) return false;

            if (!EditorUtility.DisplayDialog("删除资源", $"确定要删除资源 {entry.Name} 吗？", "删除", "取消"))
                return false;

            Undo.RegisterCompleteObjectUndo(_settings, "Remove Addressable Entry");
            parent.GroupRef.RemoveAssetEntry(entry.EntryRef);
            parent.Children.Remove(entry);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
#endif
