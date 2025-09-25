#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 主面板 Section：显示 Addressable Group 和节点树，并处理拖拽、右键操作
    /// </summary>
    public class PackSection
    {
        private readonly PackNodeService _service;
        private readonly PackNodeRenderer _renderer;
        private readonly PackDragHandler _dragHandler;

        public PackSection()
        {
            _service = new PackNodeService();
            _renderer = new PackNodeRenderer(OnAddNode, OnRemoveNode);
            _dragHandler = new PackDragHandler(OnDragPerform);

            _service.OnTreeReloaded += Repaint;
            _service.OnNodeRemoved += (n, g) => Repaint();
        }

        public void OnGUI()
        {
            // Header
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Addressable Groups", EditorStyles.boldLabel);
                if (GUILayout.Button("刷新", GUILayout.Width(60)))
                {
                    _service.Reload();
                    GUI.FocusControl(null);
                }
            }

            // 树渲染
            EditorGUILayout.Space();
            _renderer.DrawTree(_service.Groups);

            // 拖拽区域
            _dragHandler.HandleDragArea();
        }

        private void OnAddNode(string path, PackGroupNode group)
        {
            _service.AddNode(path, group);
        }

        private void OnRemoveNode(PackNode node, PackGroupNode group)
        {
            _service.RemoveNode(node, group);
        }

        private void OnDragPerform(string[] draggedPaths, PackGroupNode targetGroup)
        {
            if (draggedPaths == null || draggedPaths.Length == 0 || targetGroup == null)
                return;

            foreach (var path in draggedPaths)
                _service.AddNode(path, targetGroup);
        }

        private void Repaint()
        {
            EditorWindow.focusedWindow?.Repaint();
        }
    }
}
#endif
