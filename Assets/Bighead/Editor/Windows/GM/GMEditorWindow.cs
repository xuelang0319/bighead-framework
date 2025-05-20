using UnityEditor;
using UnityEngine;

public partial class GMEditorWindow : EditorWindow
{
    // 用于显示各区域的文本
    private string titleText = "游戏管理器";
    private string innerAreaText = "局内栏";
    private string outerAreaText = "局外栏";
    private string shortcutsText = "快捷召唤： Ctrl + ←";

    // 通过菜单或快捷键打开窗口（仅开启，不关闭）
    [MenuItem("Bighead/Game/Open GM Editor Window %LEFT")]
    public static void OpenWindow()
    {
        // GetWindow 会返回已有的窗口（并激活它），或者创建一个新的窗口
        GetWindow<GMEditorWindow>("GM Editor Window").Show();
    }

    private void OnGUI()
    {
        // 绘制标题栏
        DrawTitleBar();

        // 调用单独的辅助栏脚本绘制辅助栏
        GMAuxiliaryBar.Draw();

        // 中间区域：局内栏和局外栏横向排列
        EditorGUILayout.BeginHorizontal();
        DrawInnerArea();
        DrawOuterArea();
        EditorGUILayout.EndHorizontal();

        // 绘制底部快捷键提示栏
        DrawShortcutBar();
    }

    private void DrawTitleBar()
    {
        EditorGUILayout.BeginHorizontal("box", GUILayout.Height(30));
        GUILayout.FlexibleSpace();
        GUILayout.Label(titleText, EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawInnerArea()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.6f), GUILayout.ExpandHeight(true));
        GUILayout.Label(innerAreaText, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("这里放局内栏的详细内容...", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
    }

    private void DrawOuterArea()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width * 0.35f), GUILayout.ExpandHeight(true));
        GUILayout.Label(outerAreaText, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("这里放局外栏的详细内容...", EditorStyles.helpBox);
        EditorGUILayout.EndVertical();
    }

    private void DrawShortcutBar()
    {
        EditorGUILayout.BeginHorizontal("box", GUILayout.Height(20));
        GUILayout.Label(shortcutsText, EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }
}
