#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    public class PackNodeRenderer
    {
        public event System.Action<PackGroup> OnDeleteGroup;
        public event System.Action<PackGroup, PackEntry> OnDeleteEntry;
        public event System.Action<PackGroup, PackEntry> OnRowClicked;

        private const float IndentPerLevel = 14f;
        private const float DeleteButtonWidth = 18f;
        private static readonly Color NormalRowColor = new Color(0.85f, 0.85f, 0.85f, 0.03f);
        private static readonly Color HoverRowColor = new Color(0.3f, 0.5f, 0.9f, 0.15f);
        private static readonly Color SelectedRowColor = new Color(0.2f, 0.4f, 0.7f, 0.3f);

        public void DrawGroups(List<PackGroup> groups, PackNodeInteractionController controller)
        {
            if (groups == null) return;
            foreach (var g in groups)
                DrawGroup(g, 0, controller);
        }

        private void DrawGroup(PackGroup group, int depth, PackNodeInteractionController controller)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            controller.UpdateHover(rowRect, group);

            DrawRowBackground(rowRect, controller.IsHovering(group), controller.IsSelected(group));

            HandleRowClick(rowRect, group, null, controller);

            GUI.BeginGroup(rowRect);
            float x = depth * IndentPerLevel;

            // 折叠箭头和名称
            Rect foldoutRect = new Rect(x, 0, rowRect.width - DeleteButtonWidth, rowRect.height);
            group.IsExpanded = EditorGUI.Foldout(foldoutRect, group.IsExpanded, group.Name, true);

            // 删除按钮
            Rect deleteRect = new Rect(rowRect.xMax - DeleteButtonWidth - rowRect.x, 0, DeleteButtonWidth, rowRect.height);
            if (GUI.Button(deleteRect, EditorGUIUtility.IconContent("TreeEditor.Trash"), GUIStyle.none))
                OnDeleteGroup?.Invoke(group);

            GUI.EndGroup();

            if (group.IsExpanded)
                foreach (var entry in group.Children)
                    DrawEntry(group, entry, depth + 1, controller);
        }

        private void DrawEntry(PackGroup parent, PackEntry entry, int depth, PackNodeInteractionController controller)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            controller.UpdateHover(rowRect, parent, entry);

            DrawRowBackground(rowRect, controller.IsHovering(parent, entry), controller.IsSelected(parent, entry));

            HandleRowClick(rowRect, parent, entry, controller);

            GUI.BeginGroup(rowRect);
            float x = depth * IndentPerLevel;

            Rect nameRect = new Rect(x, 0, rowRect.width - DeleteButtonWidth, rowRect.height);
            if (entry.Children != null && entry.Children.Count > 0)
                entry.IsExpanded = EditorGUI.Foldout(nameRect, entry.IsExpanded, entry.Name, true);
            else
                EditorGUI.LabelField(nameRect, entry.Name);

            Rect deleteRect = new Rect(rowRect.xMax - DeleteButtonWidth - rowRect.x, 0, DeleteButtonWidth, rowRect.height);
            if (GUI.Button(deleteRect, EditorGUIUtility.IconContent("TreeEditor.Trash"), GUIStyle.none))
                OnDeleteEntry?.Invoke(parent, entry);

            GUI.EndGroup();

            if (entry.IsExpanded && entry.Children != null)
                foreach (var child in entry.Children)
                    DrawEntry(parent, child, depth + 1, controller);
        }

        private void DrawRowBackground(Rect rect, bool hover, bool selected)
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (selected)
                    EditorGUI.DrawRect(rect, SelectedRowColor);
                else if (hover)
                    EditorGUI.DrawRect(rect, HoverRowColor);
                else
                    EditorGUI.DrawRect(rect, NormalRowColor);
            }
        }

        private void HandleRowClick(Rect rect, PackGroup group, PackEntry entry, PackNodeInteractionController controller)
        {
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 0)
            {
                controller.Select(group, entry);
                OnRowClicked?.Invoke(group, entry);
                Event.current.Use();
            }
        }
    }
}
#endif
