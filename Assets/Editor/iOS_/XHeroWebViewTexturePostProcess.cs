#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class XHeroWebViewTexturePostProcess
{
    [PostProcessBuild(1102)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS)
            return;

        string pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        if (!File.Exists(pbxPath))
        {
            Debug.LogError($"[XHeroWV] PBX project not found: {pbxPath}");
            return;
        }

        var project = new PBXProject();
        project.ReadFromFile(pbxPath);

#if UNITY_2019_3_OR_NEWER
        string mainTarget = project.GetUnityMainTargetGuid();
        string frameworkTarget = project.GetUnityFrameworkTargetGuid();
#else
        string mainTarget = project.TargetGuidByName("Unity-iPhone");
        string frameworkTarget = mainTarget;
#endif

        AddFrameworks(project, mainTarget);
        if (frameworkTarget != mainTarget)
            AddFrameworks(project, frameworkTarget);

        project.WriteToFile(pbxPath);
        Debug.Log("[XHeroWV] Ensured native video iOS frameworks are linked.");
    }

    private static void AddFrameworks(PBXProject project, string targetGuid)
    {
        project.AddFrameworkToProject(targetGuid, "AVFoundation.framework", false);
        project.AddFrameworkToProject(targetGuid, "CoreMedia.framework", false);
        project.AddFrameworkToProject(targetGuid, "CoreVideo.framework", false);
        project.AddFrameworkToProject(targetGuid, "QuartzCore.framework", false);
    }
}
#endif
