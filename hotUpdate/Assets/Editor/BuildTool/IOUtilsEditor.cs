using System.IO;
using UnityEditor;
using UnityEngine;

public class IOUtilsEditor : Editor
{

    public static void CopyFile(string srcDir, string destDir, string fileName, string destFileName = null)
    {
        Debug.Log("拷贝文件：" + fileName);
        var from = Path.Combine(srcDir, fileName);
        var to = Path.Combine(destDir, string.IsNullOrEmpty(destFileName) ? fileName : destFileName);
        
        Directory.CreateDirectory(Directory.GetParent(to).FullName);
        
        if (File.Exists(from))
        {
            File.Copy(from, to, true);
        }
    }
}