#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;

namespace Bighead.BuildSystem.Editor
{
    public static class AssetPackProvider
    {
        public static string AssetPath => Bighead.BigheadSetting.ToConfigs("BuildSystem/AssetPackSO.asset");

        public static AssetPackSO LoadOrCreate()
        {
            var so = AssetDatabase.LoadAssetAtPath<AssetPackSO>(AssetPath);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<AssetPackSO>();
                EnsureDirectoryExists(Path.GetDirectoryName(AssetPath));
                AssetDatabase.CreateAsset(so, AssetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Bighead] 创建新的 AssetPackSO 配置文件: {AssetPath}");
            }
            return so;
        }

        public static void Ping()
        {
            var so = LoadOrCreate();
            EditorGUIUtility.PingObject(so);
            Selection.activeObject = so;
        }

        public static AssetPackSO Reset()
        {
            if (File.Exists(AssetPath))
            {
                AssetDatabase.DeleteAsset(AssetPath);
                AssetDatabase.SaveAssets();
            }
            return LoadOrCreate();
        }

        private static void EnsureDirectoryExists(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Bighead Pack", SettingsScope.Project)
            {
                label = "Bighead Pack",
                guiHandler = _ =>
                {
                    var setting = LoadOrCreate();
                    var so = new SerializedObject(setting);

                    HandleGlobalDrag(setting);

                    EditorGUILayout.HelpBox($"配置文件路径: {AssetPath}", MessageType.None);
                    EditorGUILayout.PropertyField(so.FindProperty("Compression"));
                    EditorGUILayout.LabelField("全局标签", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(so.FindProperty("Labels"), true);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("打包条目", EditorStyles.boldLabel);

                    // 提示用户可以拖拽
                    EditorGUILayout.HelpBox("💡 可以将资源文件夹或文件直接拖拽到此窗口以添加条目", MessageType.Info);

                    // 添加条目按钮
                    if (GUILayout.Button("+ 添加条目", GUILayout.Height(22)))
                        TryAddEntry(setting, new AssetPackEntry());

                    GUILayout.Space(5);
                    DrawEntries(setting);

                    // 定位和重置放在列表下方
                    GUILayout.Space(8);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("定位配置文件", GUILayout.Height(22))) Ping();
                    if (GUILayout.Button("重置为初始状态", GUILayout.Height(22)))
                    {
                        if (EditorUtility.DisplayDialog("重置确认",
                                "确定要删除当前 AssetPackSO 并重新创建吗？此操作不可撤销。",
                                "确定", "取消"))
                        {
                            Reset();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    so.ApplyModifiedProperties();
                }
            };
        }

        private static Vector2 _entryScroll;
        private static bool _menuOpen;

        private static void DrawEntries(AssetPackSO setting)
        {
            GUI.Box(EditorGUILayout.BeginVertical(GUI.skin.box), GUIContent.none);
            _entryScroll = EditorGUILayout.BeginScrollView(_entryScroll, GUILayout.Height(250));

            for (int i = 0; i < setting.Entries.Count; i++)
            {
                var entry = setting.Entries[i];
                EditorGUILayout.BeginHorizontal();

                // 路径输入框
                entry.Path = EditorGUILayout.TextField(entry.Path, GUILayout.MinWidth(200));

                // 如果路径无效，显示警告图标
                if (!string.IsNullOrEmpty(entry.Path))
                {
                    string absPath = entry.Path.StartsWith("Assets")
                        ? entry.Path.Replace("Assets", Application.dataPath)
                        : null;
                    if (absPath == null || (!Directory.Exists(absPath) && !File.Exists(absPath)))
                    {
                        GUILayout.Label(EditorGUIUtility.IconContent("console.warnicon.sml"),
                            GUILayout.Width(20), GUILayout.Height(20));
                    }
                    else GUILayout.Space(20);
                }
                else GUILayout.Space(20);

                // 选择文件夹按钮
                if (GUILayout.Button("选择", GUILayout.Width(45)))
                {
                    var selected = EditorUtility.OpenFolderPanel("选择文件夹", "Assets", "");
                    if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
                        entry.Path = "Assets" + selected.Substring(Application.dataPath.Length);
                }

                // 标签下拉
                if (setting.Labels.Count > 0)
                {
                    string labelName = entry.SelectedLabels.Count > 0
                        ? string.Join(", ", entry.SelectedLabels)
                        : "选择标签";
                    if (GUILayout.Button(labelName, EditorStyles.popup, GUILayout.Width(120)))
                        ShowPersistentLabelMenu(setting, entry);
                }

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    setting.Entries.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            if (_menuOpen && Event.current.type == EventType.MouseDown)
            {
                _menuOpen = false;
                GUI.changed = true;
            }
        }

        private static void ShowPersistentLabelMenu(AssetPackSO setting, AssetPackEntry entry)
        {
            _menuOpen = true;
            var menu = new GenericMenu();

            foreach (var label in setting.Labels)
            {
                bool selected = entry.SelectedLabels.Contains(label);
                menu.AddItem(new GUIContent(label), selected, () =>
                {
                    if (selected) entry.SelectedLabels.Remove(label);
                    else entry.SelectedLabels.Add(label);
                });
            }

            menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
        }

        private static void HandleGlobalDrag(AssetPackSO setting)
        {
            var evt = Event.current;
            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (var path in DragAndDrop.paths)
                    {
                        if (path.StartsWith("Assets"))
                            TryAddEntry(setting, new AssetPackEntry { Path = path });
                    }
                }
                evt.Use();
            }
        }

        private static void TryAddEntry(AssetPackSO setting, AssetPackEntry entry)
        {
            if (setting.Entries.Any(e => e.Path == entry.Path))
            {
                Debug.LogWarning($"[Bighead] 已存在条目: {entry.Path}");
                return;
            }
            setting.Entries.Add(entry);
        }
    }
}
#endif
