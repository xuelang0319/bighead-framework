#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>处理拖拽添加路径</summary>
    public class PackDragHandler
    {
        private readonly PackNodeService _service;

        public PackDragHandler(PackNodeService service)
        {
            _service = service;
        }

        public void DrawDragArea()
        {
            var rect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            GUI.Box(rect, "拖拽文件或文件夹到此处", EditorStyles.helpBox);

            var evt = Event.current;
            if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) &&
                rect.Contains(evt.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (string path in DragAndDrop.paths)
                    {
                        _service.AddPath(path);
                    }
                    evt.Use();
                }
            }
        }
    }
}
#endif