#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GPUInstancerPro;
using GPUInstancerPro.PrefabModule;
using HTraceSSGI.Scripts.Globals;
using HTraceSSGI.Scripts.Infrastructure.URP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class XHeroMobileSceneOptimizer
{
    private const string DefaultScene = "Assets/Scenes/New Scene.unity";
    private const string MobileRpAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
    private const string MobileRendererPath = "Assets/Settings/Mobile_Renderer.asset";
    private const string SceneLookProfilePath = "Assets/Settings/XHero_Mobile_CinematicLook.asset";
    private const string GpuiMobileProfilePath = "Assets/Settings/XHero_GPUI_Mobile_Foliage_Profile.asset";
    private const string NewSceneMobileLightingSettingsPath = "Assets/Settings/XHero_NewScene_MobileLighting.lighting";
    private const string FullProjectReportPath = "XHero_Mobile_Full_Optimization_Report.md";

    private static readonly string[] AutoGpuiPrototypePrefabPaths =
    {
        "Assets/Models_LMS_Mobile/Luat/tree_sen/ghep/Hoa Sen Group.prefab",
        "Assets/Prefabs/Models/Bon Cay Sanh.prefab",
        "Assets/Prefabs/Models/Tree/boncay sau.prefab",
        "Assets/Prefabs/Cay Co Bon Hoa.prefab"
    };

    private static readonly string[] LogicComponentNameHints =
    {
        "Handler", "Controller", "Trigger", "Interact", "Load", "Door", "NPC", "Quest", "Video",
        "Timeline", "Cinemachine", "Astar", "Path", "Button", "UI"
    };

    private static readonly string[] GpuiSafePathHints =
    {
        "tree", "cay", "sen", "bui", "co", "grass", "hoa", "flower", "leaf", "la", "rock", "stone",
        "foliage", "decor", "decoration", "prop", "trung_bay", "boncay", "khuon_vien"
    };

    private static readonly string[] GpuiUnsafePathHints =
    {
        "nha_t1", "/nha", "\\nha", "house", "player", "ui", "minimap", "trigger", "door", "room",
        "npc", "quest", "video", "webview", "canvas"
    };

    [MenuItem("Tools/XHero LMS/Optimization/Generate Current Scene Report")]
    public static void GenerateCurrentSceneReportMenu()
    {
        var scene = SceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(scene))
            scene = DefaultScene;

        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../XHero_Scene_Optimization_Report.md"));
        GenerateReport(new[] { scene }, outPath, "manual");
        EditorUtility.RevealInFinder(outPath);
    }

    [MenuItem("Tools/XHero LMS/Optimization/Apply Safe Mobile Look To Current Scene")]
    public static void ApplySafeMobileLookMenu()
    {
        ApplySafeMobileQualityPass(new[] { SceneManager.GetActiveScene().path }, null);
    }

    [MenuItem("Tools/XHero LMS/Optimization/ONE CLICK - Stable Mobile Repair + Optimize")]
    public static void ApplyOneClickMobileProjectOptimizationMenu()
    {
        var scenes = GetAllProjectScenePaths();
        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + FullProjectReportPath));
        ApplyStableMobileRepairPass(scenes, outPath);
        EditorUtility.RevealInFinder(outPath);
    }

    [MenuItem("Tools/XHero LMS/Optimization/ONE CLICK - New Scene Lighting + Sharp Mobile Shadows")]
    public static void ApplyNewSceneLightingQualityMenu()
    {
        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../XHero_NewScene_Lighting_Quality_Report.md"));
        ApplyMobileLightingQualityPass(new[] { DefaultScene }, outPath, false);
        EditorUtility.RevealInFinder(outPath);
    }

    public static void GenerateReportBatch()
    {
        var scenes = GetSceneArgs();
        var output = GetArg("-xheroOut");
        if (string.IsNullOrEmpty(output))
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../XHero_Scene_Optimization_Report.md"));

        GenerateReport(scenes, output, "batch");
    }

    public static void ApplySafeMobileQualityPassBatch()
    {
        var scenes = GetSceneArgs();
        var output = GetArg("-xheroOut");
        ApplySafeMobileQualityPass(scenes, output);
    }

    public static void ApplyFullMobileOptimizationPassBatch()
    {
        var scenes = GetSceneArgs();
        var output = GetArg("-xheroOut");
        ApplyFullMobileOptimizationPass(scenes, output);
    }

    public static void ApplyStableMobileRepairPassBatch()
    {
        var scenes = GetSceneArgs();
        var output = GetArg("-xheroOut");
        ApplyStableMobileRepairPass(scenes, output);
    }

    public static void ApplyMobileLightingQualityPassBatch()
    {
        var scenes = GetSceneArgs();
        var output = GetArg("-xheroOut");
        var bakeArg = GetArg("-xheroBake");
        var bakeNow = string.Equals(bakeArg, "true", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(bakeArg, "1", StringComparison.OrdinalIgnoreCase);
        ApplyMobileLightingQualityPass(scenes, output, bakeNow);
    }

    private static string[] GetSceneArgs()
    {
        var sceneArg = GetArg("-xheroScenes");
        if (string.IsNullOrWhiteSpace(sceneArg))
            return new[] { DefaultScene };

        return sceneArg
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().Replace('\\', '/'))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] GetAllProjectScenePaths()
    {
        var scenesRoot = Path.Combine(Application.dataPath, "Scenes");
        if (!Directory.Exists(scenesRoot))
            return new[] { DefaultScene };

        return Directory.GetFiles(scenesRoot, "*.unity", SearchOption.AllDirectories)
            .Select(path => "Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/'))
            .Where(path => !path.Contains("/HTraceSSGI/", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetArg(string name)
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        }

        return null;
    }

    private static void ApplySafeMobileQualityPass(string[] scenePaths, string outputPath)
    {
        var changes = new List<string>();

        changes.AddRange(ApplyProjectMobileSettings());
        changes.AddRange(ApplyMobileRendererSettings());
        changes.AddRange(ApplyMobileRpAssetSettings());

        foreach (var scenePath in scenePaths)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            changes.AddRange(ApplySceneLook(scene));
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(outputPath))
        {
            GenerateReport(scenePaths, outputPath, "after safe mobile pass", changes);
        }
        else
        {
            Debug.Log("[XHeroMobileSceneOptimizer] Applied safe mobile quality pass:\n" + string.Join("\n", changes));
        }
    }

    private static void ApplyFullMobileOptimizationPass(string[] scenePaths, string outputPath)
    {
        var changes = new List<string>();

        changes.AddRange(ApplyProjectMobileSettings(true));
        changes.AddRange(ApplyMobileRendererSettings(true));
        changes.AddRange(ApplyMobileRpAssetSettings(true));

        foreach (var scenePath in scenePaths)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            changes.Add($"Scene '{scenePath}':");
            changes.AddRange(ApplySceneLook(scene, true));
            changes.AddRange(ApplyTerrainMobileSettings(scene));
            changes.AddRange(ApplyRendererMobileCulling(scene));
            changes.AddRange(ApplyGpuiPrefabOptimization(scene));
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(outputPath))
        {
            GenerateReport(scenePaths, outputPath, "after full HTrace + GPUI mobile pass", changes);
        }
        else
        {
            Debug.Log("[XHeroMobileSceneOptimizer] Applied full mobile optimization pass:\n" + string.Join("\n", changes));
        }
    }

    private static void ApplyStableMobileRepairPass(string[] scenePaths, string outputPath)
    {
        var changes = new List<string>
        {
            "Stable recovery pass: HTrace, GPUI prefab rendering, HDR/post stack, and bulk material instancing are disabled by default until each group is visually verified."
        };

        changes.AddRange(ApplyProjectMobileSettings());
        changes.AddRange(ApplyStableMobileRpAssetSettings());
        changes.AddRange(ApplyStableMobileRendererSettings());
        changes.AddRange(RemoveAutoGpuiPrefabComponents());

        foreach (var scenePath in scenePaths)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            changes.Add($"Scene '{scenePath}':");
            changes.AddRange(ApplyStableSceneRendering(scene));
            changes.AddRange(DisableGpuiPrefabRendering(scene));
            changes.AddRange(DisableRiskyMaterialInstancing(scene));
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(outputPath))
        {
            GenerateReport(scenePaths, outputPath, "after stable mobile repair pass", changes);
        }
        else
        {
            Debug.Log("[XHeroMobileSceneOptimizer] Applied stable mobile repair pass:\n" + string.Join("\n", changes));
        }
    }

    private static void ApplyMobileLightingQualityPass(string[] scenePaths, string outputPath, bool bakeNow)
    {
        var changes = new List<string>
        {
            "Mobile lighting quality pass: better main-light shadows, mixed lighting bake prep, HTrace/GPUI remain disabled."
        };

        changes.AddRange(ApplyProjectMobileLightingQualitySettings());
        changes.AddRange(ApplyMobileLightingQualityRpAssetSettings());
        changes.AddRange(ApplyStableMobileRendererSettings());

        foreach (var scenePath in scenePaths)
        {
            if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
                continue;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            changes.Add($"Scene '{scenePath}':");
            changes.AddRange(ApplyMobileQualityLighting(scene));
            changes.AddRange(PrepareStaticEnvironmentForMobileBake(scene));
            changes.AddRange(OptimizeMobileShadowCasters(scene));
            EditorSceneManager.SaveScene(scene);

            if (bakeNow)
                changes.AddRange(TryBakeActiveSceneLighting(scene));
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(outputPath))
        {
            GenerateReport(scenePaths, outputPath, bakeNow ? "after mobile lighting quality pass + bake attempt" : "after mobile lighting quality pass", changes);
        }
        else
        {
            Debug.Log("[XHeroMobileSceneOptimizer] Applied mobile lighting quality pass:\n" + string.Join("\n", changes));
        }
    }

    private static IEnumerable<string> ApplyProjectMobileSettings(bool aggressive = false)
    {
        var changes = new List<string>();

        var current = QualitySettings.GetQualityLevel();
        if (current != 0)
        {
            QualitySettings.SetQualityLevel(0, false);
            changes.Add("Set active quality level to Mobile (index 0).");
        }

        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 2;
        QualitySettings.shadowDistance = aggressive ? 28f : 35f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowResolution = UnityEngine.ShadowResolution.Medium;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.streamingMipmapsActive = true;
        QualitySettings.lodBias = aggressive ? 0.75f : 1f;
        QualitySettings.maximumLODLevel = 0;

        PlayerSettings.use32BitDisplayBuffer = true;

        changes.Add(aggressive
            ? "Mobile quality/performance tuned: 2x MSAA, 28m shadows, 2 cascades, LOD bias 0.75, streaming mipmaps."
            : "Mobile quality tuned: 2x MSAA, 35m shadows, 2 cascades, display buffer 32-bit.");
        return changes;
    }

    private static IEnumerable<string> ApplyMobileRpAssetSettings(bool aggressive = false)
    {
        var changes = new List<string>();
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileRpAssetPath);
        if (!asset)
            return new[] { "Mobile_RPAsset not found; skipped URP asset tuning." };

        var so = new SerializedObject(asset);
        SetBool(so, "m_UseSRPBatcher", true, changes, "SRP Batcher enabled.");
        SetBool(so, "m_MixedLightingSupported", true, changes, "Mixed lighting support enabled.");
        SetBool(so, "m_RequireDepthTexture", true, changes, "Depth texture kept on for SSAO/post.");
        SetBool(so, "m_RequireOpaqueTexture", true, changes, "Opaque texture kept on because project already uses screen/decal effects.");
        SetBool(so, "m_SupportsHDR", true, changes, "HDR enabled on Mobile RP asset for ACES/bloom quality.");
        SetInt(so, "m_MSAA", 2, changes, "MSAA kept at 2x.");
        SetFloat(so, "m_RenderScale", aggressive ? 0.75f : 0.8f, changes, aggressive ? "Render scale set to 0.75 for mobile FPS headroom." : "Render scale kept at 0.80 for mobile performance.");
        SetInt(so, "m_MainLightShadowmapResolution", 1024, changes, "Main light shadowmap set to 1024 for cleaner outdoor shadows.");
        SetFloat(so, "m_ShadowDistance", aggressive ? 28f : 35f, changes, aggressive ? "URP shadow distance set to 28m for mobile cost control." : "URP shadow distance set to 35m for mobile cost control.");
        SetInt(so, "m_ShadowCascadeCount", 2, changes, "URP shadow cascades kept at 2.");
        SetFloat(so, "m_Cascade2Split", 0.28f, changes, "2-cascade split adjusted to 0.28 for stronger near shadows.");
        SetInt(so, "m_AdditionalLightsPerObjectLimit", aggressive ? 1 : 2, changes, aggressive ? "Additional lights per object limited to 1 for mobile." : "Additional lights per object limited to 2 for mobile.");
        SetBool(so, "m_AdditionalLightShadowsSupported", false, changes, "Additional light shadows kept disabled on mobile.");
        SetBool(so, "m_SoftShadowsSupported", true, changes, "Soft shadows kept enabled.");
        SetInt(so, "m_SoftShadowQuality", 1, changes, "Soft shadow quality set to Low/fast.");
        SetBool(so, "m_SupportsDynamicBatching", false, changes, "Dynamic batching disabled to favor SRP Batcher.");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return changes;
    }

    private static IEnumerable<string> ApplyProjectMobileLightingQualitySettings()
    {
        var changes = new List<string>();

        var current = QualitySettings.GetQualityLevel();
        if (current != 0)
        {
            QualitySettings.SetQualityLevel(0, false);
            changes.Add("Set active quality level to Mobile (index 0).");
        }

        QualitySettings.vSyncCount = 0;
        QualitySettings.antiAliasing = 2;
        QualitySettings.shadowDistance = 28f;
        QualitySettings.shadowCascades = 2;
        QualitySettings.shadowResolution = UnityEngine.ShadowResolution.High;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.streamingMipmapsActive = true;
        QualitySettings.lodBias = 1f;
        QualitySettings.maximumLODLevel = 0;

        PlayerSettings.use32BitDisplayBuffer = true;

        changes.Add("Mobile quality lighting tuned: 2x MSAA, 28m shadow distance, 2 cascades, high main shadow quality target.");
        return changes;
    }

    private static IEnumerable<string> ApplyStableMobileRpAssetSettings()
    {
        var changes = new List<string>();
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileRpAssetPath);
        if (!asset)
            return new[] { "Mobile_RPAsset not found; skipped stable URP repair." };

        var so = new SerializedObject(asset);
        SetBool(so, "m_UseSRPBatcher", true, changes, "SRP Batcher enabled.");
        SetBool(so, "m_MixedLightingSupported", true, changes, "Mixed lighting support enabled.");
        SetBool(so, "m_RequireDepthTexture", true, changes, "Depth texture restored to project baseline.");
        SetBool(so, "m_RequireOpaqueTexture", true, changes, "Opaque texture restored to project baseline.");
        SetBool(so, "m_SupportsHDR", false, changes, "HDR disabled for stable mobile baseline and lower GPU cost.");
        SetInt(so, "m_MSAA", 2, changes, "MSAA kept at 2x.");
        SetFloat(so, "m_RenderScale", 0.8f, changes, "Render scale restored to 0.80 stable baseline.");
        SetInt(so, "m_MainLightShadowmapResolution", 1024, changes, "Main light shadowmap kept at 1024.");
        SetFloat(so, "m_ShadowDistance", 35f, changes, "URP shadow distance restored to 35m stable baseline.");
        SetInt(so, "m_ShadowCascadeCount", 2, changes, "URP shadow cascades kept at 2.");
        SetFloat(so, "m_Cascade2Split", 0.28f, changes, "2-cascade split kept at 0.28.");
        SetInt(so, "m_AdditionalLightsPerObjectLimit", 2, changes, "Additional lights per object restored to 2.");
        SetBool(so, "m_AdditionalLightShadowsSupported", false, changes, "Additional light shadows disabled on mobile.");
        SetBool(so, "m_SoftShadowsSupported", true, changes, "Soft shadows kept enabled.");
        SetInt(so, "m_SoftShadowQuality", 1, changes, "Soft shadow quality kept Low.");
        SetBool(so, "m_SupportsDynamicBatching", false, changes, "Dynamic batching disabled to favor SRP Batcher.");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return changes;
    }

    private static IEnumerable<string> ApplyMobileLightingQualityRpAssetSettings()
    {
        var changes = new List<string>();
        var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileRpAssetPath);
        if (!asset)
            return new[] { "Mobile_RPAsset not found; skipped quality shadow tuning." };

        var so = new SerializedObject(asset);
        SetBool(so, "m_UseSRPBatcher", true, changes, "SRP Batcher enabled.");
        SetBool(so, "m_MixedLightingSupported", true, changes, "Mixed lighting support enabled for baked/static depth.");
        SetBool(so, "m_RequireDepthTexture", true, changes, "Depth texture kept for project camera effects.");
        SetBool(so, "m_RequireOpaqueTexture", true, changes, "Opaque texture kept for project screen/decal effects.");
        SetBool(so, "m_SupportsHDR", false, changes, "HDR kept disabled for mobile stability.");
        SetInt(so, "m_MSAA", 2, changes, "MSAA kept at 2x.");
        SetFloat(so, "m_RenderScale", 0.8f, changes, "Render scale kept at 0.80.");
        SetInt(so, "m_MainLightShadowmapResolution", 2048, changes, "Main light shadowmap raised to 2048 to reduce blocky shadows.");
        SetFloat(so, "m_ShadowDistance", 28f, changes, "Shadow distance reduced to 28m so 2048 shadows stay sharp without raising batches.");
        SetInt(so, "m_ShadowCascadeCount", 2, changes, "URP shadow cascades kept at 2 for mobile.");
        SetFloat(so, "m_Cascade2Split", 0.22f, changes, "2-cascade split set to 0.22 for a sharper near-player cascade.");
        SetInt(so, "m_AdditionalLightsPerObjectLimit", 1, changes, "Additional lights per object limited to 1.");
        SetBool(so, "m_AdditionalLightShadowsSupported", false, changes, "Additional light shadows disabled to keep GPU cost stable.");
        SetBool(so, "m_SoftShadowsSupported", true, changes, "Soft shadows enabled.");
        SetInt(so, "m_SoftShadowQuality", 2, changes, "Soft shadow quality set to Medium.");
        SetBool(so, "m_SupportsDynamicBatching", false, changes, "Dynamic batching disabled to favor SRP Batcher/static batching.");

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        return changes;
    }

    private static IEnumerable<string> ApplyMobileRendererSettings(bool enableHTrace = false)
    {
        var changes = new List<string>();
        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(MobileRendererPath);
        if (!renderer)
            return new[] { "Mobile_Renderer not found; skipped renderer feature tuning." };

        var rendererSo = new SerializedObject(renderer);

        var features = rendererSo.FindProperty("m_RendererFeatures");
        var hasSsao = false;
        var hasHTrace = false;
        if (features != null)
        {
            for (var i = 0; i < features.arraySize; i++)
            {
                var feature = features.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                if (feature != null && feature.GetType().Name.IndexOf("ScreenSpaceAmbientOcclusion", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hasSsao = true;
                    if (enableHTrace)
                        SetFeatureActive(feature, false, changes, "Disabled URP SSAO because HTrace SSGI now provides the mobile screen-space depth pass.");
                    else
                        ConfigureSsao(feature, changes);
                }

                if (feature is HTraceSSGIRendererFeature htraceFeature)
                {
                    hasHTrace = true;
                    ConfigureHTraceRendererFeature(htraceFeature, changes);
                }
            }
        }

        if (enableHTrace && !hasHTrace)
        {
            var htrace = ScriptableObject.CreateInstance<HTraceSSGIRendererFeature>();
            htrace.name = "HTraceSSGI_MobileLow";
            AssetDatabase.AddObjectToAsset(htrace, renderer);
            ConfigureHTraceRendererFeature(htrace, changes);

            rendererSo.Update();
            features = rendererSo.FindProperty("m_RendererFeatures");
            if (features != null)
            {
                features.InsertArrayElementAtIndex(features.arraySize);
                features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = htrace;
                changes.Add("Added HTrace SSGI renderer feature to Mobile_Renderer.");
            }
        }

        if (!enableHTrace && !hasSsao)
        {
            var ssaoType = Type.GetType("UnityEngine.Rendering.Universal.ScreenSpaceAmbientOcclusion, Unity.RenderPipelines.Universal.Runtime");
            if (ssaoType != null)
            {
                var ssao = ScriptableObject.CreateInstance(ssaoType) as ScriptableRendererFeature;
                ssao.name = "ScreenSpaceAmbientOcclusion_MobileLow";
                AssetDatabase.AddObjectToAsset(ssao, renderer);
                ConfigureSsao(ssao, changes);

                rendererSo.Update();
                features = rendererSo.FindProperty("m_RendererFeatures");
                if (features != null)
                {
                    features.InsertArrayElementAtIndex(features.arraySize);
                    features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;
                    changes.Add("Added low-cost SSAO feature to Mobile_Renderer.");
                }
            }
            else
            {
                changes.Add("Could not locate URP ScreenSpaceAmbientOcclusion type; skipped SSAO add.");
            }
        }

        rendererSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer);
        return changes;
    }

    private static IEnumerable<string> ApplyStableMobileRendererSettings()
    {
        var changes = new List<string>();
        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(MobileRendererPath);
        if (!renderer)
            return new[] { "Mobile_Renderer not found; skipped stable renderer feature repair." };

        var rendererSo = new SerializedObject(renderer);
        var features = rendererSo.FindProperty("m_RendererFeatures");
        var disabledHTrace = 0;
        var disabledSsao = 0;

        if (features != null)
        {
            for (var i = 0; i < features.arraySize; i++)
            {
                var feature = features.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                if (!feature)
                    continue;

                if (feature is HTraceSSGIRendererFeature htraceFeature)
                {
                    htraceFeature.UseVolumes = false;
                    SetFeatureActive(feature, false, changes, "HTrace renderer feature disabled for stable mobile baseline.");
                    disabledHTrace++;
                    continue;
                }

                if (feature.GetType().Name.IndexOf("ScreenSpaceAmbientOcclusion", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    SetFeatureActive(feature, false, changes, "SSAO renderer feature disabled for stable mobile baseline.");
                    disabledSsao++;
                }
            }
        }

        rendererSo.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer);

        if (disabledHTrace == 0)
            changes.Add("No HTrace renderer feature was active to disable.");
        if (disabledSsao == 0)
            changes.Add("No SSAO renderer feature was active to disable.");

        return changes;
    }

    private static void ConfigureHTraceRendererFeature(HTraceSSGIRendererFeature feature, ICollection<string> changes)
    {
        if (!feature)
            return;

        feature.UseVolumes = true;
        SetFeatureActive(feature, true, changes, "HTrace SSGI renderer feature active and volume-driven.");
        EditorUtility.SetDirty(feature);
    }

    private static void SetFeatureActive(ScriptableRendererFeature feature, bool active, ICollection<string> changes, string label)
    {
        if (!feature)
            return;

        var so = new SerializedObject(feature);
        SetBool(so, "m_Active", active, changes, label);
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(feature);
    }

    private static void ConfigureSsao(UnityEngine.Object ssao, ICollection<string> changes)
    {
        if (!ssao)
            return;

        var so = new SerializedObject(ssao);
        SetBool(so, "m_Active", true, changes, "Mobile SSAO active.");
        var settings = so.FindProperty("m_Settings");
        if (settings != null)
        {
            SetRelativeInt(settings, "AOMethod", 1);
            SetRelativeBool(settings, "Downsample", true);
            SetRelativeBool(settings, "AfterOpaque", false);
            SetRelativeInt(settings, "Source", 1);
            SetRelativeInt(settings, "NormalSamples", 1);
            SetRelativeFloat(settings, "Intensity", 0.32f);
            SetRelativeFloat(settings, "DirectLightingStrength", 0.25f);
            SetRelativeFloat(settings, "Radius", 0.23f);
            SetRelativeInt(settings, "Samples", 1);
            SetRelativeInt(settings, "BlurQuality", 0);
            SetRelativeFloat(settings, "Falloff", 75f);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ssao);
        changes.Add("Configured SSAO: downsampled, low samples, 0.32 intensity, 0.23 radius.");
    }

    private static IEnumerable<string> ApplySceneLook(Scene scene, bool enableHTrace = false)
    {
        var changes = new List<string>();
        if (!scene.IsValid())
            return changes;

        var roots = scene.GetRootGameObjects();
        var lights = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).ToArray();
        var directional = lights.FirstOrDefault(l => l && l.type == LightType.Directional);
        if (directional)
        {
            Undo.RecordObject(directional, "XHero Mobile Directional Light");
            directional.lightmapBakeType = LightmapBakeType.Mixed;
            directional.shadows = LightShadows.Soft;
            directional.shadowResolution = LightShadowResolution.Medium;
            directional.shadowStrength = 0.78f;
            directional.intensity = Mathf.Clamp(directional.intensity <= 0.01f ? 1.25f : directional.intensity, 1.05f, 2.0f);
            directional.useColorTemperature = true;
            directional.colorTemperature = 5600f;
            directional.color = new Color(1f, 0.956f, 0.88f, 1f);
            EditorUtility.SetDirty(directional);
            changes.Add("Directional Light tuned to mixed, warm 5600K, soft medium shadows, 0.78 shadow strength.");
        }

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = enableHTrace ? 0.95f : 1.08f;
        RenderSettings.reflectionIntensity = Mathf.Clamp(RenderSettings.reflectionIntensity <= 0 ? 0.65f : RenderSettings.reflectionIntensity, 0.6f, 0.9f);
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution = 128;
        changes.Add(enableHTrace
            ? "RenderSettings kept Skybox ambient/reflection, ambient intensity 0.95 for HTrace blend, reflection resolution 128."
            : "RenderSettings kept Skybox ambient/reflection, ambient intensity 1.08, reflection resolution 128.");

        var volume = FindOrCreateGlobalVolume(roots);
        volume.isGlobal = true;
        volume.priority = 20f;
        volume.weight = 1f;
        volume.sharedProfile = LoadOrCreateLookProfile();
        EditorUtility.SetDirty(volume);
        changes.Add("Global Volume 'XHero Mobile Cinematic Look' assigned to scene with ACES/color/bloom/vignette.");
        if (enableHTrace)
            ConfigureHTraceVolume(volume.sharedProfile, changes);

        foreach (var camera in roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)))
        {
            var data = camera.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                var isUtilityCamera = IsUtilityCamera(camera);
                data.renderPostProcessing = !isUtilityCamera;
                data.requiresDepthTexture = !isUtilityCamera;
                data.requiresColorTexture = !isUtilityCamera;
                data.volumeLayerMask = isUtilityCamera ? (LayerMask)0 : (LayerMask)(((int)data.volumeLayerMask) | 1);
                if (!isUtilityCamera)
                {
                    data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    data.antialiasingQuality = AntialiasingQuality.Low;
                }

                EditorUtility.SetDirty(data);
            }

            if (IsUtilityCamera(camera))
            {
                camera.allowHDR = false;
                camera.allowMSAA = false;
            }
            else
            {
                camera.useOcclusionCulling = true;
                camera.farClipPlane = Mathf.Min(camera.farClipPlane <= 0 ? 320f : camera.farClipPlane, 320f);
            }

            EditorUtility.SetDirty(camera);
        }

        changes.Add(enableHTrace
            ? "URP camera data: HTrace/post/depth/color enabled only on world cameras; UI/minimap volume/depth/color/post kept off."
            : "URP camera data: post-processing/SMAA enabled only on world cameras; UI/minimap post kept off.");

        var reflectionProbes = roots.SelectMany(r => r.GetComponentsInChildren<ReflectionProbe>(true)).ToArray();
        if (reflectionProbes.Length == 0)
        {
            var bounds = CalculateSceneRendererBounds(roots);
            if (bounds.size.sqrMagnitude > 0.01f)
            {
                var go = new GameObject("XHero Baked Reflection Probe");
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.position = bounds.center + Vector3.up * Mathf.Min(2f, bounds.extents.y * 0.2f);
                var probe = go.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Baked;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                probe.resolution = 128;
                probe.intensity = 0.7f;
                probe.boxProjection = true;
                probe.size = new Vector3(Mathf.Max(10f, bounds.size.x), Mathf.Max(6f, bounds.size.y), Mathf.Max(10f, bounds.size.z));
                probe.center = Vector3.zero;
                changes.Add("Added one baked box-projected Reflection Probe fitted to renderer bounds.");
            }
        }
        else
        {
            foreach (var probe in reflectionProbes)
            {
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
                probe.resolution = Mathf.Clamp(probe.resolution, 64, 128);
                probe.intensity = Mathf.Clamp(probe.intensity, 0.55f, 0.9f);
                EditorUtility.SetDirty(probe);
            }

            changes.Add($"Tuned {reflectionProbes.Length} reflection probe(s): baked/on-awake friendly resolution and intensity clamp.");
        }

        return changes;
    }

    private static IEnumerable<string> ApplyStableSceneRendering(Scene scene)
    {
        var changes = new List<string>();
        if (!scene.IsValid())
            return changes;

        var roots = scene.GetRootGameObjects();
        var lights = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).ToArray();
        var directional = lights.FirstOrDefault(l => l && l.type == LightType.Directional);
        if (directional)
        {
            Undo.RecordObject(directional, "XHero Stable Directional Light");
            directional.lightmapBakeType = LightmapBakeType.Realtime;
            directional.shadows = LightShadows.Soft;
            directional.shadowResolution = LightShadowResolution.Medium;
            directional.shadowStrength = 0.82f;
            directional.intensity = Mathf.Clamp(directional.intensity <= 0.01f ? 2f : directional.intensity, 1.25f, 2.0f);
            directional.useColorTemperature = false;
            directional.color = Color.white;
            EditorUtility.SetDirty(directional);
            changes.Add("Directional Light restored to stable realtime white light with soft medium shadows.");
        }

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.reflectionIntensity = 1f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution = 128;
        changes.Add("RenderSettings restored to Skybox ambient/reflection at intensity 1.0.");

        var disabledVolumes = 0;
        foreach (var volume in roots.SelectMany(r => r.GetComponentsInChildren<Volume>(true)).Where(v => v))
        {
            var profilePath = volume.sharedProfile ? AssetDatabase.GetAssetPath(volume.sharedProfile) : string.Empty;
            if (volume.name == "XHero Mobile Cinematic Look" || string.Equals(profilePath, SceneLookProfilePath, StringComparison.OrdinalIgnoreCase))
            {
                DisableHTraceInProfile(volume.sharedProfile, changes);
                volume.weight = 0f;
                volume.enabled = false;
                volume.gameObject.SetActive(false);
                EditorUtility.SetDirty(volume);
                disabledVolumes++;
            }
            else
            {
                DisableHTraceInProfile(volume.sharedProfile, changes);
            }
        }

        var lookProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SceneLookProfilePath);
        DisableHTraceInProfile(lookProfile, changes);

        if (disabledVolumes > 0)
            changes.Add($"Disabled {disabledVolumes} XHero cinematic/HTrace volume object(s).");

        var removedProbeCount = 0;
        foreach (var probe in roots.SelectMany(r => r.GetComponentsInChildren<ReflectionProbe>(true)).Where(p => p && p.name == "XHero Baked Reflection Probe").ToArray())
        {
            UnityEngine.Object.DestroyImmediate(probe.gameObject);
            removedProbeCount++;
        }

        if (removedProbeCount > 0)
        {
            changes.Add($"Removed {removedProbeCount} reflection probe(s) created by the optimizer.");
            roots = scene.GetRootGameObjects().Where(r => r).ToArray();
        }

        foreach (var camera in roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)))
        {
            var data = camera.GetUniversalAdditionalCameraData();
            var isUtilityCamera = IsUtilityCamera(camera);
            if (data != null)
            {
                data.renderPostProcessing = false;
                data.requiresDepthTexture = !isUtilityCamera;
                data.requiresColorTexture = !isUtilityCamera;
                data.volumeLayerMask = isUtilityCamera ? (LayerMask)0 : data.volumeLayerMask;
                EditorUtility.SetDirty(data);
            }

            camera.allowHDR = false;
            camera.allowMSAA = !isUtilityCamera;
            if (!isUtilityCamera)
            {
                camera.useOcclusionCulling = true;
                camera.farClipPlane = Mathf.Min(camera.farClipPlane <= 0 ? 320f : camera.farClipPlane, 320f);
            }

            EditorUtility.SetDirty(camera);
        }

        changes.Add("Camera post/HDR disabled for stable baseline; world cameras keep depth/color, UI/minimap stays cheap.");
        return changes;
    }

    private static IEnumerable<string> ApplyMobileQualityLighting(Scene scene)
    {
        var changes = new List<string>();
        if (!scene.IsValid())
            return changes;

        var roots = scene.GetRootGameObjects().Where(r => r).ToArray();
        var lights = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).Where(l => l).ToArray();
        var directional = lights.FirstOrDefault(l => l.type == LightType.Directional);
        if (directional)
        {
            Undo.RecordObject(directional, "XHero Mobile Quality Directional Light");
            directional.lightmapBakeType = LightmapBakeType.Mixed;
            directional.shadows = LightShadows.Soft;
            directional.shadowResolution = LightShadowResolution.High;
            directional.shadowStrength = 0.74f;
            directional.intensity = Mathf.Clamp(directional.intensity <= 0.01f ? 1.45f : directional.intensity, 1.3f, 1.55f);
            directional.useColorTemperature = true;
            directional.colorTemperature = 5900f;
            directional.color = new Color(1f, 0.975f, 0.93f, 1f);
            RenderSettings.sun = directional;
            EditorUtility.SetDirty(directional);
            changes.Add("Directional Light set to Mixed, neutral-warm 5900K, High soft shadows, 0.74 shadow strength.");
        }
        else
        {
            changes.Add("No Directional Light found; skipped main light quality tuning.");
        }

        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = 0.88f;
        RenderSettings.reflectionIntensity = 0.82f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.defaultReflectionResolution = 128;
        RenderSettings.subtractiveShadowColor = new Color(0.46f, 0.52f, 0.64f, 1f);
        changes.Add("RenderSettings balanced: Skybox ambient 0.88, reflection 0.82, neutral cool subtractive shadow color.");

        foreach (var volume in roots.SelectMany(r => r.GetComponentsInChildren<Volume>(true)).Where(v => v))
            DisableHTraceInProfile(volume.sharedProfile, changes);

        var lookProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SceneLookProfilePath);
        DisableHTraceInProfile(lookProfile, changes);

        foreach (var camera in roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).Where(c => c))
        {
            var data = camera.GetUniversalAdditionalCameraData();
            var isUtilityCamera = IsUtilityCamera(camera);
            if (data != null)
            {
                data.renderPostProcessing = false;
                data.requiresDepthTexture = !isUtilityCamera;
                data.requiresColorTexture = false;
                data.volumeLayerMask = isUtilityCamera ? (LayerMask)0 : data.volumeLayerMask;
                EditorUtility.SetDirty(data);
            }

            camera.allowHDR = false;
            camera.allowMSAA = !isUtilityCamera;
            if (!isUtilityCamera)
            {
                camera.useOcclusionCulling = true;
                camera.farClipPlane = Mathf.Min(camera.farClipPlane <= 0 ? 320f : camera.farClipPlane, 320f);
            }

            EditorUtility.SetDirty(camera);
        }

        changes.Add("Cameras kept HDR/post off; world cameras keep depth and occlusion culling.");
        changes.AddRange(ConfigureMobileLightingSettingsAsset());
        changes.AddRange(EnsureMobileReflectionProbe(scene, roots));
        changes.AddRange(EnsureMobileLightProbeGrid(scene, roots));
        return changes;
    }

    private static IEnumerable<string> ConfigureMobileLightingSettingsAsset()
    {
        var changes = new List<string>();
        var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(NewSceneMobileLightingSettingsPath);
        if (!settings)
        {
            settings = new LightingSettings();
            AssetDatabase.CreateAsset(settings, NewSceneMobileLightingSettingsPath);
            changes.Add("Created mobile LightingSettings asset for New Scene.");
        }

        var so = new SerializedObject(settings);
        SetBool(so, "m_EnableBakedLightmaps", true, changes, "Baked lightmaps enabled.");
        SetBool(so, "m_EnableRealtimeLightmaps", false, changes, "Realtime GI disabled for mobile.");
        SetBool(so, "m_RealtimeEnvironmentLighting", true, changes, "Realtime environment lighting kept enabled for sky consistency.");
        SetFloat(so, "m_BounceScale", 1f, changes, "Lighting bounce scale set to 1.");
        SetFloat(so, "m_AlbedoBoost", 1f, changes, "Lighting albedo boost set to 1.");
        SetFloat(so, "m_IndirectOutputScale", 1f, changes, "Indirect output scale set to 1.");
        SetInt(so, "m_BakeBackend", 1, changes, "Progressive CPU lightmapper selected for stable batchmode baking.");
        SetInt(so, "m_LightmapMaxSize", 1024, changes, "Lightmap max size set to 1024 for mobile memory.");
        SetFloat(so, "m_BakeResolution", 18f, changes, "Lightmap bake resolution set to 18 texels/unit for mobile.");
        SetInt(so, "m_Padding", 2, changes, "Lightmap padding set to 2.");
        SetInt(so, "m_LightmapCompression", 3, changes, "Lightmap compression enabled.");
        SetBool(so, "m_AO", true, changes, "Baked AO enabled for depth.");
        SetFloat(so, "m_AOMaxDistance", 1.25f, changes, "Baked AO max distance set to 1.25.");
        SetFloat(so, "m_CompAOExponent", 0.7f, changes, "Baked AO indirect strength set to 0.7.");
        SetFloat(so, "m_CompAOExponentDirect", 0.25f, changes, "Baked AO direct strength set to 0.25.");
        SetInt(so, "m_MixedBakeMode", 2, changes, "Mixed lighting mode set to Shadowmask where supported.");
        SetInt(so, "m_LightmapsBakeMode", 1, changes, "Directional lightmaps kept enabled.");
        SetInt(so, "m_FilterMode", 1, changes, "Lightmap filtering enabled.");
        SetFloat(so, "m_RealtimeResolution", 1f, changes, "Realtime GI resolution set low.");
        SetInt(so, "m_PVRDirectSampleCount", 24, changes, "Direct light samples set to 24.");
        SetInt(so, "m_PVRSampleCount", 256, changes, "Indirect light samples set to 256.");
        SetInt(so, "m_PVREnvironmentSampleCount", 128, changes, "Environment samples set to 128.");
        SetInt(so, "m_PVRBounces", 2, changes, "Indirect bounces set to 2.");
        SetInt(so, "m_PVRMinBounces", 1, changes, "Minimum bounces set to 1.");
        SetInt(so, "m_PVRFilteringMode", 1, changes, "Lightmap filtering mode set to auto/fast.");
        so.ApplyModifiedPropertiesWithoutUndo();

        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
        Lightmapping.lightingSettings = settings;
        EditorUtility.SetDirty(settings);
        changes.Add("Assigned XHero_NewScene_MobileLighting.lighting to the active scene.");
        return changes;
    }

    private static IEnumerable<string> EnsureMobileReflectionProbe(Scene scene, GameObject[] roots)
    {
        var changes = new List<string>();
        var bounds = CalculateSceneRendererBounds(roots);
        if (bounds.size.sqrMagnitude <= 0.01f)
            return new[] { "Renderer bounds empty; skipped reflection probe fit." };

        var probes = roots.SelectMany(r => r.GetComponentsInChildren<ReflectionProbe>(true)).Where(p => p).ToArray();
        var probe = probes.FirstOrDefault(p => p.name == "XHero Mobile Baked Reflection Probe");
        if (!probe)
        {
            var go = new GameObject("XHero Mobile Baked Reflection Probe");
            SceneManager.MoveGameObjectToScene(go, scene);
            probe = go.AddComponent<ReflectionProbe>();
            changes.Add("Added one baked box-projected mobile reflection probe.");
        }

        probe.mode = ReflectionProbeMode.Baked;
        probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.resolution = 128;
        probe.intensity = 0.68f;
        probe.boxProjection = true;
        probe.transform.position = bounds.center + Vector3.up * Mathf.Min(2.5f, bounds.extents.y * 0.18f);
        probe.center = Vector3.zero;
        probe.size = new Vector3(Mathf.Max(12f, bounds.size.x), Mathf.Max(8f, bounds.size.y), Mathf.Max(12f, bounds.size.z));
        EditorUtility.SetDirty(probe);
        return changes.Count > 0 ? changes : new[] { "Updated existing XHero mobile reflection probe." };
    }

    private static IEnumerable<string> EnsureMobileLightProbeGrid(Scene scene, GameObject[] roots)
    {
        var changes = new List<string>();
        if (roots.SelectMany(r => r.GetComponentsInChildren<LightProbeGroup>(true)).Any(g => g))
            return new[] { "Existing LightProbeGroup found; kept as-is." };

        var bounds = CalculateSceneRendererBounds(roots);
        if (bounds.size.sqrMagnitude <= 0.01f)
            return new[] { "Renderer bounds empty; skipped light probe grid." };

        var go = new GameObject("XHero Mobile Light Probe Grid");
        SceneManager.MoveGameObjectToScene(go, scene);
        go.transform.position = Vector3.zero;
        var group = go.AddComponent<LightProbeGroup>();
        var positions = new List<Vector3>();
        var min = bounds.min;
        var max = bounds.max;
        var y0 = Mathf.Max(min.y + 1.2f, bounds.center.y - bounds.extents.y * 0.35f);
        var y1 = Mathf.Min(max.y - 0.5f, y0 + 5f);

        for (var xi = 0; xi < 3; xi++)
        {
            for (var zi = 0; zi < 3; zi++)
            {
                var x = Mathf.Lerp(min.x, max.x, xi / 2f);
                var z = Mathf.Lerp(min.z, max.z, zi / 2f);
                positions.Add(new Vector3(x, y0, z));
                positions.Add(new Vector3(x, y1, z));
            }
        }

        group.probePositions = positions.ToArray();
        EditorUtility.SetDirty(group);
        changes.Add("Added sparse 18-point LightProbeGroup for dynamic objects/player lighting.");
        return changes;
    }

    private static IEnumerable<string> PrepareStaticEnvironmentForMobileBake(Scene scene)
    {
        var roots = scene.GetRootGameObjects().Where(r => r).ToArray();
        var environmentRoots = roots
            .Where(r => string.Equals(r.name, "Enviroment", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(r.name, "Environment", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (environmentRoots.Length == 0)
            environmentRoots = roots.Where(r => !HasDynamicOrLogicComponentForLighting(r)).ToArray();

        var changed = 0;
        var contributeGi = 0;
        var batching = 0;

        foreach (var transform in environmentRoots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t))
        {
            var go = transform.gameObject;
            if (!IsStaticLightingCandidate(go))
                continue;

            var flags = GameObjectUtility.GetStaticEditorFlags(go);
            var next = flags | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;

            if (!IsFoliageLike(go) && !IsWaterOrFxLike(go))
                next |= StaticEditorFlags.ContributeGI | StaticEditorFlags.OccluderStatic;

            if (next == flags)
                continue;

            GameObjectUtility.SetStaticEditorFlags(go, next);
            EditorUtility.SetDirty(go);
            changed++;
            if ((next & StaticEditorFlags.ContributeGI) != 0)
                contributeGi++;
            if ((next & StaticEditorFlags.BatchingStatic) != 0)
                batching++;
        }

        return new[]
        {
            $"Prepared static environment for mobile bake: changed={changed}, batchingStatic={batching}, contributeGI={contributeGi}."
        };
    }

    private static IEnumerable<string> OptimizeMobileShadowCasters(Scene scene)
    {
        var disabled = 0;
        var renderers = scene.GetRootGameObjects()
            .Where(r => r)
            .SelectMany(r => r.GetComponentsInChildren<Renderer>(true))
            .Where(r => r && r.enabled)
            .ToArray();

        foreach (var renderer in renderers)
        {
            if (!ShouldDisableRealtimeShadowCaster(renderer))
                continue;

            if (renderer.shadowCastingMode == ShadowCastingMode.Off && !renderer.receiveShadows)
                continue;

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            EditorUtility.SetDirty(renderer);
            disabled++;
        }

        return new[] { $"Disabled realtime shadows on {disabled} tiny/transparent/VFX renderer(s) to keep 2048 main shadows affordable." };
    }

    private static IEnumerable<string> TryBakeActiveSceneLighting(Scene scene)
    {
        var changes = new List<string>();
        try
        {
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.ClearLightingDataAsset();
            var ok = Lightmapping.Bake();
            changes.Add(ok
                ? $"Lighting bake completed for '{scene.path}'."
                : $"Lighting bake returned false for '{scene.path}'. Open Lighting window for details.");
            EditorSceneManager.SaveScene(scene);
        }
        catch (Exception ex)
        {
            changes.Add("Lighting bake failed: " + ex.GetType().Name + " - " + ex.Message);
        }

        return changes;
    }

    private static bool IsStaticLightingCandidate(GameObject go)
    {
        if (!go || HasDynamicOrLogicComponentForLighting(go))
            return false;

        return go.GetComponent<Renderer>() || go.GetComponent<MeshFilter>() || go.GetComponent<Terrain>() || go.GetComponent<Collider>();
    }

    private static bool HasDynamicOrLogicComponentForLighting(GameObject go)
    {
        foreach (var component in go.GetComponents<Component>())
        {
            if (!component)
                return true;

            if (component is Transform || component is MeshRenderer || component is MeshFilter ||
                component is Terrain || component is TerrainCollider ||
                component is BoxCollider || component is SphereCollider || component is CapsuleCollider || component is MeshCollider ||
                component is LODGroup || component is ReflectionProbe)
            {
                continue;
            }

            if (component is Animator || component is Animation || component is Rigidbody || component is CharacterController ||
                component is Camera || component is Light || component is ParticleSystem || component is AudioSource ||
                component is Canvas || component is EventSystem)
            {
                return true;
            }

            if (component is MonoBehaviour)
            {
                var name = component.GetType().Name;
                if (LogicComponentNameHints.Any(h => name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
            }
        }

        return false;
    }

    private static bool ShouldDisableRealtimeShadowCaster(Renderer renderer)
    {
        if (!renderer || renderer is SkinnedMeshRenderer)
            return false;

        if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            return true;

        var maxSize = Mathf.Max(renderer.bounds.size.x, Mathf.Max(renderer.bounds.size.y, renderer.bounds.size.z));
        if (maxSize < 0.35f)
            return true;

        if (IsWaterOrFxLike(renderer.gameObject))
            return true;

        if (IsFoliageLike(renderer.gameObject) && maxSize < 1.25f)
            return true;

        foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
        {
            if (!material)
                continue;

            var shaderName = material.shader ? material.shader.name : string.Empty;
            var materialPath = AssetDatabase.GetAssetPath(material);
            if (material.renderQueue >= 3000 ||
                shaderName.IndexOf("Particle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                shaderName.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                materialPath.IndexOf("/Tazo_fx/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                materialPath.IndexOf("/Vefects/", StringComparison.OrdinalIgnoreCase) >= 0 ||
                materialPath.IndexOf("/Water/", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsFoliageLike(GameObject go)
    {
        var text = GetObjectSearchText(go);
        return text.IndexOf("tree", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("cay", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("grass", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("foliage", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("leaf", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("la_", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("hoa", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("sen", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("bui", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("/co_", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsWaterOrFxLike(GameObject go)
    {
        var text = GetObjectSearchText(go);
        return text.IndexOf("water", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("nuoc", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("vfx", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("fx", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("particle", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("splash", StringComparison.OrdinalIgnoreCase) >= 0 ||
               text.IndexOf("caustic", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetObjectSearchText(GameObject go)
    {
        if (!go)
            return string.Empty;

        var path = GetHierarchyPath(go.transform);
        var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
        var prefabPath = prefab ? AssetDatabase.GetAssetPath(prefab) : string.Empty;
        return path + " " + prefabPath;
    }

    private static IEnumerable<string> DisableGpuiPrefabRendering(Scene scene)
    {
        var changes = new List<string>();
        if (!scene.IsValid())
            return changes;

        var managers = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<GPUIPrefabManager>(true))
            .Where(m => m)
            .ToArray();

        foreach (var manager in managers)
        {
            var prototypeCount = manager.GetPrototypeCount();
            for (var i = prototypeCount - 1; i >= 0; i--)
            {
                var prototype = manager.GetPrototype(i);
                if (prototype != null)
                    prototype.isEnabled = false;
                manager.RemovePrototypeAtIndex(i);
            }

            manager.isFindInstancesAtInitialization = false;
            manager.enabled = false;
            manager.gameObject.SetActive(false);
            EditorUtility.SetDirty(manager);
            changes.Add($"Disabled GPUI Prefab Manager '{manager.name}' and cleared {prototypeCount} prototype(s).");
        }

        if (managers.Length == 0)
            changes.Add("No GPUI Prefab Manager found in scene.");

        return changes;
    }

    private static IEnumerable<string> DisableRiskyMaterialInstancing(Scene scene)
    {
        var changes = new List<string>();
        if (!scene.IsValid())
            return changes;

        var disabled = 0;
        var touched = new HashSet<Material>();
        var renderers = scene.GetRootGameObjects()
            .Where(r => r)
            .SelectMany(r => r.GetComponentsInChildren<Renderer>(true))
            .Where(r => r)
            .ToArray();

        foreach (var renderer in renderers)
        {
            foreach (var material in renderer.sharedMaterials ?? Array.Empty<Material>())
            {
                if (!material || !material.enableInstancing || !touched.Add(material))
                    continue;

                if (!IsRiskyInstancingMaterial(material))
                    continue;

                material.enableInstancing = false;
                EditorUtility.SetDirty(material);
                disabled++;
            }
        }

        if (disabled > 0)
            changes.Add($"Disabled instancing on {disabled} risky ShaderGraph/custom/legacy material(s).");
        else
            changes.Add("No risky ShaderGraph/custom/legacy material instancing found to disable.");

        return changes;
    }

    private static bool IsRiskyInstancingMaterial(Material material)
    {
        var shaderName = material.shader ? material.shader.name : string.Empty;
        var path = AssetDatabase.GetAssetPath(material).Replace('\\', '/');

        if (shaderName.StartsWith("Universal Render Pipeline/Nature/SpeedTree", StringComparison.OrdinalIgnoreCase))
            return false;
        if (shaderName.Equals("Universal Render Pipeline/Lit", StringComparison.OrdinalIgnoreCase))
            return false;
        if (shaderName.Equals("Universal Render Pipeline/Simple Lit", StringComparison.OrdinalIgnoreCase))
            return false;
        if (shaderName.Equals("Universal Render Pipeline/Particles/Unlit", StringComparison.OrdinalIgnoreCase))
            return false;

        return shaderName.StartsWith("Shader Graphs/", StringComparison.OrdinalIgnoreCase)
            || shaderName.StartsWith("Legacy Shaders/", StringComparison.OrdinalIgnoreCase)
            || shaderName.StartsWith("Vefects/", StringComparison.OrdinalIgnoreCase)
            || shaderName.IndexOf("Autodesk", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("/HDRI_Captures/", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("/Tazo_fx/", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("/Water/", StringComparison.OrdinalIgnoreCase) >= 0
            || path.IndexOf("com.distantlands.lumen", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static IEnumerable<string> RemoveAutoGpuiPrefabComponents()
    {
        var changes = new List<string>();
        var removed = 0;

        foreach (var prefabPath in AutoGpuiPrototypePrefabPaths)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), prefabPath)))
                continue;

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var components = root.GetComponentsInChildren<GPUIPrefab>(true);
                foreach (var component in components)
                {
                    UnityEngine.Object.DestroyImmediate(component, true);
                    removed++;
                }

                if (components.Length > 0)
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        if (removed > 0)
            changes.Add($"Removed {removed} GPUIPrefab component(s) added by the previous unsafe auto-GPUI pass.");
        else
            changes.Add("No auto-added GPUIPrefab components found on the previous unsafe prototype prefabs.");

        return changes;
    }

    private static void DisableHTraceInProfile(VolumeProfile profile, ICollection<string> changes)
    {
        if (!profile || !profile.TryGet(out HTraceSSGIVolume htrace))
            return;

        htrace.active = false;
        SetVolumeParameter(htrace.Enable, false);
        EditorUtility.SetDirty(htrace);
        EditorUtility.SetDirty(profile);
        changes.Add($"Disabled HTrace volume component in profile '{AssetDatabase.GetAssetPath(profile)}'.");
    }

    private static IEnumerable<string> ApplyTerrainMobileSettings(Scene scene)
    {
        var changes = new List<string>();
        var terrains = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Terrain>(true))
            .Where(t => t)
            .ToArray();

        foreach (var terrain in terrains)
        {
            terrain.heightmapPixelError = Mathf.Max(terrain.heightmapPixelError, 6f);
            terrain.basemapDistance = Mathf.Min(terrain.basemapDistance <= 0 ? 350f : terrain.basemapDistance, 350f);
            terrain.detailObjectDistance = Mathf.Min(terrain.detailObjectDistance <= 0 ? 45f : terrain.detailObjectDistance, 45f);
            terrain.detailObjectDensity = Mathf.Min(terrain.detailObjectDensity <= 0 ? 0.65f : terrain.detailObjectDensity, 0.65f);
            terrain.treeDistance = Mathf.Min(terrain.treeDistance <= 0 ? 420f : terrain.treeDistance, 420f);
            terrain.treeBillboardDistance = Mathf.Min(terrain.treeBillboardDistance <= 0 ? 55f : terrain.treeBillboardDistance, 55f);
            terrain.treeCrossFadeLength = Mathf.Clamp(terrain.treeCrossFadeLength, 5f, 12f);
            terrain.treeMaximumFullLODCount = Mathf.Min(terrain.treeMaximumFullLODCount <= 0 ? 24 : terrain.treeMaximumFullLODCount, 24);
            EditorUtility.SetDirty(terrain);
        }

        if (terrains.Length > 0)
            changes.Add($"Tuned {terrains.Length} terrain(s): pixel error 6+, detail 45m, tree 420m, billboard 55m, max full LOD trees 24.");

        return changes;
    }

    private static IEnumerable<string> ApplyRendererMobileCulling(Scene scene)
    {
        var changes = new List<string>();
        var renderers = scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<MeshRenderer>(true))
            .Where(r => r && r.enabled && r.gameObject.activeInHierarchy)
            .ToArray();

        var instancedMaterials = new HashSet<Material>();
        var shadowOptimized = 0;
        var staticFlagged = 0;

        foreach (var renderer in renderers)
        {
            var mesh = GetMeshFromRenderer(renderer);
            if (!mesh)
                continue;

            var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject);
            var pathText = GetRendererSourceText(renderer);
            var decorative = IsDecorativePath(pathText);

            if (decorative)
            {
                foreach (var mat in renderer.sharedMaterials ?? Array.Empty<Material>())
                {
                    if (!mat || instancedMaterials.Contains(mat))
                        continue;

                    mat.enableInstancing = true;
                    EditorUtility.SetDirty(mat);
                    instancedMaterials.Add(mat);
                }
            }

            if (decorative && mesh.vertexCount <= 3500 && renderer.bounds.size.y <= 5f)
            {
                if (renderer.shadowCastingMode != ShadowCastingMode.Off || renderer.receiveShadows)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    EditorUtility.SetDirty(renderer);
                    shadowOptimized++;
                }
            }

            var staticScope = prefabRoot ? prefabRoot : renderer.gameObject;
            if (!HasUnsafeLogicNearby(staticScope) && renderer.gameObject.isStatic)
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
                var wanted = flags | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.ReflectionProbeStatic;
                if (flags != wanted)
                {
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, wanted);
                    staticFlagged++;
                }
            }
        }

        if (instancedMaterials.Count > 0)
            changes.Add($"Enabled GPU instancing on {instancedMaterials.Count} decorative material(s).");
        if (shadowOptimized > 0)
            changes.Add($"Disabled realtime shadows on {shadowOptimized} small decorative renderer(s).");
        if (staticFlagged > 0)
            changes.Add($"Ensured batching/occludee/reflection-probe static flags on {staticFlagged} already-static renderer object(s).");

        return changes;
    }

    private static IEnumerable<string> ApplyGpuiPrefabOptimization(Scene scene)
    {
        var changes = new List<string>();
        var roots = scene.GetRootGameObjects();
        var manager = FindOrCreateGpuiPrefabManager(scene, roots);
        if (!manager)
            return new[] { "GPUI Prefab Manager not available; skipped GPUI prefab optimization." };

        manager.gameObject.SetActive(true);
        manager.enabled = true;
        manager.isFindInstancesAtInitialization = true;
        EditorUtility.SetDirty(manager);
        changes.Add("GPUI Prefab Manager enabled and set to find prefab instances at initialization.");

        var profile = LoadOrCreateGpuiMobileProfile();
        var candidates = FindSafeGpuiPrefabRootCandidates(roots)
            .Where(c => c.InstanceCount >= 6 && c.RendererCountPerInstance > 0)
            .OrderByDescending(c => c.EstimatedMaterialSlots)
            .ThenByDescending(c => c.InstanceCount)
            .Take(18)
            .ToArray();

        var added = 0;
        var updated = 0;
        foreach (var candidate in candidates)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidate.PrefabPath);
            if (!prefab || PrefabUtility.GetPrefabAssetType(prefab) == PrefabAssetType.Model)
                continue;

            var gpuiPrefab = GPUIPrefabUtility.AddOrGetComponentToPrefab<GPUIPrefab>(prefab);
            if (!gpuiPrefab)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidate.PrefabPath);
                gpuiPrefab = prefab ? prefab.GetComponent<GPUIPrefab>() : null;
            }

            if (!gpuiPrefab)
                continue;

            gpuiPrefab.GetPrefabID();
            EditorUtility.SetDirty(prefab);

            var prototypeIndex = GetPrototypeIndexByPrefabPath(manager, candidate.PrefabPath);
            if (prototypeIndex < 0)
            {
                prototypeIndex = manager.AddPrototype(prefab, profile);
                if (prototypeIndex >= 0)
                {
                    ConfigureGpuiPrototype(manager.GetPrototype(prototypeIndex), candidate);
                    added++;
                    changes.Add($"GPUI prototype added: {candidate.PrefabPath} instances={candidate.InstanceCount} estimatedSlots={candidate.EstimatedMaterialSlots}.");
                }
            }
            else
            {
                var prototype = manager.GetPrototype(prototypeIndex);
                if (prototype != null)
                {
                    prototype.profile = profile;
                    ConfigureGpuiPrototype(prototype, candidate);
                    updated++;
                }
            }
        }

        EditorUtility.SetDirty(manager);
        changes.Add($"GPUI prefab optimization complete: added={added}, updated={updated}, safe candidates scanned={candidates.Length}.");
        return changes;
    }

    private static GPUIPrefabManager FindOrCreateGpuiPrefabManager(Scene scene, GameObject[] roots)
    {
        var manager = roots.SelectMany(r => r.GetComponentsInChildren<GPUIPrefabManager>(true)).FirstOrDefault(m => m);
        if (manager)
            return manager;

        var go = new GameObject("GPUI Prefab Manager - XHero Mobile");
        SceneManager.MoveGameObjectToScene(go, scene);
        return go.AddComponent<GPUIPrefabManager>();
    }

    private static GPUIProfile LoadOrCreateGpuiMobileProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<GPUIProfile>(GpuiMobileProfilePath);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<GPUIProfile>();
            AssetDatabase.CreateAsset(profile, GpuiMobileProfilePath);
        }

        profile.isShadowCasting = false;
        profile.isDistanceCulling = true;
        profile.isFrustumCulling = true;
        profile.isOcclusionCulling = true;
        profile.isShadowDistanceCulling = true;
        profile.isShadowFrustumCulling = false;
        profile.isShadowOcclusionCulling = false;
        profile.isLODCrossFade = false;
        profile.isCalculateInstancingBounds = false;
        profile.minCullingDistance = 0f;
        profile.minMaxDistance = new Vector2(0f, 220f);
        profile.frustumOffset = 0.05f;
        profile.occlusionAccuracy = 1;
        profile.occlusionOffset = 0.00025f;
        profile.occlusionOffsetSizeMultiplier = 0.25f;
        profile.minShadowCullingDistance = 12f;
        profile.customShadowDistance = 24f;
        profile.lodBiasAdjustment = 0.7f;
        profile.maximumLODLevel = 0;
        profile.enablePerObjectMotionVectors = false;
        profile.lightProbeSetting = GPUILightProbeSetting.Off;
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void ConfigureGpuiPrototype(GPUIPrototype prototype, GpuiPrefabRootCandidate candidate)
    {
        if (prototype == null)
            return;

        prototype.isEnabled = true;
        prototype.isGenerateBillboard = false;
        prototype.isBillboardReplaceLODCulled = true;
        prototype.billboardDistance = 0.82f;
        prototype.name = Path.GetFileNameWithoutExtension(candidate.PrefabPath);
    }

    private static int GetPrototypeIndexByPrefabPath(GPUIPrefabManager manager, string prefabPath)
    {
        if (!manager)
            return -1;

        for (var i = 0; i < manager.GetPrototypeCount(); i++)
        {
            var prototype = manager.GetPrototype(i);
            if (prototype?.prefabObject && string.Equals(AssetDatabase.GetAssetPath(prototype.prefabObject), prefabPath, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static Volume FindOrCreateGlobalVolume(GameObject[] roots)
    {
        var existing = roots
            .SelectMany(r => r.GetComponentsInChildren<Volume>(true))
            .FirstOrDefault(v => v && v.name == "XHero Mobile Cinematic Look");

        if (existing)
            return existing;

        var go = new GameObject("XHero Mobile Cinematic Look");
        return go.AddComponent<Volume>();
    }

    private static VolumeProfile LoadOrCreateLookProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(SceneLookProfilePath);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, SceneLookProfilePath);
        }

        CompactVolumeProfile(profile);

        if (!profile.TryGet(out Tonemapping tonemapping))
            tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.active = true;
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.ACES;

        if (!profile.TryGet(out ColorAdjustments color))
            color = profile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.overrideState = true;
        color.postExposure.value = 0.05f;
        color.contrast.overrideState = true;
        color.contrast.value = 12f;
        color.saturation.overrideState = true;
        color.saturation.value = 6f;
        color.colorFilter.overrideState = true;
        color.colorFilter.value = new Color(1f, 0.985f, 0.95f, 1f);

        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.92f;
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 0.22f;
        bloom.scatter.overrideState = true;
        bloom.scatter.value = 0.58f;
        bloom.highQualityFiltering.overrideState = true;
        bloom.highQualityFiltering.value = false;
        bloom.maxIterations.overrideState = true;
        bloom.maxIterations.value = 4;

        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>(true);
        vignette.active = true;
        vignette.intensity.overrideState = true;
        vignette.intensity.value = 0.11f;
        vignette.smoothness.overrideState = true;
        vignette.smoothness.value = 0.34f;
        vignette.rounded.overrideState = true;
        vignette.rounded.value = false;

        PersistVolumeProfileComponents(profile);
        EditorUtility.SetDirty(profile);
        return profile;
    }

    private static void ConfigureHTraceVolume(VolumeProfile profile, ICollection<string> changes)
    {
        if (!profile)
            return;

        if (!profile.TryGet(out HTraceSSGIVolume htrace))
            htrace = profile.Add<HTraceSSGIVolume>(true);

        htrace.active = true;
        SetVolumeParameter(htrace.Enable, true);
        SetVolumeParameter(htrace.DebugMode, HTraceSSGI.Scripts.Globals.DebugMode.None);
        SetVolumeParameter(htrace.HBuffer, HBuffer.Multi);
        SetVolumeParameter(htrace.FallbackType, FallbackType.Sky);
        SetVolumeParameter(htrace.SkyIntensity, 0.28f);
        SetVolumeParameter(htrace.ViewBias, 0.08f);
        SetVolumeParameter(htrace.NormalBias, 0.28f);
        SetVolumeParameter(htrace.SamplingNoise, 0.12f);
        SetVolumeParameter(htrace.IntensityMultiplier, 0.75f);
        SetVolumeParameter(htrace.DenoiseFallback, true);
        SetVolumeParameter(htrace.MetallicIndirectFallback, true);
        SetVolumeParameter(htrace.AmbientOverride, false);
        SetVolumeParameter(htrace.Multibounce, false);

        SetVolumeParameter(htrace.BackfaceLighting, 0.12f);
        SetVolumeParameter(htrace.MaxRayLength, 22f);
        SetVolumeParameter(htrace.ThicknessMode, ThicknessMode.Relative);
        SetVolumeParameter(htrace.Thickness, 0.42f);
        SetVolumeParameter(htrace.Intensity, 0.62f);
        SetVolumeParameter(htrace.Falloff, 0.55f);

        SetVolumeParameter(htrace.RayCount, 2);
        SetVolumeParameter(htrace.StepCount, 10);
        SetVolumeParameter(htrace.RefineIntersection, false);
        SetVolumeParameter(htrace.FullResolutionDepth, false);
        SetVolumeParameter(htrace.Checkerboard, true);
        SetVolumeParameter(htrace.RenderScale, 0.5f);

        SetVolumeParameter(htrace.BrightnessClamp, BrightnessClamp.Manual);
        SetVolumeParameter(htrace.MaxValueBrightnessClamp, 5.5f);
        SetVolumeParameter(htrace.MaxDeviationBrightnessClamp, 2.2f);
        SetVolumeParameter(htrace.HalfStepValidation, true);
        SetVolumeParameter(htrace.SpatialOcclusionValidation, false);
        SetVolumeParameter(htrace.TemporalLightingValidation, true);
        SetVolumeParameter(htrace.TemporalOcclusionValidation, true);
        SetVolumeParameter(htrace.SpatialRadius, 0.72f);
        SetVolumeParameter(htrace.Adaptivity, 0.82f);
        SetVolumeParameter(htrace.RecurrentBlur, true);
        SetVolumeParameter(htrace.FireflySuppression, true);
        SetVolumeParameter(htrace.ShowBowels, false);

        PersistVolumeProfileComponents(profile);
        EditorUtility.SetDirty(profile);
        changes.Add("Configured HTrace SSGI mobile-low volume: 2 rays, 10 steps, checkerboard, 0.5 render scale, sky fallback, no ambient override.");
    }

    private static void CompactVolumeProfile(VolumeProfile profile)
    {
        if (!profile || profile.components == null || !profile.components.Any(c => !c))
            return;

        profile.components.RemoveAll(c => !c);
        EditorUtility.SetDirty(profile);
    }

    private static void PersistVolumeProfileComponents(VolumeProfile profile)
    {
        if (!profile)
            return;

        var profilePath = AssetDatabase.GetAssetPath(profile);
        foreach (var component in profile.components.Where(c => c))
        {
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(component)) && !string.IsNullOrEmpty(profilePath))
                AssetDatabase.AddObjectToAsset(component, profile);
            EditorUtility.SetDirty(component);
        }

        EditorUtility.SetDirty(profile);
    }

    private static void SetVolumeParameter<T>(VolumeParameter<T> parameter, T value)
    {
        parameter.overrideState = true;
        parameter.value = value;
    }

    private static Bounds CalculateSceneRendererBounds(IEnumerable<GameObject> roots)
    {
        var initialized = false;
        var bounds = new Bounds();
        foreach (var renderer in roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).Where(r => r && r.enabled))
        {
            if (!initialized)
            {
                bounds = renderer.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return bounds;
    }

    private static void SetBool(SerializedObject so, string path, bool value, ICollection<string> changes, string label)
    {
        var prop = so.FindProperty(path);
        if (prop == null || prop.boolValue == value)
            return;
        prop.boolValue = value;
        changes.Add(label);
    }

    private static void SetInt(SerializedObject so, string path, int value, ICollection<string> changes, string label)
    {
        var prop = so.FindProperty(path);
        if (prop == null || prop.intValue == value)
            return;
        prop.intValue = value;
        changes.Add(label);
    }

    private static void SetFloat(SerializedObject so, string path, float value, ICollection<string> changes, string label)
    {
        var prop = so.FindProperty(path);
        if (prop == null || Mathf.Approximately(prop.floatValue, value))
            return;
        prop.floatValue = value;
        changes.Add(label);
    }

    private static void SetRelativeBool(SerializedProperty parent, string path, bool value)
    {
        var prop = parent.FindPropertyRelative(path);
        if (prop != null) prop.boolValue = value;
    }

    private static void SetRelativeInt(SerializedProperty parent, string path, int value)
    {
        var prop = parent.FindPropertyRelative(path);
        if (prop != null) prop.intValue = value;
    }

    private static void SetRelativeFloat(SerializedProperty parent, string path, float value)
    {
        var prop = parent.FindPropertyRelative(path);
        if (prop != null) prop.floatValue = value;
    }

    private static void GenerateReport(string[] scenePaths, string outputPath, string mode, IEnumerable<string> appliedChanges = null)
    {
        var sb = new StringBuilder(32768);
        sb.AppendLine("# XHero LMS URP Mobile Scene Optimization Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Mode: {mode}");
        sb.AppendLine($"Unity: {Application.unityVersion}");
        sb.AppendLine($"Active build target: {EditorUserBuildSettings.activeBuildTarget}");
        sb.AppendLine();

        if (appliedChanges != null)
        {
            sb.AppendLine("## Applied Changes");
            foreach (var change in appliedChanges)
                sb.AppendLine($"- {change}");
            sb.AppendLine();
        }

        AppendProjectPipelineReport(sb);
        AppendHTraceStatus(sb);

        foreach (var scenePath in scenePaths)
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath)))
            {
                sb.AppendLine($"## Scene: {scenePath}");
                sb.AppendLine("Scene file not found.");
                sb.AppendLine();
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AppendSceneReport(sb, scene);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[XHeroMobileSceneOptimizer] Report written: {outputPath}");
    }

    private static void AppendProjectPipelineReport(StringBuilder sb)
    {
        sb.AppendLine("## URP / Quality Baseline");
        sb.AppendLine($"Quality level: {QualitySettings.names[QualitySettings.GetQualityLevel()]} ({QualitySettings.GetQualityLevel()})");
        sb.AppendLine("Static batching project flag: use Player Settings UI; scene renderer static flags are reported below.");
        sb.AppendLine("Dynamic batching project flag: controlled in URP asset below.");
        sb.AppendLine($"Quality shadow distance: {QualitySettings.shadowDistance.ToString("0.##", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"Quality anti-aliasing: {QualitySettings.antiAliasing}");
        sb.AppendLine($"Quality realtime reflection probes: {QualitySettings.realtimeReflectionProbes}");
        sb.AppendLine();

        var mobileAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileRpAssetPath);
        AppendSerializedAssetValues(sb, "Mobile_RPAsset", mobileAsset, new[]
        {
            "m_UseSRPBatcher", "m_SupportsDynamicBatching", "m_RenderScale", "m_SupportsHDR",
            "m_MSAA", "m_RequireDepthTexture", "m_RequireOpaqueTexture", "m_MainLightShadowmapResolution",
            "m_ShadowDistance", "m_ShadowCascadeCount", "m_AdditionalLightsPerObjectLimit",
            "m_AdditionalLightShadowsSupported", "m_SoftShadowsSupported", "m_SoftShadowQuality"
        });

        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(MobileRendererPath);
        sb.AppendLine("### Mobile Renderer Features");
        if (renderer)
        {
            var so = new SerializedObject(renderer);
            var features = so.FindProperty("m_RendererFeatures");
            if (features != null && features.arraySize > 0)
            {
                for (var i = 0; i < features.arraySize; i++)
                {
                    var feature = features.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                    sb.AppendLine(feature
                        ? $"- {feature.name} ({feature.GetType().Name}) active={feature.isActive}"
                        : "- <missing feature reference>");
                }
            }
            else
            {
                sb.AppendLine("- None");
            }
        }
        else
        {
            sb.AppendLine("- Mobile renderer asset not found.");
        }

        sb.AppendLine();
    }

    private static void AppendSerializedAssetValues(StringBuilder sb, string title, UnityEngine.Object asset, IEnumerable<string> propertyPaths)
    {
        sb.AppendLine($"### {title}");
        if (!asset)
        {
            sb.AppendLine("- Asset not found.");
            sb.AppendLine();
            return;
        }

        var so = new SerializedObject(asset);
        foreach (var path in propertyPaths)
        {
            var prop = so.FindProperty(path);
            if (prop == null)
                continue;

            sb.AppendLine($"- {path}: {SerializedValueToString(prop)}");
        }

        sb.AppendLine();
    }

    private static string SerializedValueToString(SerializedProperty prop)
    {
        return prop.propertyType switch
        {
            SerializedPropertyType.Boolean => prop.boolValue.ToString(),
            SerializedPropertyType.Integer => prop.intValue.ToString(CultureInfo.InvariantCulture),
            SerializedPropertyType.Enum => prop.enumValueIndex >= 0 && prop.enumDisplayNames != null && prop.enumValueIndex < prop.enumDisplayNames.Length
                ? prop.enumDisplayNames[prop.enumValueIndex]
                : prop.intValue.ToString(CultureInfo.InvariantCulture),
            SerializedPropertyType.Float => prop.floatValue.ToString("0.###", CultureInfo.InvariantCulture),
            SerializedPropertyType.ObjectReference => prop.objectReferenceValue ? AssetDatabase.GetAssetPath(prop.objectReferenceValue) : "null",
            _ => prop.ToString()
        };
    }

    private static void AppendHTraceStatus(StringBuilder sb)
    {
        sb.AppendLine("## HTrace SSGI Status");
        var htraceRoot = Path.Combine(Application.dataPath, "HTraceSSGI");
        var htraceScripts = Path.Combine(htraceRoot, "Scripts");
        var htraceResources = Path.Combine(htraceRoot, "Resources");
        sb.AppendLine($"- Assets/HTraceSSGI exists: {Directory.Exists(htraceRoot)}");
        sb.AppendLine($"- Scripts folder exists: {Directory.Exists(htraceScripts)}");
        sb.AppendLine($"- Resources folder exists: {Directory.Exists(htraceResources)}");
        sb.AppendLine("- Renderer feature detected in Mobile_Renderer: " + RendererHasFeatureName(MobileRendererPath, "HTrace"));
        sb.AppendLine("- Mobile use: feature should be active + volume-driven; XHero tool configures HTrace in the scene Volume at low-cost mobile settings.");
        sb.AppendLine();
    }

    private static bool RendererHasFeatureName(string rendererPath, string namePart)
    {
        var renderer = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
        if (!renderer)
            return false;

        var so = new SerializedObject(renderer);
        var features = so.FindProperty("m_RendererFeatures");
        if (features == null)
            return false;

        for (var i = 0; i < features.arraySize; i++)
        {
            var feature = features.GetArrayElementAtIndex(i).objectReferenceValue;
            if (feature && feature.GetType().Name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static void AppendSceneReport(StringBuilder sb, Scene scene)
    {
        sb.AppendLine($"## Scene: {scene.path}");
        var roots = scene.GetRootGameObjects();
        var allTransforms = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        var allObjects = allTransforms.Select(t => t.gameObject).ToArray();
        var renderers = roots.SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).ToArray();
        var enabledRenderers = renderers.Where(r => r && r.enabled && r.gameObject.activeInHierarchy).ToArray();
        var lights = roots.SelectMany(r => r.GetComponentsInChildren<Light>(true)).ToArray();
        var cameras = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)).ToArray();
        var volumes = roots.SelectMany(r => r.GetComponentsInChildren<Volume>(true)).ToArray();
        var reflectionProbes = roots.SelectMany(r => r.GetComponentsInChildren<ReflectionProbe>(true)).ToArray();
        var lightProbeGroups = roots.SelectMany(r => r.GetComponentsInChildren<LightProbeGroup>(true)).ToArray();
        var lodGroups = roots.SelectMany(r => r.GetComponentsInChildren<LODGroup>(true)).ToArray();
        var colliders = roots.SelectMany(r => r.GetComponentsInChildren<Collider>(true)).ToArray();
        var gpuiPrefabManagers = roots.SelectMany(r => r.GetComponentsInChildren<GPUIPrefabManager>(true)).ToArray();

        var meshStats = CollectMeshStats(enabledRenderers);
        var materialSlotCount = enabledRenderers.Sum(r => r.sharedMaterials?.Length ?? 0);
        var materialCount = enabledRenderers.SelectMany(r => r.sharedMaterials ?? Array.Empty<Material>()).Where(m => m).Distinct().Count();
        var meshCount = enabledRenderers.Select(GetMeshFromRenderer).Where(m => m).Distinct().Count();
        var staticBatchingCount = enabledRenderers.Count(r => (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.BatchingStatic) != 0);
        var instancingMatCount = enabledRenderers
            .SelectMany(r => r.sharedMaterials ?? Array.Empty<Material>())
            .Where(m => m && m.enableInstancing)
            .Distinct()
            .Count();

        sb.AppendLine("### Scene Summary");
        sb.AppendLine($"- Root objects: {roots.Length}");
        sb.AppendLine($"- GameObjects: {allObjects.Length} active={allObjects.Count(o => o.activeInHierarchy)} inactive={allObjects.Count(o => !o.activeInHierarchy)}");
        sb.AppendLine($"- Renderers: {renderers.Length} enabled+active={enabledRenderers.Length}");
        sb.AppendLine($"- MeshRenderers: {renderers.OfType<MeshRenderer>().Count()} SkinnedMeshRenderers: {renderers.OfType<SkinnedMeshRenderer>().Count()} Terrains: {roots.SelectMany(r => r.GetComponentsInChildren<Terrain>(true)).Count()}");
        sb.AppendLine($"- Meshes: unique={meshCount} approx rendered tris={meshStats.triangles:N0} verts={meshStats.vertices:N0}");
        sb.AppendLine($"- Material slots approx draw submissions before batching: {materialSlotCount:N0}; unique materials={materialCount:N0}; instancing-enabled materials={instancingMatCount:N0}");
        sb.AppendLine($"- Static batching flagged renderers: {staticBatchingCount:N0}/{enabledRenderers.Length:N0}");
        sb.AppendLine("- Actual draw calls/batches: not available in Unity batchmode; use Game view Stats or Frame Debugger on device after opening this report.");
        sb.AppendLine();

        sb.AppendLine("### Lighting / Probes");
        sb.AppendLine($"- Lights: total={lights.Length}, active={lights.Count(l => l && l.isActiveAndEnabled)}, realtime={lights.Count(l => l && l.lightmapBakeType == LightmapBakeType.Realtime)}, mixed={lights.Count(l => l && l.lightmapBakeType == LightmapBakeType.Mixed)}, baked={lights.Count(l => l && l.lightmapBakeType == LightmapBakeType.Baked)}");
        foreach (var group in lights.Where(l => l).GroupBy(l => l.type))
            sb.AppendLine($"- {group.Key}: {group.Count()}");
        foreach (var l in lights.Where(l => l && l.isActiveAndEnabled).Take(12))
            sb.AppendLine($"  - {GetHierarchyPath(l.transform)} type={l.type} mode={l.lightmapBakeType} intensity={l.intensity:0.###} shadows={l.shadows} range={l.range:0.##}");
        sb.AppendLine($"- ReflectionProbes: {reflectionProbes.Length}");
        sb.AppendLine($"- LightProbeGroups: {lightProbeGroups.Length}, probe count={lightProbeGroups.Sum(g => g.probePositions?.Length ?? 0):N0}");
        sb.AppendLine($"- Ambient mode={RenderSettings.ambientMode}, ambientIntensity={RenderSettings.ambientIntensity:0.###}, reflectionIntensity={RenderSettings.reflectionIntensity:0.###}, skybox={(RenderSettings.skybox ? AssetDatabase.GetAssetPath(RenderSettings.skybox) : "null")}");
        sb.AppendLine();

        sb.AppendLine("### Cameras / Volumes");
        foreach (var camera in cameras)
        {
            var data = camera.GetUniversalAdditionalCameraData();
            sb.AppendLine($"- Camera {GetHierarchyPath(camera.transform)} active={camera.isActiveAndEnabled} post={(data ? data.renderPostProcessing.ToString() : "n/a")} depth={(data ? data.requiresDepthTexture.ToString() : "n/a")} color={(data ? data.requiresColorTexture.ToString() : "n/a")} hdr={camera.allowHDR} msaa={camera.allowMSAA}");
        }
        foreach (var volume in volumes)
        {
            var profile = volume.sharedProfile ? volume.sharedProfile : volume.profile;
            var profileLabel = profile ? AssetDatabase.GetAssetPath(profile) : "null";
            if (profile && string.IsNullOrWhiteSpace(profileLabel))
                profileLabel = profile.name;

            sb.AppendLine($"- Volume {GetHierarchyPath(volume.transform)} active={volume.isActiveAndEnabled} global={volume.isGlobal} weight={volume.weight:0.##} profile={profileLabel}");
            if (profile && profile.TryGet(out HTraceSSGIVolume htrace))
                sb.AppendLine($"  - HTrace enabled={htrace.Enable.value} active={htrace.active} rays={htrace.RayCount.value} steps={htrace.StepCount.value} scale={htrace.RenderScale.value:0.##} checkerboard={htrace.Checkerboard.value} intensity={htrace.Intensity.value:0.##}");
        }
        sb.AppendLine();

        sb.AppendLine("### GPU Instancer Pro");
        if (gpuiPrefabManagers.Length == 0)
        {
            sb.AppendLine("- No GPUIPrefabManager in scene.");
        }
        else
        {
            foreach (var manager in gpuiPrefabManagers)
            {
                var so = new SerializedObject(manager);
                var prototypes = so.FindProperty("_prototypes");
                sb.AppendLine($"- {GetHierarchyPath(manager.transform)} active={manager.gameObject.activeInHierarchy} enabled={manager.enabled} prototypes={(prototypes != null ? prototypes.arraySize : -1)} findAtInit={manager.isFindInstancesAtInitialization}");
            }
        }

        var gpuiPrefabs = roots.SelectMany(r => r.GetComponentsInChildren<GPUIPrefab>(true)).ToArray();
        sb.AppendLine($"- GPUIPrefab components in scene hierarchy: {gpuiPrefabs.Length}");
        var treeManagerCount = roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).Count(c => c && c.GetType().FullName == "GPUInstancerPro.TerrainModule.GPUITreeManager");
        var terrainManagerCount = roots.SelectMany(r => r.GetComponentsInChildren<Component>(true)).Count(c => c && c.GetType().FullName == "GPUInstancerPro.TerrainModule.GPUITerrainBuiltin");
        sb.AppendLine($"- GPUI Tree Managers: {treeManagerCount}; GPUI Terrain components: {terrainManagerCount}");
        sb.AppendLine();

        AppendGpuiPrefabPrototypeReport(sb, roots, gpuiPrefabManagers);
        AppendInstancingCandidates(sb, enabledRenderers);
        AppendLodCandidates(sb, enabledRenderers, lodGroups);
        AppendColliderCandidates(sb, colliders);
        AppendMaterialShaderReport(sb, enabledRenderers);
    }

    private static void AppendGpuiPrefabPrototypeReport(StringBuilder sb, GameObject[] roots, GPUIPrefabManager[] managers)
    {
        sb.AppendLine("### GPUI Prefab Prototype Status");
        if (managers.Length > 0)
        {
            foreach (var manager in managers.Where(m => m))
            {
                for (var i = 0; i < manager.GetPrototypeCount(); i++)
                {
                    var prototype = manager.GetPrototype(i);
                    var path = prototype?.prefabObject ? AssetDatabase.GetAssetPath(prototype.prefabObject) : "";
                    var instances = CountPrefabInstancesInScene(roots, path);
                    sb.AppendLine($"- {GetHierarchyPath(manager.transform)} prototype[{i}] enabled={prototype?.isEnabled} prefab={path} estimatedSceneInstances={instances}");
                }
            }
        }

        var candidates = FindSafeGpuiPrefabRootCandidates(roots)
            .OrderByDescending(c => c.EstimatedMaterialSlots)
            .ThenByDescending(c => c.InstanceCount)
            .Take(20)
            .ToArray();

        if (candidates.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("| Candidate prefab | Instances | Renderers/instance | Estimated material slots | Estimated verts |");
            sb.AppendLine("|---|---:|---:|---:|---:|");
            foreach (var c in candidates)
                sb.AppendLine($"| {Escape(c.PrefabPath)} | {c.InstanceCount} | {c.RendererCountPerInstance} | {c.EstimatedMaterialSlots:N0} | {c.EstimatedVerts:N0} |");
        }
        else
        {
            sb.AppendLine("- No safe repeated decorative prefab root candidates found.");
        }

        sb.AppendLine();
    }

    private static int CountPrefabInstancesInScene(GameObject[] roots, string prefabPath)
    {
        if (string.IsNullOrWhiteSpace(prefabPath))
            return 0;

        return roots
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject))
            .Where(r => r && r.activeInHierarchy)
            .Distinct()
            .Count(root =>
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                return source && string.Equals(AssetDatabase.GetAssetPath(source), prefabPath, StringComparison.OrdinalIgnoreCase);
            });
    }

    private static (long vertices, long triangles) CollectMeshStats(IEnumerable<Renderer> renderers)
    {
        long vertices = 0;
        long triangles = 0;
        foreach (var renderer in renderers)
        {
            var mesh = GetMeshFromRenderer(renderer);
            if (!mesh)
                continue;

            vertices += mesh.vertexCount;
            for (var i = 0; i < mesh.subMeshCount; i++)
                triangles += mesh.GetIndexCount(i) / 3;
        }

        return (vertices, triangles);
    }

    private static Mesh GetMeshFromRenderer(Renderer renderer)
    {
        if (!renderer)
            return null;
        if (renderer is SkinnedMeshRenderer smr)
            return smr.sharedMesh;
        var mf = renderer.GetComponent<MeshFilter>();
        return mf ? mf.sharedMesh : null;
    }

    private static void AppendInstancingCandidates(StringBuilder sb, Renderer[] enabledRenderers)
    {
        sb.AppendLine("### Safe GPU Instancer / GPU Instancing Candidates");
        var candidates = enabledRenderers
            .Where(r => r is MeshRenderer && GetMeshFromRenderer(r) && !HasUnsafeLogicNearby(r.gameObject))
            .GroupBy(GetInstancingKey)
            .Select(g => CandidateGroup.From(g))
            .Where(g => g.Count >= 5)
            .OrderByDescending(g => g.Count)
            .ThenByDescending(g => g.TotalMaterialSlots)
            .Take(30)
            .ToArray();

        if (candidates.Length == 0)
        {
            sb.AppendLine("- No safe repeated MeshRenderer groups above threshold 5. Existing terrain/tree GPUI is likely the main win.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Count | Material slots | Mesh | Material | Prefab | Instancing | Note |");
        sb.AppendLine("|---:|---:|---|---|---|---|---|");
        foreach (var c in candidates)
        {
            sb.AppendLine($"| {c.Count} | {c.TotalMaterialSlots} | {Escape(c.MeshName)} | {Escape(c.MaterialName)} | {Escape(c.PrefabPath)} | {c.MaterialInstancingEnabled} | {Escape(c.Note)} |");
        }

        sb.AppendLine();
    }

    private static string GetInstancingKey(Renderer renderer)
    {
        var mesh = GetMeshFromRenderer(renderer);
        var meshPath = mesh ? AssetDatabase.GetAssetPath(mesh) : "";
        var materials = renderer.sharedMaterials ?? Array.Empty<Material>();
        var matKeys = string.Join("|", materials.Select(m => m ? AssetDatabase.GetAssetPath(m) + "#" + m.GetInstanceID() : "null"));
        var prefab = PrefabUtility.GetCorrespondingObjectFromSource(renderer.gameObject);
        var prefabPath = prefab ? AssetDatabase.GetAssetPath(prefab) : "";
        return $"{meshPath}|{mesh?.GetInstanceID()}|{matKeys}|{prefabPath}";
    }

    private static bool HasUnsafeLogicNearby(GameObject go)
    {
        var root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
        var scope = root ? root : go.transform.root.gameObject;
        var components = scope.GetComponentsInChildren<Component>(true);
        foreach (var component in components)
        {
            if (!component)
                continue;
            var t = component.GetType();
            if (t == typeof(Transform) || t == typeof(MeshRenderer) || t == typeof(MeshFilter) ||
                t == typeof(BoxCollider) || t == typeof(SphereCollider) || t == typeof(CapsuleCollider) ||
                t == typeof(MeshCollider) || t == typeof(LODGroup) || t == typeof(GPUIPrefab))
                continue;

            if (component is MonoBehaviour)
            {
                var name = t.Name;
                if (LogicComponentNameHints.Any(h => name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;
            }

            if (component is Animator || component is Rigidbody || component is CharacterController)
                return true;
        }

        return false;
    }

    private static IEnumerable<GpuiPrefabRootCandidate> FindSafeGpuiPrefabRootCandidates(GameObject[] roots)
    {
        var prefabRoots = roots
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Select(t => PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject))
            .Where(r => r && r.activeInHierarchy)
            .Distinct()
            .Select(root =>
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(root);
                var path = source ? AssetDatabase.GetAssetPath(source) : "";
                return new { Root = root, Source = source, Path = path };
            })
            .Where(x => x.Source && !string.IsNullOrWhiteSpace(x.Path) && x.Path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var group in prefabRoots.GroupBy(x => x.Path, StringComparer.OrdinalIgnoreCase))
        {
            var instances = group.Select(x => x.Root).Distinct().ToArray();
            var source = group.First().Source;
            var pathText = group.Key + " " + source.name;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(group.Key);
            if (!prefab || !IsSafeGpuiPrefabPath(pathText) || HasUnsafeLogicForGpui(prefab))
                continue;

            var renderers = instances
                .SelectMany(i => i.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r => r && r.enabled)
                .ToArray();
            if (renderers.Length == 0)
                continue;

            var rendererCountPerInstance = Mathf.Max(1, Mathf.RoundToInt(renderers.Length / (float)instances.Length));
            var materialSlots = renderers.Sum(r => r.sharedMaterials?.Length ?? 0);
            var verts = renderers.Sum(r => GetMeshFromRenderer(r) ? GetMeshFromRenderer(r).vertexCount : 0);
            yield return new GpuiPrefabRootCandidate
            {
                PrefabPath = group.Key,
                PrefabName = source.name,
                InstanceCount = instances.Length,
                RendererCountPerInstance = rendererCountPerInstance,
                EstimatedMaterialSlots = materialSlots,
                EstimatedVerts = verts
            };
        }
    }

    private static bool HasUnsafeLogicForGpui(GameObject scope)
    {
        foreach (var component in scope.GetComponentsInChildren<Component>(true))
        {
            if (!component)
                return true;

            var t = component.GetType();
            if (t == typeof(Transform) || t == typeof(MeshRenderer) || t == typeof(MeshFilter) ||
                t == typeof(BoxCollider) || t == typeof(SphereCollider) || t == typeof(CapsuleCollider) ||
                t == typeof(MeshCollider) || t == typeof(LODGroup) || t == typeof(GPUIPrefab) ||
                t.Name == "Tree" || t.Name == "BillboardRenderer" || t.Name == "GPUIOptionalRenderer")
                continue;

            if (component is Animator || component is Rigidbody || component is CharacterController)
                return true;

            if (component is MonoBehaviour)
                return true;
        }

        return false;
    }

    private static string GetRendererSourceText(Renderer renderer)
    {
        var mesh = GetMeshFromRenderer(renderer);
        var prefab = PrefabUtility.GetCorrespondingObjectFromSource(renderer.gameObject);
        var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject);
        var parts = new[]
        {
            renderer.name,
            renderer.sharedMaterial ? renderer.sharedMaterial.name : "",
            renderer.sharedMaterial ? AssetDatabase.GetAssetPath(renderer.sharedMaterial) : "",
            mesh ? mesh.name : "",
            mesh ? AssetDatabase.GetAssetPath(mesh) : "",
            prefab ? AssetDatabase.GetAssetPath(prefab) : "",
            prefabRoot ? prefabRoot.name : ""
        };

        return string.Join(" ", parts);
    }

    private static bool IsDecorativePath(string value)
    {
        return ContainsAny(value, GpuiSafePathHints) && !ContainsAny(value, GpuiUnsafePathHints);
    }

    private static bool IsSafeGpuiPrefabPath(string value)
    {
        return IsDecorativePath(value);
    }

    private static bool ContainsAny(string value, IEnumerable<string> needles)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        foreach (var needle in needles)
        {
            if (value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static void AppendLodCandidates(StringBuilder sb, Renderer[] enabledRenderers, LODGroup[] existingLods)
    {
        sb.AppendLine("### LOD Candidates");
        var withLod = new HashSet<GameObject>(existingLods.Where(l => l).Select(l => l.gameObject));
        var candidates = enabledRenderers
            .Where(r => r is MeshRenderer && GetMeshFromRenderer(r) && !HasLodInParents(r.transform))
            .Select(r =>
            {
                var mesh = GetMeshFromRenderer(r);
                return new
                {
                    Renderer = r,
                    Mesh = mesh,
                    Verts = mesh.vertexCount,
                    Tris = mesh.subMeshCount > 0 ? mesh.GetIndexCount(0) / 3 : 0,
                    Bounds = r.bounds.size.magnitude
                };
            })
            .Where(x => x.Verts >= 1500 || x.Bounds >= 12f)
            .OrderByDescending(x => x.Verts)
            .Take(25)
            .ToArray();

        if (candidates.Length == 0)
        {
            sb.AppendLine("- No obvious high-vertex/no-LOD MeshRenderer candidates found.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Object | Verts | Approx tris LOD0 submesh | Bounds diag | Mesh |");
        sb.AppendLine("|---|---:|---:|---:|---|");
        foreach (var c in candidates)
            sb.AppendLine($"| {Escape(GetHierarchyPath(c.Renderer.transform))} | {c.Verts:N0} | {c.Tris:N0} | {c.Bounds:0.##} | {Escape(AssetDatabase.GetAssetPath(c.Mesh))} |");
        sb.AppendLine();
    }

    private static bool HasLodInParents(Transform transform)
    {
        while (transform)
        {
            if (transform.GetComponent<LODGroup>())
                return true;
            transform = transform.parent;
        }

        return false;
    }

    private static void AppendColliderCandidates(StringBuilder sb, Collider[] colliders)
    {
        sb.AppendLine("### Collider Simplification Candidates");
        var candidates = colliders
            .OfType<MeshCollider>()
            .Where(c => c && c.sharedMesh && c.sharedMesh.vertexCount >= 1200)
            .OrderByDescending(c => c.sharedMesh.vertexCount)
            .Take(25)
            .ToArray();

        if (candidates.Length == 0)
        {
            sb.AppendLine("- No MeshCollider above 1,200 vertices found.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Object | Mesh collider verts | Convex | Mesh | Recommendation |");
        sb.AppendLine("|---|---:|---|---|---|");
        foreach (var c in candidates)
        {
            var recommendation = c.GetComponent<Rigidbody>() ? "Keep or use convex/simple compound if moving." : "Replace with Box/Capsule/compound collider if player collision can be approximate.";
            sb.AppendLine($"| {Escape(GetHierarchyPath(c.transform))} | {c.sharedMesh.vertexCount:N0} | {c.convex} | {Escape(AssetDatabase.GetAssetPath(c.sharedMesh))} | {Escape(recommendation)} |");
        }
        sb.AppendLine();
    }

    private static void AppendMaterialShaderReport(StringBuilder sb, Renderer[] enabledRenderers)
    {
        sb.AppendLine("### Materials / Shaders");
        var materials = enabledRenderers
            .SelectMany(r => r.sharedMaterials ?? Array.Empty<Material>())
            .Where(m => m)
            .Distinct()
            .ToArray();

        var shaderGroups = materials
            .GroupBy(m => m.shader ? m.shader.name : "<missing>")
            .OrderByDescending(g => g.Count())
            .Take(25)
            .ToArray();

        sb.AppendLine("| Shader | Material count | Instancing materials | Note |");
        sb.AppendLine("|---|---:|---:|---|");
        foreach (var group in shaderGroups)
        {
            var shader = group.Key;
            var note = shader.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase) ||
                       shader.StartsWith("Shader Graphs/", StringComparison.OrdinalIgnoreCase) ||
                       shader.StartsWith("GPUInstancerPro/", StringComparison.OrdinalIgnoreCase)
                ? "URP/GPUI friendly"
                : "Review SRP Batcher compatibility and mobile cost";
            sb.AppendLine($"| {Escape(shader)} | {group.Count()} | {group.Count(m => m.enableInstancing)} | {Escape(note)} |");
        }

        var duplicateNameGroups = materials
            .GroupBy(m => NormalizeMaterialName(m.name))
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Count())
            .Take(20)
            .ToArray();

        if (duplicateNameGroups.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Potential duplicate material names:");
            foreach (var group in duplicateNameGroups)
                sb.AppendLine($"- {Escape(group.Key)}: {group.Count()} materials");
        }

        sb.AppendLine();
    }

    private static string NormalizeMaterialName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "";

        var n = name.Replace(" (Instance)", "");
        while (n.EndsWith(")") && n.LastIndexOf(" (", StringComparison.Ordinal) >= 0)
        {
            var idx = n.LastIndexOf(" (", StringComparison.Ordinal);
            var suffix = n.Substring(idx + 2, n.Length - idx - 3);
            if (!int.TryParse(suffix, out _))
                break;
            n = n.Substring(0, idx);
        }

        return n;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (!transform)
            return "";

        var stack = new Stack<string>();
        while (transform)
        {
            stack.Push(transform.name);
            transform = transform.parent;
        }

        return string.Join("/", stack);
    }

    private static bool IsUtilityCamera(Camera camera)
    {
        if (!camera)
            return false;

        var path = GetHierarchyPath(camera.transform);
        return path.IndexOf("UI", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("Minimap", StringComparison.OrdinalIgnoreCase) >= 0 ||
               path.IndexOf("Topdown", StringComparison.OrdinalIgnoreCase) >= 0 ||
               (path.IndexOf("Map", StringComparison.OrdinalIgnoreCase) >= 0 &&
                path.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    private sealed class CandidateGroup
    {
        public int Count;
        public int TotalMaterialSlots;
        public string MeshName;
        public string MaterialName;
        public string PrefabPath;
        public bool MaterialInstancingEnabled;
        public string Note;

        public static CandidateGroup From(IGrouping<string, Renderer> group)
        {
            var renderers = group.ToArray();
            var first = renderers[0];
            var mesh = GetMeshFromRenderer(first);
            var material = first.sharedMaterial;
            var prefab = PrefabUtility.GetCorrespondingObjectFromSource(first.gameObject);
            var prefabPath = prefab ? AssetDatabase.GetAssetPath(prefab) : "";
            var hasGpuiPrefab = renderers.Any(r =>
            {
                var root = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
                return root && root.GetComponent<GPUIPrefab>();
            });

            return new CandidateGroup
            {
                Count = renderers.Length,
                TotalMaterialSlots = renderers.Sum(r => r.sharedMaterials?.Length ?? 0),
                MeshName = mesh ? mesh.name : "",
                MaterialName = material ? $"{material.name} [{AssetDatabase.GetAssetPath(material)}]" : "",
                PrefabPath = prefabPath,
                MaterialInstancingEnabled = renderers.SelectMany(r => r.sharedMaterials ?? Array.Empty<Material>()).Where(m => m).All(m => m.enableInstancing),
                Note = hasGpuiPrefab ? "Has GPUIPrefab on prefab root; verify manager prototype active." : "Safe candidate only if object is decorative/static."
            };
        }
    }

    private sealed class GpuiPrefabRootCandidate
    {
        public string PrefabPath;
        public string PrefabName;
        public int InstanceCount;
        public int RendererCountPerInstance;
        public int EstimatedMaterialSlots;
        public int EstimatedVerts;
    }
}
#endif
