using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;

public static class GraphicsApplier
{
    public static void Apply(GraphicsConfigData config)
    {
        // FPS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = config.targetFPS;

        // Render Scale (URP)
        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
            urpAsset.renderScale = config.renderScale;

        // Shadow
        if (config.shadowEnabled)
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = config.shadowDistance;
        }
        else
        {
            QualitySettings.shadows = ShadowQuality.Disable;
        }

        // Texture
        QualitySettings.globalTextureMipmapLimit = config.textureMipmapLimit;
    }
}