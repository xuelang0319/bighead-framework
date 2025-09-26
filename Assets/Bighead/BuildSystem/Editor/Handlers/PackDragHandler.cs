#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    public class PackDragHandler
    {
        private readonly Action<string[]> _onDragPerform;

        public PackDragHandler(Action<string[]> onDragPerform)
        {
            _onDragPerform = onDragPerform;
        }

        public void HandleDragArea()
        {
            var rect = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "拖拽文件/文件夹到此处", EditorStyles.helpBox);

            var evt = Event.current;
            if (!rect.Contains(evt.mousePosition)) return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();
                    _onDragPerform?.Invoke(DragAndDrop.paths);
                    evt.Use();
                    break;
            }
        }
    }
}
#endif