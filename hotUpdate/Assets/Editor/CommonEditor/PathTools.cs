using System.IO;
using UnityEngine;

public static class PathTools
{
    /// <summary> Assets 目录 </summary>
    private static readonly DirectoryInfo s_AssetsDirInfo = new DirectoryInfo(Application.dataPath);
    
    /// <summary> Git 仓库目录 </summary>
    private static readonly string s_RepoDir = s_AssetsDirInfo.Parent.Parent.FullName;
    
    /// <summary> 仓库目录/Data </summary>
    public static readonly string RepoDataDir = Path.Combine(s_RepoDir, "Data");
    
    /// <summary> 仓库目录/Data/Package - 热更新输出的根目录 </summary>
    public static readonly string HotUpdateOutputDir = Path.Combine(RepoDataDir, "Package");
    
    /// <summary> 仓库目录/Data/PythonTools - Python工具脚本目录 </summary>
    public static readonly string PythonToolsDir = Path.Combine(RepoDataDir, "PythonTools");
    
    /// <summary> 仓库目录/Data/Agent - 所有代理的根目录 </summary>
    public static readonly string AgentRootDir = Path.Combine(RepoDataDir, "AgentConfig");
    
    /// <summary> 仓库目录/IOS_project - IOS工程目录 </summary>
    public static readonly string IosProjectPath = Path.Combine(s_RepoDir, "IOS_project");
    
    /// <summary> 仓库目录/MacOS_project - MacOS工程目录 </summary>
    public static readonly string MacProjectPath = Path.Combine(s_RepoDir, "MacOS_project");

    /// <summary> 仓库目录/hotUpdate - unity 工程目录（Assets的父级目录） </summary>
    public static readonly string ProjectDir = s_AssetsDirInfo.Parent.FullName;
    
    /// <summary> 仓库目录/hotUpdate/Assets/Resources </summary>
    public static readonly string ResourcesPath = Path.Combine(Application.dataPath, "Resources");

    /// <summary> 仓库目录/hotUpdate/Assets/Extend - 待打包热更新资源的根目录 </summary>
    public static readonly string AssetsExtendDir = Path.Combine(s_AssetsDirInfo.FullName, "Extend");
    
    /// <summary> 仓库目录/hotUpdate/Assets/Extend/ScriptableObjects - ScriptableObject 资源目录 </summary>
    public static readonly string ScriptableObjectsDir = Path.Combine(AssetsExtendDir, "ScriptableObjects");
    
    /// <summary> 仓库目录/hotUpdate/Assets/Extend/Fonts - 字体资源目录 </summary>
    public static readonly string FontsDir = Path.Combine(AssetsExtendDir, "Fonts");
    
    /// <summary> 当前平台的热更新根目录 </summary>
    public static string CurHotUpdateRootDir => Path.Combine(HotUpdateOutputDir, PackAssetTools.GetPlatformDirName());
}
