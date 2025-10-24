// ExcelBuildActionsBlock.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Bighead.Core.Editor; // TitleBlock
using Bighead.ExcelBuild; // ExcelMetaController / ExcelSetting(存储路径相关)

namespace Bighead.ExcelBuild.Editor
{
    /// <summary>
    /// “执行构建”动作块：点击按钮 → 调用 ExcelMetaController.BuildExcelMetaCollection(...)
    /// 依赖 ExcelFoldersSetting 中的相对路径（基于 Assets，可含 ../../）。
    /// </summary>
    public sealed class ExcelBuildActionsBlock : TitleBlock
    {
        public override string Id => "ExcelBuild.Actions";
        public override string Title => "Actions";

        private const string FoldersSettingPath = "Assets/Bighead/Settings/ExcelFoldersSetting.asset";

        protected override void OnRender()
        {
            // 读取已持久化的文件夹列表
            var foldersSetting = AssetDatabase.LoadAssetAtPath<ExcelFoldersSetting>(FoldersSettingPath);
            if (foldersSetting == null)
            {
                EditorGUILayout.HelpBox("Folders setting not found. Please select folders first.", MessageType.Warning);
                return;
            }

            if (foldersSetting.Folders == null || foldersSetting.Folders.Count == 0)
            {
                EditorGUILayout.HelpBox("No folders selected.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var canBuild = GUILayout.Button("Build Excel Meta", GUILayout.MaxWidth(180));
            EditorGUILayout.EndHorizontal();

            if (!canBuild) return;

            // 将相对路径（基于 Assets，可含 ../../）转换为绝对路径
            var absDirs = ToAbsoluteDirs(foldersSetting.Folders);
            if (absDirs.Count == 0)
            {
                Debug.LogWarning("[ExcelBuild] No valid directories after normalization.");
                return;
            }

            // 调用控制器执行构建（内部负责增量、保存与日志）
            ExcelMetaController.BuildExcelMetaCollection(absDirs); // :contentReference[oaicite:3]{index=3}
        }

        private static List<string> ToAbsoluteDirs(IEnumerable<string> rels)
        {
            var assets = Application.dataPath.Replace('\\', '/'); // <proj>/Assets
            return rels
                .Where(s => !string.IsNullOrEmpty(s))
                .Select(s => s.Trim().Replace('\\', '/'))
                .Select(s => Path.GetFullPath(Path.Combine(assets, s)).Replace('\\', '/'))
                .Where(Directory.Exists)
                .Distinct(System.StringComparer.Ordinal)
                .ToList();
        }
    }
}
