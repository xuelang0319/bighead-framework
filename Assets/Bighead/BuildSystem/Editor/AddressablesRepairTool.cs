#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public static class AddressablesRepairTool
{
    [MenuItem("Tools/Repair/Remove Invalid Addressables Entries")]
    public static void RemoveInvalidEntries()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogError("AddressableAssetSettings not found!");
            return;
        }

        int removed = 0;
        foreach (var group in settings.groups)
        {
            if (group == null) continue;
            var snapshot = new System.Collections.Generic.List<AddressableAssetEntry>(group.entries);
            foreach (var entry in snapshot)
            {
                if (entry == null || string.IsNullOrEmpty(entry.guid))
                {
                    group.RemoveAssetEntry(entry);
                    removed++;
                }
            }
        }

        if (removed > 0)
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"清理完成，共移除无效条目 {removed} 个。");
        }
        else
        {
            UnityEngine.Debug.Log("未发现无效条目。");
        }
    }
}
#endif