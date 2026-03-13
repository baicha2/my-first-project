using System;
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using Debug = UnityEngine.Debug;

[InitializeOnLoad]
public class AssetBundleAutoBuilder
{
    private static readonly string triggerFilePath = Path.Combine(Application.dataPath, "build_assetbundles.trigger");
    private static bool isProcessing = false;

    static AssetBundleAutoBuilder()
    {
        EditorApplication.update += CheckTrigger;
        Debug.Log("AssetBundleAutoBuilder 初始化完成！");
    }

    private static void CheckTrigger()
    {
        if (File.Exists(triggerFilePath) && !isProcessing)
        {
            File.Delete(triggerFilePath);
            TriggerBuildHotUpdate();
        }
    }

    private static void TriggerBuildHotUpdate()
    {
        if (isProcessing) return;
        isProcessing = true;

        try
        {
            Debug.Log("开始强制重新编译脚本...");
            // 订阅编译完成事件
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            // 触发重新编译
            CompilationPipeline.RequestScriptCompilation();
        }
        catch (Exception e)
        {
            isProcessing = false;
            Debug.LogError($"触发编译失败：{e.Message}\n{e.StackTrace}");
            CreateResultFile("生成热更新资源包失败");
        }
    }

    private static void OnCompilationFinished(object obj)
    {
        // 取消订阅事件，避免重复调用
        CompilationPipeline.compilationFinished -= OnCompilationFinished;
        Debug.Log("脚本编译完成，开始生成AssetBundle...");
        BeginBuildRes();
        isProcessing = false;
    }

    private static void BeginBuildRes()
    {
        Debug.Log("开始生成热更新...");
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            PackAssetTools.OnContentBuildCompleted.AddListener(OnContentBuildCompleted);
            PackAssetTools.GenerateFullResourcePack(false, false);
        }
        catch (Exception e)
        {
            Debug.LogError($"生成热更新失败：{e.Message}\n{e.StackTrace}");
            CreateResultFile("生成热更新资源包-失败");
        }
    }

    private static void OnContentBuildCompleted(bool success, string resVer)
    {
        if (success)
        {
            Debug.Log("生成热更新资源包成功！");
            CreateResultFile($"生成热更新资源包-成功！资源版本：{resVer}");
        }
        else
        {
            Debug.LogError("生成热更新资源包失败！");
            CreateResultFile("生成热更新资源包-失败");
        }
    }

    private static void CreateResultFile(string arguments)
    {
        string resultPath = Path.Combine(PathTools.RepoDataDir, "Generate_resource_pack_result.txt");
        File.WriteAllText(resultPath, arguments);
    }
}