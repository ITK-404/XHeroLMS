using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class TutorialSetting
{
    [Range(0, 0.95f)] public float Strength;
    [Range(0, 1f)] public float Radius = 0.1f;
    public Vector2 Center = new Vector2(0.5f, 0.5f);
}
public class TutorialRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Shader shader;
    private Material material;
    [SerializeField] private TutorialSetting setting;
    private TutorialRenderPass tutorialRenderPass;
    public override void Create()
    {
        if (shader == null) return;
        material = new Material(shader);
        tutorialRenderPass = new TutorialRenderPass(material, setting);
        tutorialRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (material == null) return;

        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(tutorialRenderPass);
        }
    }

    // 2 method đơn giản, sửa trực tiếp lên setting (class = reference)
    public void SetRadius(float value)
    {
        setting.Radius = Mathf.Clamp01(value);
    }

    public void SetCenter(Vector2 value)
    {
        setting.Center = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (Application.isPlaying)
            Destroy(material);
        else
            DestroyImmediate(material);
        base.Dispose(disposing);
    }
}