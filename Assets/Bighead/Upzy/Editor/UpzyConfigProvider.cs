#if UNITY_EDITOR
using Bighead.Upzy;
using UnityEditor;
using UnityEngine;

static class UpzyConfigProvider
{
    private const string kPath = "Project/Bighead Upzy";

    [SettingsProvider]
    public static SettingsProvider CreateProvider()
    {
        return new SettingsProvider(kPath, SettingsScope.Project)
        {
            label = "Bighead Upzy",
            guiHandler = (searchContext) =>
            {
                var setting = GetSetting();
                if (setting == null)
                {
                    if (GUILayout.Button("创建 UpzySetting"))
                        CreateSetting();
                    return;
                }

                var so = new SerializedObject(setting);
                so.Update();

                EditorGUILayout.PropertyField(so.FindProperty("rootFolder"));
                EditorGUILayout.PropertyField(so.FindProperty("currentFolder"));
                EditorGUILayout.PropertyField(so.FindProperty("backupFolder"));
                EditorGUILayout.PropertyField(so.FindProperty("stagingFolder"));
                EditorGUILayout.PropertyField(so.FindProperty("modulesFolder"));
                EditorGUILayout.PropertyField(so.FindProperty("registeredModules"), true);

                so.ApplyModifiedProperties();
            }
        };
    }
    
    private static UpzySetting GetSetting()
    {
        return AssetDatabase.LoadAssetAtPath<UpzySetting>("Assets/Bighead/Configs/UpzySetting.asset");
    }

    private static void CreateSetting()
    {
        const string folder = "Assets/Bighead/Configs";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            string[] parts = folder.Split('/');
            string path = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{path}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(path, parts[i]);
                path = next;
            }
        }

        var setting = ScriptableObject.CreateInstance<UpzySetting>();
        string assetPath = $"{folder}/UpzySetting.asset";
        AssetDatabase.CreateAsset(setting, assetPath);
        AssetDatabase.SaveAssets();
        Selection.activeObject = setting;
    }
}
#endif