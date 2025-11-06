using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class OpenPersistentDataPath
{
    [MenuItem("Tools/Open Persistent Data Path")]
    public static void Open()
    {
        var path = Application.persistentDataPath;
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);

#if UNITY_EDITOR_WIN
        // Open folder in Windows Explorer
        Process.Start("explorer.exe", $"\"{path}\"");
#else
        // Fallback for macOS / Linux in editor
        EditorUtility.RevealInFinder(path);
#endif
    }
}