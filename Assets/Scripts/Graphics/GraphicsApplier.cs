using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;

public static class GraphicsApplier
{
    public static void Apply(GraphicsConfigData config)
    {
        // FPS
        // Application.targetFrameRate = config.targetFPS;

        // Render Scale (URP)
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            urpAsset.renderScale = config.renderScale;
            // urpAsset.msaaSampleCount = config.msaaSampleCount;
            // urpAsset.supportsHDR = config.hdrEnabled;
        }

        // Shadow
        if (config.shadowEnabled)
        {
            QualitySettings.shadows = ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = config.shadowDistance;
            QualitySettings.shadowResolution = config.shadowResolution;
            QualitySettings.shadowCascades = config.shadowCascades;
        }
        else
        {
            QualitySettings.shadows = ShadowQuality.Disable;
        }

        // Texture
        // QualitySettings.globalTextureMipmapLimit = config.textureMipmapLimit;
        // QualitySettings.anisotropicFiltering = config.anisotropicFiltering;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        // LOD
        QualitySettings.lodBias = config.lodBias;
        QualitySettings.maximumLODLevel = config.maxLODLevel;
    }

    public static void ApplyFps(int fps)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
    }
}