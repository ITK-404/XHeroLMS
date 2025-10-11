using UnityEngine;
using UnityEngine.Rendering.Universal;

public class QualityManager : MonoBehaviour {
    public enum Preset { Low, Medium, High, Ultra }
    public Preset currentPreset = Preset.Medium;

    public void ApplyQuality(Preset p){
        currentPreset = p;
        switch(p){
            case Preset.Low:
                QualitySettings.shadowDistance = 40;
                QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                UniversalRenderPipeline.asset.renderScale = 0.8f;
                RenderSettings.fog = false;
                PostProcessingOff();
                break;

            case Preset.Medium:
                QualitySettings.shadowDistance = 60;
                UniversalRenderPipeline.asset.renderScale = 1.0f;
                PostProcessingOnMinimal();
                break;

            case Preset.High:
                QualitySettings.shadowDistance = 100;
                UniversalRenderPipeline.asset.renderScale = 1.0f;
                PostProcessingOnFull();
                break;
        }
    }

    void PostProcessingOnMinimal(){ /* enable bloom/fxaa */ }
    void PostProcessingOnFull(){ /* enable ao, bloom, vignette */ }
    void PostProcessingOff(){ /* disable all post-volume */ }
}

