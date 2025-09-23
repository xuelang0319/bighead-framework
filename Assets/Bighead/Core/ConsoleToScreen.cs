using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bighead.Utility
{
    public class ConsoleToScreen : MonoBehaviour
    {
        [Serializable]
        private struct LogEntry
        {
            public string Message;
            public string Stack;
            public LogType Type;
            public DateTime Time;
            public bool Expanded;
        }

        public enum DockEdge
        {
            Top,
            Bottom
        }

        public enum IconStyle
        {
            Text,
            Emoji,
            Texture
        }

        private enum ViewState
        {
            Panel,
            Bubble
        } // 面板 or 气泡

        [Header("View")] public DockEdge dock = DockEdge.Bottom; // 顶部/底部停靠
        [Range(0.15f, 0.6f)] public float heightPercent = 0.33f; // 面板占屏幕高度
        public int fontSize = 14;
        public KeyCode toggleKey = KeyCode.F1; // F1 在 面板/气泡 之间切换
        public int maxEntries = 300;

        [Header("Bubble")] public Vector2 bubbleSize = new Vector2(54, 54);
        public string bubbleText = "LOG"; // 气泡显示文字
        public Color bubbleColor = new Color(0, 0, 0, 0.55f);
        public Color bubbleAccent = new Color(0.25f, 0.8f, 0.5f);
        public bool bubbleShowBadge = true; // 气泡右上角未读徽标
        public Vector2 bubbleStartOffset = new Vector2(12, 12); // 初始位置（相对安全区左下角）
        public float bubbleCorner = 27f; // 圆角

        [Header("Filter")] public bool showInfo = true;
        public bool showWarning = true;
        public bool showError = true; // 含 Error / Assert / Exception
        public bool pauseOnError = false;
        public bool autoScroll = true;

        [Header("Icons & Colors")] public IconStyle iconStyle = IconStyle.Emoji;
        public Texture2D infoIcon;
        public Texture2D warnIcon;
        public Texture2D errorIcon;
        public Color infoColor = new Color(0.45f, 0.80f, 0.50f);
        public Color warnColor = new Color(0.88f, 0.66f, 0.05f);
        public Color errorColor = new Color(0.90f, 0.33f, 0.33f);
        public bool showBadgeCount = true;
        public Vector2 iconSize = new Vector2(18, 18);
        public float iconButtonWidth = 90f;

        // 内部
        private readonly List<LogEntry> _entries = new List<LogEntry>(256);
        private Vector2 _scroll;
        private GUIStyle _label, _btn, _toggle;
        private bool _paused;
        private Rect _safe; // 安全区
        private Rect _panelRect; // 面板区域
        private Rect _tabRect; // 折叠“展开”标签（面板隐藏时不再用）

        // 状态
        private ViewState _state = ViewState.Panel;
        private Rect _bubbleRect; // 气泡区域（像素）
        private bool _bubbleDragging;
        private Vector2 _dragOffset;
        private int _bubbleWinId;

        // 纯色纹理 + 计数
        public Texture2D _infoTex, _warnTex, _errorTex, _bubbleTex, _accentTex;
        int _infoCount, _warnCount, _errorCount; // 用于徽标

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
            EnsureIconTextures();
            _bubbleWinId = GetInstanceID() ^ 0xC0FFEE;
            _safe = GetSafeAreaPixels();
            // 气泡初始位置：安全区左下角 + 偏移
            _bubbleRect = new Rect(_safe.xMin + bubbleStartOffset.x,
                _safe.yMin + bubbleStartOffset.y,
                bubbleSize.x, bubbleSize.y);
            UpdateRects();
        }

        void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }

        void Update()
        {
            if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
                _state = _state == ViewState.Panel ? ViewState.Bubble : ViewState.Panel;

            var nowSafe = GetSafeAreaPixels();
            if (nowSafe != _safe)
            {
                _safe = nowSafe;
                UpdateRects();
                // 同时把气泡位置夹到安全区内
                ClampBubbleToSafe();
            }
        }

        void OnGUI()
        {
            if (_label == null) BuildStyles();
            UpdateRects();

            if (_state == ViewState.Bubble)
            {
                DrawBubble();
                return;
            }

            // == 面板 ==
            GUILayout.BeginArea(_panelRect, GUI.skin.window);
            DrawHeader();
            DrawListArea();
            GUILayout.EndArea();

            if (autoScroll && !_paused && Event.current.type == EventType.Repaint)
                _scroll.y = float.MaxValue;
        }

        // ==================== 绘制：气泡 ====================

        private void DrawBubble()
        {
            // 使用极简 Window 以获得“可拖拽”的能力
            _bubbleRect = GUI.Window(_bubbleWinId, _bubbleRect, BubbleWindow, GUIContent.none, GUIStyle.none);
            ClampBubbleToSafe(); // 保证拖动后不出安全区
        }

        private void BubbleWindow(int id)
        {
            // 背景圆角矩形（用纯色纹理模拟）
            if (_bubbleTex == null) _bubbleTex = MakeTex(8, 8, bubbleColor);
            if (_accentTex == null) _accentTex = MakeTex(8, 8, bubbleAccent);

            // 背景
            var r = new Rect(0, 0, _bubbleRect.width, _bubbleRect.height);
            GUI.DrawTexture(r, _bubbleTex);

            // 中心文字
            var label = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Max(12, fontSize),
                normal = { textColor = Color.white },
                richText = true
            };
            GUI.Label(r, $"<b>{bubbleText}</b>", label);

            // 未读徽标（右上角）
            if (bubbleShowBadge)
            {
                int count = _warnCount + _errorCount; // 只统计重要级别，也可改成总数
                if (count > 0)
                {
                    var badge = new Rect(r.xMax - 18, r.yMin + 2, 16, 16);
                    GUI.DrawTexture(badge, _accentTex);
                    var mini = new GUIStyle(GUI.skin.label)
                        { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.white } };
                    GUI.Label(badge, count > 99 ? "99+" : count.ToString(), mini);
                }
            }

            // 点击→展开为面板
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                _state = ViewState.Panel;
                // 展开后自动滚到底
                if (autoScroll) _scroll.y = float.MaxValue;
            }

            // 拖动（整个区域都可拖动）
            GUI.DragWindow(new Rect(0, 0, _bubbleRect.width, _bubbleRect.height));
        }

        private void ClampBubbleToSafe()
        {
            var x = Mathf.Clamp(_bubbleRect.x, _safe.xMin, _safe.xMax - _bubbleRect.width);
            var y = Mathf.Clamp(_bubbleRect.y, _safe.yMin, _safe.yMax - _bubbleRect.height);
            _bubbleRect.position = new Vector2(x, y);
        }

        // ==================== 绘制：面板 ====================
        private const float HeaderH = 32f;
        private const float HPad = 6f; // 左右内边距
        private const float VPad = 4f; // 上下内边距
        private const float Gap = 6f; // 按钮间距
        private const float RightBtnMinW = 68f; // 右侧按钮最小宽
    
        private void DrawHeader()
        {
            // 整个 Header 用垂直布局，严格分两行，避免任何重叠
            GUILayout.BeginVertical(GUI.skin.box);

            // ===== Row 1: 级别筛选（左） + Minimize（右） =====
            GUILayout.BeginHorizontal();

            // 左侧：Info / Warn / Error（用弹性布局，自动平分空间）
            GUILayout.BeginHorizontal();
            var expandW = GUILayout.ExpandWidth(true);
            showInfo    = DrawLevelToggle(showInfo,    "Info",  "ℹ️", infoIcon,  infoColor,  _infoTex,  iconButtonWidth, _infoCount);
            showWarning = DrawLevelToggle(showWarning, "Warn",  "⚠️", warnIcon,  warnColor,  _warnTex,  iconButtonWidth, _warnCount);
            showError   = DrawLevelToggle(showError,   "Error", "❌", errorIcon, errorColor, _errorTex, iconButtonWidth, _errorCount);
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            // 右侧：停靠切换 + 最小化
            if (GUILayout.Button(dock == DockEdge.Top ? "Top ▾" : "Bottom ▴", _btn, GUILayout.Width(90)))
            {
                dock = dock == DockEdge.Top ? DockEdge.Bottom : DockEdge.Top;
                UpdateRects();
            }
            if (GUILayout.Button("Minimize", _btn, GUILayout.Width(90)))
                _state = ViewState.Bubble;

            GUILayout.EndHorizontal(); // Row 1

            // ===== Row 2: 其它控制（自动换行由GUILayout自身处理） =====
            GUILayout.BeginHorizontal();
            autoScroll   = GUILayout.Toggle(autoScroll,   "Auto",       _toggle, GUILayout.MinWidth(60));
            pauseOnError = GUILayout.Toggle(pauseOnError, "PauseOnErr", _toggle, GUILayout.MinWidth(110));

            if (GUILayout.Button(_paused ? "Resume" : "Pause", _btn, GUILayout.MinWidth(80)))
                _paused = !_paused;

            if (GUILayout.Button("Clear", _btn, GUILayout.MinWidth(70)))
            {
                _entries.Clear();
                _infoCount = _warnCount = _errorCount = 0;
                _scroll = Vector2.zero;
            }

            GUILayout.EndHorizontal(); // Row 2

            GUILayout.EndVertical();   // Header
        }

// 绘制右侧控制按钮，空间不足时自动换行
        private void DrawRightControlsResponsive(Rect area)
        {
            float x = area.x;
            float y = area.y;
            float h = area.height;

            void Btn(string text, Action onClick, float minW = RightBtnMinW)
            {
                var content = new GUIContent(text);
                var size = GUI.skin.button.CalcSize(content);
                float w = Mathf.Max(minW, size.x + 12f);
                // 不够放就换行
                if (x + w > area.xMax)
                {
                    x = area.x;
                    y += h + 4f;
                }

                if (GUI.Button(new Rect(x, y, w, h), content, _btn)) onClick?.Invoke();
                x += w + Gap;
            }

            // Auto / PauseOnErr 两个 Toggle 紧凑点
            float togW = Mathf.Max(60f, GUI.skin.toggle.CalcSize(new GUIContent("Auto")).x + 10f);
            autoScroll = GUI.Toggle(new Rect(x, y, togW, h), autoScroll, "Auto", _toggle);
            x += togW + Gap;
            pauseOnError = GUI.Toggle(
                new Rect(x, y, Mathf.Max(110f, GUI.skin.toggle.CalcSize(new GUIContent("PauseOnErr")).x + 10f), h),
                pauseOnError, "PauseOnErr", _toggle);

            x += Gap;

            // Dock 切换 / Pause / Clear / Minimize
            Btn(dock == DockEdge.Top ? "Top ▾" : "Bottom ▴", () =>
            {
                dock = dock == DockEdge.Top ? DockEdge.Bottom : DockEdge.Top;
                UpdateRects();
            });
            Btn(_paused ? "Resume" : "Pause", () => _paused = !_paused);
            Btn("Clear", () =>
            {
                _entries.Clear();
                _infoCount = _warnCount = _errorCount = 0;
                _scroll = Vector2.zero;
            });
            Btn("Minimize", () => _state = ViewState.Bubble);
        }

// 渲染等分按钮（Rect 版本）：支持图标/颜色/徽标
        private bool DrawLevelToggleRect(Rect r, bool value, string labelText, string emoji, Texture2D icon, Color tint,
            Texture2D bgTex, int count)
        {
            // 背景（选中时加一层色）
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = value ? new Color(tint.r, tint.g, tint.b, 0.25f) : new Color(0, 0, 0, 0.12f);
            GUI.Box(r, GUIContent.none);
            GUI.backgroundColor = prevBg;

            // 内边距
            var pad = 6f;
            var iconRect = new Rect(r.x + pad, r.y + (r.height - iconSize.y) * 0.5f, iconSize.x, iconSize.y);
            var textRect = new Rect(iconRect.xMax + 6f, r.y, r.width - (iconSize.x + 6f + pad * 2f), r.height);

            // 图标
            switch (iconStyle)
            {
                case IconStyle.Texture:
                    if (icon != null)
                    {
                        var old = GUI.color;
                        GUI.color = tint;
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                        GUI.color = old;
                    }
                    else GUI.DrawTexture(iconRect, bgTex);

                    break;
                case IconStyle.Emoji:
                    var emojiStyle = new GUIStyle(GUI.skin.label)
                        { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(12, fontSize) };
                    GUI.Label(iconRect, emoji, emojiStyle);
                    break;
                default:
                    GUI.DrawTexture(iconRect, bgTex);
                    break;
            }

            // 文字
            var label = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft, fontSize = Mathf.Max(10, fontSize), richText = true,
                padding = new RectOffset(0, 6, 0, 0)
            };
            string colored = $"<b><color=#{ColorUtility.ToHtmlStringRGB(tint)}>{labelText}</color></b>";
            if (GUI.Button(textRect, colored, label)) value = !value;

            // 徽标
            if (showBadgeCount && count > 0)
            {
                var badge = new Rect(r.xMax - 18, r.y + 2, 16, 16);
                var old = GUI.color;
                GUI.color = tint;
                GUI.DrawTexture(badge, bgTex);
                GUI.color = Color.white;
                var mini = new GUIStyle(GUI.skin.label)
                    { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.white } };
                GUI.Label(badge, count > 99 ? "99+" : count.ToString(), mini);
                GUI.color = old;
            }

            // 整块可点击（扩大可点击区域）
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                value = !value;

            return value;
        }

// Header 背景（可替换为你喜欢的风格）
        private void EditorLikeBar(Rect r)
        {
            var c = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.25f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(1, 1, 1, 0.05f);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1, r.width, 1), Texture2D.whiteTexture);
            GUI.color = c;
        }
    
        private void DrawListArea()
        {
            // Header 已经占据了自己的高度；下面是列表区域
            // 用 FlexibleSpace 确保该区域把剩余高度全部吃掉，不会互相覆盖
            GUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            _scroll = GUILayout.BeginScrollView(_scroll, false, true, GUILayout.ExpandHeight(true));

            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (!PassFilter(e.Type)) continue;

                string tag, color;
                switch (e.Type)
                {
                    case LogType.Warning:   tag = "W"; color = "#E0A800"; break;
                    case LogType.Error:
                    case LogType.Assert:
                    case LogType.Exception: tag = "E"; color = "#E55353"; break;
                    default:                tag = "I"; color = "#7FC97F"; break;
                }
                string head = $"<b><color={color}>[{tag}]</color> [{e.Time:HH:mm:ss}]</b> ";
                string summary = e.Message ?? string.Empty;
                if (!e.Expanded && summary.Length > 200) summary = summary.Substring(0, 200) + " ...";

                var line = new GUIContent(head + summary);
                if (GUILayout.Button(line, _label, GUILayout.ExpandWidth(true)))
                {
                    e.Expanded = !e.Expanded;
                    _entries[i] = e;
                }

                if (e.Expanded && !string.IsNullOrEmpty(e.Stack))
                {
                    GUILayout.Space(2);
                    GUILayout.Label("<color=#AAAAAA><b>StackTrace:</b></color>\n" + e.Stack, _label);
                }

                DrawSeparator( e.Expanded ? 6 : 2 );
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        // ==================== 事件/数据 ====================
        private void HandleLog(string condition, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                Message = condition ?? string.Empty,
                Stack = stackTrace ?? string.Empty,
                Type = type,
                Time = DateTime.Now,
                Expanded = false
            };
            _entries.Add(entry);
            if (_entries.Count > maxEntries)
                _entries.RemoveRange(0, _entries.Count - maxEntries);

            switch (type)
            {
                case LogType.Warning: _warnCount++; break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception: _errorCount++; break;
                default: _infoCount++; break;
            }

            if (autoScroll && !_paused) _scroll.y = float.MaxValue;
            if (pauseOnError && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
                _paused = true;
        }

        private bool PassFilter(LogType t)
        {
            if (t == LogType.Warning && !showWarning) return false;
            if ((t == LogType.Error || t == LogType.Assert || t == LogType.Exception) && !showError) return false;
            if (t == LogType.Log && !showInfo) return false;
            return true;
        }

        // ==================== 布局/样式 ====================

        private void UpdateRects()
        {
            _safe = GetSafeAreaPixels();

            float panelH = Mathf.Clamp01(heightPercent) * _safe.height;
            float x = _safe.xMin;
            float w = _safe.width;
            float y = dock == DockEdge.Bottom ? (_safe.yMin) : (_safe.yMax - panelH);
            _panelRect = new Rect(x, y, w, panelH);

            // 已改成“气泡”样式，不再使用折叠标签 _tabRect
        }

        private Rect GetSafeAreaPixels()
        {
            var sa = Screen.safeArea;
            if (sa.width <= 0 || sa.height <= 0) return new Rect(0, 0, Screen.width, Screen.height);
            return sa;
        }

        private void BuildStyles()
        {
            _label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(10, fontSize), wordWrap = true, richText = true };
            _btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.Max(10, fontSize) };
            _toggle = new GUIStyle(GUI.skin.toggle) { fontSize = Mathf.Max(10, fontSize) };
        }

        private void DrawSeparator(int pad = 4)
        {
            GUILayout.Space(pad);
            var c = GUI.color;
            GUI.color = new Color(1, 1, 1, 0.15f);
            GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(1));
            GUI.color = c;
            GUILayout.Space(pad);
        }

        private void EnsureIconTextures()
        {
            if (_infoTex == null) _infoTex = MakeTex(8, 8, infoColor);
            if (_warnTex == null) _warnTex = MakeTex(8, 8, warnColor);
            if (_errorTex == null) _errorTex = MakeTex(8, 8, errorColor);
            if (_bubbleTex == null) _bubbleTex = MakeTex(8, 8, bubbleColor);
            if (_accentTex == null) _accentTex = MakeTex(8, 8, bubbleAccent);
        }

        private Texture2D MakeTex(int w, int h, Color c)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var arr = new Color[w * h];
            for (int i = 0; i < arr.Length; i++) arr[i] = c;
            tex.SetPixels(arr);
            tex.Apply(false, true);
            return tex;
        }

        // === 图标化 Toggle ===
        private bool DrawLevelToggle(bool value, string labelText, string emoji, Texture2D icon, Color tint,
            Texture2D bgTex, float width, int count)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Max(10, fontSize),
                padding = new RectOffset(6, 6, 4, 4)
            };

            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = value ? new Color(tint.r, tint.g, tint.b, 0.28f) : new Color(0, 0, 0, 0.15f);

            GUILayout.BeginHorizontal(GUILayout.Width(width), GUILayout.Height(Mathf.Max(iconSize.y + 6, 26)));

            // 左侧图标
            Rect iconRect = GUILayoutUtility.GetRect(iconSize.x, iconSize.y, GUILayout.ExpandWidth(false));
            iconRect.y += (GUILayoutUtility.GetLastRect().height - iconSize.y) * 0.5f;

            switch (iconStyle)
            {
                case IconStyle.Texture:
                    if (icon != null)
                    {
                        var old = GUI.color;
                        GUI.color = tint;
                        GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                        GUI.color = old;
                    }
                    else GUI.DrawTexture(iconRect, bgTex);

                    break;
                case IconStyle.Emoji:
                    var emojiStyle = new GUIStyle(GUI.skin.label)
                        { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.Max(12, fontSize) };
                    GUI.Label(iconRect, emoji, emojiStyle);
                    break;
                case IconStyle.Text:
                    GUI.DrawTexture(iconRect, bgTex);
                    break;
            }

            GUILayout.Space(6);

            // 文字（带颜色）
            var label = new GUIStyle(GUI.skin.label)
                { fontSize = Mathf.Max(10, fontSize), richText = true, alignment = TextAnchor.MiddleLeft };
            string colored = $"<b><color=#{ColorUtility.ToHtmlStringRGB(tint)}>{labelText}</color></b>";
            if (GUILayout.Button(colored, style))
                value = !value;

            // 徽标
            if (showBadgeCount && count > 0)
            {
                var last = GUILayoutUtility.GetLastRect();
                var badge = new Rect(last.xMax - 18, last.yMin + 2, 16, 16);
                var old = GUI.color;
                GUI.color = tint;
                GUI.DrawTexture(badge, bgTex);
                GUI.color = Color.white;
                var mini = new GUIStyle(GUI.skin.label)
                    { alignment = TextAnchor.MiddleCenter, fontSize = 10, normal = { textColor = Color.white } };
                GUI.Label(badge, count > 99 ? "99+" : count.ToString(), mini);
                GUI.color = old;
            }

            GUILayout.EndHorizontal();
            GUI.backgroundColor = prevBg;
            return value;
        }
    }
}