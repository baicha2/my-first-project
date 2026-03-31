using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class MainScene : MonoBehaviour
{
    public void ChangeScene()
    {
        StartCoroutine(LoadScene());
    }

    // 加载场景
    private IEnumerator LoadScene()
    {
        Debug.Log("开始加载场景");

        var handle = Addressables.LoadSceneAsync("Assets/TestScene.unity", LoadSceneMode.Single, false); // 异步加载场景
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