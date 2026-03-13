using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class ExternalScriptRunner
{
    /// <summary>
    /// 调用外部脚本
    /// </summary>
    /// <param name="args">参数</param>
    public static void RunPythonScript(string args)
    {
        string pythonExe = Application.platform == RuntimePlatform.WindowsEditor ? "python" : "python3";
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // --- 关键：手动清除当前unity进程的环境变量 ---
        var env = startInfo.EnvironmentVariables;
        string[] keysToRemove = { "UNITY_IL2CPP_PATH", "IL2CPP_PATH", "HYBRIDCLR_UNITY_ASSETS_DIR" };
        foreach (var key in keysToRemove)
        {
            if (env.ContainsKey(key)) env.Remove(key);
        }
        // 修改Python环境变量
        env["PYTHONUNBUFFERED"] = "1";
        env["PYTHONIOENCODING"] = "UTF-8";

        using Process process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (s, e) => { if (e.Data != null) Debug.Log($"[Py]: {e.Data}"); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) Debug.LogError($"[Py Err]: {e.Data}"); };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit(); 
        }
        catch (Exception e) { Debug.LogError(e.Message); }
    }
}