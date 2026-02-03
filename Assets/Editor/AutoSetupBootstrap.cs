#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class AutoSetupBootstrap
{
    private const string BootstrapGroupName = "Local_Bootstrap";

    // Built-in (StreamingAssets) cho bootstrap
    private const string LocalBuildPathValue =
        "{UnityEngine.AddressableAssets.Addressables.BuildPath}/aa/[BuildTarget]";
    private const string LocalLoadPathValue =
        "{UnityEngine.AddressableAssets.Addressables.StreamingAssetsPath}/aa/[BuildTarget]";

    [MenuItem("Tools/Addressables/Auto Setup Bootstrap (ONLY Local_Bootstrap Built-in)")]
    public static void Run()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[AutoSetupBootstrap] AddressableAssetSettingsDefaultObject.Settings is null. Open Addressables Groups once and try again.");
            return;
        }

        // 1) Ensure ONLY Local vars (do not touch remote)
        EnsureProfileVarIfMissingOrEmpty(settings, "LocalBuildPath", LocalBuildPathValue, overwriteIfExists:false);
        EnsureProfileVarIfMissingOrEmpty(settings, "LocalLoadPath",  LocalLoadPathValue,  overwriteIfExists:false);

        // 2) Find/create bootstrap group
        var group = settings.FindGroup(BootstrapGroupName);
        if (group == null)
        {
            group = settings.CreateGroup(
                BootstrapGroupName,
                false,  // readOnly
                false,  // postEvent
                false,  // setAsDefault
                null,
                typeof(BundledAssetGroupSchema),
                typeof(ContentUpdateGroupSchema)
            );
            Debug.Log($"[AutoSetupBootstrap] Created group: {BootstrapGroupName}");
        }

        // 3) Ensure schemas exist
        var bund = group.GetSchema<BundledAssetGroupSchema>();
        if (bund == null) bund = group.AddSchema<BundledAssetGroupSchema>();

        var cu = group.GetSchema<ContentUpdateGroupSchema>();
        if (cu == null) cu = group.AddSchema<ContentUpdateGroupSchema>();

        // 4) Force bootstrap group to StreamingAssets (Built-in style)
        bund.BuildPath.SetVariableByName(settings, "LocalBuildPath");
        bund.LoadPath.SetVariableByName(settings, "LocalLoadPath");

        // Recommended for bootstrap
        bund.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;

        // 5) Persist
        EditorUtility.SetDirty(bund);
        EditorUtility.SetDirty(group);
        EditorUtility.SetDirty(settings);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            "[AutoSetupBootstrap] Done.\n" +
            "- Did NOT modify RemoteLoadPath/RemoteBuildPath\n" +
            "- Did NOT change active profile\n" +
            "Next: Addressables Groups -> Build -> New Build -> Default Build Script, then build APK."
        );
    }

    /// <summary>
    /// Create var if missing. If exists but empty, set it. By default DOES NOT overwrite non-empty values.
    /// </summary>
    private static void EnsureProfileVarIfMissingOrEmpty(
        AddressableAssetSettings settings,
        string varName,
        string value,
        bool overwriteIfExists)
    {
        var ps = settings.profileSettings;
        if (ps == null)
        {
            Debug.LogError("[AutoSetupBootstrap] profileSettings is null.");
            return;
        }

        var profileId = settings.activeProfileId;

        string current = null;
        try { current = ps.GetValueByName(profileId, varName); } catch { /* ignore */ }

        bool hasVar = false;
        try { hasVar = ps.GetVariableNames().Contains(varName); } catch { /* ignore */ }

        if (!hasVar)
        {
            try
            {
                ps.CreateValue(varName, value);
                Debug.Log($"[AutoSetupBootstrap] Created profile var: {varName} = {value}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AutoSetupBootstrap] CreateValue failed for '{varName}': {e.Message}");
            }
        }

        // Set only if empty OR overwrite requested
        if (overwriteIfExists || string.IsNullOrEmpty(current))
        {
            try
            {
                ps.SetValue(profileId, varName, value);
                Debug.Log($"[AutoSetupBootstrap] Set profile var (active): {varName} = {value}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[AutoSetupBootstrap] SetValue failed for '{varName}': {e}");
            }
        }
        else
        {
            Debug.Log($"[AutoSetupBootstrap] Kept existing var (active): {varName} = {current}");
        }
    }
}
#endif
