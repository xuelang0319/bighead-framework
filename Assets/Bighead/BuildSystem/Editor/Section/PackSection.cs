#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;

namespace Bighead.BuildSystem.Editor
{
    public class PackSection
    {
        private readonly PackNodeService _service;
        private readonly PackNodeRenderer _renderer;
        private readonly PackNodeInteractionController _controller;
        private readonly PackDragHandler _dragHandler;
        private List<PackGroup> _groups;

        public PackSection()
        {
            _service = new PackNodeService();
            _controller = new PackNodeInteractionController();
            _renderer = new PackNodeRenderer();
            _dragHandler = new PackDragHandler(OnDragPerform);

            // 绑定事件
            _controller.OnHoverChanged += RequestRepaint;
            _controller.OnSelectionChanged += RequestRepaint;

            _renderer.OnDeleteGroup += OnDeleteGroup;
            _renderer.OnDeleteEntry += OnDeleteEntry;
            _renderer.OnRowClicked += (g, e) => RequestRepaint();

            Reload();
        }

        public void Draw()
        {
            _renderer.DrawGroups(_groups, _controller);
        }

        private void Reload()
        {
            _service.Reload();
            _groups = _service.Groups;
            RequestRepaint();
        }

        private void OnDeleteGroup(PackGroup group)
        {
            if (EditorUtility.DisplayDialog("删除分组", $"确定删除分组 {group.Name} 吗？", "删除", "取消"))
            {
                _service.RemoveGroup(group);
                Reload();
            }
        }

        private void OnDeleteEntry(PackGroup group, PackEntry entry)
        {
            if (EditorUtility.DisplayDialog("删除资源", $"确定删除资源 {entry.Name} 吗？", "删除", "取消"))
            {
                _service.RemoveEntry(group, entry);
                Reload();
            }
        }

        private void RequestRepaint()
        {
            if (EditorWindow.focusedWindow != null)
                EditorWindow.focusedWindow.Repaint();
        }

        private void OnDragPerform(string[] assetPaths)
        {
            // 拖拽逻辑可在这里实现
        }
    }
}
#endif
