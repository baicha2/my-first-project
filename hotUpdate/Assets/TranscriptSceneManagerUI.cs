using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TranscriptSceneManagerUI : MonoBehaviour
{
    //副本管理UI

    [Header("按钮配置")]
    //public Button backButton;           //返回按钮
    public Button TranscriptButton1;            //副本1
    public Button TranscriptButton2;            //副本2
    public Button TranscriptButton3;            //副本3
    public Button TranscriptButton4;            //副本4
    public Button TranscriptButton5;            //副本5
    public Button TranscriptButton6;            //副本6

    [Header("场景名称")]
    //public string backScene = "Game";
    public string TranscriptScene1 = "TranscriptScene1";
    public string TranscriptScene2 = "TranscriptScene2";
    public string TranscriptScene3 = "TranscriptScene3";
    public string TranscriptScene4 = "TranscriptScene4";
    public string TranscriptScene5 = "TranscriptScene5";
    public string TranscriptScene6 = "TranscriptScene6";





    void Start()
    {
        // 绑定按钮事件

        if (TranscriptButton1 != null)
            TranscriptButton1.onClick.AddListener(() => ChangeScene(TranscriptScene1));

        if (TranscriptButton2 != null)
            TranscriptButton2.onClick.AddListener(() => ChangeScene(TranscriptScene2));

        if (TranscriptButton3 != null)
            TranscriptButton3.onClick.AddListener(() => ChangeScene(TranscriptScene3));

        if (TranscriptButton4 != null)
            TranscriptButton4.onClick.AddListener(() => ChangeScene(TranscriptScene4));

        if (TranscriptButton5 != null)
            TranscriptButton5.onClick.AddListener(() => ChangeScene(TranscriptScene5));

        if (TranscriptButton6 != null)
            TranscriptButton6.onClick.AddListener(() => ChangeScene(TranscriptScene6));

    }

    
    void Update()
    {
        
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
