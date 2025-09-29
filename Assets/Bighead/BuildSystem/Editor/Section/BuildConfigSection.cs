#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    public class BuildConfigSection
    {
        private readonly BuildSystemSetting _setting;
        private bool _incrementalAvailable = true;
        private ReorderableList _platformList;

        public BuildConfigSection(BuildSystemSetting setting)
        {
            _setting = setting;
            InitPlatformList();
            RefreshIncrementalAvailable();
        }

        public void RefreshIncrementalAvailable()
        {
            _incrementalAvailable = CheckIncrementalAvailable();
        }

        private bool CheckIncrementalAvailable()
        {
            Debug.Log("[BuildConfigSection] 强制允许增量打包（测试模式）");
            return true;
        }

        private void InitPlatformList()
        {
            if (_setting.BuildPlatformSettings == null)
                _setting.BuildPlatformSettings = new List<BuildPlatformSetting>();

            _platformList = new ReorderableList(_setting.BuildPlatformSettings, typeof(BuildPlatformSetting), true, true, true, true);

            _platformList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, "目标平台配置");
            };

            _platformList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index < 0 || index >= _setting.BuildPlatformSettings.Count)
                    return;

                var element = _setting.BuildPlatformSettings[index];
                float lineHeight = EditorGUIUtility.singleLineHeight;
                float padding = 2f;

                // 第一行：平台选择 + 上传开关
                Rect line1 = new Rect(rect.x, rect.y + padding, rect.width, lineHeight);
                float halfWidth = rect.width * 0.5f;

                Rect platformRect = new Rect(line1.x, line1.y, halfWidth - 5, lineHeight);
                Rect uploadRect   = new Rect(line1.x + halfWidth, line1.y, halfWidth, lineHeight);

                element.Platform = (BuildTarget)EditorGUI.EnumPopup(platformRect, "平台", element.Platform);
                element.Upload2Server = EditorGUI.ToggleLeft(uploadRect, "上传到服务器", element.Upload2Server);

                // 第二行：服务器地址 + 秘钥（仅当 Upload2Server = true）
                if (element.Upload2Server)
                {
                    Rect line2 = new Rect(rect.x, line1.y + lineHeight + padding, rect.width, lineHeight);

                    float labelWidth = 60;
                    float fieldWidth = (rect.width - labelWidth * 2 - 10) / 2;

                    Rect urlLabelRect = new Rect(line2.x, line2.y, labelWidth, lineHeight);
                    Rect urlFieldRect = new Rect(urlLabelRect.xMax, line2.y, fieldWidth, lineHeight);
                    Rect secretLabelRect = new Rect(urlFieldRect.xMax + 5, line2.y, labelWidth, lineHeight);
                    Rect secretFieldRect = new Rect(secretLabelRect.xMax, line2.y, fieldWidth, lineHeight);

                    EditorGUI.LabelField(urlLabelRect, "服务器");
                    element.ServerUrl = EditorGUI.TextField(urlFieldRect, element.ServerUrl);

                    EditorGUI.LabelField(secretLabelRect, "秘钥");
                    element.Secret = EditorGUI.PasswordField(secretFieldRect, element.Secret);
                }
            };

            // 动态高度：Upload2Server = true 时增加一行高度
            _platformList.elementHeightCallback = index =>
            {
                if (index < 0 || index >= _setting.BuildPlatformSettings.Count)
                    return EditorGUIUtility.singleLineHeight + 4;

                var element = _setting.BuildPlatformSettings[index];
                float baseHeight = EditorGUIUtility.singleLineHeight + 4;
                if (element.Upload2Server)
                    baseHeight += EditorGUIUtility.singleLineHeight + 4;

                return baseHeight;
            };

            _platformList.onAddCallback = list =>
            {
                _setting.BuildPlatformSettings.Add(new BuildPlatformSetting { Platform = BuildTarget.NoTarget });
            };

            _platformList.onRemoveCallback = list =>
            {
                if (list.index >= 0 && list.index < _setting.BuildPlatformSettings.Count)
                    _setting.BuildPlatformSettings.RemoveAt(list.index);
            };
        }

        public void Draw()
        {
            if (_setting == null) return;

            DrawAddressablesLink();
            DrawBuildModeSelector();

            // 渲染目标平台配置
            _platformList.DoLayoutList();

            DrawPaths();

            GUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("打开输出目录", GUILayout.Width(120), GUILayout.Height(24)))
                {
                    OpenOutputDirectory();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("开始打包", GUILayout.Width(120), GUILayout.Height(26)))
                {
                    EditorApplication.delayCall += () => BuildSystemPipeline.RunAsync(_setting, this).Forget();
                }
            }

            if (GUI.changed)
                EditorUtility.SetDirty(_setting);
        }

        private void DrawAddressablesLink()
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            var content = new GUIContent("打开 Addressables Groups");
            EditorGUI.LabelField(rect, content, EditorStyles.linkLabel);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
                Event.current.Use();
            }

            GUILayout.Space(6);
        }

        private void DrawBuildModeSelector()
        {
            EditorGUILayout.LabelField("构建模式", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            using (new EditorGUILayout.HorizontalScope())
            {
                bool fullSelected = _setting.Mode == BuildMode.FullBuild;
                bool newFullSelected = EditorGUILayout.ToggleLeft("全量打包", fullSelected);
                if (newFullSelected && !fullSelected)
                    _setting.Mode = BuildMode.FullBuild;

                using (new EditorGUI.DisabledScope(!_incrementalAvailable))
                {
                    bool incSelected = _setting.Mode == BuildMode.Incremental;
                    bool newIncSelected = EditorGUILayout.ToggleLeft("增量打包", incSelected);
                    if (newIncSelected && !incSelected)
                        _setting.Mode = BuildMode.Incremental;
                }
            }

            if (!_incrementalAvailable)
                EditorGUILayout.HelpBox("当前平台/版本尚未执行过全量打包，无法启用增量打包。", MessageType.Info);

            EditorGUI.indentLevel--;
            GUILayout.Space(6);
        }

        private void DrawPaths()
        {
            EditorGUILayout.LabelField("输出路径预览", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            if (_setting.BuildPlatformSettings != null && _setting.BuildPlatformSettings.Count > 0)
            {
                string version = PlayerSettings.bundleVersion;
                foreach (var platformSetting in _setting.BuildPlatformSettings)
                {
                    if (platformSetting.Platform == BuildTarget.NoTarget) continue;

                    string finalPath = Path.Combine(_setting.BuildPath, platformSetting.Platform.ToString(), version);
                    EditorGUILayout.HelpBox($"平台 {platformSetting.Platform}: {finalPath}", MessageType.None);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未添加平台，无法预览拼接结果", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(6);
        }

        private void OpenOutputDirectory()
        {
            string version = PlayerSettings.bundleVersion;

            if (_setting.BuildPlatformSettings == null || _setting.BuildPlatformSettings.Count == 0)
            {
                Debug.LogWarning("[BuildConfigSection] 没有选择平台，无法打开输出目录。");
                return;
            }

            if (_setting.BuildPlatformSettings.Count > 1)
            {
                string rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", _setting.BuildPath));
                if (Directory.Exists(rootPath))
                    EditorUtility.RevealInFinder(rootPath);
                else
                    Debug.LogWarning($"[BuildConfigSection] 目录不存在: {rootPath}");
                return;
            }

            var platform = _setting.BuildPlatformSettings[0].Platform;
            string fullPath = Path.Combine(_setting.BuildPath, platform.ToString(), version);
            string resolvedFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", fullPath));
            if (Directory.Exists(resolvedFullPath))
                EditorUtility.RevealInFinder(resolvedFullPath);
            else
                Debug.LogWarning($"[BuildConfigSection] 目录不存在: {resolvedFullPath}");
        }
    }
}
#endif
