using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HybridCLR;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

[Serializable]
public class DllJson
{
    // 仅用于注释
    public string comment;

    public List<string> hotfixDllNames; // 热更新DLL名称列表

    public List<string> aotDllNames; // AOT元数据DLL名称列表
}

public class Launch : MonoBehaviour
{
    [SerializeField] private LaunchUIBase launchUI; // 启动界面UI控制器
    [SerializeField] private GameObject reporter; // 调试报告器对象

    const float downloadPercent = 0.4f;
    const float loadDllPercent = 0.2f;
    const float loadSceneAbPercent = 0.38f;

    // 退出应用程序静态方法
    public static void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 启动主流程
    private IEnumerator Start()
    {
        Debug.Log("开始启动流程");
        bool isSuccess = true;
        // 加载配置文件
        yield return AppConfig.LoadConfig(failedCallback: msg =>
        {
            isSuccess = false;
            launchUI.ErrorTips(msg, retry: () => { StartCoroutine(Start()); });
        });

        if (!isSuccess)
        {
            Debug.LogError("配置文件加载失败，终止启动流程");
            yield break;
        }

        Debug.Log("配置文件加载成功");

        reporter.SetActive(AppConfig.IsDebug); // 根据调试模式启用报告器

        launchUI.SetVersionInfo(AppConfig.GameVersionText); // 设置版本信息

        Debug.Log($"设置版本信息: {AppConfig.GameVersionText}");

        if (AppConfig.IsFirstOpen)
        {
            // 当前版本处于首次运行状态，必须先展示隐私政策
            Debug.Log("首次打开应用，显示隐私政策");
            launchUI.OnFirstOpen();

            yield return new WaitUntil(() => !AppConfig.IsFirstOpen);
            Debug.Log("用户已确认隐私政策");
        }

        if (AppConfig.IsUnderReview && (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.IPhonePlayer))
        {
            Debug.Log("审核模式，跳过热更新");
            launchUI.OnHotUpdateFinish();
            StartCoroutine(LoadDllConfig()); // 加载DLL配置
            yield break;
        }

        Debug.Log("开始检查更新流程");
        StartCoroutine(CheckUpdate()); // 开始检查更新流程
    }

    // 检查更新流程
    private IEnumerator CheckUpdate()
    {
        launchUI.OnCheckUpdate();

        Debug.Log("初始化 Addressables");
        var initHandle = Addressables.InitializeAsync();
        yield return initHandle;

        // 此时可以安全加载新资源
        StartCoroutine(DownloadMainContent());
    }

    // 下载主要内容
    private IEnumerator DownloadMainContent()
    {
        Debug.Log("开始获取主要资源下载大小");
        // 获取 Main label 的下载大小
        var downloadSizeHandle = Addressables.GetDownloadSizeAsync("Main");
        yield return downloadSizeHandle;
        if (downloadSizeHandle.Status != AsyncOperationStatus.Succeeded)
        {
            launchUI.ErrorTips("下载资源失败", retry: () => { StartCoroutine(DownloadMainContent()); });
            AddressableFail(downloadSizeHandle, "获取下载大小失败");
            yield break;
        }

        // 需要更新
        if (downloadSizeHandle.Result > 0)
        {
            Debug.Log($"需要下载 {downloadSizeHandle.Result} 字节的数据");

            // 开始下载 Main label 的所有依赖（包括间接依赖）
            var downloadHandle = Addressables.DownloadDependenciesAsync("Main");
            Debug.Log("开始下载依赖资源");

            while (!downloadHandle.IsDone)
            {
                var downloadStatus = downloadHandle.GetDownloadStatus();
                launchUI.RefreshPrecentProgress(downloadStatus.Percent * downloadPercent, downloadStatus.DownloadedBytes, downloadStatus.TotalBytes);
                // Debug.Log($"downloadHandle 进度: {downloadHandle.PercentComplete}, 下载进度: {downloadStatus.Percent * 100:F1}%  {downloadStatus.DownloadedBytes}/{downloadStatus.TotalBytes}");
                yield return null;
            }

            if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
            {
                launchUI.ErrorTips("下载资源失败", retry: () => { StartCoroutine(DownloadMainContent()); });
                AddressableFail(downloadHandle, "Main 资源下载失败");
                yield break;
            }

            Debug.Log("资源下载完成");
            // 更新本地资源版本号信息
            AppConfig.RefreshResVersion();

            // 释放 handle（重要！避免内存泄漏）
            Addressables.Release(downloadHandle);
        }
        else
        {
            Debug.Log("没有需要下载的资源");
        }

        Addressables.Release(downloadSizeHandle);

        if (!Application.isEditor)
        {
            Debug.Log($"清理不再被catalog引用的资源");
            yield return Addressables.CleanBundleCache();
        }

        launchUI.OnHotUpdateFinish();
        yield return null;

        Debug.Log("开始加载DLL配置");
        StartCoroutine(LoadDllConfig()); // 加载DLL配置
    }

    // 加载DLL配置文件
    private IEnumerator LoadDllConfig()
    {
        launchUI.OnLoadingResource();

        var dllHandle = Addressables.LoadAssetAsync<TextAsset>("Assets/Extend/Config/DllConfig.json");
        yield return dllHandle;

        if (dllHandle.Status != AsyncOperationStatus.Succeeded)
        {
            launchUI.ErrorTips("加载配置文件失败", retry: () => { StartCoroutine(LoadDllConfig()); });
            AddressableFail(dllHandle, "加载DllConfig配置文件失败");
            yield break;
        }

        try
        {
            TextAsset textAsset = dllHandle.Result;
            var config = JsonUtility.FromJson<DllJson>(textAsset.text);
            Debug.Log("DllJson加载并解析成功");
            StartCoroutine(LoadDll(config)); // 开始加载DLL
        }
        catch (Exception e)
        {
            launchUI.ErrorTips("解析配置文件失败", retry: () => { StartCoroutine(LoadDllConfig()); });
            Debug.LogError($"JSON解析失败: {e.Message}");
        }

        Addressables.Release(dllHandle);
    }

    // 加载DLL文件
    private IEnumerator LoadDll(DllJson config)
    {
        var totalCount = config.aotDllNames.Count + config.hotfixDllNames.Count;
        int curCount = 0;

        // 加载AOT元数据DLL
        foreach (string aotDllName in config.aotDllNames)
        {
            string fileName = aotDllName + ".bytes";
            var dllHandle = Addressables.LoadAssetAsync<TextAsset>("Assets/Extend/GameDll/" + fileName);
            yield return dllHandle;

            if (dllHandle.Status != AsyncOperationStatus.Succeeded)
            {
                launchUI.ErrorTips("加载配置资源失败", retry: () => { StartCoroutine(LoadDll(config)); });
                AddressableFail(dllHandle, $"获取DLL失败：{fileName}");
                yield break;
            }

            try
            {
                TextAsset dllData = dllHandle.Result;
                byte[] dllBytes = dllData.bytes;
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
                Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
            }
            catch (Exception e)
            {
                launchUI.ErrorTips("解析配置资源失败", retry: () => { StartCoroutine(LoadDll(config)); });
                AddressableFail(dllHandle, $"加载DLL失败：{fileName}");
                Debug.LogError($"加载DLL失败: {e.Message}\n{e.StackTrace}");
                yield break;
            }

            Addressables.Release(dllHandle);
            curCount++;
            launchUI.RefreshPrecentProgress(downloadPercent + (float)curCount / totalCount * loadDllPercent);
            yield return null;
        }

        // 加载热更新DLL
        foreach (var dllName in config.hotfixDllNames.Select(t => t + ".bytes"))
        {
            // 运行时动态加载DLL
            Debug.Log("动态加载DLL：   " + dllName);
            var dllHandle = Addressables.LoadAssetAsync<TextAsset>("Assets/Extend/GameDll/" + dllName);
            yield return dllHandle;
            if (dllHandle.Status != AsyncOperationStatus.Succeeded)
            {
                launchUI.ErrorTips("加载配置资源失败.", retry: () => { StartCoroutine(LoadDll(config)); });
                AddressableFail(dllHandle, $"获取DLL失败：{dllName}");
                yield break;
            }

            try
            {
                Assembly.Load(dllHandle.Result.bytes);
                Debug.Log("动态加载DLL成功：   " + dllName);
            }
            catch (Exception e)
            {
                launchUI.ErrorTips("解析配置资源失败.", retry: () => { StartCoroutine(LoadDll(config)); });
                AddressableFail(dllHandle, "动态加载DLL失败：" + dllName);
                Debug.LogError($"动态加载DLL失败: {e.Message}\n{e.StackTrace}");
                yield break;
            }

            Addressables.Release(dllHandle);

            curCount++;
            launchUI.RefreshPrecentProgress(downloadPercent + (float)curCount / totalCount * loadDllPercent);
            yield return null;
        }

        launchUI.RefreshPrecentProgress(downloadPercent + loadDllPercent);

        StartCoroutine(LoadMainScene()); // 加载场景
    }

    // 加载场景
    private IEnumerator LoadMainScene()
    {
        Debug.Log("开始加载场景");
        launchUI.OnBeginLoadSceneBundle(); // 开始加载场景Bundle

        var handle = Addressables.LoadSceneAsync("Assets/MainScene.unity", LoadSceneMode.Single, false); // 异步加载场景
        Debug.Log("开始异步加载场景");
        float curProgress = 0f;
        while (handle is { IsDone: false })
        {
            // 资源加载内部可能是分阶段进行的，会出现 PercentComplete 回退，这里限制只允许更大值
            // Debug.Log($"场景资源加载进度: {handle.PercentComplete:P2}");
            curProgress = Mathf.Max(curProgress, handle.PercentComplete);
            launchUI.RefreshPrecentProgress(downloadPercent + loadDllPercent + curProgress * loadSceneAbPercent);
            yield return null;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("场景资源加载失败");
            launchUI.ErrorTips("加载资源失败", retry: () => { StartCoroutine(LoadMainScene()); });
            AddressableFail(handle, "场景资源加载失败");
            yield break;
        }

        launchUI.RefreshPrecentProgress(downloadPercent + loadDllPercent + loadSceneAbPercent);

        launchUI.OnBeginLoadScene(); // 开始加载场景

        yield return new WaitForSeconds(0.7f);
        launchUI.RefreshPrecentProgress(1);
        yield return new WaitForSeconds(0.3f);

        var sceneHandle = handle.Result.ActivateAsync();
        yield return sceneHandle;
        Addressables.Release(sceneHandle);
        Addressables.Release(handle);
        Debug.Log("场景加载完成");
    }

    // 输入处理
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            launchUI.OnEscBtnDown(); // 处理ESC按键
        }
    }

    private void AddressableFail(AsyncOperationHandle handle, string error)
    {
        Debug.LogError(error);
        Debug.LogError("AddressableFail Error\n" + handle.OperationException);
        Addressables.Release(handle);
    }
}