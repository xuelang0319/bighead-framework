#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Bighead.Upzy
{
    static class UpzyConfigProvider
    {
        private const string kPath = "Project/Bighead Upzy";
        private const string kAssetPath = "Assets/Bighead/Configs/UpzySetting.asset";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(kPath, SettingsScope.Project)
            {
                label = "Bighead Upzy",
                guiHandler = _ => DrawGUI()
            };
        }

        private static void DrawGUI()
        {
            var setting = GetOrCreateSettingAsset();
            var so = new SerializedObject(setting);
            so.Update();

            EditorGUILayout.LabelField("基础路径配置", EditorStyles.boldLabel);
            DrawFolderField(so.FindProperty("rootFolder"), "热更根目录");
            DrawFolderField(so.FindProperty("currentFolder"), "当前版本目录");
            DrawFolderField(so.FindProperty("backupFolder"), "备份目录");
            DrawFolderField(so.FindProperty("stagingFolder"), "中间下载目录");
            DrawFolderField(so.FindProperty("modulesFolder"), "模块目录");

            so.ApplyModifiedProperties();
        }

        private static void DrawFolderField(SerializedProperty prop, string label)
        {
            EditorGUILayout.BeginHorizontal();
            prop.stringValue = EditorGUILayout.TextField(label, prop.stringValue);
            if (GUILayout.Button("...", GUILayout.MaxWidth(50)))
            {
                string selected = EditorUtility.OpenFolderPanel("选择路径", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.StartsWith(Application.dataPath))
                    {
                        // 转换为相对路径
                        selected = "Assets" + selected.Substring(Application.dataPath.Length);
                    }
                    prop.stringValue = selected;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        private static UpzySetting GetOrCreateSettingAsset()
        {
            var setting = AssetDatabase.LoadAssetAtPath<UpzySetting>(kAssetPath);
            if (setting != null) return setting;

            EnsureFolder("Assets/Bighead/Configs");
            setting = ScriptableObject.CreateInstance<UpzySetting>();
            AssetDatabase.CreateAsset(setting, kAssetPath);
            AssetDatabase.SaveAssets();
            return setting;
        }

        private static void EnsureFolder(string folder)
        {
            var parts = folder.Split('/');
            string path = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{path}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(path, parts[i]);
                path = next;
            }
        }
    }
}
#endif
