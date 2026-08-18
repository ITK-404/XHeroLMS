using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public class TutorialRenderPass : ScriptableRenderPass
{
    private readonly Material material;
    private TutorialSetting setting;

    private const string TutorialRenderPassName = "TutorialRenderSRP";
    private const string TutorialRenderTextureName = "TutorialTexture";

    private static readonly int StrengthId = Shader.PropertyToID("_Strength");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int CenterId = Shader.PropertyToID("_Center");

    private TextureDesc tutorialRenderDesc;

    public TutorialRenderPass(Material material, TutorialSetting setting)
    {
        this.material = material;
        this.setting = setting;
    }

    // 1 method duy nhất để cập nhật cả setting
    public void SetSetting(TutorialSetting newSetting)
    {
        setting = newSetting;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourcesData = frameData.Get<UniversalResourceData>();

        if (resourcesData.isActiveTargetBackBuffer)
        {
            return;
        }

        var srcCamColor = resourcesData.activeColorTexture;
        tutorialRenderDesc = srcCamColor.GetDescriptor(renderGraph);
        tutorialRenderDesc.name = TutorialRenderTextureName;
        tutorialRenderDesc.depthBufferBits = 0;
        var dst = renderGraph.CreateTexture(tutorialRenderDesc);

        if (!srcCamColor.IsValid() || !dst.IsValid())
        {
            return;
        }

        // Đọc trực tiếp từ setting, không cần field riêng
        material.SetFloat(StrengthId, setting.Strength);
        material.SetFloat(RadiusId, setting.Radius);
        material.SetVector(CenterId, new Vector4(setting.Center.x, setting.Center.y, 0, 0));

        RenderGraphUtils.BlitMaterialParameters blitParams = new(srcCamColor, dst, material, 0);
        renderGraph.AddBlitPass(blitParams, TutorialRenderPassName);

        resourcesData.cameraColor = dst;
        base.RecordRenderGraph(renderGraph, frameData);
    }
}