using System;
using UnityEngine;

[System.Serializable]
public class GraphicsConfigData
{
    [Header("FPS")]
    [Tooltip("Target frame rate. Common values: 30 (battery saver), 60 (balanced), 120 (smooth)")]
    public int targetFPS = 60;

    [Header("Render")]
    [Tooltip("Resolution scale relative to screen size. 0.75 = performance, 1.0 = native")]
    [Range(0.5f, 1.0f)]
    public float renderScale = 1.0f;

    [Tooltip("MSAA sample count. 1 = off, 2 = low, 4 = medium, 8 = high. Higher = more GPU cost")]
    public int msaaSampleCount = 2;

    [Tooltip("High Dynamic Range rendering. Improves color depth, disable on low-end devices")]
    public bool hdrEnabled = true;

    [Header("Shadow")]
    [Tooltip("Disable shadows entirely for maximum performance on low-end devices")]
    public bool shadowEnabled = true;

    [Tooltip("Max distance (meters) at which shadows are rendered. Lower = better performance")]
    [Range(10f, 150f)]
    public float shadowDistance = 50f;

    [Tooltip("Shadow map resolution. Higher = sharper shadows, more GPU/memory usage")]
    public ShadowResolution shadowResolution = ShadowResolution.Medium;

    [Tooltip("Number of shadow cascade splits. 0 = off, 2 = balanced, 4 = best quality")]
    public int shadowCascades = 2;

    [Header("Texture")]
    [Tooltip("Mipmap reduction level. 0 = full resolution, 1 = half, 2 = quarter")]
    public int textureMipmapLimit = 0;

    [Tooltip("Anisotropic filtering improves texture clarity at angles. Disable to save GPU")]
    public AnisotropicFiltering anisotropicFiltering = AnisotropicFiltering.Enable;

    [Header("LOD")]
    [Tooltip("LOD switch distance bias. 1.0 = default, lower = switch to lower LOD sooner (better perf)")]
    [Range(0.1f, 2.0f)]
    public float lodBias = 1.0f;

    [Tooltip("Minimum LOD level to use. 0 = highest detail, 1+ = skip top LOD levels")]
    public int maxLODLevel = 0;
}
public enum GraphicsPreset { Low, Medium, High, Ultra }