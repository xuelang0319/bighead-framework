#if UNITY_EDITOR
using UnityEngine;
using System;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 交互控制器，负责悬停行、选中行状态，通知外部刷新
    /// </summary>
    public class PackNodeInteractionController
    {
        public PackGroup HoverGroup { get; private set; }
        public PackEntry HoverEntry { get; private set; }

        public PackGroup SelectedGroup { get; private set; }
        public PackEntry SelectedEntry { get; private set; }

        public event Action OnHoverChanged;
        public event Action OnSelectionChanged;

        /// <summary>
        /// 每行绘制时调用，更新悬停状态
        /// </summary>
        public void UpdateHover(Rect rowRect, PackGroup group, PackEntry entry = null)
        {
            bool isHover = rowRect.Contains(Event.current.mousePosition);
            bool changed = false;

            if (isHover)
            {
                if (HoverGroup != group || HoverEntry != entry)
                {
                    HoverGroup = group;
                    HoverEntry = entry;
                    changed = true;
                }
            }
            else
            {
                if (HoverGroup == group && HoverEntry == entry)
                {
                    HoverGroup = null;
                    HoverEntry = null;
                    changed = true;
                }
            }

            if (changed)
                OnHoverChanged?.Invoke();
        }

        /// <summary>
        /// 点击行时调用，更新选中状态
        /// </summary>
        public void Select(PackGroup group, PackEntry entry = null)
        {
            SelectedGroup = group;
            SelectedEntry = entry;
            OnSelectionChanged?.Invoke();
        }

        public bool IsHovering(PackGroup group, PackEntry entry = null) =>
            HoverGroup == group && HoverEntry == entry;

        public bool IsSelected(PackGroup group, PackEntry entry = null) =>
            SelectedGroup == group && SelectedEntry == entry;
    }
}
#endif