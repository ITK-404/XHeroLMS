#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class XHeroAddressablesBatchBuild
{
    public static void BuildDevAndroidGlesVulkanAndUpload()
    {
        BuildDevAndroidAddressablesWithGlesFirst();
    }

    public static void BuildDevAndroidVulkanGlesAndUpload()
    {
        BuildDevAndroidAddressablesWithGlesFirst();
    }

    private static void BuildDevAndroidAddressablesWithGlesFirst()
    {
        Debug.Log("[XHeroAddressablesBatchBuild] Build Dev Android Addressables with GLES3 first + Vulkan fallback.");

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            bool switched = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            if (!switched)
            {
                Debug.LogError("[XHeroAddressablesBatchBuild] Cannot switch active build target to Android.");
                EditorApplication.Exit(1);
                return;
            }
        }

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.Android,
            new[] { GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.Vulkan });

        AssetDatabase.SaveAssets();

        AddressablesCloudAutoSetup.SaveEnvironmentMode(EnvironmentMode.Dev);
        AddressablesCloudAutoSetup.EnsureAddressablesSetup(EnvironmentMode.Dev);
        AddressablesCloudAutoSetup.WriteRuntimeBuildEnv(
            EnvironmentMode.Dev,
            AddressablesCloudAutoSetup.GetSavedDevApiEnvironment());
        AddressablesCloudAutoSetup.BuildAndUpload(EnvironmentMode.Dev);
    }
}
#endif
