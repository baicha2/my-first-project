using System.IO;
using UnityEngine;
using UnityEngine.Networking;


public class AgentPackageInfo
{
    public int Agent;
    public string AppVersion;
}

/// <summary>
/// 负责热更新的类
/// </summary>
public class HotUpdateManager
{
    public static readonly HotUpdateManager instance = new HotUpdateManager();
    public static HotUpdateManager Instance => instance;

    // 本地路径
    public string HotUpdatePath; // 存放下载的热更新文件的目录，位于APP持久化数据目录中
    private string LocalVersionFilePath; // 本地版本标识文件的路径

    // 文件夹名称
    public const string HotUpdateDirName = "SelfUpdateFile";

    // 将 Bytes 转 MB 的常数（1024 * 1024）
    public const float BytesToMB = 1048576f;

    /// <summary>
    /// 加载本地版本信息
    /// </summary>
    public void LoadLocalVersion()
    {
        HotUpdatePath = Path.Combine(Application.persistentDataPath, HotUpdateDirName);
        Directory.CreateDirectory(HotUpdatePath);

        Debug.Log($"<color=#FF51FF>LocalPath......{HotUpdatePath}</color>");

        // 通过版本标识文件来判断是不是一个新的版本
        LocalVersionFilePath = Path.Combine(HotUpdatePath, $"ResVersion-{AppConfig.AppVersion}.txt");
        Debug.Log($"<color=#FF51FF>LocalVersionFilePath......{LocalVersionFilePath}</color>");
        if (File.Exists(LocalVersionFilePath))
        {
            // 保存本地热更新资源版本号
            AppConfig.ResVersion = File.ReadAllText(LocalVersionFilePath);
        }
        else
        {
            Debug.Log($"首次安装，或者主包版本发生变化，清空本地热更目录");
            CleanHotUpdateDir();
        }
        Debug.Log($"<color=#FF51FF>本地热更新版本：{AppConfig.ResVersion}</color>");
    }

    public void CleanHotUpdateDir()
    {
        // 删除本地热更文件
        if (Directory.Exists(HotUpdatePath))
        {
            foreach (var file in Directory.GetFiles(HotUpdatePath))
                File.Delete(file);
            foreach (var directory in Directory.GetDirectories(HotUpdatePath))
                Directory.Delete(directory, true);
        }

        Directory.CreateDirectory(HotUpdatePath);
        File.WriteAllText(LocalVersionFilePath, "");
    }

    public static bool IsWebRequestSuccess(UnityWebRequest req)
    {
        if (req.isDone && req.result == UnityWebRequest.Result.Success)
        {
            return true;
        }
        else
        {
            Debug.LogWarning($"Request Failed: {req.error}, url: {req.url}");
            return false;
        }
    }

    // 更新主场景资源成功后，写入本地缓存的文件，记录当前的资源版本号
    public void RefreshLocalResVersion(string resVer)
    {
        File.WriteAllText(LocalVersionFilePath, resVer);
    }
}