using UnityEditor;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;

[InitializeOnLoad]
public class UnityPackageAutoExporter
{
    private const string TRIGGER_FILE = "build_unity_package.trigger";
    private static bool isProcessing = false; // 添加标志位防止重复处理

    static UnityPackageAutoExporter()
    {
        EditorApplication.update += CheckTrigger;
        Debug.Log("UnityPackageAutoExporter 初始化完成！");
    }

    private static void CheckTrigger()
    {
        string triggerPath = Path.Combine(Application.dataPath, TRIGGER_FILE);
        if (File.Exists(triggerPath))
        {
            File.Delete(triggerPath);
            BuildMulAgentWithHotUpdate();
        }
    }

    public static void BuildMulAgentWithHotUpdate()
    {
        // 如果正在处理中，直接返回
        if (isProcessing) return;
        try
        {
            isProcessing = true; // 标记为处理中
            MulAgent.BeginBuildForMulAgent(false);
            Debug.Log("成功");
            isProcessing = false; // 处理完成，重置标志位
            RunBat("打多代理包(已有热更新)成功");
        }
        catch (Exception e)
        {
            isProcessing = false; // 异常时重置标志位
            Debug.LogError($"失败：{e.Message}\n{e.StackTrace}");
            RunBat("打多代理包(已有热更新)失败");
        }
    }

    private static void RunBat(string arguments)
    {
        string resultPath = Path.Combine(PathTools.RepoDataDir, "unity_packaging_result.txt");
        File.WriteAllText(resultPath, arguments);
    }
}