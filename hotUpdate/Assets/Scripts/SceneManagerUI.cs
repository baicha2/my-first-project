using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneManagerUI : MonoBehaviour
{
    [Header("按钮配置")]
    //public Button backButton;           //返回按钮
    public Button transcriptButton;     //副本按钮
    public Button settingsButton;       //设置按钮
    public Button startButton;          //开始游戏按钮
    public Button shopButton;           //商店按钮
    public Button hereButton;           //英雄选择按钮
    public Button taskButton;           //任务列表

    [Header("场景名称")]
    //public string backScene = "Game";
    public string transcriptScene = "TranscriptScene";
    public string settingsScene = "Shop";
    public string startScene = "MainMenu";
    public string shopScene = "MainMenu";
    public string hereScene = "MainMenu";
    public string taskScene = "MainMenu";

    [Header("设置面板")]
    public GameObject SettingsPanel;           //设置面板



    void Start()
    {
        // 绑定按钮事件
        //if (backButton != null)
        //    startButton.onClick.AddListener(() => ChangeScene(backScene));

        if (transcriptButton != null)
            transcriptButton.onClick.AddListener(() => ChangeScene(transcriptScene));

        //打开设置面板
        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => OpenTheSettingsPanel());


        if (startButton != null)
            startButton.onClick.AddListener(() => ChangeScene(startScene));

        if (shopButton != null)
            shopButton.onClick.AddListener(() => ChangeScene(shopScene));

        if (hereButton != null)
            hereButton.onClick.AddListener(() => ChangeScene(hereScene));

        if (taskButton != null)
            taskButton.onClick.AddListener(() => ChangeScene(taskScene));
    }

    public void OpenTheSettingsPanel() 
    {
       
       SettingsPanel.SetActive(true);      // 激活指定的对象
    }



    public void ChangeScene(string sceneName)
    {
        StartCoroutine(LoadScene(sceneName));
    }

    // 加载场景
    private IEnumerator LoadScene(string sceneName)
    {
        Debug.Log("开始加载场景");

        var handle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single, false); // 异步加载场景
        Debug.Log("开始异步加载场景");
        float curProgress = 0f;
        while (handle is { IsDone: false })
        {
            // 资源加载内部可能是分阶段进行的，会出现 PercentComplete 回退，这里限制只允许更大值
            // Debug.Log($"场景资源加载进度: {handle.PercentComplete:P2}");
            curProgress = Mathf.Max(curProgress, handle.PercentComplete);
            yield return null;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("场景资源加载失败");
            AddressableFail(handle, "场景资源加载失败");
            yield break;
        }

        var sceneHandle = handle.Result.ActivateAsync();
        yield return sceneHandle;
        Addressables.Release(sceneHandle);
        Addressables.Release(handle);
        Debug.Log("场景加载完成");
    }

    private void AddressableFail(AsyncOperationHandle handle, string error)
    {
        Debug.LogError(error);
        Debug.LogError("AddressableFail Error\n" + handle.OperationException);
        Addressables.Release(handle);
    }





}