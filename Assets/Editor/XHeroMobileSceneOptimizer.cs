#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class XHeroMobileSceneOptimizer
{
    private const string DefaultScene = "Assets/Scenes/New Scene.unity";
    private const string FullProjectReportPath = "XHero_Mobile_Batch_FPS_Report.md";
    private const string UniversalAdditionalCameraDataTypeName = "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData";

    private static readonly string[] CameraRenderOptionProperties =
    {
        "m_RenderShadows",
        "m_RequiresDepthTextureOption",
        "m_RequiresOpaqueTextureOption",
        "m_RendererIndex",
        "m_VolumeFrameworkUpdateModeOption",
        "m_RenderPostProcessing",
        "m_Antialiasing",
        "m_AntialiasingQuality",
        "m_StopNaN",
        "m_Dithering",
        "m_ClearDepth",
        "m_AllowXRRendering"
    };

    private static readonly string[] GeneratedScenePathHints =
    {
        "/Bundle_NewScene/",
        "/New Scene_AddressableGenerated/",
        "/New Scene_AddressableLate/"
    };

    private static readonly string[] DynamicNameHints =
    {
        "player", "npc", "ui", "button", "trigger", "interact", "door", "quest", "timeline",
        "cinemachine", "camera", "webview", "joystick", "controller", "handler", "loader",
        "manager", "event", "anim", "vehicle", "character"
    };

    private static readonly string[] LogicComponentNameHints =
    {
        "Handler", "Controller", "Trigger", "Interact", "Load", "Door", "NPC", "Quest",
        "Timeline", "Cinemachine", "Astar", "Path", "Button", "UI", "WebView", "Input"
    };

    private static readonly string[] StaticBatchingRiskyPathHints =
    {
        "enviroment/toachanhdien",
        "modelslms/toachanhdien"
    };

    [MenuItem("Tools/XHero LMS/Optimization/Report Current Scene Batches/FPS")]
    public static void GenerateCurrentSceneReportMenu()
    {
        var scene = SceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(scene))
            scene = DefaultScene;

        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../XHero_Scene_Batch_FPS_Report.md"));
        GenerateReport(new[] { scene }, outPath, "manual report");
        EditorUtility.RevealInFinder(outPath);
    }

    public static void ApplySafeMobileLookMenu()
    {
        ApplySafeMobileQualityPass(new[] { SceneManager.GetActiveScene().path }, null);
    }

    [MenuItem("Tools/XHero LMS/Optimization/ONE CLICK - Safe Batches and FPS Only")]
    public static void ApplyOneClickMobileProjectOptimizationMenu()
    {
        var scenes = GetAllProjectScenePaths();
        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + FullProjectReportPath));
        ApplyOneClickDeepMobileProjectOptimization(scenes, outPath, false, false);
        EditorUtility.RevealInFinder(outPath);
    }

    public static void ApplyNewSceneLightingQualityMenu()
    {
        var outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../XHero_NewScene_Batch_FPS_Report.md"));
        ApplyMobileLightingQualityPass(new[] { DefaultScene }, outPath, false);
        EditorUtility.RevealInFinder(outPath);
    }

    public static void GenerateReportBatch()
    {
        var scenes = GetSceneArgs();
        var output = GetArg("-xheroOut");
        if (string.IsNullOrEmpty(output))
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../XHero_Scene_Batch_FPS_Report.md"));

        GenerateReport(scenes, output, "batch report");
    }

    public static void ApplySafeMobileQualityPassBatch()
    {
        ApplySafeMobileQualityPass(GetSceneArgs(), GetArg("-xheroOut"));
    }

    public static void ApplyFullMobileOptimizationPassBatch()
    {
        ApplyFullMobileOptimizationPass(GetSceneArgs(), GetArg("-xheroOut"));
    }

    public static void ApplyStableMobileRepairPassBatch()
    {
        ApplyStableMobileRepairPass(GetSceneArgs(), GetArg("-xheroOut"));
    }

    public static void ApplyMobileLightingQualityPassBatch()
    {
        var bakeArg = GetArg("-xheroBake");
        var bakeNow = string.Equals(bakeArg, "true", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(bakeArg, "1", StringComparison.OrdinalIgnoreCase);
        ApplyMobileLightingQualityPass(GetSceneArgs(), GetArg("-xheroOut"), bakeNow);
    }

    public static void ApplyOneClickDeepMobileProjectOptimizationBatch()
    {
        var explicitScenes = GetArg("-xheroScenes");
        var scenes = (string.IsNullOrWhiteSpace(explicitScenes) ? GetAllProjectScenePaths() : GetSceneArgs())
            .Where(path => !IsGeneratedSplitScenePath(path))
            .ToArray();

        var output = GetArg("-xheroOut");
        if (string.IsNullOrEmpty(output))
            output = Path.GetFullPath(Path.Combine(Application.dataPath, "../" + FullProjectReportPath));

        var bakeArg = GetArg("-xheroBake");
        var bakeNow = string.Equals(bakeArg, "true", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(bakeArg, "1", StringComparison.OrdinalIgnoreCase);

        ApplyOneClickDeepMobileProjectOptimization(scenes, output, bakeNow, false);
    }

    private static void ApplySafeMobileQualityPass(string[] scenePaths, string outputPath)
    {
        ApplyBatchAndFpsOptimizationPass(scenePaths, outputPath, false, "safe batch/FPS pass");
    }

    private static void ApplyFullMobileOptimizationPass(string[] scenePaths, string outputPath)
    {
        ApplyBatchAndFpsOptimizationPass(scenePaths, outputPath, true, "full batch/FPS pass");
    }

    private static void ApplyStableMobileRepairPass(string[] scenePaths, string outputPath)
    {
        ApplyBatchAndFpsOptimizationPass(scenePaths, outputPath, false, "stable batch/FPS repair pass");
    }

    private static void ApplyMobileLightingQualityPass(string[] scenePaths, string outputPath, bool bakeNow)
    {
        var extraChanges = bakeNow
            ? new[] { "Ignored -xheroBake: this optimizer no longer bakes or edits scene lighting." }
            : null;
        ApplyBatchAndFpsOptimizationPass(scenePaths, outputPath, false, "batch/FPS pass; old lighting entry point is deprecated", extraChanges);
    }

    private static void ApplyOneClickDeepMobileProjectOptimization(
        string[] scenePaths,
        string outputPath,
        bool bakeNow,
        bool regenerateNewSceneAddressables)
    {
        var extraChanges = new List<string>();
        if (bakeNow)
            extraChanges.Add("Ignored -xheroBake: this optimizer no longer bakes or edits scene lighting.");
        if (regenerateNewSceneAddressables)
            extraChanges.Add("Skipped scene splitting: this optimizer no longer calls NewSceneAddressablesSplitter.");

        ApplyBatchAndFpsOptimizationPass(scenePaths, outputPath, true, "one-click safe batch/FPS pass", extraChanges);
    }

    private static void ApplyBatchAndFpsOptimizationPass(
        string[] scenePaths,
        string outputPath,
        bool aggressive,
        string mode,
        IEnumerable<string> extraChanges = null)
    {
        scenePaths = NormalizeSceneList(scenePaths);

        var changes = new List<string>
        {
            "Safe batch/FPS optimizer. It does not edit materials, shaders, prefab assets, GPUI components, HTrace, RenderSettings, lights, lightmaps, bake data, Addressables, scene splitting, QualitySettings, URP assets, or camera render options.",
            "URP camera opaque/depth/post settings are snapshotted before optimization and restored after scene save.",
            "Applied changes are limited to gameplay camera occlusion culling, conservative terrain distance clamps, renderer occlusion flags, and static batching flags only on objects already marked static."
        };
        if (extraChanges != null)
            changes.AddRange(extraChanges);

        foreach (var scenePath in scenePaths)
        {
            if (!SceneFileExists(scenePath))
            {
                changes.Add($"Skipped missing scene: {scenePath}");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var cameraRenderOptions = CaptureCameraRenderOptions(scene);
            var sceneChanges = new List<string>();
            sceneChanges.AddRange(ApplyCameraOcclusionCulling(scene));
            sceneChanges.AddRange(ApplyTerrainFpsBudget(scene, aggressive));
            sceneChanges.AddRange(ApplyRendererOcclusionAndStaticBatching(scene));
            sceneChanges.AddRange(RestoreCameraRenderOptions(cameraRenderOptions));

            if (sceneChanges.Count == 0)
            {
                changes.Add($"Scene '{scenePath}': no safe writable optimization needed.");
                continue;
            }

            changes.Add($"Scene '{scenePath}':");
            changes.AddRange(sceneChanges.Select(change => "  " + change));
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(outputPath))
        {
            GenerateReport(scenePaths, outputPath, "after " + mode, changes);
        }
        else
        {
            Debug.Log("[XHeroMobileSceneOptimizer] Applied " + mode + ":\n" + string.Join("\n", changes));
        }
    }

    private static IEnumerable<string> ApplyCameraOcclusionCulling(Scene scene)
    {
        var changed = 0;
        foreach (var camera in GetSceneComponents<Camera>(scene))
        {
            if (!camera || IsUtilityCamera(camera) || camera.useOcclusionCulling)
                continue;

            camera.useOcclusionCulling = true;
            EditorUtility.SetDirty(camera);
            changed++;
        }

        if (changed > 0)
            return new[] { $"Enabled camera occlusion culling on {changed} gameplay camera(s)." };
        return Array.Empty<string>();
    }

    private static List<CameraRenderOptionsSnapshot> CaptureCameraRenderOptions(Scene scene)
    {
        var snapshots = new List<CameraRenderOptionsSnapshot>();
        foreach (var camera in GetSceneComponents<Camera>(scene))
        {
            var cameraData = GetUniversalAdditionalCameraData(camera);
            if (!cameraData)
                continue;

            var snapshot = new CameraRenderOptionsSnapshot(cameraData);
            var so = new SerializedObject(cameraData);
            foreach (var propertyPath in CameraRenderOptionProperties)
            {
                var property = so.FindProperty(propertyPath);
                if (property == null)
                    continue;

                if (property.propertyType == SerializedPropertyType.Boolean)
                    snapshot.BooleanValues[propertyPath] = property.boolValue;
                else if (property.propertyType == SerializedPropertyType.Integer)
                    snapshot.IntValues[propertyPath] = property.intValue;
                else if (property.propertyType == SerializedPropertyType.Enum)
                    snapshot.EnumValues[propertyPath] = property.enumValueIndex;
            }

            snapshots.Add(snapshot);
        }

        return snapshots;
    }

    private static IEnumerable<string> RestoreCameraRenderOptions(IEnumerable<CameraRenderOptionsSnapshot> snapshots)
    {
        var restored = 0;
        foreach (var snapshot in snapshots)
        {
            if (!snapshot.CameraData)
                continue;

            var so = new SerializedObject(snapshot.CameraData);
            var changed = false;

            foreach (var pair in snapshot.BooleanValues)
            {
                var property = so.FindProperty(pair.Key);
                if (property == null || property.propertyType != SerializedPropertyType.Boolean || property.boolValue == pair.Value)
                    continue;

                property.boolValue = pair.Value;
                changed = true;
            }

            foreach (var pair in snapshot.IntValues)
            {
                var property = so.FindProperty(pair.Key);
                if (property == null || property.propertyType != SerializedPropertyType.Integer || property.intValue == pair.Value)
                    continue;

                property.intValue = pair.Value;
                changed = true;
            }

            foreach (var pair in snapshot.EnumValues)
            {
                var property = so.FindProperty(pair.Key);
                if (property == null || property.propertyType != SerializedPropertyType.Enum || property.enumValueIndex == pair.Value)
                    continue;

                property.enumValueIndex = pair.Value;
                changed = true;
            }

            if (!changed)
                continue;

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(snapshot.CameraData);
            restored++;
        }

        if (restored > 0)
            return new[] { $"Restored URP camera render options on {restored} camera(s); opaque/depth/post settings are preserved." };

        return Array.Empty<string>();
    }

    private static Component GetUniversalAdditionalCameraData(Camera camera)
    {
        if (!camera)
            return null;

        return camera.GetComponents<Component>()
            .FirstOrDefault(component => component && component.GetType().FullName == UniversalAdditionalCameraDataTypeName);
    }

    private static IEnumerable<string> ApplyTerrainFpsBudget(Scene scene, bool aggressive)
    {
        var changes = new List<string>();
        var terrains = GetSceneComponents<Terrain>(scene).Where(t => t && t.terrainData).ToArray();
        if (terrains.Length == 0)
            return changes;

        var changed = 0;
        foreach (var terrain in terrains)
        {
            var before = TerrainBudget.From(terrain);
            var target = aggressive
                ? new TerrainBudget(16f, 260f, 75f, 0.75f, 900f)
                : new TerrainBudget(12f, 320f, 95f, 0.85f, 1100f);

            terrain.heightmapPixelError = Mathf.Max(terrain.heightmapPixelError, target.PixelError);
            terrain.basemapDistance = Mathf.Min(terrain.basemapDistance, target.BasemapDistance);
            terrain.detailObjectDistance = Mathf.Min(terrain.detailObjectDistance, target.DetailDistance);
            terrain.detailObjectDensity = Mathf.Min(terrain.detailObjectDensity, target.DetailDensity);
            terrain.treeDistance = Mathf.Min(terrain.treeDistance, target.TreeDistance);

            var after = TerrainBudget.From(terrain);
            if (!before.Equals(after))
            {
                EditorUtility.SetDirty(terrain);
                changed++;
            }
        }

        if (changed > 0)
            changes.Add($"Clamped terrain FPS budgets on {changed} terrain(s) without changing terrain materials/shaders/render path.");
        return changes;
    }

    private static IEnumerable<string> ApplyRendererOcclusionAndStaticBatching(Scene scene)
    {
        var changes = new List<string>();
        var renderers = GetSceneComponents<Renderer>(scene)
            .Where(r => r && r.enabled && r.gameObject.activeInHierarchy && !(r is SkinnedMeshRenderer))
            .ToArray();

        var occlusionFlagged = 0;
        var staticBatchFlagged = 0;

        foreach (var renderer in renderers)
        {
            if (!renderer.allowOcclusionWhenDynamic)
            {
                renderer.allowOcclusionWhenDynamic = true;
                EditorUtility.SetDirty(renderer);
                occlusionFlagged++;
            }

            if (!renderer.gameObject.isStatic || !IsStaticBatchingSafe(renderer.gameObject))
                continue;

            var flags = GameObjectUtility.GetStaticEditorFlags(renderer.gameObject);
            var wanted = flags | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic;
            if (wanted == flags)
                continue;

            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, wanted);
            EditorUtility.SetDirty(renderer.gameObject);
            staticBatchFlagged++;
        }

        if (occlusionFlagged > 0)
            changes.Add($"Enabled renderer occlusion allowance on {occlusionFlagged} renderer(s).");
        if (staticBatchFlagged > 0)
            changes.Add($"Ensured static batching/occlusion flags on {staticBatchFlagged} renderer object(s) that were already static.");

        return changes;
    }

    private static string[] NormalizeSceneList(string[] scenePaths)
    {
        return (scenePaths == null || scenePaths.Length == 0 ? GetAllProjectScenePaths() : scenePaths)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path.Trim().Replace('\\', '/'))
            .Where(path => !IsGeneratedSplitScenePath(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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
            .Where(path => !IsGeneratedSplitScenePath(path))
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

    private static bool SceneFileExists(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return false;
        return File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scenePath));
    }

    private static bool IsGeneratedSplitScenePath(string path)
    {
        var normalized = (path ?? string.Empty).Replace('\\', '/');
        return GeneratedScenePathHints.Any(hint => normalized.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static T[] GetSceneComponents<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return Array.Empty<T>();

        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .Where(component => component)
            .ToArray();
    }

    private static bool IsStaticBatchingSafe(GameObject go)
    {
        if (!go || HasUnsafeLogicNearby(go))
            return false;

        var text = NormalizeSearchText(GetHierarchyPath(go.transform) + " " + GetPrefabPath(go));
        if (ContainsAny(text, DynamicNameHints) || ContainsAny(text, StaticBatchingRiskyPathHints))
            return false;

        var renderer = go.GetComponent<Renderer>();
        if (!renderer || renderer is SkinnedMeshRenderer)
            return false;

        return GetMeshFromRenderer(renderer) != null;
    }

    private static bool HasUnsafeLogicNearby(GameObject go)
    {
        if (!go)
            return true;

        var root = PrefabUtility.GetNearestPrefabInstanceRoot(go);
        var scope = root ? root : go;
        var transforms = scope.GetComponentsInChildren<Transform>(true);
        foreach (var transform in transforms)
        {
            if (!transform)
                continue;

            var searchText = NormalizeSearchText(GetHierarchyPath(transform) + " " + transform.name);
            if (ContainsAny(searchText, DynamicNameHints))
                return true;

            var components = transform.GetComponents<Component>();
            foreach (var component in components)
            {
                if (!component)
                    continue;

                if (IsSafeStaticComponent(component))
                    continue;

                var typeName = component.GetType().Name;
                if (LogicComponentNameHints.Any(hint => typeName.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;

                if (component is Animator || component is Rigidbody || component is Rigidbody2D ||
                    component is Camera || component is Canvas || component is EventSystem ||
                    component is ParticleSystem || component is AudioSource)
                    return true;
            }
        }

        return false;
    }

    private static bool IsSafeStaticComponent(Component component)
    {
        return component is Transform ||
               component is MeshFilter ||
               component is MeshRenderer ||
               component is BoxCollider ||
               component is SphereCollider ||
               component is CapsuleCollider ||
               component is MeshCollider ||
               component is LODGroup ||
               component is Terrain ||
               component is TerrainCollider ||
               component is Light ||
               component is ReflectionProbe;
    }

    private static bool IsUtilityCamera(Camera camera)
    {
        if (!camera)
            return true;

        if (camera.targetTexture)
            return true;

        var name = NormalizeSearchText(GetHierarchyPath(camera.transform));
        return name.Contains("ui") ||
               name.Contains("minimap") ||
               name.Contains("preview") ||
               name.Contains("webview") ||
               name.Contains("rendertexture") ||
               name.Contains("overlay");
    }

    private static Mesh GetMeshFromRenderer(Renderer renderer)
    {
        if (!renderer)
            return null;

        var meshFilter = renderer.GetComponent<MeshFilter>();
        if (meshFilter && meshFilter.sharedMesh)
            return meshFilter.sharedMesh;

        var skinned = renderer as SkinnedMeshRenderer;
        return skinned ? skinned.sharedMesh : null;
    }

    private static string GetPrefabPath(GameObject go)
    {
        if (!go)
            return string.Empty;

        var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go);
        return string.IsNullOrEmpty(path) ? AssetDatabase.GetAssetPath(go) : path;
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (!transform)
            return string.Empty;

        var names = new Stack<string>();
        var current = transform;
        while (current)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static string NormalizeSearchText(string value)
    {
        return (value ?? string.Empty).Replace('\\', '/').ToLowerInvariant();
    }

    private static bool ContainsAny(string value, IEnumerable<string> needles)
    {
        var normalized = NormalizeSearchText(value);
        return needles.Any(needle => normalized.IndexOf(NormalizeSearchText(needle), StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static void GenerateReport(string[] scenePaths, string outputPath, string mode, IEnumerable<string> appliedChanges = null)
    {
        scenePaths = NormalizeSceneList(scenePaths);

        var sb = new StringBuilder();
        sb.AppendLine("# XHero Mobile Batch/FPS Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Mode: {mode}");
        sb.AppendLine();
        sb.AppendLine("## Safety Contract");
        sb.AppendLine("- Does not edit materials, shaders, prefab assets, GPUI components, HTrace, lighting, bake data, Addressables, generated split scenes, QualitySettings, URP assets, or camera render options.");
        sb.AppendLine("- URP camera opaque/depth/post settings are snapshotted before optimization and restored after scene save.");
        sb.AppendLine("- Writes only conservative scene-level FPS flags: gameplay camera occlusion, terrain distance clamps, renderer occlusion allowance, and static batching flags on objects that are already static.");
        sb.AppendLine();

        if (appliedChanges != null)
        {
            sb.AppendLine("## Applied Changes");
            foreach (var change in appliedChanges)
                sb.AppendLine("- " + change);
            sb.AppendLine();
        }

        AppendProjectReport(sb);

        foreach (var scenePath in scenePaths)
        {
            if (!SceneFileExists(scenePath))
            {
                sb.AppendLine($"## Scene: {scenePath}");
                sb.AppendLine("Missing scene file.");
                sb.AppendLine();
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            AppendSceneReport(sb, scene);
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        Debug.Log("[XHeroMobileSceneOptimizer] Report written: " + outputPath);
    }

    private static void AppendProjectReport(StringBuilder sb)
    {
        sb.AppendLine("## Project Settings Snapshot");
        var qualityIndex = QualitySettings.GetQualityLevel();
        var qualityName = QualitySettings.names.Length > qualityIndex ? QualitySettings.names[qualityIndex] : qualityIndex.ToString(CultureInfo.InvariantCulture);
        sb.AppendLine($"- Quality level: {qualityName} ({qualityIndex})");
        sb.AppendLine($"- VSync: {QualitySettings.vSyncCount}");
        sb.AppendLine($"- Anti-aliasing: {QualitySettings.antiAliasing}");
        sb.AppendLine($"- Streaming mipmaps: {QualitySettings.streamingMipmapsActive}");
        sb.AppendLine($"- LOD bias: {QualitySettings.lodBias.ToString("0.###", CultureInfo.InvariantCulture)}");

        var rp = GraphicsSettings.defaultRenderPipeline;
        sb.AppendLine($"- Active render pipeline asset: {(rp ? AssetDatabase.GetAssetPath(rp) : "built-in/null")}");
        sb.AppendLine("- Snapshot only. This optimizer does not write ProjectSettings or URP assets.");
        sb.AppendLine();
    }

    private static void AppendSceneReport(StringBuilder sb, Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        var renderers = roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)).Where(r => r).ToArray();
        var enabledRenderers = renderers.Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
        var meshRenderers = enabledRenderers.OfType<MeshRenderer>().ToArray();
        var skinnedRenderers = enabledRenderers.OfType<SkinnedMeshRenderer>().ToArray();
        var terrains = roots.SelectMany(root => root.GetComponentsInChildren<Terrain>(true)).Where(t => t).ToArray();
        var cameras = roots.SelectMany(root => root.GetComponentsInChildren<Camera>(true)).Where(c => c).ToArray();

        var materialSlots = enabledRenderers.Sum(r => r.sharedMaterials == null ? 0 : r.sharedMaterials.Length);
        var uniqueMaterials = enabledRenderers
            .SelectMany(r => r.sharedMaterials ?? Array.Empty<Material>())
            .Where(m => m)
            .Distinct()
            .Count();
        var uniqueMeshes = enabledRenderers
            .Select(GetMeshFromRenderer)
            .Where(m => m)
            .Distinct()
            .Count();
        var staticBatchingReady = meshRenderers.Count(r =>
        {
            var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
            return (flags & StaticEditorFlags.BatchingStatic) != 0;
        });

        sb.AppendLine($"## Scene: {scene.path}");
        sb.AppendLine("### Summary");
        sb.AppendLine($"- Renderers: total={renderers.Length:N0}, enabled={enabledRenderers.Length:N0}, mesh={meshRenderers.Length:N0}, skinned={skinnedRenderers.Length:N0}");
        sb.AppendLine($"- Material slots: {materialSlots:N0}; unique materials={uniqueMaterials:N0}; unique meshes={uniqueMeshes:N0}");
        sb.AppendLine($"- Static batching flagged mesh renderers: {staticBatchingReady:N0}/{meshRenderers.Length:N0}");
        sb.AppendLine($"- Terrains: {terrains.Length:N0}; cameras={cameras.Length:N0}");
        sb.AppendLine();

        AppendCameraReport(sb, cameras);
        AppendTerrainReport(sb, terrains);
        AppendShaderReport(sb, enabledRenderers);
        AppendBatchCandidateReport(sb, enabledRenderers);
        AppendLodCandidateReport(sb, enabledRenderers);
        sb.AppendLine();
    }

    private static void AppendCameraReport(StringBuilder sb, Camera[] cameras)
    {
        sb.AppendLine("### Cameras");
        if (cameras.Length == 0)
        {
            sb.AppendLine("- No cameras found.");
            sb.AppendLine();
            return;
        }

        foreach (var camera in cameras.OrderBy(c => GetHierarchyPath(c.transform), StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine($"- {Escape(GetHierarchyPath(camera.transform))}: occlusion={camera.useOcclusionCulling}, utility={IsUtilityCamera(camera)}, targetTexture={(camera.targetTexture ? camera.targetTexture.name : "null")}");
        }
        sb.AppendLine();
    }

    private static void AppendTerrainReport(StringBuilder sb, Terrain[] terrains)
    {
        sb.AppendLine("### Terrains");
        if (terrains.Length == 0)
        {
            sb.AppendLine("- No terrains found.");
            sb.AppendLine();
            return;
        }

        foreach (var terrain in terrains.OrderBy(t => GetHierarchyPath(t.transform), StringComparer.OrdinalIgnoreCase))
        {
            sb.AppendLine(
                "- " + Escape(GetHierarchyPath(terrain.transform)) +
                $": pixelError={terrain.heightmapPixelError:0.##}, basemap={terrain.basemapDistance:0.#}, detailDistance={terrain.detailObjectDistance:0.#}, detailDensity={terrain.detailObjectDensity:0.##}, treeDistance={terrain.treeDistance:0.#}, instanced={terrain.drawInstanced}");
        }
        sb.AppendLine();
    }

    private static void AppendShaderReport(StringBuilder sb, Renderer[] enabledRenderers)
    {
        sb.AppendLine("### Materials / Shaders");
        var materials = enabledRenderers
            .SelectMany(r => r.sharedMaterials ?? Array.Empty<Material>())
            .Where(m => m)
            .Distinct()
            .ToArray();

        if (materials.Length == 0)
        {
            sb.AppendLine("- No materials found on enabled renderers.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Shader | Material count | Note |");
        sb.AppendLine("| --- | ---: | --- |");
        foreach (var group in materials.GroupBy(GetShaderName).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            var note = GetShaderNote(group.Key);
            sb.AppendLine($"| {Escape(group.Key)} | {group.Count()} | {Escape(note)} |");
        }
        sb.AppendLine();
    }

    private static void AppendBatchCandidateReport(StringBuilder sb, Renderer[] enabledRenderers)
    {
        sb.AppendLine("### Batch Reduction Candidates");
        var groups = enabledRenderers
            .Where(r => r && !(r is SkinnedMeshRenderer) && GetMeshFromRenderer(r))
            .GroupBy(GetBatchKey)
            .Select(CandidateGroup.From)
            .Where(g => g.Count >= 3)
            .OrderByDescending(g => g.TotalMaterialSlots)
            .ThenByDescending(g => g.Count)
            .Take(30)
            .ToArray();

        if (groups.Length == 0)
        {
            sb.AppendLine("- No repeated mesh/material groups large enough to report.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Count | Material slots | Static ready | Mesh | Materials | Example | Recommendation |");
        sb.AppendLine("| ---: | ---: | --- | --- | --- | --- | --- |");
        foreach (var group in groups)
        {
            sb.AppendLine($"| {group.Count} | {group.TotalMaterialSlots} | {group.StaticReady} | {Escape(group.MeshName)} | {Escape(group.MaterialNames)} | {Escape(group.ExamplePath)} | {Escape(group.Recommendation)} |");
        }
        sb.AppendLine();
    }

    private static void AppendLodCandidateReport(StringBuilder sb, Renderer[] enabledRenderers)
    {
        sb.AppendLine("### LOD Candidates");
        var candidates = enabledRenderers
            .Where(r => r && !(r is SkinnedMeshRenderer) && !HasLodInParents(r.transform))
            .Select(r => new { Renderer = r, Mesh = GetMeshFromRenderer(r) })
            .Where(x => x.Mesh && x.Mesh.vertexCount >= 2500)
            .OrderByDescending(x => x.Mesh.vertexCount)
            .Take(25)
            .ToArray();

        if (candidates.Length == 0)
        {
            sb.AppendLine("- No large no-LOD mesh renderers found.");
            sb.AppendLine();
            return;
        }

        sb.AppendLine("| Vertices | Renderer | Mesh | Note |");
        sb.AppendLine("| ---: | --- | --- | --- |");
        foreach (var candidate in candidates)
        {
            sb.AppendLine($"| {candidate.Mesh.vertexCount:N0} | {Escape(GetHierarchyPath(candidate.Renderer.transform))} | {Escape(candidate.Mesh.name)} | Add LODGroup or mesh simplification manually. |");
        }
        sb.AppendLine();
    }

    private static string GetShaderName(Material material)
    {
        if (!material)
            return "<null material>";
        if (!material.shader)
            return "<missing shader>";
        return material.shader.name;
    }

    private static string GetShaderNote(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName) ||
            shaderName.Equals("<missing shader>", StringComparison.OrdinalIgnoreCase) ||
            shaderName.IndexOf("InternalErrorShader", StringComparison.OrdinalIgnoreCase) >= 0)
            return "ERROR: will render pink. Restore/fix the material shader before optimizing.";

        if (shaderName.StartsWith("Universal Render Pipeline/", StringComparison.OrdinalIgnoreCase) ||
            shaderName.StartsWith("Shader Graphs/", StringComparison.OrdinalIgnoreCase))
            return "OK";

        return "Verify mobile/URP compatibility.";
    }

    private static string GetBatchKey(Renderer renderer)
    {
        var mesh = GetMeshFromRenderer(renderer);
        var meshPath = mesh ? AssetDatabase.GetAssetPath(mesh) : string.Empty;
        var meshId = string.IsNullOrEmpty(meshPath) && mesh ? mesh.name : meshPath;
        var materials = renderer.sharedMaterials ?? Array.Empty<Material>();
        var materialKey = string.Join("|", materials.Select(m =>
        {
            if (!m)
                return "<null>";
            var path = AssetDatabase.GetAssetPath(m);
            return string.IsNullOrEmpty(path) ? m.name : path;
        }));

        return meshId + "::" + materialKey;
    }

    private static bool HasLodInParents(Transform transform)
    {
        var current = transform;
        while (current)
        {
            if (current.GetComponent<LODGroup>())
                return true;
            current = current.parent;
        }

        return false;
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    private struct TerrainBudget : IEquatable<TerrainBudget>
    {
        public readonly float PixelError;
        public readonly float BasemapDistance;
        public readonly float DetailDistance;
        public readonly float DetailDensity;
        public readonly float TreeDistance;

        public TerrainBudget(float pixelError, float basemapDistance, float detailDistance, float detailDensity, float treeDistance)
        {
            PixelError = pixelError;
            BasemapDistance = basemapDistance;
            DetailDistance = detailDistance;
            DetailDensity = detailDensity;
            TreeDistance = treeDistance;
        }

        public static TerrainBudget From(Terrain terrain)
        {
            return new TerrainBudget(
                terrain.heightmapPixelError,
                terrain.basemapDistance,
                terrain.detailObjectDistance,
                terrain.detailObjectDensity,
                terrain.treeDistance);
        }

        public bool Equals(TerrainBudget other)
        {
            return Mathf.Approximately(PixelError, other.PixelError) &&
                   Mathf.Approximately(BasemapDistance, other.BasemapDistance) &&
                   Mathf.Approximately(DetailDistance, other.DetailDistance) &&
                   Mathf.Approximately(DetailDensity, other.DetailDensity) &&
                   Mathf.Approximately(TreeDistance, other.TreeDistance);
        }
    }

    private sealed class CandidateGroup
    {
        public int Count;
        public int TotalMaterialSlots;
        public bool StaticReady;
        public string MeshName;
        public string MaterialNames;
        public string ExamplePath;
        public string Recommendation;

        public static CandidateGroup From(IGrouping<string, Renderer> group)
        {
            var renderers = group.Where(r => r).ToArray();
            var first = renderers.First();
            var mesh = GetMeshFromRenderer(first);
            var allStaticReady = renderers.All(r =>
            {
                var flags = GameObjectUtility.GetStaticEditorFlags(r.gameObject);
                return (flags & StaticEditorFlags.BatchingStatic) != 0;
            });
            var materialNames = first.sharedMaterials == null
                ? string.Empty
                : string.Join(", ", first.sharedMaterials.Where(m => m).Select(m => m.name).Distinct());

            return new CandidateGroup
            {
                Count = renderers.Length,
                TotalMaterialSlots = renderers.Sum(r => r.sharedMaterials == null ? 0 : r.sharedMaterials.Length),
                StaticReady = allStaticReady,
                MeshName = mesh ? mesh.name : "<missing mesh>",
                MaterialNames = materialNames,
                ExamplePath = GetHierarchyPath(first.transform),
                Recommendation = allStaticReady
                    ? "Should batch through Unity static batching if materials are compatible."
                    : "Mark only verified non-moving environment objects static, then rerun."
            };
        }
    }

    private sealed class CameraRenderOptionsSnapshot
    {
        public readonly Component CameraData;
        public readonly Dictionary<string, bool> BooleanValues = new Dictionary<string, bool>();
        public readonly Dictionary<string, int> IntValues = new Dictionary<string, int>();
        public readonly Dictionary<string, int> EnumValues = new Dictionary<string, int>();

        public CameraRenderOptionsSnapshot(Component cameraData)
        {
            CameraData = cameraData;
        }
    }
}
#endif
