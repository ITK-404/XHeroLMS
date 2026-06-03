#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class Sample_AddFileToBuild
{
    private const string CanOpenPath = "Libraries/Plugins/iOS/CanOpenURL.mm";
    private const string HardwareMachinePath = "Libraries/Plugins/iOS/HWMachine.mm";
    private const string StoragePluginPath = "Libraries/Plugins/iOS/StoragePlugin.mm";

    [PostProcessBuild(9999)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        // 0) Verify file exists in built Xcode folder
        LoadFile(pathToBuiltProject, CanOpenPath);
        LoadFile(pathToBuiltProject, HardwareMachinePath);
        LoadFile(pathToBuiltProject, StoragePluginPath);
    }

    private static void LoadFile(string pathToBuiltProject,string RelPath)
    {
        var absPath = Path.Combine(pathToBuiltProject, RelPath);
        if (!File.Exists(absPath))
        {
            UnityEngine.Debug.LogError($"[AddCanOpenURL] File not found in build folder: {absPath}");
            return;
        }

        // 1) Load pbxproj
        var pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var pbx = new PBXProject();
        pbx.ReadFromFile(pbxPath);

        var mainTarget = pbx.GetUnityMainTargetGuid();            // Unity-iPhone
        var frameworkTarget = pbx.GetUnityFrameworkTargetGuid();  // UnityFramework

        // 2) Find existing file GUID first (tránh duplicate GUID)
        string fileGuid = pbx.FindFileGuidByProjectPath(RelPath);

        if (string.IsNullOrEmpty(fileGuid))
        {
            fileGuid = pbx.AddFile(RelPath, RelPath, PBXSourceTree.Source);
            UnityEngine.Debug.Log($"[AddCanOpenURL] Added file reference: {RelPath}");
        }
        else
        {
            UnityEngine.Debug.Log($"[AddCanOpenURL] Found existing file reference: {RelPath}");
        }

        // 3) Add to Compile Sources (Sources build phase) for both targets
        pbx.AddFileToBuild(frameworkTarget, fileGuid);
        pbx.AddFileToBuild(mainTarget, fileGuid);

        pbx.WriteToFile(pbxPath);

        UnityEngine.Debug.Log("[AddCanOpenURL] Added CanOpenURL.mm to Compile Sources (UnityFramework + Unity-iPhone).");
    }
}
#endif