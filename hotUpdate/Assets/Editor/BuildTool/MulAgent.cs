using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

#if UNITY_STANDALONE_OSX || UNITY_IOS
using UnityEditor.iOS.Xcode;
using UnityEditor.Callbacks;
using System.Threading;
#endif


public class MulAgent : IPreprocessBuildWithReport
{
    /// <summary> Mac, iOS打包配置文件 </summary>
    public const string MacBuildConfigFile = "MacBuildConfig.json";

    public const string IosBuildConfigFile = "IosBuildConfig.json";

    private static readonly string[] s_buildScenes = { "Assets/Custom/Launch.unity" };

    private class AgentConfig
    {
        public AppConfigData AppConfig;
        public BuildConfig BuildConfig;
    }

    private class BuildConfig
    {
        public string ProductName;
        public string PackageName;
        public string NsiProductName;
    }

    private class MacBuildConfig
    {
        public string BundleVersion;
    }

    private class IosBuildConfig
    {
        public string BundleVersion;
    }

    public int callbackOrder => 0;

    private static string s_curBuildInfo = "";
    private static readonly string s_agentListFile = Path.Combine(PathTools.AgentRootDir, "BuildAgentList.json");

    private static List<int> AgentListToBuild()
    {
        var result = new List<int>();
        var jsonObject = JObject.Parse(File.ReadAllText(s_agentListFile));
        if (jsonObject.TryGetValue("Agents", out var agents))
        {
            result = agents.ToObject<List<int>>();
            Debug.Log("打包代理列表：" + GetListValueString(result));
        }

        return result;
    }

    /// <summary>
    /// 应用代理的配置信息
    /// </summary>
    private static void ApplyAgentConfig(int agent)
    {
        Debug.LogFormat("<color=#EE82EE>{0}</color>", "开始生成代理包：" + agent + "===================================");
        CopyAgentFile(agent);

        // 获取代理的配置信息
        var agentConfigPath = Path.Combine(PathTools.AgentRootDir, agent.ToString(), "Config.json");
        var agentConfig = JsonConvert.DeserializeObject<AgentConfig>(File.ReadAllText(agentConfigPath));
        // 设置产品名和包名
        //  如果当前平台为Windows,并且NsiProductName存在，则使用NsiProductName
        var productName = agentConfig.BuildConfig.ProductName;
        if (EditorUserBuildSettings.activeBuildTarget is BuildTarget.StandaloneWindows64 or BuildTarget.StandaloneWindows && !string.IsNullOrEmpty(agentConfig.BuildConfig.NsiProductName))
            productName = agentConfig.BuildConfig.NsiProductName;
        PlayerSettings.productName = productName;
        Debug.Log("设置应用名：" + productName);
        PlayerSettings.applicationIdentifier = agentConfig.BuildConfig.PackageName;
        Debug.Log("设置包名：" + agentConfig.BuildConfig.PackageName);
        AssetDatabase.Refresh();
    }

    private static void Build_Android(int agentID)
    {
        Debug.Log("====> 开始 Android 打包");
        // 检查并配置Keystore密码
        if (PlayerSettings.Android.keystorePass.Length < 6) PlayerSettings.Android.keystorePass = "123456";
        if (PlayerSettings.Android.keyaliasPass.Length < 6) PlayerSettings.Android.keyaliasPass = "123456";

        var outputPath = $"{PathTools.ProjectDir}/Release-Android";
        Directory.CreateDirectory(outputPath);

        var apkName = $"{PlayerSettings.productName}{agentID}v{PackAssetTools.TargetAppVersion}.apk";
        var location = Path.Combine(outputPath, apkName);

        s_curBuildInfo =
            $"{agentID}\t{PlayerSettings.productName}\t{PackAssetTools.TargetAppVersion}\t开始时间-{DateTime.Now:HH:mm}";

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = s_buildScenes,
            locationPathName = location,
            options = BuildOptions.None,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android
        });
        s_curBuildInfo += $"\t结束时间-{DateTime.Now:HH:mm}";
        if (report.summary.result != BuildResult.Succeeded)
        {
            s_curBuildInfo += "\t打包失败";
            Debug.LogError("打包失败");
            return;
        }

        s_curBuildInfo += $"\t打包成功！\t{apkName}";

        Debug.LogFormat("<color=#EE82EE>{0}</color>", "====> 打包结束，输出路径：" + location);

        // 删除无用的文件夹
        DeleteUnusedDirectory(Path.Combine(outputPath,
            $"{PlayerSettings.productName}_BurstDebugInformation_DoNotShip"));
    }

    private static void Build_PC(int agentID)
    {
        Debug.Log("====> 开始 PC 打包");

        var outputPath = $"{PathTools.ProjectDir}/Release-Win32";
        Directory.CreateDirectory(outputPath);

        var buildOutputName = $"{agentID}_{PackAssetTools.TargetAppVersion}/{PlayerSettings.productName}";
        var buildOutputExeName = $"{buildOutputName}.exe";
        var location = Path.Combine(outputPath, buildOutputExeName);

        s_curBuildInfo =
            $"{agentID}\t{PlayerSettings.productName}\t{PackAssetTools.TargetAppVersion}\t开始时间-{DateTime.Now:HH:mm}";

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = s_buildScenes,
            locationPathName = location,
            options = BuildOptions.None,
            target = BuildTarget.StandaloneWindows,
            targetGroup = BuildTargetGroup.Standalone
        });
        s_curBuildInfo += $"\t结束时间-{DateTime.Now:HH:mm}";

        if (report.summary.result != BuildResult.Succeeded)
        {
            s_curBuildInfo += "\t打包失败";
            Debug.LogError("打包失败");
            return;
        }

        var exeName = $"{PlayerSettings.productName}{agentID}v{PackAssetTools.TargetAppVersion}.exe";
        s_curBuildInfo += $"\t打包成功！\t{exeName}";

        //Application.OpenURL($"file:///{outputPath}");
        Debug.LogFormat("<color=#EE82EE>{0}</color>", "====> 打包结束，输出路径：" + location);

        // 删除无用的文件夹
        DeleteUnusedDirectory(Path.Combine(outputPath,
            $"{buildOutputName}_BackUpThisFolder_ButDontShipItWithYourGame"));
        DeleteUnusedDirectory(Path.Combine(outputPath, $"{buildOutputName}_BurstDebugInformation_DoNotShip"));
    }

    private static void Build_IOS()
    {
        Debug.Log("====> 开始 IOS 打包");

        // 修改 iOS 的构建版本号
        // 获取配置信息
        var iosBuildConfigPath = Path.Combine(PathTools.IosProjectPath, IosBuildConfigFile);
        var iosBuildConfig = JsonConvert.DeserializeObject<IosBuildConfig>(File.ReadAllText(iosBuildConfigPath));
        PlayerSettings.iOS.buildNumber = iosBuildConfig.BundleVersion + 1;

        var report = BuildPipeline.BuildPlayer(s_buildScenes, PathTools.IosProjectPath, BuildTarget.iOS, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("打包失败");
            return;
        }

        Debug.LogFormat("<color=#EE82EE>{0}</color>", "====> 打包结束");
        // 删除无用的文件夹
        var delPath = Path.Combine(Directory.GetParent(PathTools.IosProjectPath).FullName,
            $"{PlayerSettings.productName}_BurstDebugInformation_DoNotShip");
        DeleteUnusedDirectory(delPath);
    }

    private static void Build_MacOS()
    {
        Debug.Log("====> 开始 MacOS 打包");

        // 目前因为 unity bug，导出 macOS 工程不能包含中文，这里强制使用固定的全英文包名
        PlayerSettings.productName = "VR-Fire-Software";
        Debug.Log("设置 macOS 应用名：" + PlayerSettings.productName);

        // 修改 macOS 的构建版本号
        // 获取配置信息
        var macBuildConfigPath = Path.Combine(PathTools.MacProjectPath, MacBuildConfigFile);
        var macBuildConfig = JsonConvert.DeserializeObject<MacBuildConfig>(File.ReadAllText(macBuildConfigPath));
        PlayerSettings.macOS.buildNumber = macBuildConfig.BundleVersion + 1;

        var report = BuildPipeline.BuildPlayer(s_buildScenes, PathTools.MacProjectPath, BuildTarget.StandaloneOSX, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("打包失败");
            return;
        }

        Debug.LogFormat("<color=#EE82EE>{0}</color>", "====> 打包结束");

        // 删除无用的文件夹
        var delPath = Path.Combine(Directory.GetParent(PathTools.MacProjectPath).FullName,
            $"{PlayerSettings.productName}_BurstDebugInformation_DoNotShip");
        DeleteUnusedDirectory(delPath);
        delPath = Path.Combine(Directory.GetParent(PathTools.MacProjectPath).FullName,
            $"{PlayerSettings.productName}_BackUpThisFolder_ButDontShipItWithYourGame");
        DeleteUnusedDirectory(delPath);
    }

    [MenuItem("EditorTools/使用1代理信息", false, 101)]
    public static void CopyAgent1Info()
    {
        CopyAgentFile(1);
    }

    [MenuItem("EditorTools/使用指定代理信息", false, 110)]
    public static void CopyAgentInfo()
    {
        InputDialogWindow.ShowWindow(
            title: "指定代理信息",
            prompt: "请输入代理ID：",
            onConfirmCallback: input =>
            {
                Debug.Log($"用户输入：{input}");
                if (int.TryParse(input, out var agent))
                {
                    Debug.Log($"代理ID：{agent}");
                    try
                    {
                        CopyAgentFile(agent);
                    }
                    catch (Exception e)
                    {
                        s_curBuildInfo = "\t指定代理信息 Exception:" + e;
                        Debug.LogError("指定代理信息失败:" + e);
                    }
                }
                else
                {
                    Debug.LogError($"代理ID：{input} 不是数字！");
                }
            }
        );
    }

    [MenuItem("EditorTools/打苹果过审包", true)] // 校验方法
    private static bool ValidateBuildAppleAgent()
    {
        return EditorUserBuildSettings.activeBuildTarget is BuildTarget.iOS or BuildTarget.StandaloneOSX;
    }

    [MenuItem("EditorTools/打苹果过审包", false, 201)]
    public static void BuildAppleAgent()
    {
        if (!CheckForBuildTarget())
            return;

        // 使用审核模式生成资源
        PackAssetTools.GenerateFullResourcePack(true, true);

        BuildForAgent(PackAssetTools.AppleAgentID);
    }

    [MenuItem("EditorTools/打指定代理包（已有热更新）", false, 201)]
    public static void BuildTargetAgentWithHotUpdate()
    {
        InputDialogWindow.ShowWindow(
            title: "打指定代理包",
            prompt: "请输入代理ID：",
            onConfirmCallback: input =>
            {
                Debug.Log($"用户输入：{input}");
                if (int.TryParse(input, out var agent))
                {
                    Debug.Log($"代理ID：{agent}");
                    try
                    {
                        BuildForAgent(agent);
                    }
                    catch (Exception e)
                    {
                        s_curBuildInfo = "\t打包Exception:" + e;
                        Debug.LogError("打包失败:" + e);
                    }
                }
                else
                {
                    Debug.LogError($"代理ID：{input} 不是数字！");
                }
            }
        );
    }

    [MenuItem("EditorTools/打多代理包（已有热更新）", false, 201)]
    public static void BuildMulAgentWithHotUpdate()
    {
        BeginBuildForMulAgent(true);
    }

    public static void BeginBuildForMulAgent(bool needConfirm)
    {
        if (!CheckForBuildTarget())
            return;

        var list = AgentListToBuild();
        if (list.Count == 0)
        {
            Debug.LogWarning("待打包代理列表为空！");
            return;
        }

        if (needConfirm)
            if (!ConfirmBuildMultiAgent(list))
                return;

        // 准备写入打包结果
        var buildResultFile =
            Path.Combine(PathTools.AgentRootDir, $"agentBuildResult-{DateTime.Now:yyyy-MM-dd-HH-mm}.txt");
        using var streamWriter = File.CreateText(buildResultFile);
        // 写入开始信息
        var beginInfo = $"{DateTime.Now:yyyy-MM-dd HH:mm} 开始为 {list.Count} 名代理打包 ===>";
        streamWriter.WriteLine(beginInfo);
        streamWriter.Flush();

        foreach (var agent in list)
        {
            try
            {
                BuildForAgent(agent);
            }
            catch (Exception e)
            {
                s_curBuildInfo = "\t打包Exception:" + e;
                Debug.LogError("打包失败:" + e);
            }

            streamWriter.WriteLine(s_curBuildInfo);
            streamWriter.Flush();
        }

        streamWriter.WriteLine($"打包结束");
        streamWriter.Flush();
    }

    private static bool ConfirmBuildMultiAgent(List<int> list)
    {
        return EditorUtility.DisplayDialog(
            "打多代理包",
            $"请确认！即将为{list.Count}名代理打包：\n{GetListValueString(list)}",
            "继 续",
            "取 消"
        );
    }

    /// <summary>
    /// 检查各平台设置是否合理
    /// </summary>
    private static bool CheckForBuildTarget()
    {
        Debug.Log($"BuildTarget: {EditorUserBuildSettings.activeBuildTarget}");
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android
            && EditorUserBuildSettings.exportAsGoogleAndroidProject)
            // Android平台，勾选了 'Export Project'，弹窗提示
            return EditorUtility.DisplayDialog(
                "注 意！",
                "在 Build Settings - Android 配置下，勾选了 'Export Project'，请确认是否继续打包？",
                "继 续",
                "取 消"
            );
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneOSX
            && EditorUserBuildSettings.macOSXcodeBuildConfig != XcodeBuildConfig.Release)
        {
            // MacOS平台，没有勾选 'Create Xcode Project'，弹窗提示
            // TODO unity暂未提供获取此选项的字段或方法，这里仅判断 macOSXcodeBuildConfig 其实是不准确的，如果在打包后发现并未导出Xcode工程，记得在 BuildSettings 中手动检查
            Debug.Log($"EditorUserBuildSettings.macOSXcodeBuildConfig : {EditorUserBuildSettings.macOSXcodeBuildConfig}");
            return EditorUtility.DisplayDialog(
                "注 意！",
                "在 Build Settings - Mac 配置下，没有勾选 'Create Xcode Project' 并且设置为 'Run In Xcode as - Release'，请确认是否继续打包？",
                "继 续",
                "取 消"
            );
        }

        return true;
    }


    /// <summary>
    /// 拷贝代理目录下的图片，创建安装包信息文件
    /// </summary>
    private static void CopyAgentFile(int agent)
    {
        var srcDir = Path.Combine(PathTools.AgentRootDir, agent.ToString());
        var destDir1 = Application.dataPath + "/Resources/AppIcon/";
        IOUtilsEditor.CopyFile(srcDir, destDir1, "launch.png");
        IOUtilsEditor.CopyFile(srcDir, destDir1, "logo.png");
        IOUtilsEditor.CopyFile(srcDir, destDir1, "app_logo.png");

        // 获取代理的配置信息
        var agentConfigPath = Path.Combine(PathTools.AgentRootDir, agent.ToString(), "Config.json");
        var agentConfig = JsonConvert.DeserializeObject<AgentConfig>(File.ReadAllText(agentConfigPath));
        // 设置产品名和包名
        PlayerSettings.productName = agentConfig.BuildConfig.ProductName;
        Debug.Log("设置应用名：" + agentConfig.BuildConfig.ProductName);
        PlayerSettings.applicationIdentifier = agentConfig.BuildConfig.PackageName;
        Debug.Log("设置包名：" + agentConfig.BuildConfig.PackageName);

        CreateAgentPackageInfo(agent);
        AssetDatabase.Refresh();
    }

    private static void CreateAgentPackageInfo(int agent)
    {
        var agentPackageInfo = new AgentPackageInfo
        {
            Agent = agent,
            AppVersion = PackAssetTools.TargetAppVersion
        };
        using var sw = File.CreateText(Path.Combine(PathTools.ResourcesPath, AppConfig.AgentPackageInfoFileName));
        sw.Write(JsonConvert.SerializeObject(agentPackageInfo, Formatting.Indented));
    }

    private static void BuildForAgent(int agent)
    {
        if (!CheckForBuildTarget())
            return;

        ApplyAgentConfig(agent);

        PlayerSettings.bundleVersion = PackAssetTools.TargetAppVersion;
        var activeTarget = EditorUserBuildSettings.activeBuildTarget;
        if (activeTarget == BuildTarget.Android)
            Build_Android(agent);
        else if (activeTarget == BuildTarget.StandaloneWindows64 || activeTarget == BuildTarget.StandaloneWindows)
            Build_PC(agent);
        else if (activeTarget == BuildTarget.iOS)
            Build_IOS();
        else if (activeTarget == BuildTarget.StandaloneOSX)
            Build_MacOS();

        OnBuildFinished();
    }

    /// <summary>
    /// 监听 build 前的事件回调
    /// </summary>
    public void OnPreprocessBuild(BuildReport report)
    {
        // 打正式包，需要隐藏unity logo
        PlayerSettings.SplashScreen.showUnityLogo = false;
    }

    /// <summary>
    /// 在打包完成后，比 [PostProcessBuild] 监听的时间更晚
    /// </summary>
    private static void OnBuildFinished()
    {
        var activeTarget = EditorUserBuildSettings.activeBuildTarget;
        if (activeTarget == BuildTarget.StandaloneOSX)
        {
#if UNITY_STANDALONE_OSX
            XcodeAddCapabilityForMacOS(PathTools.MacProjectPath);
            
            ModifyRTVoiceIdentifier();
            
            ModifyMacBuildConfig();
#endif
        }
        else if (activeTarget == BuildTarget.iOS)
        {
#if UNITY_IOS
            ModifyIosBuildConfig();
#endif
        }
    }


    private static void DeleteUnusedDirectory(string delPath)
    {
        if (Directory.Exists(delPath))
        {
            Debug.Log("====> 删除无用的文件夹：" + delPath);
            Directory.Delete(delPath, true);
        }
    }

    #region 苹果端：iOS、macOS

#if UNITY_STANDALONE_OSX || UNITY_IOS
    //按照自己工程，修改links
    static string[] universalLinks = { "applinks:m68.bjzjxf.com" };
    static string teamId = "AEKWFC5UZR";

    /// <summary>
    /// 写入Xcode中的pinfo
    /// </summary>
    static void SetPInfo(string projPath, Action<PlistDocument> action)
    {
        // Get the Info.plist file path.
        string plistPath = Path.Combine(projPath, "Info.plist");
        Debug.Log("SetPInfo plistPath: " + plistPath);
        
        // Open and parse the Info.plist file.
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        action?.Invoke(plist);

        // Write the modified Info.plist file.
        plist.WriteToFile(plistPath);
    }

    /// <summary>
    /// 在编辑器添加iOS能力，这个代码在编辑器打包iOS项目后会自动执行
    /// </summary>
    /// <param name="buildTarget"></param>
    /// <param name="path"></param>
    [PostProcessBuild]
    public static void XcodeAddCapability(BuildTarget buildTarget, string path)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);

        string targetGuid = proj.GetUnityMainTargetGuid();
        proj.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", "Apple Development");
        proj.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");
        proj.SetTeamId(targetGuid, teamId);

        targetGuid = proj.GetUnityFrameworkTargetGuid();
        proj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-ObjC");
        proj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-l\"z\"");
        proj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-l\"sqlite3\"");
        proj.AddBuildProperty(targetGuid, "OTHER_LDFLAGS", "-l\"c++\"");

        //添加Frameworks
        proj.AddFrameworkToProject(targetGuid, "WebKit.framework", false);

        // 保存
        File.WriteAllText(projPath, proj.WriteToString());

        var projCapability = new ProjectCapabilityManager(projPath, "Unity-iPhone.entitlements", "Unity-iPhone");
        projCapability.AddAssociatedDomains(universalLinks);
        projCapability.WriteToFile();

        // 自动添加 URL Types 的配置
        SetPInfo(path, (plist) =>
        {
            var urlTypeDict = plist.root.CreateArray("CFBundleURLTypes").AddDict();
            urlTypeDict.SetString("CFBundleURLName", PlayerSettings.applicationIdentifier);

            //在“info”标签栏的“URL type“添加“URL scheme”为你所注册的应用程序 id
            var urlSchemes = urlTypeDict.CreateArray("CFBundleURLSchemes");
            urlSchemes.AddString("wxdfa4908637b1a639");

            // Add the schemes to the LSApplicationQueriesSchemes key.
            PlistElementArray schemes = plist.root.CreateArray("LSApplicationQueriesSchemes");
            schemes.AddString("weixin");
            schemes.AddString("weixinULAPI");
            schemes.AddString("weixinURLParamsAPI");

            // 添加属性：[App Uses Non-Exempt Encryption]，值为 false，表示app没有使用加密
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            // 添加属性：[Privacy - Microphone Usage Description]，用于获取麦克风权限
            plist.root.SetString("NSMicrophoneUsageDescription", "需要麦克风权限来录制语音");
            // 添加属性：[Privacy - Camera Usage Description]，用于获取摄像头权限
            plist.root.SetString("NSCameraUsageDescription", "需要摄像头权限来扫一扫");
        });
    }
    
    private static void ModifyIosBuildConfig()
    {
        var iosBuildConfig = new IosBuildConfig
        {
            BundleVersion = PlayerSettings.iOS.buildNumber
        };
        using var sw = File.CreateText(Path.Combine(PathTools.IosProjectPath, IosBuildConfigFile));
        sw.Write(JsonConvert.SerializeObject(iosBuildConfig, Formatting.Indented));
    }
    
    public static void XcodeAddCapabilityForMacOS(string path)
    {
        Debug.Log("path: " + path);
        
        if (path.Contains("HybridCLRData")) return; // 打包时热更新框架会先执行一次build，需要跳过这一次的后处理
        
        Thread.Sleep(1000);

        string projPath = Path.Combine(path, "MacOS_project.xcodeproj", "project.pbxproj");
        Debug.Log("projPath: " + projPath);

        string entitlementsPath = $"{PlayerSettings.productName}.entitlements";
        Debug.Log("entitlementsPath: " + entitlementsPath);

        PBXProject proj = new PBXProject();
        proj.ReadFromFile(projPath);
        string targetGuid = proj.GetUnityMainTargetGuid();

        // 配置构建属性和Team
        proj.SetBuildProperty(targetGuid, "CODE_SIGN_IDENTITY", "Apple Development");
        proj.SetBuildProperty(targetGuid, "CODE_SIGN_STYLE", "Automatic");
        proj.SetTeamId(targetGuid, teamId);

        // 添加 Capability
        PBXCapabilityType capabilityType = new PBXCapabilityType("com.apple.security.app-sandbox", true);
        bool res = proj.AddCapability(targetGuid, capabilityType, entitlementsPath);
        Debug.Log("AddCapability res: " + res);

        // 自动添加URL Types的配置
        var pInfoPath = Path.Combine(path, PlayerSettings.productName);
        SetPInfo(pInfoPath, (plist) =>
        {
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.root.SetString("CFBundleDisplayName", "${PRODUCT_NAME}");
        });
        
        // 本地化
        string localizationSrcPath = Path.Combine(path, "Localization", "en.lproj", "InfoPlist.strings");
        string localizationDestPath = Path.Combine($"{PlayerSettings.productName}", "InfoPlist.strings");
        string fileGuid = proj.AddFile(localizationSrcPath, localizationDestPath);
        proj.AddFileToBuild(targetGuid, fileGuid);
        
        // 保存
        proj.WriteToFile(projPath);
    }

    static void ModifyRTVoiceIdentifier()
    {
        // RT-Voice（2023.1.0）版本会在打包完成后自动给 ProcessStart.bundle 的 CFBundleIdentifier 加上时间戳作为后缀，导致发布时签名标识符不一致的报错。这里强制改回来
        var RTVoiceInfoPath =
 Path.Combine(PathTools.MacProjectPath, PlayerSettings.productName, "Plugins", "ProcessStart.bundle", "Contents");
        SetPInfo(RTVoiceInfoPath, (plist) =>
        {
            plist.root.SetString("CFBundleIdentifier", "com.crosstales.procstart");
        });
    }
    
    private static void ModifyMacBuildConfig()
    {
        // 修改 macOS 的配置信息
        var macBuildConfig = new MacBuildConfig
        {
            BundleVersion = PlayerSettings.macOS.buildNumber
        };
        using var sw = File.CreateText(Path.Combine(PathTools.MacProjectPath, MacBuildConfigFile));
        sw.Write(JsonConvert.SerializeObject(macBuildConfig, Formatting.Indented));
    }

#endif

    #endregion
    
    public static string GetListValueString<T>(ICollection<T> collection, char separator = '，')
    {
        if (collection == null || collection.Count == 0)
        {
            return "";
        }

        string[] strArr = new string[collection.Count];
        int i = 0;
        foreach (var item in collection)
        {
            strArr[i] = item.ToString();
            i++;
        }
        return string.Join(separator, strArr);
    }
}