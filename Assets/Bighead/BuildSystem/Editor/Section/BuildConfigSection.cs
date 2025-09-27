#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 构建配置界面 Section
    /// 负责渲染构建模式、多平台选择、路径配置、同步服务器设置
    /// </summary>
    public class BuildConfigSection
    {
        private readonly BuildSystemSetting _setting;
        private bool _incrementalAvailable = true; // 外部可动态设置增量打包是否可用

        public BuildConfigSection(BuildSystemSetting setting)
        {
            _setting = setting;
            RefreshIncrementalAvailable();
        }

        /// <summary>供外部调用手动刷新增量打包可用状态</summary>
        public void RefreshIncrementalAvailable()
        {
            _incrementalAvailable = CheckIncrementalAvailable();
        }

        /// <summary>内部检查当前版本是否允许增量打包</summary>
        private bool CheckIncrementalAvailable()
        {
            if (_setting == null) return false;

            var (buildPath, _) = _setting.GetAddressablePaths();
            string currentVersion = PlayerSettings.bundleVersion;

            string resolvedPath = buildPath
                .Replace("{Platform}", EditorUserBuildSettings.activeBuildTarget.ToString())
                .Replace("{Version}", currentVersion);

            string catalogPath = Path.Combine(resolvedPath, "catalog.json");
            return File.Exists(catalogPath);
        }

        /// <summary>绘制 UI</summary>
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
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("开始打包", GUILayout.Width(120), GUILayout.Height(26)))
                {
                    // 未来接入构建控制模块
                    Debug.Log("[BuildSystem] 点击开始打包（待接入控制模块）");
                }
            }

            if (GUI.changed)
                EditorUtility.SetDirty(_setting);
        }

        // ---------------------- Addressables 快捷入口 ----------------------
        private void DrawAddressablesLink()
        {
            var rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            var content = new GUIContent("Addressables 组（点击打开）");
            EditorGUI.LabelField(rect, content, EditorStyles.linkLabel);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
                Event.current.Use();
            }

            GUILayout.Space(6);
        }

        // ---------------------- 构建模式选择 ----------------------
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
            {
                EditorGUILayout.HelpBox("当前版本尚未执行全量打包，无法启用增量打包。", MessageType.Info);
            }

            EditorGUI.indentLevel--;
            GUILayout.Space(6);
        }

        // ---------------------- 平台选择 ----------------------
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
            foreach (var t in kCommonTargets)
            {
                bool selected = _setting.SelectedPlatforms.Contains(t);
                bool newSelected = EditorGUILayout.ToggleLeft(t.ToString(), selected);
                if (newSelected && !selected) _setting.SelectedPlatforms.Add(t);
                else if (!newSelected && selected) _setting.SelectedPlatforms.Remove(t);
            }
            EditorGUI.indentLevel--;
            GUILayout.Space(6);
        }

        // ---------------------- BuildPath / LoadPath ----------------------
        private void DrawPaths()
        {
            EditorGUILayout.LabelField("Addressables 路径", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _setting.BuildPath = EditorGUILayout.TextField("Build Path", _setting.BuildPath);
            _setting.LocalLoadPath = EditorGUILayout.TextField("Load Path (本地)", _setting.LocalLoadPath);
            EditorGUI.indentLevel--;
            GUILayout.Space(6);
        }

        // ---------------------- 同步服务器配置 ----------------------
        private void DrawSyncServerSection()
        {
            EditorGUILayout.LabelField("同步服务器", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            _setting.SyncConfig.Enable = EditorGUILayout.ToggleLeft("启用", _setting.SyncConfig.Enable);

            if (_setting.SyncConfig.Enable)
            {
                _setting.SyncConfig.DownloadPath = EditorGUILayout.TextField("下载路径 (LoadPath)", _setting.SyncConfig.DownloadPath);
                _setting.SyncConfig.UploadPath = EditorGUILayout.TextField("上传路径", _setting.SyncConfig.UploadPath);
                _setting.SyncConfig.AuthToken = EditorGUILayout.PasswordField("Token", _setting.SyncConfig.AuthToken);
                _setting.SyncConfig.ExtraArgs = EditorGUILayout.TextField("附加参数", _setting.SyncConfig.ExtraArgs);
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif
