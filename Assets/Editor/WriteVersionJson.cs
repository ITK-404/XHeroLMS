// Assets/Editor/WriteVersionJson.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using UnityEngine;

public static class WriteVersionJson
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        // pathToBuiltProject trỏ tới file exe: .../XHeroLMS.exe
        var buildDir = Path.GetDirectoryName(pathToBuiltProject);
        var jsonPath = Path.Combine(buildDir, "version.json");

        // Lấy version từ PlayerSettings (chính là Application.version sau khi build)
        var ver = PlayerSettings.bundleVersion; // ví dụ "1.0.0"

        var json = "{\"version\":\"" + ver + "\"}";
        File.WriteAllText(jsonPath, json);

        Debug.Log($"[Build] Wrote version.json at: {jsonPath} (version={ver})");
    }
}
#endif
