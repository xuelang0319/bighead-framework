#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    public class BuildConfigSection
    {
        private readonly BuildSystemSetting _setting;
        private bool _incrementalAvailable = true;

        public BuildConfigSection(BuildSystemSetting setting)
        {
            _setting = setting;
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

        public void Draw()
        {
            if (_setting == null) return;

            DrawAddressablesLink();
            DrawBuildModeSelector();
            DrawPlatformSelector();
            DrawPaths();
            DrawSyncServerSection();

            GUILayout.Space(10);
            using (new EditorGUILayout.HorizontalScope())
            {
                // 打开目录按钮
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

        private static readonly BuildTarget[] kCommonTargets =
        {
            BuildTarget.StandaloneWindows64,
            BuildTarget.Android,
            BuildTarget.iOS
        };

        private void DrawPlatformSelector()
        {
            EditorGUILayout.LabelField("平台选择", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            var list = (_setting.SelectedPlatforms != null && _setting.SelectedPlatforms.Length > 0)
                ? new List<BuildTarget>(_setting.SelectedPlatforms)
                : new List<BuildTarget>();

            foreach (var t in kCommonTargets)
            {
                bool selected = list.Contains(t);
                bool newSelected = EditorGUILayout.ToggleLeft(t.ToString(), selected);
                if (newSelected && !selected) list.Add(t);
                else if (!newSelected && selected) list.Remove(t);
            }

            _setting.SelectedPlatforms = list.ToArray();

            EditorGUI.indentLevel--;
            GUILayout.Space(6);
        }

        private void DrawPaths()
        {
            EditorGUILayout.LabelField("输出路径设置", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _setting.BuildPath = EditorGUILayout.TextField("根路径", _setting.BuildPath);
            _setting.LocalLoadPath = EditorGUILayout.TextField("加载路径(本地)", _setting.LocalLoadPath);

            // 实时显示拼接结果
            if (_setting.SelectedPlatforms != null && _setting.SelectedPlatforms.Length > 0)
            {
                string version = PlayerSettings.bundleVersion;
                foreach (var platform in _setting.SelectedPlatforms)
                {
                    string finalPath = Path.Combine(_setting.BuildPath, platform.ToString(), version);
                    EditorGUILayout.HelpBox($"平台 {platform}: {finalPath}", MessageType.None);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("未选择平台，无法预览拼接结果", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(6);
        }

        private void DrawSyncServerSection()
        {
            EditorGUILayout.LabelField("同步服务器", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            _setting.SyncConfig.Enable = EditorGUILayout.ToggleLeft("启用", _setting.SyncConfig.Enable);

            if (_setting.SyncConfig.Enable)
            {
                _setting.SyncConfig.DownloadPath =
                    EditorGUILayout.TextField("下载路径", _setting.SyncConfig.DownloadPath);
                _setting.SyncConfig.UploadPath =
                    EditorGUILayout.TextField("上传路径", _setting.SyncConfig.UploadPath);
                _setting.SyncConfig.AuthToken =
                    EditorGUILayout.PasswordField("Token", _setting.SyncConfig.AuthToken);
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 打开拼接后的输出目录：多平台时打开根路径，单平台时打开该平台版本目录
        /// </summary>
        private void OpenOutputDirectory()
        {
            string version = PlayerSettings.bundleVersion;

            if (_setting.SelectedPlatforms == null || _setting.SelectedPlatforms.Length == 0)
            {
                Debug.LogWarning("[BuildConfigSection] 没有选择平台，无法打开输出目录。");
                return;
            }

            // 多平台 → 打开根路径
            if (_setting.SelectedPlatforms.Length > 1)
            {
                string rootPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", _setting.BuildPath));
                if (Directory.Exists(rootPath))
                    EditorUtility.RevealInFinder(rootPath);
                else
                    Debug.LogWarning($"[BuildConfigSection] 目录不存在: {rootPath}");
                return;
            }

            // 单平台 → 打开该平台对应目录
            string fullPath = Path.Combine(_setting.BuildPath, _setting.SelectedPlatforms[0].ToString(), version);
            string resolvedFullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", fullPath));
            if (Directory.Exists(resolvedFullPath))
                EditorUtility.RevealInFinder(resolvedFullPath);
            else
                Debug.LogWarning($"[BuildConfigSection] 目录不存在: {resolvedFullPath}");
        }
    }
}
#endif
