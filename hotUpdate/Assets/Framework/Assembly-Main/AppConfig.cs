using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AppConfigData
{
    public int Agent;
    public string UnderReviewVersion;
    public string UnderReviewVersionMacOS;
    public bool IsDebug;
    public string WebABUrl;
    public List<string> ApiUrlList;
    public string ServiceAgreement;
    public string PrivatePolicy;
}

public static class AppConfig
{
    private static string[] SvrConfigUrlArr =
    {
        "https://vrdown.bjzjxf.com",
    };

    #region AgentPackageInfo

    public const string AgentPackageInfoFileName = "AgentPackageInfo.json";
    private static AgentPackageInfo agentPackageInfo;
    public static int Agent => agentPackageInfo?.Agent ?? 1;
    public static string AppVersion => agentPackageInfo.AppVersion ?? "1.0.0";

    #endregion

    #region Version

    public const string SvrUpdatePathFileName = "hotUpdate.txt";
    public static string ResVersion = "0";
    public static string GameVersionText => $"App:{AppVersion} Res:{ResVersion}";
    public static string SvrResVersion = "0";

    #endregion

    #region AppConfigData

    private static AppConfigData appConfigData;

    public static bool IsDebug => appConfigData?.IsDebug ?? true;

    public static string WebABUrl => appConfigData?.WebABUrl ?? "";
    public static List<string> ApiUrlList => appConfigData?.ApiUrlList ?? new List<string>();

    // 下载热更新资源的服务器路径（目前也是 Addressables 配置文件中依赖的运行时字段）
    public static string TargetUpdateUrl;

    /// <summary>
    /// 是否处于审核中（用于苹果端发布新版本）。
    /// 默认为 false，当下载到配置文件的 UnderReviewVersion 字段与本地 AppVersion 相同时，改为 true
    /// </summary>
    // 标记当前版本是否正在审核中
    // 提交审核之前：把对应的代理配置文件中 "UnderReviewVersion" 字段值改为审核的App版本号，如："1.0.0"，
    // 审核通过后，把 "UnderReviewVersion" 字段值改回 ""
    public static bool IsUnderReview => (Application.platform is RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor
        ? appConfigData?.UnderReviewVersionMacOS
        : appConfigData?.UnderReviewVersion) == AppVersion;

    // 服务协议
    public static string ServiceAgreement => appConfigData?.ServiceAgreement ?? "";
    public static string PrivatePolicy => appConfigData?.PrivatePolicy ?? "";

    /// <summary>
    /// 本地缓存的隐私政策的内容长度
    /// </summary>
    public static int PrivatePolicyLength
    {
        get => PlayerPrefs.GetInt("PrivatePolicyLength", 0);
        set => PlayerPrefs.SetInt("PrivatePolicyLength", value);
    }

    /// <summary>
    /// 本地缓存的服务协议的内容长度
    /// </summary>
    public static int ServiceAgreementLength
    {
        get => PlayerPrefs.GetInt("ServiceAgreementLength", 0);
        set => PlayerPrefs.SetInt("ServiceAgreementLength", value);
    }

    #endregion

    #region PlayerPrefs

    // 首次运行的字段名，带上版本号，这样版本更新后，就是首次运行的状态
    public static bool IsFirstOpen
    {
        get => PlayerPrefs.GetInt($"IsFirstOpen_{AppVersion}", 1) > 0;
        set => PlayerPrefs.SetInt($"IsFirstOpen_{AppVersion}", value ? 1 : 0);
    }

    /// <summary>
    /// Windows客户端，是不是全屏状态
    /// </summary>
    /// <value></value>
    public static bool IsWindowsFullScreen
    {
        get => PlayerPrefs.GetInt("IsWindowsFullScreen", 1) == 1;
        set => PlayerPrefs.SetInt("IsWindowsFullScreen", value ? 1 : 0);
    }

    #endregion

    static int failedCount;

    public static bool isLoad;

    public static IEnumerator LoadConfig(Action<string> failedCallback)
    {
        if (isLoad) yield break;
        // 读取安装包内的信息文件
        string agentPackageInfoTxt = "";

        try
        {
            TextAsset textAsset = Resources.Load<TextAsset>("AgentPackageInfo");
            agentPackageInfoTxt = textAsset.text;
        }
        catch (Exception e)
        {
            Debug.Log("LoadTxt exception: " + e.Message);
        }

        Debug.Log("agentPackageInfo: " + agentPackageInfoTxt);
        if (string.IsNullOrEmpty(agentPackageInfoTxt))
        {
            Debug.LogError("AgentPackageInfo.json File Missed!");
            failedCallback("软件包异常，请联系客服");
            yield break;
        }

        // 获取安装包的代理编号
        agentPackageInfo = JsonConvert.DeserializeObject<AgentPackageInfo>(agentPackageInfoTxt);
        Debug.Log("=====> Agent: " + Agent);
        Debug.Log("=====> AppVersion: " + AppVersion);

        // 加载本地版本信息
        HotUpdateManager.Instance.LoadLocalVersion();
        Debug.Log("=====> ResVersion: " + ResVersion);

        // 下载安装包所属代理的配置信息，并使用最先返回的那一个
        failedCount = 0;
        foreach (var url in SvrConfigUrlArr)
        {
            try
            {
                var req = UnityWebRequest.Get($"{url}/hotUpdate{AppVersion}/AppConfig/{Agent}/AppConfig.json");
                req.SendWebRequest().completed += OnReqSvrConfigComplete;
            }
            catch (Exception e)
            {
                Debug.Log("=====> SvrConfigUrl Exception: " + e);
            }
        }

        yield return new WaitUntil(() => appConfigData != null || failedCount >= SvrConfigUrlArr.Length);

        if (appConfigData == null)
        {
            failedCallback("获取配置信息失败\n1、请检查设备网络连接或换个网络后重试\n2、请联系开发管理");
            yield break;
        }
        
        Debug.Log("IsUnderReview: " + IsUnderReview);
        Debug.Log("IsDebug: " + IsDebug);
        Debug.Log("WebABUrl: " + WebABUrl);
        
        string WebABPackagePlatformUrl = $"{WebABUrl}{AppVersion}/Package/{GetPlatformName()}";
        Debug.Log("WebABPackagePlatformUrl: " + WebABPackagePlatformUrl);

        // 下载 ===> 热更新资源路径文件 hotUpdate.txt
        var downloadUrl = $"{WebABPackagePlatformUrl}/{SvrUpdatePathFileName}";
        
        Debug.Log($"下载文件: {downloadUrl}");
        using var request = UnityWebRequest.Get(downloadUrl);
        yield return request.SendWebRequest();
        if (HotUpdateManager.IsWebRequestSuccess(request))
        {
            // hotUpdate.txt 下载成功
            var dirName = request.downloadHandler.text.Trim(); // 服务器当前热更新资源的目录名称，如："PC-202510241622"
            TargetUpdateUrl = $"{WebABPackagePlatformUrl}/{dirName}"; // 完整的下载路径
            SvrResVersion = dirName.Split('-')[1]; // 服务器资源版本号，如："202510241622"
            Debug.Log($"<color=#FF51FF>服务器资源版本号：{SvrResVersion}</color>");
        }
        else
        {
            Debug.Log($"下载 {downloadUrl} 失败");
            failedCallback("获取热更新资源失败\n1、请检查设备网络连接或换个网络后重试\n2、请联系开发管理");
            yield break;
        }

        Debug.Log("TargetUpdateUrl: " + TargetUpdateUrl);
        isLoad = true;
    }
    
    // 更新主场景资源成功后，写入本地缓存的文件，记录当前的资源版本号
    public static void RefreshResVersion()
    {
        ResVersion = SvrResVersion;
        HotUpdateManager.Instance.RefreshLocalResVersion(ResVersion);
    }
    
    private static IEnumerator DownloadMainTxt(string downloadUrl, Action<DownloadHandler> onDownloaded)
    {
        Debug.Log($"下载文件: {downloadUrl}");
        using var req = UnityWebRequest.Get(downloadUrl);
        yield return req.SendWebRequest();
        if (HotUpdateManager.IsWebRequestSuccess(req))
        {
            // 下载成功
            onDownloaded.Invoke(req.downloadHandler);
        }
        else
        {
            Debug.Log($"下载 {downloadUrl} 失败");
        }
    }
    /// <summary>
    /// 请求获取服务器配置文件的回调
    /// </summary>
    static void OnReqSvrConfigComplete(AsyncOperation asyncOperation)
    {
        if (appConfigData != null) return;
        var req = ((UnityWebRequestAsyncOperation)asyncOperation).webRequest;
        if (req == null)
        {
            failedCount++;
            return;
        }

        if (HotUpdateManager.IsWebRequestSuccess(req))
        {
            var json = req.downloadHandler.text;
            // 请求成功
            Debug.Log("=====> svrAppConfigStr: \n" + json);
            // 加载成功
            appConfigData = JsonConvert.DeserializeObject<AppConfigData>(json);
            if (Agent != appConfigData.Agent)
            {
                failedCount++;
                return;
            }
        }
        else
        {
            // 请求失败，增加失败计数
            failedCount++;
        }

        req.Abort();
        req.Dispose();
    }

    public static string GetPlatformName()
    {
        string platformName = "PC";
#if UNITY_IOS
        platformName = "IOS";
#elif UNITY_ANDROID
        platformName = "Android";
#elif UNITY_STANDALONE_OSX
        platformName = "Mac";
#endif
        return platformName;
    }
}