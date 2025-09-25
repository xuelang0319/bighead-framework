#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Bighead.BuildSystem.Editor
{
    /// <summary>
    /// 顶部标题 + 版本号显示模块
    /// </summary>
    public class HeaderSection
    {
        public void Draw()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                // 左侧标题
                EditorGUILayout.LabelField("Bighead / Build System", EditorStyles.boldLabel);

                GUILayout.FlexibleSpace();

                // 右侧版本信息（整块可点击）
                var rect = EditorGUILayout.BeginHorizontal(EditorStyles.label, GUILayout.MinWidth(150));
                bool isHover = rect.Contains(Event.current.mousePosition);

                if (isHover)
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

                if (Event.current.type == EventType.MouseDown && isHover)
                {
                    SettingsService.OpenProjectSettings("Project/Player");
                    Event.current.Use();
                }

                var style = new GUIStyle(EditorStyles.label);
                if (isHover)
                    style.normal.textColor = new Color(0.25f, 0.55f, 1f); // 悬停高亮

                EditorGUILayout.LabelField("App Version", style, GUILayout.Width(80));
                EditorGUILayout.LabelField(PlayerSettings.bundleVersion ?? "-", style, GUILayout.ExpandWidth(true));

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
#endif