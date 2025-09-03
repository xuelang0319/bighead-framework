#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Bighead.Csv.Editor
{
    [InitializeOnLoad]
    public static class CsvSettingsCreator
    {
        public const string AssetDir  = "Assets/BigheadCsv/Settings";
        public const string AssetPath = AssetDir + "/CsvSettings.asset";

        static CsvSettingsCreator()
        {
            EditorApplication.delayCall += EnsureCreatedOnce;
        }

        public static void EnsureCreatedOnce()
        {
            // 可能存在“资产文件在但脚本类型变更/编译失败导致加载为空”的情况
            var exists = File.Exists(AssetPath);
            var loaded = exists ? AssetDatabase.LoadAssetAtPath<CsvSettings>(AssetPath) : null;
            if (exists && loaded != null) return;

            if (!AssetDatabase.IsValidFolder(AssetDir))
            {
                var parent = "Assets/BigheadCsv";
                if (!AssetDatabase.IsValidFolder(parent))
                    AssetDatabase.CreateFolder("Assets", "BigheadCsv");
                AssetDatabase.CreateFolder(parent, "Settings");
            }

            // 若存在但加载失败（类名改动/命名空间变动），先删后建
            if (exists && loaded == null)
                AssetDatabase.DeleteAsset(AssetPath);

            var settings = ScriptableObject.CreateInstance<CsvSettings>();
            settings.Normalize();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Bighead.Csv] CsvSettings.asset 已创建/修复：" + AssetPath);
        }

        [MenuItem("Bighead/Csv/Settings", false, 0)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/Bighead Csv");
        }
    }
}
#endif