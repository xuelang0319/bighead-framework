#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
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

            if (!group.IsExpanded) return;

            EditorGUI.indentLevel++;
            foreach (var c in group.Children)
                DrawNode(c, group);
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
            float nameW = Mathf.Min(total * 0.35f, 320f);
            float pathW = total - nameW - 8f;

            var nameRect = new Rect(rowRect.x, rowRect.y, nameW, rowRect.height);
            var pathRect = new Rect(nameRect.xMax + 8f, rowRect.y, pathW, rowRect.height);

            var nameStyle = folder.IsInGroup ? EditorStyles.boldLabel : EditorStyles.miniLabel;
            EditorGUI.LabelField(nameRect, folder.Name, nameStyle);
            EditorGUI.LabelField(pathRect, folder.Path, EditorStyles.miniLabel);

            if (folder.IsExpanded && folder.Children.Count > 0)
            {
                EditorGUI.indentLevel++;
                foreach (var child in folder.Children)
                    DrawNode(child, ownerGroup);
                EditorGUI.indentLevel--;
            }

            if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                if (folder.IsInGroup)
                    menu.AddItem(new GUIContent("从该组移除"), false, () => _onRemoveNode?.Invoke(folder, ownerGroup));
                else
                    menu.AddItem(new GUIContent("加入该组"), false, () => _onAddNode?.Invoke(folder.Path, ownerGroup));
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        private void DrawFileRow(PackFileNode file, PackGroupNode ownerGroup)
        {
            var rowRect = EditorGUILayout.GetControlRect();
            float total = rowRect.width;
            float nameW = Mathf.Min(total * 0.35f, 320f);
            float pathW = total - nameW - 8f;

            var nameRect = new Rect(rowRect.x, rowRect.y, nameW, rowRect.height);
            var pathRect = new Rect(nameRect.xMax + 8f, rowRect.y, pathW, rowRect.height);

            var style = file.IsInGroup ? EditorStyles.label : EditorStyles.miniLabel;
            EditorGUI.LabelField(nameRect, file.Name, style);
            EditorGUI.LabelField(pathRect, file.Path, EditorStyles.miniLabel);

            if (Event.current.type == EventType.ContextClick && rowRect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
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
