#if UNITY_IOS
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public class UniWebViewPostBuildProcessorIOS
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.iOS) {
            var projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            
            // ── Push Notification ──────────────────────────────────────────
            AddPushNotificationCapability(projectPath, pathToBuiltProject);

            // ── Auth Callback URLs (giữ nguyên) ────────────────────────────
            var settings = UniWebViewEditorSettings.GetOrCreateSettings();
            if (settings.authCallbackUrls.Length > 0) {
                var domains = GetHttpsAssociatedDomains(settings.authCallbackUrls);
                if (domains.Length > 0) {
                    Debug.Log("<UniWebView> Patching associated domains for auth callbacks...");
                    AddAssociatedDomain(projectPath, domains);
                }
            }
        }
    }

    // ── PUSH NOTIFICATION ────────────────────────────────────────────────────

    public static void AddPushNotificationCapability(string projectPath, string pathToBuiltProject)
    {
        Debug.Log("<UniWebView> Adding Push Notification capability...");

        PBXProject project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));

        var entitlementsFileName = "Unity-iPhone.entitlements";
        var targetGUID = project.GetUnityMainTargetGuid();

        var capabilityManager = new ProjectCapabilityManager(
            projectPath,
            entitlementsFileName,
            null,
            targetGUID
        );

        // Thêm capability Push Notifications
        capabilityManager.AddPushNotifications(development: false);
        capabilityManager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
        // development: true  -> dùng APNs Sandbox (Debug build)
        // development: false -> dùng APNs Production (Release build)

        capabilityManager.WriteToFile();

        // Thêm framework UserNotifications nếu chưa có
        project.ReadFromString(File.ReadAllText(projectPath));
        project.AddFrameworkToProject(targetGUID, "UserNotifications.framework", weak: false);
        File.WriteAllText(projectPath, project.WriteToString());

        Debug.Log("<UniWebView> Push Notification capability added.");
    }

    public static string[] GetHttpsAssociatedDomains(string[] urls) {
        return urls
            .Where(url => Uri.TryCreate(url, UriKind.Absolute, out Uri uri) && uri.Scheme == "https")
            .Select(url => new Uri(url).Host)
            .Distinct()
            .Select(domain => "applinks:" + domain)
            .ToArray();
    }

    public static void AddAssociatedDomain(string projectPath, string[] domains) {
        PBXProject project = new PBXProject();
        project.ReadFromString(File.ReadAllText(projectPath));

        var entitlementsFileName = "Unity-iPhone.entitlements";
        var targetGUID = project.GetUnityMainTargetGuid();
        var capabilityManager = new ProjectCapabilityManager(projectPath, entitlementsFileName, null, targetGUID);

        capabilityManager.AddAssociatedDomains(domains);
        capabilityManager.WriteToFile();
    }
}
#endif