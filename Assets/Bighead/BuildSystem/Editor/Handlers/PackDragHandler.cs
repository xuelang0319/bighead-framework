#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 拖拽文件/文件夹到面板区域，添加到当前选中的 Addressable Group
    /// </summary>
    public class PackDragHandler
    {
        private readonly Action<string[], PackGroupNode> _onDragPerform;
        private PackGroupNode _currentTargetGroup; // 可以在外部赋值当前激活的组

        public PackDragHandler(Action<string[], PackGroupNode> onDragPerform)
        {
            _onDragPerform = onDragPerform;
        }

        public void HandleDragArea()
        {
            var rect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "拖拽文件或文件夹到此处加入选中组", EditorStyles.helpBox);

            Event evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    var paths = DragAndDrop.paths;
                    _onDragPerform?.Invoke(paths, _currentTargetGroup);
                    evt.Use();
                    break;
            }
        }

        /// <summary>外部可以更新当前目标 Group</summary>
        public void SetCurrentTargetGroup(PackGroupNode group)
        {
            _currentTargetGroup = group;
        }
    }
}
#endif