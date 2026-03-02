using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Android;

// Shared utilities for Android assets copying functionality
static class UniWebViewAndroidAssetsHelper {

    /// <summary>
    /// Copies configured asset folders to Android assets directory
    /// </summary>
    /// <param name="assetFolders">Array of folder names relative to Assets directory</param>
    /// <param name="targetAssetsPath">Target Android assets path</param>
    public static void CopyConfiguredAssets(string[] assetFolders, string targetAssetsPath) {
        if (assetFolders == null || assetFolders.Length == 0) {
            return; // No folders configured, skip copying
        }

        Debug.Log("<UniWebView> Copying configured asset folders to Android assets...");

        foreach (var folder in assetFolders) {
            if (string.IsNullOrWhiteSpace(folder)) {
                continue;
            }

            try {
                // Source: Assets/folder (relative to Assets directory)
                var sourceDir = Path.Combine(Application.dataPath, folder);

                // Target: targetAssetsPath/folder (preserve relative path structure)
                var targetDir = Path.Combine(targetAssetsPath, folder);

                if (!Directory.Exists(sourceDir)) {
                    Debug.LogWarning($"<UniWebView> Source folder not found: {sourceDir}");
                    continue;
                }

                // Create target directory if it doesn't exist
                var targetParent = Path.GetDirectoryName(targetDir);
                if (!Directory.Exists(targetParent)) {
                    Directory.CreateDirectory(targetParent);
                }

                // Remove existing target directory to ensure clean copy
                if (Directory.Exists(targetDir)) {
                    Directory.Delete(targetDir, true);
                }

                // Copy directory recursively
                CopyDirectoryRecursively(sourceDir, targetDir);

                Debug.Log($"<UniWebView> Successfully copied '{folder}' to Android assets");

            } catch (Exception e) {
                Debug.LogError($"<UniWebView> Failed to copy folder '{folder}': {e.Message}");
            }
        }
    }

    /// <summary>
    /// Recursively copies directory contents, excluding Unity .meta files
    /// </summary>
    /// <param name="sourceDir">Source directory path</param>
    /// <param name="targetDir">Target directory path</param>
    public static void CopyDirectoryRecursively(string sourceDir, string targetDir) {
        Directory.CreateDirectory(targetDir);

        // Copy all files except .meta files
        foreach (var file in Directory.GetFiles(sourceDir)) {
            var fileName = Path.GetFileName(file);

            // Skip Unity .meta files
            if (fileName.EndsWith(".meta")) {
                continue;
            }

            var targetFile = Path.Combine(targetDir, fileName);
            File.Copy(file, targetFile, true);
        }

        // Copy all subdirectories recursively
        foreach (var subDir in Directory.GetDirectories(sourceDir)) {
            var subDirName = Path.GetFileName(subDir);
            var targetSubDir = Path.Combine(targetDir, subDirName);
            CopyDirectoryRecursively(subDir, targetSubDir);
        }
    }
}

// #if UNITY_2023_2_OR_NEWER
#if UNIWEBVIEW_NEW_ANDROID_BUILD_SYSTEM

using System.Linq;
using Unity.Android.Gradle;
using Unity.Android.Gradle.Manifest;
using UnityEditor.Android;
using Application = UnityEngine.Application;
using Action = Unity.Android.Gradle.Manifest.Action;

class UniWebViewPostBuildModifier : AndroidProjectFilesModifier {
    
    const string ContextSettingsKey = "uniwebview_settings";
    
    public override AndroidProjectFilesModifierContext Setup() {
        var context = new AndroidProjectFilesModifierContext {
            Dependencies = {
                DependencyFiles = new[] {
                    // Set UniWebView editor settings asset path to be included in the build. With that we do not need
                    // a clean build to update the settings anymore (maybe, not confirmed).
                    UniWebViewEditorSettings.AssetPath
                }
            }
        };

        var settings = UniWebViewEditorSettings.GetOrCreateSettings();
        context.SetData(ContextSettingsKey, settings);
        return context;
    }

    public override void OnModifyAndroidProjectFiles(AndroidProjectFiles projectFiles) {
        var settings = projectFiles.GetData<UniWebViewEditorSettingsReading>(ContextSettingsKey);
        PatchUnityLibraryAndroidManifest(projectFiles.UnityLibraryManifest, settings);
        PatchUnityLibraryBuildGradle(projectFiles.UnityLibraryBuildGradle, settings);
        PatchGradleProperty(projectFiles.GradleProperties, settings);
        CopyAndroidAssetsNew(projectFiles, settings);
    }

    private void PatchUnityLibraryAndroidManifest(
        AndroidManifestFile manifest, UniWebViewEditorSettingsReading settings
    ) {
        // Set hardwareAccelerated
        var launcherActivities = manifest.Manifest.GetActivitiesWithLauncherIntent();
        foreach (var activity in launcherActivities)  {
            // Required for playing video in web view.
            activity.Attributes.HardwareAccelerated.Set(true);
        }
        
        // Set usesCleartextTraffic
        if (settings.usesCleartextTraffic) {
            manifest.Manifest.Application.Attributes.UsesCleartextTraffic.Set(true);
        }
        
        // Set WRITE_EXTERNAL_STORAGE permission
        if (settings.writeExternalStorage) {
            AddUsesPermission(manifest, "android.permission.WRITE_EXTERNAL_STORAGE");
        }

        // Set ACCESS_FINE_LOCATION permission
        if (settings.accessFineLocation) {
            AddUsesPermission(manifest, "android.permission.ACCESS_FINE_LOCATION");
        }
        
        // Set auth callback intent filter
        if (settings.authCallbackUrls.Length > 0 || settings.supportLINELogin) {
            var authActivity = new Activity();
            authActivity.Attributes.Name.Set("com.onevcat.uniwebview.UniWebViewAuthenticationActivity");
            authActivity.Attributes.Exported.Set(true);
            authActivity.Attributes.LaunchMode.Set(LaunchMode.SingleTask);
            authActivity.Attributes.ConfigChanges.Set(
                new[] {
                    ConfigChanges.Orientation, ConfigChanges.ScreenSize, ConfigChanges.KeyboardHidden
                });
            
            foreach (var url in settings.authCallbackUrls) {
                AddAuthCallbacksIntentFilter(authActivity, url);
            }
            if (settings.supportLINELogin) {
                AddAuthCallbacksIntentFilter(authActivity, "lineauth://auth");
            }
            manifest.Manifest.Application.ActivityList.AddElement(authActivity);            
        }
    }

    private void PatchUnityLibraryBuildGradle(ModuleBuildGradleFile gradleFile, UniWebViewEditorSettingsReading settings) {
        if (settings.addsKotlin) {
            var kotlinPrefix = "org.jetbrains.kotlin:kotlin-stdlib-jdk7:";
            var kotlinVersion = String.IsNullOrWhiteSpace(settings.kotlinVersion) 
                ? UniWebViewEditorSettings.defaultKotlinVersion : settings.kotlinVersion;
            ReplaceContentOrAddStartsWith(gradleFile.Dependencies, 
                kotlinPrefix, 
                kotlinVersion);
            Debug.Log("<UniWebView> Updated Kotlin dependency in build.gradle.");
        }

        if (settings.addsAndroidBrowser) {
            var browserPrefix = "androidx.browser:browser:";
            var browserVersion = String.IsNullOrWhiteSpace(settings.androidBrowserVersion) 
                ? UniWebViewEditorSettings.defaultAndroidBrowserVersion : settings.androidBrowserVersion;
            ReplaceContentOrAddStartsWith(gradleFile.Dependencies,
                browserPrefix, 
                browserVersion);
            Debug.Log("<UniWebView> Updated Browser dependency in build.gradle.");
        }

        if (!settings.addsAndroidBrowser && settings.addsAndroidXCore) {
            var androidXCorePrefix = "androidx.core:core:";
            var androidXCoreVersion = String.IsNullOrWhiteSpace(settings.androidXCoreVersion) 
                ? UniWebViewEditorSettings.defaultAndroidXCoreVersion : settings.androidXCoreVersion;
            ReplaceContentOrAddStartsWith(gradleFile.Dependencies,
                androidXCorePrefix, 
                androidXCoreVersion);
            Debug.Log("<UniWebView> Updated Android X Core dependency in build.gradle.");
        }
    }

    private void PatchGradleProperty(GradlePropertiesFile file, UniWebViewEditorSettingsReading settings) {
        var values = file.GetElements();
        foreach (var ele in values) {
            if (ele.GetRaw().Contains("android.enableJetifier")) {
                ele.SetRaw("android.enableJetifier=" + (settings.enableJetifier ? "true": "false"));
            }
        }
    }

    private void AddUsesPermission(AndroidManifestFile manifest, string name) {

        var list = manifest.Manifest.UsesPermissionList;
        foreach (var item in list) {
            Debug.Log(item.GetName());
        }
        
        var existing = manifest.Manifest.UsesPermissionList
            .FirstOrDefault(ele => ele.Attributes.Name.Get() == name);
        if (existing != null) {
            return;
        }
        var permission = new UsesPermission();
        permission.Attributes.Name.Set(name);
        manifest.Manifest.UsesPermissionList.AddElement(permission);
    }

    private void AddAuthCallbacksIntentFilter(Activity activity, string callbackUrl) {
        var uri = new Uri(callbackUrl);
        var scheme = uri.Scheme;
        
        if (String.IsNullOrEmpty(scheme)) {
            Debug.LogError("<UniWebView> Auth callback url contains an empty scheme. Please check the url: " + callbackUrl);
            return;
        }
        
        var intentFilter = new IntentFilter();
        
        var action = new Action();
        action.Attributes.Name.Set("android.intent.action.VIEW");
        intentFilter.ActionList.AddElement(action);
        
        var defaultCategory = new Category();
        intentFilter.CategoryList.AddElement(defaultCategory);
        defaultCategory.Attributes.Name.Set("android.intent.category.DEFAULT");

        var browsableCategory = new Category();
        browsableCategory.Attributes.Name.Set("android.intent.category.BROWSABLE");
        intentFilter.CategoryList.AddElement(browsableCategory);

        var data = new Data();
        data.Attributes.Scheme.Set(scheme);
        if (!String.IsNullOrEmpty(uri.Host)) {
            data.Attributes.Host.Set(uri.Host);
        }
        if (uri.Port != -1) {
            data.Attributes.Port.Set(uri.Port.ToString());
        }
        if (!String.IsNullOrEmpty(uri.PathAndQuery) && uri.PathAndQuery != "/") {
            data.Attributes.Path.Set(uri.PathAndQuery);
        }
        intentFilter.DataList.AddElement(data);
        activity.IntentFilterList.AddElement(intentFilter);
    }

    private void ReplaceContentOrAddStartsWith(Dependencies dependencies, string prefix, string version) {
        var all = dependencies.GetElements();
        var matching = "implementation '" + prefix;
        var found = all.FirstOrDefault(ele => ele.GetRaw().StartsWith(matching));
        if (found != null) {
            found.SetRaw($"implementation '{prefix}{version}'");
        } else {
            dependencies.AddDependencyImplementationRaw($"'{prefix}{version}'");
        }
    }

    private void CopyAndroidAssetsNew(AndroidProjectFiles projectFiles, UniWebViewEditorSettingsReading settings) {
        var targetPath = Path.Combine(projectFiles.UnityLibraryModule.ModuleDirectoryPath, "src", "main", "assets");
        UniWebViewAndroidAssetsHelper.CopyConfiguredAssets(settings.androidAssetsFolders, targetPath);
    }
}

#else

class UniWebViewPostBuildProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder { get { return 1; } }
    public void OnPostGenerateGradleAndroidProject(string path) {
        Debug.Log("<UniWebView> UniWebView Post Build Script is patching manifest file and gradle file...");
        PatchAndroidManifest(path);
        PatchBuildGradle(path);
        PatchGradleProperty(path);
        CopyAndroidAssets(path);
    }

    private void PatchAndroidManifest(string root) {
        var manifestFilePath = GetManifestFilePath(root);
        var manifest = new UniWebViewAndroidManifest(manifestFilePath);
        
        var changed = false;
        
        Debug.Log("<UniWebView> Set hardware accelerated to enable smooth web view experience and HTML5 support like video and canvas.");
        changed = manifest.SetHardwareAccelerated() || changed;

        var settings = UniWebViewEditorSettings.GetOrCreateSettings();
        if (settings.usesCleartextTraffic) {
            changed = manifest.SetUsesCleartextTraffic() || changed;
        }
        if (settings.writeExternalStorage) {
            changed = manifest.AddWriteExternalStoragePermission() || changed;
        }
        if (settings.accessFineLocation) {
            changed = manifest.AddAccessFineLocationPermission() || changed;
        }
        if (settings.authCallbackUrls.Length > 0) {
            changed = manifest.AddAuthCallbacksIntentFilter(settings.authCallbackUrls) || changed;
        }

        if (settings.supportLINELogin) {
            changed = manifest.AddAuthCallbacksIntentFilter(new string[] { "lineauth://auth" }) || changed;
        }
        
        if (changed) {
            manifest.Save();
        }
    }

    private Version GetVersion(string versionStr)
    {
        if (UniWebViewEditorSettings.TryParseVersion(versionStr, out var version))
        {
            return version;
        }

        return null;
    }

    // Returns true if version >= minVersion
    public static bool IsAtLeastVersion(Version version, Version minVersion)
    {
        if (version == null || minVersion == null)
        {
            return false;
        }

        return version.CompareTo(minVersion) >= 0;
    }

    private void PatchBuildGradle(string root) {
        var gradleFilePath = GetGradleFilePath(root);

        var config = new UniWebViewGradleConfig(gradleFilePath);

        var settings = UniWebViewEditorSettings.GetOrCreateSettings();
        
        var kotlinPrefix = "implementation 'org.jetbrains.kotlin:kotlin-stdlib:";
        var kotlinVersion = String.IsNullOrWhiteSpace(settings.kotlinVersion) 
            ? UniWebViewEditorSettings.defaultKotlinVersion : settings.kotlinVersion;
        var kotlinVersionParsed = GetVersion(kotlinVersion);
        var minKotlinVersion = GetVersion(UniWebViewEditorSettings.defaultKotlinVersion);
        if (IsAtLeastVersion(kotlinVersionParsed, minKotlinVersion) == false)
        {
            Debug.LogWarning($"<UniWebView> Kotlin version {kotlinVersion} is lower than supported minimum {UniWebViewEditorSettings.defaultKotlinVersion}, clamping to {UniWebViewEditorSettings.defaultKotlinVersion}.");
            kotlinVersion = UniWebViewEditorSettings.defaultKotlinVersion;
        }

        var browserPrefix = "implementation 'androidx.browser:browser:";
        var browserVersion = String.IsNullOrWhiteSpace(settings.androidBrowserVersion) 
            ? UniWebViewEditorSettings.defaultAndroidBrowserVersion : settings.androidBrowserVersion;

        var browserVersionInSetting = GetVersion(browserVersion);
        var minBrowserVersionStr = UniWebViewEditorSettings.defaultAndroidBrowserVersion;
        var minBrowserVersion = GetVersion(minBrowserVersionStr);
        if (IsAtLeastVersion(browserVersionInSetting, minBrowserVersion) == false)
        {
            Debug.LogWarning($"<UniWebView> Browser version {browserVersion} is lower than supported minimum {minBrowserVersionStr}, clamping to {minBrowserVersionStr}.");
            browserVersion = minBrowserVersionStr;
            browserVersionInSetting = minBrowserVersion;
        }

        // Browser package >= 1.9.0 needs at least Gradle android plugin 8.9.1. 
        var minBrowserForGradleCheck = new Version(1, 9, 0);
        if (IsAtLeastVersion(browserVersionInSetting, minBrowserForGradleCheck))
        {
            var gradleVersionStr = GetGradleVersion(root);
            if (gradleVersionStr == null)
            {
                Debug.LogWarning($"<UniWebView> Unable to check gradle version, going to let you change browser version to {browserVersion}.");
            }
            else
            {
                var gradleSemanticVersion = GetVersion(gradleVersionStr);
                var minGradleVersion = new Version(8, 9, 1);
                if (IsAtLeastVersion(gradleSemanticVersion, minGradleVersion) == false)
                {
                    Debug.LogWarning($"<UniWebView> Gradle plugin version {gradleVersionStr} is lower than the recommended minimum {minGradleVersion} for androidx.browser >= 1.9.0. Build might fail; consider upgrading AGP.");
                }
            }
        }
        
        var androidXCorePrefix = "implementation 'androidx.core:core:";
        var androidXCoreVersion = String.IsNullOrWhiteSpace(settings.androidXCoreVersion) 
            ? UniWebViewEditorSettings.defaultAndroidXCoreVersion : settings.androidXCoreVersion;
        
        var dependenciesNode = config.Root.FindChildNodeByName("dependencies");
        if (dependenciesNode != null) {
            // Add kotlin
            if (settings.addsKotlin) {
                // remove legacy kotlin stdlib variants to avoid duplicate classes
                dependenciesNode.RemoveContentNode("kotlin-stdlib-jdk7");
                dependenciesNode.RemoveContentNode("kotlin-stdlib-jdk8");
                var kotlinBomPrefix = "implementation platform('org.jetbrains.kotlin:kotlin-bom:";
                dependenciesNode.ReplaceContentOrAddStartsWith(kotlinBomPrefix, kotlinBomPrefix + kotlinVersion + "')");
                dependenciesNode.ReplaceContentOrAddStartsWith(kotlinPrefix, kotlinPrefix + kotlinVersion + "'");
                Debug.Log("<UniWebView> Updated Kotlin dependency and BOM in build.gradle.");
            }

            // Add browser package
            if (settings.addsAndroidBrowser) {
                dependenciesNode.ReplaceContentOrAddStartsWith(browserPrefix, browserPrefix + browserVersion + "'");
                Debug.Log("<UniWebView> Updated Browser dependency in build.gradle.");
            }

            // Add Android X Core package
            if (!settings.addsAndroidBrowser && settings.addsAndroidXCore) {
                // When adding android browser to the project, we don't need to add Android X Core package, since gradle resolves for it.
                dependenciesNode.ReplaceContentOrAddStartsWith(androidXCorePrefix, androidXCorePrefix + androidXCoreVersion + "'");
                Debug.Log("<UniWebView> Updated Android X Core dependency in build.gradle.");
            }
        } else {
            Debug.LogError("UniWebViewPostBuildProcessor didn't find the `dependencies` field in build.gradle.");
            Debug.LogError("Although we can continue to add a `dependencies`, make sure you have setup Gradle and the template correctly.");

            var newNode = new UniWebViewGradleNode("dependencies", config.Root);
            if (settings.addsKotlin) {
                var kotlinBomPrefix = "implementation platform('org.jetbrains.kotlin:kotlin-bom:";
                newNode.AppendContentNode(kotlinBomPrefix + kotlinVersion + "')");
                newNode.AppendContentNode(kotlinPrefix + kotlinVersion + "'");
            }
            if (settings.addsAndroidBrowser) {
                newNode.AppendContentNode(browserPrefix + browserVersion + "'");
            }

            if (settings.addsAndroidXCore) {
                newNode.AppendContentNode(androidXCorePrefix + androidXCoreVersion + "'");
            }
            newNode.AppendContentNode("implementation(name: 'UniWebView', ext:'aar')");
            config.Root.AppendChildNode(newNode);
        }

        if (settings.addsKotlin && !HasResolutionStrategy(config.Root)) {
            // Force Kotlin stdlib versions to avoid duplicate classes pulled by mixed transitive deps.
            var resolutionBlock =
                "configurations.all {\n" +
                "    resolutionStrategy {\n" +
                "        force 'org.jetbrains.kotlin:kotlin-stdlib:" + kotlinVersion + "',\n" +
                "              'org.jetbrains.kotlin:kotlin-stdlib-jdk7:" + kotlinVersion + "',\n" +
                "              'org.jetbrains.kotlin:kotlin-stdlib-jdk8:" + kotlinVersion + "',\n" +
                "              'org.jetbrains.kotlin:kotlin-stdlib-common:" + kotlinVersion + "'\n" +
                "    }\n" +
                "}";
            config.Root.AppendChildNode(new UniWebViewGradleContentNode(resolutionBlock, config.Root));
        }
        config.Save();
    }

    private bool HasResolutionStrategy(UniWebViewGradleNode root)
    {
        var found = false;
        root.Each(node =>
        {
            if (node is UniWebViewGradleContentNode content && content.Name.Contains("resolutionStrategy"))
            {
                found = true;
            }
        });
        return found;
    }

    private void PatchGradleProperty(string root) {
        var gradlePropertyFilePath = GetGradlePropertyFilePath(root);
        var patcher =
            new UniWebViewGradlePropertyPatcher(gradlePropertyFilePath, UniWebViewEditorSettings.GetOrCreateSettings());
        patcher.Patch();
    }

    private string CombinePaths(string[] paths) {
        var path = "";
        foreach (var item in paths) {
            path = Path.Combine(path, item);
        }
        return path;
    }

    private string GetManifestFilePath(string root) {
        string[] comps = {root, "src", "main", "AndroidManifest.xml"};
        return CombinePaths(comps);
    }

    private string GetGradleFilePath(string root) {
        string[] comps = {root, "build.gradle"};
        return CombinePaths(comps);
    }

    private string GetGradlePropertyFilePath(string root) {
        #if UNITY_2019_3_OR_NEWER
        string[] compos = {root, "..", "gradle.properties"};
        #else
        string[] compos = {root, "gradle.properties"};
        #endif
        return CombinePaths(compos);
    }

    private string GetGradleVersion(string pathToBuiltProject)
    {
        var rootBuildGradle = Path.Combine(pathToBuiltProject, "..", "build.gradle");
        if (!File.Exists(rootBuildGradle))
        {
            Debug.LogError("Gradle root build file not found at: " + rootBuildGradle);
            return null;
        }

        var content = File.ReadAllText(rootBuildGradle);

        // Prefer the plugins {} block syntax (Unity 2022+ templates)
        var pluginMatch = Regex.Match(
            content,
            @"id\s+['""]com\.android\.(?:application|library)['""]\s+version\s+['""](?<version>[0-9]+\.[0-9]+(?:\.[0-9]+)?(?:-[^'""]+)?)['""]",
            RegexOptions.IgnoreCase);
        if (pluginMatch.Success)
        {
            var version = pluginMatch.Groups["version"].Value;
            Debug.Log("<UniWebView> Detected Android Gradle Plugin version (plugins block): " + version);
            return version;
        }

        // Fallback to buildscript classpath declaration (Unity 2021 and below)
        var classpathMatch = Regex.Match(
            content,
            @"classpath\s+['""]com\.android\.tools\.build:gradle:(?<version>[0-9]+\.[0-9]+(?:\.[0-9]+)?(?:-[^'""]+)?)['""]",
            RegexOptions.IgnoreCase);
        if (classpathMatch.Success)
        {
            var version = classpathMatch.Groups["version"].Value;
            Debug.Log("<UniWebView> Detected Android Gradle Plugin version (buildscript classpath): " + version);
            return version;
        }

        Debug.LogWarning("<UniWebView> Could not detect Android Gradle Plugin version from " + rootBuildGradle);
        return null;
    }

    private void CopyAndroidAssets(string projectPath) {
        var settings = UniWebViewEditorSettings.GetOrCreateSettings();
        var targetPath = Path.Combine(projectPath, "src", "main", "assets");
        UniWebViewAndroidAssetsHelper.CopyConfiguredAssets(settings.androidAssetsFolders, targetPath);
    }
}
#endif
