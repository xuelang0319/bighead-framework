#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 递归渲染：Group → Folder*(递归) / File
    /// - 所有层级文件夹都有 Foldout
    /// - 名称 + 路径 两列
    /// - IsInGroup=false 用灰色样式显示（未加入 Addressables）
    /// </summary>
    public class PackNodeRenderer
    {
        private readonly Action<string, PackGroupNode> _onAddNode;
        private readonly Action<PackNode, PackGroupNode> _onRemoveNode;

        public PackNodeRenderer(Action<string, PackGroupNode> onAddNode,
                                Action<PackNode, PackGroupNode> onRemoveNode)
        {
            _onAddNode = onAddNode;
            _onRemoveNode = onRemoveNode;
        }

        public void DrawTree(IReadOnlyList<PackGroupNode> groups)
        {
            if (groups == null) return;
            foreach (var g in groups) DrawGroup(g);
        }

        private void DrawGroup(PackGroupNode group)
        {
            var row = EditorGUILayout.GetControlRect();
            group.IsExpanded = EditorGUI.Foldout(row, group.IsExpanded, group.Name, true);

            // 双击：打开 Addressables Groups 并选中该组
            if (Event.current.type == EventType.MouseDown &&
                Event.current.clickCount == 2 &&
                row.Contains(Event.current.mousePosition))
            {
                EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
                if (group.GroupRef != null)
                {
                    EditorGUIUtility.PingObject(group.GroupRef);
                    Selection.activeObject = group.GroupRef;
                }
                Event.current.Use();
            }

            if (!group.IsExpanded) return;

            EditorGUI.indentLevel++;
            if (group.Children != null)
            {
                foreach (var c in group.Children)
                    DrawNode(c, group);
            }
            EditorGUI.indentLevel--;
        }

        private void DrawNode(PackNode node, PackGroupNode ownerGroup)
        {
            if (node is PackFolderNode folder)
                DrawFolderRow(folder, ownerGroup);
            else if (node is PackFileNode file)
                DrawFileRow(file, ownerGroup);
        }

        private void DrawFolderRow(PackFolderNode folder, PackGroupNode ownerGroup)
        {
            var rowRect = EditorGUILayout.GetControlRect();
            float total = rowRect.width;
            float nameW = Mathf.Min(total * 0.4f, 360f);
            float pathW = total - nameW - 8f;

            var nameRect = new Rect(rowRect.x, rowRect.y, nameW, rowRect.height);
            var pathRect = new Rect(nameRect.xMax + 8f, rowRect.y, pathW, rowRect.height);

            // 名称列用 Foldout
            var labelContent = new GUIContent(folder.Name, folder.Path);
            var nameStyle = folder.IsInGroup ? EditorStyles.foldout : EditorStyles.foldout; // 统一控件，颜色靠路径列区分
            folder.IsExpanded = EditorGUI.Foldout(nameRect, folder.IsExpanded, labelContent, true);

            // 路径列
            var pathStyle = folder.IsInGroup ? EditorStyles.miniLabel : EditorStyles.miniLabel;
            using (new EditorGUI.DisabledScope(!folder.IsInGroup))
            {
                // 灰色表现：未加入的我们用 DisabledScope + miniLabel（也可自定义颜色）
                EditorGUI.LabelField(pathRect, new GUIContent(folder.Path, folder.Path), pathStyle);
            }

            // 右键菜单
            if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("复制路径"), false, () => EditorGUIUtility.systemCopyBuffer = folder.Path);
                menu.AddItem(new GUIContent("在资源管理器中显示"), false, () => EditorUtility.RevealInFinder(folder.Path));
                menu.AddSeparator("");

                if (folder.IsInGroup)
                    menu.AddItem(new GUIContent("从该组移除（仅移除该文件夹 entry）"), false, () => _onRemoveNode?.Invoke(folder, ownerGroup));
                else
                    menu.AddItem(new GUIContent("加入该组（将此文件夹作为 entry）"), false, () => _onAddNode?.Invoke(folder.Path, ownerGroup));

                menu.ShowAsContext();
                Event.current.Use();
            }

            // 递归子节点
            if (folder.IsExpanded && folder.Children != null && folder.Children.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var child in folder.Children)
                    DrawNode(child, ownerGroup);
                EditorGUI.indentLevel--;
            }
        }

        private void DrawFileRow(PackFileNode file, PackGroupNode ownerGroup)
        {
            var rowRect = EditorGUILayout.GetControlRect();
            float total = rowRect.width;
            float nameW = Mathf.Min(total * 0.4f, 360f);
            float pathW = total - nameW - 8f;

            var nameRect = new Rect(rowRect.x, rowRect.y, nameW, rowRect.height);
            var pathRect = new Rect(nameRect.xMax + 8f, rowRect.y, pathW, rowRect.height);

            var nameStyle = file.IsInGroup ? EditorStyles.label : EditorStyles.miniLabel;
            EditorGUI.LabelField(nameRect, new GUIContent(file.Name, file.Path), nameStyle);
            EditorGUI.LabelField(pathRect, new GUIContent(file.Path, file.Path), EditorStyles.miniLabel);

            // 双击 Ping
            if (Event.current.type == EventType.MouseDown &&
                Event.current.clickCount == 2 &&
                rowRect.Contains(Event.current.mousePosition))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file.Path);
                if (obj != null)
                {
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                }
                Event.current.Use();
            }

            // 右键菜单
            if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("复制路径"), false, () => EditorGUIUtility.systemCopyBuffer = file.Path);
                menu.AddItem(new GUIContent("在资源管理器中显示"), false, () => EditorUtility.RevealInFinder(file.Path));
                menu.AddSeparator("");

                if (file.IsInGroup)
                    menu.AddItem(new GUIContent("从该组移除"), false, () => _onRemoveNode?.Invoke(file, ownerGroup));
                else
                    menu.AddItem(new GUIContent("加入该组"), false, () => _onAddNode?.Invoke(file.Path, ownerGroup));

                menu.ShowAsContext();
                Event.current.Use();
            }
        }
    }
}
#endif
