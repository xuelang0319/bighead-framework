// ExcelFoldersBlock.cs
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Bighead.Core.Editor; // TitleBlock

namespace Bighead.ExcelBuild.Editor
{
    /// <summary>集成 TitleBlock；支持多文件夹选择；相对路径存储；父子包含去重；变更即写盘</summary>
    public sealed class ExcelFoldersBlock : TitleBlock
    {
        public override string Id => "ExcelBuild.Folders";
        public override string Title => "Excel Folders";

        private const string kAssetPath = "Assets/Bighead/Settings/ExcelFoldersSetting.asset";
        private static ExcelFoldersSetting _setting;

        private static ExcelFoldersSetting GetOrCreateSetting()
        {
            if (_setting != null) return _setting;

            _setting = AssetDatabase.LoadAssetAtPath<ExcelFoldersSetting>(kAssetPath);
            if (_setting == null)
            {
                var dir = Path.GetDirectoryName(kAssetPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                _setting = ScriptableObject.CreateInstance<ExcelFoldersSetting>();
                AssetDatabase.CreateAsset(_setting, kAssetPath);
                AssetDatabase.SaveAssets();
            }
            return _setting;
        }

        protected override void OnRender()
        {
            var setting = GetOrCreateSetting();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Folder", GUILayout.MaxWidth(120)))
            {
                var pickedAbs = EditorUtility.OpenFolderPanel("Select Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(pickedAbs))
                {
                    var rel = ToAssetsRelative(pickedAbs);
                    if (!string.IsNullOrEmpty(rel))
                    {
                        if (TryMergeWithParents(ref setting.Folders, rel))
                            Persist(setting);
                    }
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(4);

            if (setting.Folders.Count == 0)
            {
                EditorGUILayout.HelpBox("No folders selected.", MessageType.Info);
                return;
            }

            for (int i = 0; i < setting.Folders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                var before = setting.Folders[i];
                var after = EditorGUILayout.TextField(before);

                if (after != before)
                {
                    // 手动编辑路径：转相对 & 去重合并
                    var normalized = NormalizeRelative(after);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        setting.Folders[i] = normalized;
                        if (CompactParents(ref setting.Folders))
                            Persist(setting);
                        else
                            Persist(setting);
                    }
                }

                if (GUILayout.Button("Browse", GUILayout.MaxWidth(80)))
                {
                    var start = ToAbsolutePath(setting.Folders[i]);
                    var pickedAbs = EditorUtility.OpenFolderPanel("Select Folder", string.IsNullOrEmpty(start) ? Application.dataPath : start, "");
                    if (!string.IsNullOrEmpty(pickedAbs))
                    {
                        var rel = ToAssetsRelative(pickedAbs);
                        if (!string.IsNullOrEmpty(rel))
                        {
                            setting.Folders[i] = rel;
                            if (CompactParents(ref setting.Folders))
                                Persist(setting);
                            else
                                Persist(setting);
                        }
                    }
                }

                if (GUILayout.Button("Remove", GUILayout.MaxWidth(80)))
                {
                    setting.Folders.RemoveAt(i);
                    Persist(setting);
                    GUIUtility.ExitGUI();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // —— 工具区（相对路径、去重 / 父子包含合并）——

        private static string ToAssetsRelative(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;
            absolutePath = FixSeparators(absolutePath);

            var assets = FixSeparators(Application.dataPath);             // <proj>/Assets
            var project = FixSeparators(Directory.GetParent(assets)!.FullName); // <proj>

            // 基于 Assets 计算相对路径；允许越级出现 ../../
            return NormalizeRelative(Path.GetRelativePath(assets, absolutePath));
        }

        private static string ToAbsolutePath(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return null;
            relative = NormalizeRelative(relative);

            var assets = FixSeparators(Application.dataPath);
            var abs = FixSeparators(Path.GetFullPath(Path.Combine(assets, relative)));
            return abs;
        }

        private static string NormalizeRelative(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return null;
            relative = FixSeparators(relative.Trim());

            // 清理 ./ 与重复斜杠
            try
            {
                var parts = relative.Split('/');
                var stack = new System.Collections.Generic.List<string>(parts.Length);
                foreach (var p in parts)
                {
                    if (string.IsNullOrEmpty(p) || p == ".") continue;
                    if (p == "..")
                    {
                        if (stack.Count > 0 && stack.Last() != "..") stack.RemoveAt(stack.Count - 1);
                        else stack.Add(".."); // 允许越级
                    }
                    else stack.Add(p);
                }
                var normalized = string.Join("/", stack);
                return normalized;
            }
            catch { return relative; }
        }

        private static string FixSeparators(string path) => path.Replace('\\', '/');

        /// <summary>添加新路径时：若已被某父路径包含，则忽略；否则加入并移除其所有子路径。</summary>
        private static bool TryMergeWithParents(ref System.Collections.Generic.List<string> list, string newRel)
        {
            newRel = NormalizeRelative(newRel);
            // 若已被现有父路径包含，则不变
            if (list.Any(p => IsParentOf(p, newRel))) return false;

            // 移除其子路径
            list.RemoveAll(p => IsParentOf(newRel, p));
            list.Add(newRel);
            list.Sort(StringComparer.Ordinal); // 稳定展示
            return true;
        }

        /// <summary>全表压缩：去重 + 父子包含合并（保留父，删除子）。</summary>
        private static bool CompactParents(ref System.Collections.Generic.List<string> list)
        {
            var before = string.Join("|", list);
            var arr = list.Select(NormalizeRelative).Where(s => !string.IsNullOrEmpty(s)).Distinct(StringComparer.Ordinal).OrderBy(s => s, StringComparer.Ordinal).ToList();

            // O(n^2) 简实现：对每个路径，若被前面任一路径包含则丢弃
            var compact = new System.Collections.Generic.List<string>(arr.Count);
            foreach (var p in arr)
            {
                if (compact.Any(parent => IsParentOf(parent, p))) continue;
                compact.Add(p);
            }

            list.Clear();
            list.AddRange(compact);
            var after = string.Join("|", list);
            return !string.Equals(before, after, StringComparison.Ordinal);
        }

        private static bool IsParentOf(string parentRel, string childRel)
        {
            parentRel = NormalizeRelative(parentRel);
            childRel = NormalizeRelative(childRel);
            if (string.IsNullOrEmpty(parentRel) || string.IsNullOrEmpty(childRel)) return false;
            if (string.Equals(parentRel, childRel, StringComparison.Ordinal)) return true;

            // 规范为带结尾斜杠的前缀匹配
            if (!parentRel.EndsWith("/")) parentRel += "/";
            return childRel.StartsWith(parentRel, StringComparison.Ordinal);
        }

        private static void Persist(ExcelFoldersSetting setting)
        {
            EditorUtility.SetDirty(setting);
            AssetDatabase.SaveAssets(); // 变更即写入
        }
    }
}
