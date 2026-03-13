using System;
using UnityEngine;
using UnityEngine.UI;

public class LaunchUI : LaunchUIBase
{
    [SerializeField] Text versionText;

    [SerializeField] LoadingProgress _loadingProgress;

    [SerializeField] SimpleDialog _simpleDialog;

    [SerializeField] SimpleDialog _linkTextDialog;

    [SerializeField] SimpleDialog _privacyDialog;

    float _totalProgress;

    private void Awake()
    {
        _simpleDialog.Hide();
    }

    private void OnApplicationFocus(bool focus)
    {
        Debug.Log("OnApplicationFocus: " + focus);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        // Windows 客户端进入前台时，初始化 Windows 工具类
        if (!WindowsUtil.IsInitialized && focus)
        {
            WindowsUtil.Init(() =>
            {
                Debug.Log("WindowsUtil.Init 完成");
                if (!AppConfig.IsWindowsFullScreen)
                {
                    // 窗口化状态启动时，要隐藏系统默认菜单栏
                    StartCoroutine(WindowsUtil.RestoreWindowAsync());
                }
            });
        }
#endif
    }

    /// <summary>
    /// 异常提示，退出或者重试
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="retry"></param>
    public override void ErrorTips(string msg, Action retry)
    {
        _simpleDialog.Show(msg, "退出", "重试", Launch.QuitApplication, retry);
    }

    /// <summary>
    /// 设置版本号信息
    /// </summary>
    public override void SetVersionInfo(string version)
    {
        versionText.text = version;
    }

    /// <summary>
    /// 首次运行
    /// </summary>
    public override void OnFirstOpen()
    {
        _linkTextDialog.Show(
            title: "服务协议和隐私政策",
            content: "　　本应用尊重并保护所有用户的个人隐私权，本应用会按照隐私政策的规定使用和披露您的个人信息。\r\n可阅读<color=green><link=服务协议>《服务协议》</link></color>和<color=green><link=隐私政策>《隐私政策》</link></color>。",
            leftBtnText: "不同意",
            rightBtnText: "同 意",
            leftAction: Launch.QuitApplication,
            rightAction: () =>
            {
                // 同意之后，修改状态
                AppConfig.IsFirstOpen = false;
            }
        );
        _linkTextDialog.SetLinkClickCallback((linkName) =>
        {
            // 处理点击的文字链接
            if (linkName == "隐私政策")
            {
                _privacyDialog.Show("隐私政策", AppConfig.PrivatePolicy);
                _privacyDialog.HideLeftBtn();
            }
            else if (linkName == "服务协议")
            {
                _privacyDialog.Show("服务协议", AppConfig.ServiceAgreement);
                _privacyDialog.HideLeftBtn();
            }
        });

        // 记录长度
        AppConfig.PrivatePolicyLength = AppConfig.PrivatePolicy.Length;
        AppConfig.ServiceAgreementLength = AppConfig.ServiceAgreement.Length;
    }


    /// <summary>
    /// 用户按下了Esc键
    /// </summary>
    public override void OnEscBtnDown()
    {
        _simpleDialog.Show("确定要退出程序吗？", Launch.QuitApplication);
    }


    #region 热更新过程

    private const float BytesToMB = 1048576f;
    private const float BytesToKB = 1024f;
    private float downloadSpeed;
    private float downloadedBytes;
    private float lastDownloadedTime;

    /// <summary>
    /// 检测更新
    /// </summary>
    public override void OnCheckUpdate()
    {
        downloadSpeed = 0f;
        downloadedBytes = 0f;
        lastDownloadedTime = Time.time;
        _loadingProgress.RefreshHint("正在准备下载内容");
    }

    /// <summary>
    /// 刷新热更新进度
    /// </summary>
    /// <param name="curBytes">已下载多少 MB</param>
    /// <param name="totalBytes">需要下载多少 MB</param>
    /// <param name="percent">总进度（范围：0.0 ~ 1.0）</param>
    public override void RefreshPrecentProgress(float percent, float curBytes = 0f, float totalBytes = 0f)
    {
        _loadingProgress.RefreshProgress(percent);

        if (!(totalBytes > 0)) return;
        string hint = $"系统内容更新，请耐心等待！\n努力下载更新中:{curBytes / BytesToMB:F2}MB / {totalBytes / BytesToMB:F2}MB";

        // 统计一秒内的下载速度
        var time = Time.time - lastDownloadedTime;
        // 如果累计下载量为0，需要计算下载速度，否则每一秒更新下载速度
        if (downloadedBytes == 0 || time > 1f)
        {
            // 计算一秒内下载量
            // 资源下载失败可能会发起重试，导致传入的已下载字节数比上一次更小，所以这里取最大值
            var curLoaded = Mathf.Max(0f, curBytes - downloadedBytes);
            downloadSpeed = curLoaded / time;

            // 如果时间间隔大于1秒，则更新累计下载量
            if (time > 1f)
            {
                lastDownloadedTime = Time.time;
                downloadedBytes = curBytes;
            }
        }

        var speed = downloadSpeed / BytesToKB;
        string speedUnit = "KB/秒";
        if (speed >= BytesToKB)
        {
            speed /= BytesToKB;
            speedUnit = "MB/秒";
        }

        var speedHint = $"  {speed:F2} {speedUnit}";
        Debug.Log(speedHint);
        hint += speedHint;

        _loadingProgress.RefreshHint(hint);
    }

    /// <summary>
    /// 热更新已完成
    /// </summary>
    public override void OnHotUpdateFinish()
    {
        _loadingProgress.RefreshHint("下载完成");
    }

    #endregion

    #region 加载资源

    /// <summary>
    /// 加载资源
    /// </summary>
    public override void OnLoadingResource()
    {
        _loadingProgress.RefreshHint("正在加载配置...");
    }

    #endregion


    #region 加载场景

    /// <summary>
    /// 开始加载主场景的AB包
    /// </summary>
    public override void OnBeginLoadSceneBundle()
    {
        _loadingProgress.RefreshHint("正在加载资源...");
    }

    /// <summary>
    /// 开始加载场景
    /// </summary>
    public override void OnBeginLoadScene()
    {
        _loadingProgress.RefreshHint("正在加载场景...");
    }

    #endregion
}