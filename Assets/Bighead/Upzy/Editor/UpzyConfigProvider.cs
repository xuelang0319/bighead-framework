#if UNITY_EDITOR
using System.IO;
using System.Linq;
using Bighead.Upzy.Core;
using Bighead.Upzy.Runtime;
using UnityEditor;
using UnityEngine;

namespace Bighead.Upzy.Editor
{
    public static class UpzyConfigProvider
    {
        private const string kProviderPath = "Project/Bighead Upzy";
        private const string kSettingAssetPath = "Assets/Bighead/Configs/Upzy/UpzySetting.asset";
        private const string kModulesAssetsDir = "Assets/Bighead/Configs/Upzy/Modules";

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider(kProviderPath, SettingsScope.Project)
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

            DrawVersionStatus(setting);

            EditorGUILayout.Space(10);
            DrawGlobalActions(setting);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("已注册模块", EditorStyles.boldLabel);

            var listProp = so.FindProperty("registeredModules");
            if (listProp == null || listProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("暂无已注册模块，请点击刷新按钮重新扫描模块。", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var entry = listProp.GetArrayElementAtIndex(i);
                    var soProp = entry.FindPropertyRelative("configSO");
                    if (soProp.objectReferenceValue is not UpzyModuleSO moduleSO) continue;

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField($"{moduleSO.name}  v{moduleSO.version}", EditorStyles.boldLabel);

                    var modObj = new SerializedObject(moduleSO);
                    modObj.Update();
                    var it = modObj.GetIterator();
                    bool enterChildren = true;
                    while (it.NextVisible(enterChildren))
                    {
                        if (it.propertyPath == "m_Script") continue;
                        EditorGUILayout.PropertyField(it, true);
                        enterChildren = false;
                    }
                    modObj.ApplyModifiedProperties();

                    EditorGUILayout.Space(3);
                    if (GUILayout.Button("构建模块", GUILayout.Height(20)))
                        UpzyMenuBuilder.BuildModule(setting, moduleSO);

                    EditorGUILayout.EndVertical();
                }
            }

            so.ApplyModifiedProperties();
        }

        private static void DrawVersionStatus(UpzySetting setting)
        {
            EditorGUILayout.LabelField("📂 当前版本状态", EditorStyles.boldLabel);

            // 直接读取 latest Menu
            var latestMenu = LoadMenu(Path.Combine(setting.LatestAbs, "Menu.bd"));
            if (latestMenu != null)
            {
                EditorGUILayout.LabelField($"latest:  {latestMenu.meta.version}");
                EditorGUILayout.LabelField($"模块数: {latestMenu.modules.Length}");
            }
            else
            {
                EditorGUILayout.LabelField("latest:  (未生成)");
            }

            // 读取 release Menu
            var releaseMenu = LoadMenu(Path.Combine(setting.ReleaseAbs, "Menu.bd"));
            if (releaseMenu != null)
            {
                EditorGUILayout.LabelField($"release: {releaseMenu.meta.version}");
                EditorGUILayout.LabelField($"模块数: {releaseMenu.modules.Length}");
            }
            else
            {
                EditorGUILayout.LabelField("release: (未发布)");
            }

            // 读取 rollback Menu
            var rollbackMenu = LoadMenu(Path.Combine(setting.RollbackAbs, "Menu.bd"));
            if (rollbackMenu != null)
            {
                EditorGUILayout.LabelField($"rollback: {rollbackMenu.meta.version}");
            }
            else
            {
                EditorGUILayout.LabelField("rollback: (无快照)");
            }
        }


        private static void DrawGlobalActions(UpzySetting setting)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("构建全部模块", GUILayout.Height(25)))
                UpzyMenuBuilder.BuildAll(setting);
            if (GUILayout.Button("发版 (Full)", GUILayout.Height(25)))
                UpzyMenuBuilder.Publish(setting, true);
            if (GUILayout.Button("发版 (Incremental)", GUILayout.Height(25)))
                UpzyMenuBuilder.Publish(setting, false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("回滚 release", GUILayout.Height(25)))
                UpzyMenuBuilder.RollbackRelease(setting);
            if (GUILayout.Button("刷新模块列表", GUILayout.Height(25)))
                RefreshModules(setting);
            if (GUILayout.Button("打开生成目录", GUILayout.Height(25)))
                EditorUtility.RevealInFinder(setting.LatestAbs);
            EditorGUILayout.EndHorizontal();
        }

        private static void RefreshModules(UpzySetting setting)
        {
            EnsureFolder(kModulesAssetsDir);

            var derived = UnityEditor.TypeCache.GetTypesDerivedFrom<UpzyModuleSO>();
            var seen = new System.Collections.Generic.HashSet<UpzyModuleSO>();
            int before = setting.registeredModules.Count;

            foreach (var t in derived)
            {
                if (t.IsAbstract) continue; // 跳过抽象基类
                var so = FindOrCreateModuleAsset(t);
                if (so == null) continue;

                seen.Add(so);
                if (!setting.registeredModules.Any(e => e != null && e.configSO == so))
                {
                    setting.registeredModules.Add(new UpzyEntry { configSO = so });
                }
            }

            // 清理无效/已删除的引用，或不是当前派生类集合中的条目
            setting.registeredModules.RemoveAll(e => e == null || e.configSO == null || !seen.Contains(e.configSO));

            EditorUtility.SetDirty(setting);
            AssetDatabase.SaveAssets();

            int after = setting.registeredModules.Count;
            Debug.Log($"[Upzy] 刷新完成：共 {seen.Count} 个模块；新增 {after - before}，清理 {before - after}。");
        }
        
        private static UpzyModuleSO FindOrCreateModuleAsset(System.Type t)
        {
            // 先找现有资产（只接受“精确类型匹配”的资产）
            var guids = AssetDatabase.FindAssets($"t:{t.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var obj = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (obj != null && obj.GetType() == t)
                    return obj as UpzyModuleSO;
            }

            // 没找到就创建一个到 Upzy/Modules 目录
            var asset = ScriptableObject.CreateInstance(t) as UpzyModuleSO;
            if (asset == null) return null;

            string fileName = $"{t.Name}.asset";
            string createPath = Path.Combine(kModulesAssetsDir, fileName).Replace("\\", "/");

            // 避免同名冲突：如果已存在重名文件，自动加序号
            createPath = AssetDatabase.GenerateUniqueAssetPath(createPath);

            AssetDatabase.CreateAsset(asset, createPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Upzy] 已创建模块配置：{createPath}");
            return asset;
        }

        private static UpzySetting GetOrCreateSettingAsset()
        {
            EnsureFolder("Assets/Bighead/Configs/Upzy");
            var setting = AssetDatabase.LoadAssetAtPath<UpzySetting>(kSettingAssetPath);
            if (setting != null) return setting;

            setting = ScriptableObject.CreateInstance<UpzySetting>();
            AssetDatabase.CreateAsset(setting, kSettingAssetPath);
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

        private static UpzyMenu LoadMenu(string menuPath)
        {
            if (!File.Exists(menuPath)) return null;
            try
            {
                return JsonUtility.FromJson<UpzyMenu>(File.ReadAllText(menuPath));
            }
            catch
            {
                return null;
            }
        }
    }
}
#endif
