using UnityEditor;
using UnityEngine;

public static class GMAuxiliaryBar
{
    private static string moduleTitle = "辅助栏";

    // 自定义背景板样式（可根据需要调整内外边距）
    private static GUIStyle panelStyle;

    static GMAuxiliaryBar()
    {
        panelStyle = new GUIStyle("box")
        {
            margin = new RectOffset(2, 2, 2, 2),
            padding = new RectOffset(4, 4, 2, 2)
        };
    }

    /// <summary>
    /// 绘制辅助栏模块，包括模块标题和各功能控件背景板的排列。
    /// </summary>
    public static void Draw()
    {
        // 使用垂直布局，将模块标题放在上方
        EditorGUILayout.BeginVertical(panelStyle);
        // 模块标题
        GUILayout.Label(moduleTitle, EditorStyles.boldLabel);

        // 功能控件采用水平布局排列
        EditorGUILayout.BeginHorizontal();
        
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 绘制一个按钮背景板，其宽度根据按钮文本自动调整
    /// </summary>
    /// <param name="buttonText">按钮显示文本</param>
    private static void DrawButtonBoard(string buttonText)
    {
        // 计算按钮文本尺寸，并增加适当的内边距
        Vector2 textSize = EditorStyles.miniButton.CalcSize(new GUIContent(buttonText));
        float boardWidth = textSize.x + 20; // 20像素内边距

        EditorGUILayout.BeginHorizontal(panelStyle, GUILayout.Width(boardWidth), GUILayout.Height(30));
        if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
        {
            Debug.Log("点击了 " + buttonText);
        }
        EditorGUILayout.EndHorizontal();
    }
}
