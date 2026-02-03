#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

public static class AddressablesBuildHelper
{
    [MenuItem("Tools/Addressables/Build Player Content (Force)")]
    public static void Build()
    {
        var s = AddressableAssetSettingsDefaultObject.Settings;
        if (s == null)
        {
            UnityEngine.Debug.LogError("[Addr] AddressableAssetSettingsDefaultObject.Settings = NULL (Settings bị mất/corrupt).");
            return;
        }

        AddressableAssetSettings.BuildPlayerContent();
        UnityEngine.Debug.Log("[Addr] BuildPlayerContent DONE");
    }
}
#endif
