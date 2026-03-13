#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

using Lsj.Util.Win32;
using Lsj.Util.Win32.BaseTypes;
using Lsj.Util.Win32.Enums;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

#endif

public static class WindowsUtil
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    #region Windows API

    /// <summary>
    /// 标题栏
    /// </summary>
    const int WS_CAPTION = 0x00c00000;

    /// <summary>
    /// 获取指定窗口所属类的名称
    /// </summary>
    [DllImport("user32.dll")]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    #endregion

    // 当前Unity程序的窗口句柄
    private static IntPtr s_hWnd = IntPtr.Zero;

    // Unity程序的窗口类名
    private const string UNITY_WND_CLASSNAME = "UnityWndClass";

    // 默认宽高比
    private const float WindowAspectRatio = 16f / 9f;

    public static bool IsInitialized = false;

    /// <summary>
    /// 初始化获取程序的窗口句柄，应该在程序窗口激活时调用
    /// </summary>
    public static void Init(Action onSuccess)
    {
        if (IsInitialized)
        {
            return;
        }

        // 获取当前激活的窗口句柄
        var hWnd = User32.GetActiveWindow();

        // 检查一次窗口的 ClassName 是不是unity程序
        var classText = new StringBuilder(UNITY_WND_CLASSNAME.Length + 1);

        GetClassName(hWnd, classText, classText.Capacity);
        //DebugLog.LogError("classText: " + classText);

        if (classText.ToString() == UNITY_WND_CLASSNAME)
        {
            s_hWnd = hWnd;

            IsInitialized = true;

            onSuccess.Invoke();
        }
    }

    /// <summary>
    /// 隐藏标题栏
    /// </summary>
    public static void HideTitleBar()
    {
        if (IsInitialized)
        {
            int wl = GetWindowStyleInt32(s_hWnd);
            wl &= ~WS_CAPTION;
            User32.SetWindowLong(s_hWnd, GetWindowLongIndexes.GWL_STYLE, wl);
        }
    }

    /// <summary>
    /// 显示标题栏
    /// </summary>
    public static void ShowTitleBar()
    {
        if (IsInitialized)
        {
            int wl = GetWindowStyleInt32(s_hWnd);
            wl |= WS_CAPTION;
            User32.SetWindowLong(s_hWnd, GetWindowLongIndexes.GWL_STYLE, wl);
        }
    }

    /// <summary>
    /// 拖动窗口
    /// </summary>
    public static void DragWindow()
    {
        if (IsInitialized)
        {
            User32.ReleaseCapture();
            User32.SendMessage(s_hWnd, WindowMessages.WM_NCLBUTTONDOWN, 0x02, 0);
            User32.SendMessage(s_hWnd, WindowMessages.WM_LBUTTONUP, 0, 0);
        }
    }

    /// <summary>
    /// 最小化窗口
    /// </summary>
    public static void MinimizeWindow()
    {
        if (IsInitialized)
        {
            User32.ShowWindowAsync(s_hWnd, ShowWindowCommands.SW_MINIMIZE);
        }
    }

    /// <summary>
    /// 最大化窗口（切换为全屏模式）
    /// </summary>
    public static void MaximizeWindow()
    {
        Vector2 size = GetFullScreenSizeByAspectRatio(WindowAspectRatio);
        Screen.SetResolution((int)size.x, (int)size.y, true);
    }

    /// <summary>
    /// （异步）还原到窗口尺寸
    /// </summary>
    public static IEnumerator RestoreWindowAsync()
    {
        Vector2 size = GetFullScreenSizeByAspectRatio(WindowAspectRatio);
        Vector2 restoreSize = size * 0.8f;
        Screen.SetResolution((int)restoreSize.x, (int)restoreSize.y, false);

        yield return null;

        // 改变屏幕尺寸后，需要等待1帧，再隐藏标题栏才能生效
        HideTitleBar();

        yield return null;

        // 隐藏标题栏之后，等待1帧，获取新的窗口位置信息并刷新一次，才能让UI元素根据新的尺寸正确显示
        Lsj.Util.Win32.Structs.RECT rect;
        User32.GetWindowRect(s_hWnd, out rect);
        User32.SetWindowPos(s_hWnd, IntPtr.Zero, rect.left, rect.top, (int)restoreSize.x, (int)restoreSize.y, SetWindowPosFlags.SWP_FRAMECHANGED);
    }

    static Vector2 GetFullScreenSizeByAspectRatio(float aspectRatio)
    {
        int screenMaxWidth = Screen.currentResolution.width;
        int screenMaxHeight = Screen.currentResolution.height;

        // 先计算出APP全屏时的宽度（如果超出了屏幕宽度，就替换为屏幕宽度）
        int fullScreenWidth = (int)Mathf.Min(screenMaxHeight * aspectRatio, screenMaxWidth);

        // 再根据全屏时的宽度，计算全屏时的高度
        int fullScreenHeight = Mathf.RoundToInt(fullScreenWidth / aspectRatio);

        return new Vector2(fullScreenWidth, fullScreenHeight);
    }

    static int GetWindowStyleInt32(IntPtr hWnd)
    {
        LONG_PTR wlp = User32.GetWindowLong(hWnd, GetWindowLongIndexes.GWL_STYLE);
        return ((IntPtr)wlp).ToInt32();
    }
#endif

}
