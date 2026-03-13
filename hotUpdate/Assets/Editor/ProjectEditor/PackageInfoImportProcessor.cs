using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class PackageInfoImportProcessor : Editor
{
    /// <summary>
    /// 当代理包信息文件：Assets/Resources/AgentPackageInfo.json 不存在时，创建一个测试代理的文件
    /// </summary>
    [InitializeOnLoadMethod]
    static void CheckPackageInfo()
    {
        var packageInfoPath = Path.Combine(PathTools.ResourcesPath, AppConfig.AgentPackageInfoFileName);
        if (File.Exists(packageInfoPath))
            return;

        Debug.Log("创建默认的代理包信息文件：Assets/Resources/AgentPackageInfo.json");

        Directory.CreateDirectory(PathTools.ResourcesPath);
        
        AgentPackageInfo agentPackageInfo = new AgentPackageInfo()
        {
            Agent = PackAssetTools.TestAgentID,
            AppVersion = PackAssetTools.TargetAppVersion,
        };
        using var sw = File.CreateText(packageInfoPath);
        sw.Write(JsonConvert.SerializeObject(agentPackageInfo, Formatting.Indented));

        AssetDatabase.Refresh();
    }
}
