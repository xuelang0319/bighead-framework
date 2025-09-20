#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Bighead.Upzy.Core;
using UnityEditor;
using UnityEngine;

namespace Bighead.Upzy.Editor
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

            EditorGUILayout.LabelField("生成产物路径", EditorStyles.boldLabel);
            DrawFolderField(so.FindProperty("rootFolder"), "Root Folder");

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(so.FindProperty("currentRel"));
            EditorGUILayout.PropertyField(so.FindProperty("backupRel"));
            EditorGUILayout.PropertyField(so.FindProperty("modulesRel"));

            EditorGUILayout.Space();

            // 构建按钮
            if (GUILayout.Button("构建全部模块", GUILayout.Height(25)))
                UpzyMenuBuilder.BuildAll(setting);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("发版（Full）", GUILayout.Height(25)))
                UpzyMenuBuilder.Publish(setting, true);
            if (GUILayout.Button("发版（Incremental）", GUILayout.Height(25)))
                UpzyMenuBuilder.Publish(setting, false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 绘制模块列表
            EditorGUILayout.LabelField("已注册模块", EditorStyles.boldLabel);
            var modulesProp = so.FindProperty("registeredModules");
            for (int i = 0; i < modulesProp.arraySize; i++)
            {
                var entryProp = modulesProp.GetArrayElementAtIndex(i);
                var configSOProp = entryProp.FindPropertyRelative("configSO");
                var configSO = configSOProp.objectReferenceValue as UpzyModuleSO;
                if (configSO == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(configSO.name, EditorStyles.boldLabel);

                // 展开 SO 字段
                var configSerialized = new SerializedObject(configSO);
                configSerialized.Update();
                var iterator = configSerialized.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    if (iterator.propertyPath == "m_Script") continue;
                    EditorGUILayout.PropertyField(iterator, true);
                    enterChildren = false;
                }

                configSerialized.ApplyModifiedProperties();

                if (GUILayout.Button("构建该模块", GUILayout.Height(20)))
                    UpzyMenuBuilder.BuildModule(setting, configSO);

                EditorGUILayout.EndVertical();
            }

            so.ApplyModifiedProperties();
        }

        private static void DrawFolderField(SerializedProperty prop, string label)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(110));
            prop.stringValue = EditorGUILayout.TextField(prop.stringValue);

            if (GUILayout.Button("选择", GUILayout.Width(50)))
            {
                string startPath = Path.GetFullPath(prop.stringValue);
                if (!Directory.Exists(startPath)) startPath = Application.dataPath;

                string selected = EditorUtility.OpenFolderPanel($"选择 {label}", startPath, "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (!selected.Replace("\\", "/").Contains("/Assets/"))
                    {
                        EditorUtility.DisplayDialog("无效路径", "必须选择工程 Assets 目录内的路径", "好的");
                    }
                    else
                    {
                        prop.stringValue = ToRelUnderProject(selected);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static string ToRelUnderProject(string fullPath)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string norm = Path.GetFullPath(fullPath).Replace("\\", "/");
            if (!norm.StartsWith(projectRoot)) return "Assets/UpzyGenerated";
            string rel = norm.Substring(projectRoot.Length + 1).Replace("\\", "/");
            if (!rel.StartsWith("Assets")) rel = "Assets/" + rel;
            return rel.TrimEnd('/');
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

        private static void RefreshModules(UpzySetting setting)
        {
            var buildableTypes = TypeCache.GetTypesDerivedFrom<UpzyBuildableBase>();
            foreach (var type in buildableTypes)
            {
                var configType = type.Assembly.GetTypes()
                    .FirstOrDefault(t => t.IsSubclassOf(typeof(UpzyModuleSO)));
                if (configType == null) continue;

                string assetDir = "Assets/Bighead/Modules";
                EnsureFolder(assetDir);
                string assetPath = Path.Combine(assetDir, $"{type.Name}Config.asset").Replace("\\", "/");

                var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (so == null)
                {
                    so = ScriptableObject.CreateInstance(configType);
                    AssetDatabase.CreateAsset(so, assetPath);
                    AssetDatabase.SaveAssets();
                }

                if (!setting.registeredModules.Any(e => e.configSO == so))
                    setting.registeredModules.Add(new UpzyEntry { configSO = so });
            }
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