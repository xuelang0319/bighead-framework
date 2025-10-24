using UnityEditor;
using UnityEngine;

namespace Bighead.ExcelBuild.Editor
{
    /// <summary>
    /// Project Settings > Bighead/ExcelBuild
    /// 仅基础骨架：注册到ProjectSettings并渲染最小占位界面。
    /// </summary>
    public sealed class ExcelBuildProvider : SettingsProvider
    {
        private Vector2 _scroll;
        private ExcelFoldersBlock _excelFoldersBlock = new ExcelFoldersBlock();
        private ExcelBuildActionsBlock _excelBuildActionsBlock = new ExcelBuildActionsBlock();
        

        private ExcelBuildProvider(string path, SettingsScope scope)
            : base(path, scope)
        {
            keywords = new System.Collections.Generic.HashSet<string>(new[] { "Bighead", "Excel", "Build" });
        }

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            // 在 Project Settings 面板的路径
            return new ExcelBuildProvider("Project/Bighead/ExcelBuild", SettingsScope.Project);
        }

        public override void OnGUI(string searchContext)
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            // 占位内容；后续由你按计划接入具体块的 Render 调用
            EditorGUILayout.LabelField("Bighead Excel Build", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Provider 基础已注册到 Project Settings：Bighead/ExcelBuild。", MessageType.Info);
            _excelFoldersBlock.Render();
            _excelBuildActionsBlock.Render();
            EditorGUILayout.EndScrollView();
        }
    }
}