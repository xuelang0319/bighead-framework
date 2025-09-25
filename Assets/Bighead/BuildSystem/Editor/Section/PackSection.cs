#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>Pack 面板 Section，入口类</summary>
    public class PackSection
    {
        private readonly PackNodeService _service;
        private readonly PackNodeRenderer _renderer;
        private readonly PackDragHandler _dragHandler;
        private Vector2 _scroll;

        public PackSection()
        {
            _service = new PackNodeService();
            _renderer = new PackNodeRenderer();
            _dragHandler = new PackDragHandler(_service);
        }

        public void Draw()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.LabelField("Pack Nodes", EditorStyles.boldLabel);
                GUILayout.Space(4);

                using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
                {
                    _scroll = scrollScope.scrollPosition;
                    _renderer.DrawTree(_service.Roots);
                }

                GUILayout.Space(6);
                _dragHandler.DrawDragArea();
            }
        }
    }
}
#endif