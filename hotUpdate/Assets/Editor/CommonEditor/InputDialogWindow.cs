using UnityEditor;
using UnityEngine;

public class InputDialogWindow : EditorWindow
{
    private string inputText = ""; // 存储用户输入的文本
    private string windowTitle = "输入框示例"; // 窗口标题
    private string promptText = "请输入内容："; // 提示文本

    // 回调函数：用户点击确定后执行的逻辑
    private System.Action<string> onConfirm;


    /// <summary>
    /// 显示输入窗口
    /// </summary>
    /// <param name="title">窗口标题</param>
    /// <param name="prompt">提示文本</param>
    /// <param name="onConfirmCallback">确定按钮的回调（参数为输入的文本）</param>
    public static void ShowWindow(string title, string prompt, System.Action<string> onConfirmCallback)
    {
        // 创建并显示窗口
        InputDialogWindow window = GetWindow<InputDialogWindow>(true, title);
        window.windowTitle = title;
        window.promptText = prompt;
        window.onConfirm = onConfirmCallback;
        window.minSize = new Vector2(300, 120); // 窗口最小尺寸
        window.Show();
    }


    // 绘制窗口UI
    private void OnGUI()
    {
        // 绘制提示文本
        EditorGUILayout.LabelField(promptText, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 绘制输入框（绑定到inputText变量）
        inputText = EditorGUILayout.TextField("输入内容：", inputText);
        EditorGUILayout.Space(20);

        // 绘制按钮（水平布局）
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace(); // 自动填充空白，让按钮靠右

        // 确定按钮
        if (GUILayout.Button("确定", GUILayout.Width(80)))
        {
            onConfirm?.Invoke(inputText); // 执行确定回调
            Close(); // 关闭窗口
        }

        // 取消按钮
        if (GUILayout.Button("取消", GUILayout.Width(80)))
        {
            Close(); // 直接关闭窗口，不执行逻辑
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }
}
