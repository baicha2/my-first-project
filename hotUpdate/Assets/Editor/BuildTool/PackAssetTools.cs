using System;
using System.Collections.Generic;
using System.IO;
using HybridCLR.Editor;
using HybridCLR.Editor.Commands;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public static class PackAssetTools
{
    public const string TargetAppVersion = "1.0.0";

    /// <summary> 热更新资源版本号 </summary>
    private static string s_TargetResVer;

    /// <summary> 生成热更新资源的输出路径 </summary>
    /// <remarks> 在 Addressables Profiles 窗口中，作为 Remote.BuildPath 的值被使用 </remarks>
    public static string RemoteResBuildPath =>
        Path.Combine(PathTools.HotUpdateOutputDir, GetPlatformDirName(), s_TargetResVer);

    public static string MainResBuildPath;
    public static string MainResLoadPath;

    /// <summary> 苹果（iOS、macOS）端的代理ID </summary>
    public const int AppleAgentID = 2;

    /// <summary> 内部测试的代理ID </summary>
    public const int TestAgentID = 6;

    /// <summary> 内部测试的代理ID </summary>
    public const int SpecialAgentID = 7;
    
    /// <summary>
    /// 热更新资源构建完成事件
    /// </summary>
    public static readonly UnityEvent<bool, string> OnContentBuildCompleted = new();


    /// <summary> 获取编辑器目标构建平台对应的目录名称 </summary>
    public static string GetPlatformDirName()
    {
        return EditorUserBuildSettings.activeBuildTarget switch
        {
            BuildTarget.Android => "Android",
            BuildTarget.iOS => "IOS",
            BuildTarget.StandaloneOSX => "Mac",
            _ => "PC"
        };
    }

    [MenuItem("EditorTools/生成热更dll+资源包", false, 10)]
    public static void Generate_Dll_Res()
    {
        GenerateFullResourcePack(false, true);
    }
    
    [MenuItem("EditorTools/生成热更dll+资源包+表数据", false, 10)]
    public static void Generate_Dll_Res_Table()
    {
        // 1. 执行资源生成逻辑
        GenerateFullResourcePack(false, true);

        // 2. 调用Python脚本：PythonTools\export_excel_data.py
        string pyScriptPath = Path.Combine(PathTools.PythonToolsDir, "export_excel_data.py");
        pyScriptPath = Path.GetFullPath(pyScriptPath);

        // 加上 -u 参数是一种强制不缓冲的保险手段
        string arguments = $"-u \"{pyScriptPath}\" --platform {GetPlatformDirName()} --unity-path \"{EditorApplication.applicationPath}\"";

        Debug.Log($"开始调用Python生成表数据，args：{arguments}");

        ExternalScriptRunner.RunPythonScript(arguments);
    }

    public static void GenerateFullResourcePack(bool isReview, bool needConfirm)
    {
        if (needConfirm)
            if (!ConfirmBuild())
                return;

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        
        // 丢弃可能尚未保存的场景修改
        DiscardSceneChanges();

        var beginTime = DateTime.Now;

        // 生成热更新程序集
        GenerateHotUpdateDlls();

        var duration1 = DateTime.Now - beginTime;
        Debug.Log($"<color=#00FF00>HybridCLR GenerateAll耗时：{duration1.TotalMinutes:F} 分钟</color>");

        bool buildSuccess = ContentBuild(isReview);
        OnContentBuildCompleted.Invoke(buildSuccess, s_TargetResVer);
        OnContentBuildCompleted.RemoveAllListeners();
        if (buildSuccess)
        {
            var duration2 = DateTime.Now - beginTime;
            Debug.Log($"<color=#00FF00>总耗时：{duration2.TotalMinutes:F} 分钟</color>");
        }
    }

    [MenuItem("EditorTools/生成热更dll", false, 10)]
    public static void GenerateHotUpdateDlls()
    {
        Debug.LogFormat("<color={0}>{1}</color>", "#33CCFF", "HybridCLR -> GenerateAll......");
        PrebuildCommand.GenerateAll();

        // Assets/Extend/GameDll - 存放以 .bytes 后缀保存的，编译后的 dll 文件，以便将 dll 打包成 AssetBundle
        var dllOutputDir = Path.Combine(PathTools.AssetsExtendDir, "GameDll");
        var target = EditorUserBuildSettings.activeBuildTarget;
        Debug.LogFormat("<color=#8A2BE2>当前平台为:{0}</color>", target);

        // 先清空 GameDll 文件夹
        Debug.LogFormat("<color=#8A2BE2>{0}</color>", "删除 Extend/GameDll 目录......");
        if (Directory.Exists(dllOutputDir)) Directory.Delete(dllOutputDir, true);

        Directory.CreateDirectory(dllOutputDir);

        // 为 aot、热更新程序集创建配置文件，以便运行时下载并读取这些程序集的名称
        var dllJson = new DllJson();
        dllJson.comment = "此文件是每次打包时，编译生成dll之后，由代码自动根据 HybridCLR 配置来写入的，请勿手动修改";

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        // 处理AOT程序集
        Debug.LogFormat("<color=#8A2BE2>{0}</color>", "拷贝 Aot Dlls......");
        CopyDlls(SettingsUtil.AOTAssemblyNames, SettingsUtil.GetAssembliesPostIl2CppStripDir(target), dllOutputDir,
            out dllJson.aotDllNames);

        // 处理热更新程序集
        Debug.LogFormat("<color=#8A2BE2>{0}</color>", "拷贝 热更新 DLLs......");
        CopyDlls(SettingsUtil.HotUpdateAssemblyFilesExcludePreserved,
            SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target), dllOutputDir, out dllJson.hotfixDllNames);

        // 修改拷贝的文件之前需要刷新资源数据库
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        // 将 dll 名称列表写入配置文件
        var jsonStr = JsonUtility.ToJson(dllJson, true);
        var configDir = Path.Combine(PathTools.AssetsExtendDir, "Config");
        Directory.CreateDirectory(configDir);
        using (var sw = new StreamWriter(configDir + "/DllConfig.json"))
        {
            sw.Write(jsonStr);
        }

        // 最后刷新一次以便在 Project 窗口中可以看到修改后的文件
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
    }

    private static void CopyDlls(List<string> dllNames, string srcDir, string outputDir, out List<string> dllBundleNames)
    {
        dllBundleNames = new List<string>();
        foreach (var dllName in dllNames)
        {
            var srcDllName = dllName.Contains(".dll") ? dllName : $"{dllName}.dll";
            var dllPath = Path.Combine(srcDir, srcDllName);

            if (!File.Exists(dllPath))
            {
                Debug.LogError($"dll 文件不存在:{dllPath}。HybridCLRData 中的 dll 需要构建一次 App 才会生成。");
                return;
            }

            // 修改文件名，修改后缀为.bytes，拷贝到临时文件夹中
            var bundleName = srcDllName.ToLower().Replace(".dll", "").Replace(".", "");
            dllBundleNames.Add(bundleName);
            IOUtilsEditor.CopyFile(srcDir, outputDir, srcDllName, $"{bundleName}.bytes");
        }
    }

    private static bool ConfirmBuild()
    {
        return EditorUtility.DisplayDialog(
            "生成热更新",
            $"开始生成【{GetPlatformDirName()}】端热更新",
            "确 定",
            "取 消"
        );
    }

    /// <summary>
    /// 开始构建热更新的内容资源
    /// </summary>
    /// <param name="isReview">是否为审核模式构建</param>
    public static bool ContentBuild(bool isReview = false)
    {
        // 获取本次构建的资源版本号和输出路径
        s_TargetResVer = GetBuildResVer();

        // 修改当前配置文件的构建路径
        var profileSettings = AddressableAssetSettingsDefaultObject.Settings.profileSettings;
        var profileId = AddressableAssetSettingsDefaultObject.Settings.activeProfileId;

        var remoteBuildPath = profileSettings.GetValueByName(profileId, "Remote.BuildPath");
        var remoteLoadPath = profileSettings.GetValueByName(profileId, "Remote.LoadPath");

        // 配置主场景资源的路径，默认使用远程路径
        MainResBuildPath = remoteBuildPath;
        MainResLoadPath = remoteLoadPath;
        if (isReview)
        {
            // 审核模式，主场景资源使用本地路径
            MainResBuildPath = profileSettings.GetValueByName(profileId, "Local.BuildPath");
            MainResLoadPath = profileSettings.GetValueByName(profileId, "Local.LoadPath");
        }

        Debug.Log($"【主场景】资源输出路径 => {MainResBuildPath}");
        Debug.Log($"【主场景】资源加载路径 => {MainResLoadPath}");
        Debug.Log($"【远程】资源输出路径 => {remoteBuildPath}");
        Debug.Log($"【远程】资源加载路径 => {remoteLoadPath}");

        var packageDir = Path.Combine(PathTools.HotUpdateOutputDir, GetPlatformDirName());
        if (Directory.Exists(packageDir))
        {
            Debug.Log($"删除 {packageDir}");
            Directory.Delete(packageDir, true);
        }
        
        Debug.Log($"开始 Addressables Content Build");
        AddressableAssetSettings.BuildPlayerContent(out var buildResult);
        
        if (string.IsNullOrEmpty(buildResult.Error))
        {
            // 构建成功，更新 hotUpdate.txt
            var hotUpdatePath = Path.Combine(packageDir, AppConfig.SvrUpdatePathFileName); // 如：仓库目录/Data/Package/PC/hotUpdate.txt
            File.WriteAllText(hotUpdatePath, s_TargetResVer);

            // todo 检查主场景AB包是否依赖于游戏内AB包
            Debug.Log($"<color=#00FF00>生成热更新资源包已完成 => 【{s_TargetResVer}】</color>");
            return true;
        }

        // 构建失败
        Debug.LogError("Addressables build error: " + buildResult.Error);
        return false;
    }

    private static string GetBuildResVer()
    {
        var platformName = GetPlatformDirName(); // 示例：PC
        var time = DateTime.Now.ToLocalTime().ToString("yyyyMMddHHmm"); // 示例：202510241622
        return $"{platformName}-{time}"; // 示例：PC-202510241622
    }

    // TODO Jenkins构建时还是有可能失败
    private static void DiscardSceneChanges()
    {
        var curScene = SceneManager.GetActiveScene();
        if (curScene.isDirty)
        {
            // 当前场景有改动，执行丢弃操作（重新打开最后一次保存的状态）
            EditorSceneManager.OpenScene(curScene.path);
            Debug.Log("已丢弃未保存的场景修改");
        }
    }
}