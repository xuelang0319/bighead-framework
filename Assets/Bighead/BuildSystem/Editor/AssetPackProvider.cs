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
                    var soObj = new SerializedObject(setting);

                    HandleGlobalDrag(setting);

                    // 显示配置文件路径
                    EditorGUILayout.HelpBox($"配置文件路径: {AssetPath}", MessageType.None);

                    // 全局压缩设置
                    EditorGUILayout.PropertyField(soObj.FindProperty("Compression"));

                    // 全局标签
                    EditorGUILayout.LabelField("全局标签", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(soObj.FindProperty("Labels"), true);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("打包条目", EditorStyles.boldLabel);

                    if (GUILayout.Button("+ 添加条目", GUILayout.Height(22)))
                    {
                        setting.Entries.Add(new AssetPackEntry());
                        MarkSettingDirtyAndSave(setting);
                    }

                    // 条目渲染
                    DrawEntries(setting);

                    GUILayout.Space(5);
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

                    GUILayout.Space(10);
                    DrawBuildSection(setting);

                    soObj.ApplyModifiedProperties();
                }
            };
        }

        // ======================= 条目渲染 =======================
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

                EditorGUI.BeginChangeCheck();

                // 路径输入框
                string newPath = EditorGUILayout.TextField(entry.Path, GUILayout.MinWidth(200));
                if (newPath != entry.Path)
                    entry.Path = newPath;

                // 选择路径
                if (GUILayout.Button("选择", GUILayout.Width(45)))
                {
                    var selected = EditorUtility.OpenFolderPanel("选择文件夹", "Assets", "");
                    if (!string.IsNullOrEmpty(selected) && selected.StartsWith(Application.dataPath))
                        entry.Path = "Assets" + selected.Substring(Application.dataPath.Length);
                }

                // 标签选择
                if (setting.Labels.Count > 0)
                {
                    string labelName = entry.SelectedLabels.Count > 0
                        ? string.Join(", ", entry.SelectedLabels)
                        : "选择标签";

                    if (GUILayout.Button(labelName, EditorStyles.popup, GUILayout.Width(120)))
                        ShowPersistentLabelMenu(setting, entry);
                }

                // 删除条目
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    setting.Entries.RemoveAt(i);
                    MarkSettingDirtyAndSave(setting);
                    GUIUtility.ExitGUI();
                }
                GUI.backgroundColor = Color.white;

                if (EditorGUI.EndChangeCheck())
                    MarkSettingDirtyAndSave(setting);

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
                    MarkSettingDirtyAndSave(setting);
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
                        if (!path.StartsWith("Assets")) continue;
                        if (setting.Entries.Any(e => e.Path == path)) continue;

                        setting.Entries.Add(new AssetPackEntry { Path = path });
                        MarkSettingDirtyAndSave(setting);
                    }
                }
                evt.Use();
            }
        }

        private static void MarkSettingDirtyAndSave(AssetPackSO setting)
        {
            EditorUtility.SetDirty(setting);
            AssetDatabase.SaveAssets();
        }

        // ======================= 发版区（多平台选择 + 打包按钮） =======================
        private const string PREF_WIN = "Bighead.AssetPack.Build.Windows";
        private const string PREF_IOS = "Bighead.AssetPack.Build.iOS";
        private const string PREF_ANDROID = "Bighead.AssetPack.Build.Android";

        private static bool _prefsLoaded;
        private static bool _platformWindows;
        private static bool _platformIOS;
        private static bool _platformAndroid;

        private static void EnsurePlatformPrefsLoaded()
        {
            if (_prefsLoaded) return;
            _platformWindows = EditorPrefs.GetBool(PREF_WIN, true);
            _platformIOS = EditorPrefs.GetBool(PREF_IOS, false);
            _platformAndroid = EditorPrefs.GetBool(PREF_ANDROID, false);
            _prefsLoaded = true;
        }

        private static void DrawBuildSection(AssetPackSO setting)
        {
            EnsurePlatformPrefsLoaded();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("发版平台", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _platformWindows = EditorGUILayout.ToggleLeft("Windows", _platformWindows);
            _platformIOS = EditorGUILayout.ToggleLeft("iOS", _platformIOS);
            _platformAndroid = EditorGUILayout.ToggleLeft("Android", _platformAndroid);
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(PREF_WIN, _platformWindows);
                EditorPrefs.SetBool(PREF_IOS, _platformIOS);
                EditorPrefs.SetBool(PREF_ANDROID, _platformAndroid);
            }

            GUILayout.Space(5);
            GUI.enabled = _platformWindows || _platformIOS || _platformAndroid;
            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("立即打包（多平台）", GUILayout.Height(28)))
            {
                EditorUtility.SetDirty(setting);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string platformList =
                    $"{(_platformWindows ? "• Windows\n" : "")}" +
                    $"{(_platformIOS ? "• iOS\n" : "")}" +
                    $"{(_platformAndroid ? "• Android\n" : "")}";

                if (EditorUtility.DisplayDialog("确认打包",
                        $"将为以下平台执行打包：\n{platformList}执行前将清空 Addressables 临时 Group，确定继续？",
                        "确定", "取消"))
                {
                    if (_platformWindows) AssetPackPipeline.BuildForPlatform(BuildTarget.StandaloneWindows64);
                    if (_platformIOS) AssetPackPipeline.BuildForPlatform(BuildTarget.iOS);
                    if (_platformAndroid) AssetPackPipeline.BuildForPlatform(BuildTarget.Android);
                }
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            GUILayout.Space(5);
            if (GUILayout.Button("打开输出目录", GUILayout.Height(22)))
            {
                AssetPackPipeline.OpenOutputFolder();
            }
        }
    }
}
#endif
