using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class NewSceneAddressablesSplitter
{
    private const string AuthoringScenePath = "Assets/Scenes/New Scene.unity";
    private const string GeneratedRootDirectory = "Assets/Scenes/Bundle_NewScene";
    private const string GeneratedSceneDirectory = GeneratedRootDirectory + "/Scenes";
    private const string GeneratedMainScenePath = GeneratedSceneDirectory + "/New Scene.unity";
    private const string LegacyGeneratedSceneDirectory = "Assets/Scenes/New Scene_AddressableGenerated";
    private const string LegacyLateSceneDirectory = "Assets/Scenes/New Scene_AddressableLate";
    private const string ReportDirectory = "Library/NewSceneAddressableSplit";
    private const string LoaderObjectName = "[New Scene Late Content Loader]";
    private const string BoxLoadObjectName = "boxLoad";
    private const string FirstEntryReadyMarkerName = "[New Scene First Entry Ready]";
    private const string EnvironmentRootPath = "Enviroment";
    private const string CanvasLoginPrefabPath = "Assets/Shaders/Prefabs_UI/Login/CanvasLogin.prefab";
    private const string XHeroDeepLinkHost = "xhero.deeplink";
    private const string XHeroAndroidPackageName = "com.xhero_app";

    private const string MainGroupName = "Cloud_New Scene";
    private const string LegacyLateGroupPrefix = "Cloud_New Scene_Late_";
    private const string LegacyInitialUiGroupName = "Cloud_New Scene_Initial_UI";
    private const string GeneratedMainAddress = "New Scene";
    private const string CloudLabel = "cloud";
    private const string MainLabel = "new_scene_entry";
    private const string LateLabel = "new_scene_late";
    private const string InitialLabel = "new_scene_initial";
    private const string SharedLabel = "new_scene_shared";
    private const string LateSharedLabel = "new_scene_late_shared";

    private const int MaxConcurrentWebRequests = 8;
    private const long MaxGroupBytes = 50L * 1024L * 1024L;
    private const long MinCandidateBytes = 2L * 1024L * 1024L;
    private const long MinEnvironmentChildLateBytes = 256L * 1024L;
    private const long SharedDependencyMinBytes = 256L * 1024L;
    private const long SharedDependencyMinDuplicateSavingsBytes = 1L * 1024L * 1024L;

    private static readonly string[] LateAssetPathPrefixes =
    {
        "Assets/Models_LMS/",
        "Assets/Models_LMS_Mobile/",
        "Assets/GD_SanhTruoc/",
        "Assets/Prefabs/Models/",
        "Assets/Prefabs/KY MON DON GIAP/",
        "Assets/mountain/",
        "Assets/tex/",
        "Assets/Model/Tree/",
        "Assets/Grass/",
        "Assets/Vefects/",
        "Assets/Tazo_fx/",
        "Assets/khutrungbayvatpham_TTB/",
        "Assets/Ky Mon Don Giap/"
    };

    private static readonly string[] GeneratedOnlyDiscardRootNames =
    {
        "testttt"
    };

    private static readonly string[] CriticalNameFragments =
    {
        "player",
        "camera",
        "canvas",
        "ui",
        "eventsystem",
        "manager",
        "controller",
        "handler",
        "bootstrap",
        "boot",
        "preload",
        "api",
        "session",
        "login",
        "logout",
        "auth",
        "token",
        "firebase",
        "qr",
        "deeplink",
        "register",
        "forgot",
        "resetpassword",
        "network",
        "minimap",
        "map",
        "course",
        "plot",
        "trigger",
        "teleport",
        "spawn",
        "start",
        "audio",
        "light",
        "terrain",
        "navmesh"
    };

    private static readonly string[] InitialMainHierarchyPaths =
    {
        BoxLoadObjectName,
        "Enviroment/NenSanhTruoc",
        "Enviroment/MB_Nen_Sau (1)",
        "Blocker sanh sau",
        "Blocker sanh truoc"
    };

    private static readonly string[][] PlannedEnvironmentLateBatches =
    {
        new[]
        {
            "Enviroment/Mot Goc Khuon Vien",
            "Enviroment/Mot Goc Khuon Vien (1)"
        },
        new[]
        {
            "Enviroment/Tuong_Thanh",
            "Enviroment/CongTrong",
            "Enviroment/MB_VachDa"
        },
        new[]
        {
            "Enviroment/Bon Cay Sanh",
            "Enviroment/Bon Cay Sanh (1)",
            "Enviroment/Cay Co Bon Hoa",
            "Enviroment/Cay Co Bon Hoa (1)",
            "Enviroment/CayCauLon",
            "Enviroment/BonHoaNho",
            "Enviroment/BonHoaNho (1)",
            "Enviroment/BonHoaNho (2)",
            "Enviroment/BonHoaNho (3)",
            "Enviroment/caygameS",
            "Enviroment/caygameS (1)",
            "Enviroment/caygameS (2)",
            "Enviroment/caygameS (3)"
        },
        new[]
        {
            "Enviroment/KMDG (1)",
            "Enviroment/KMDG (3)",
            "Enviroment/ChoiNho",
            "Enviroment/Hoa Sen Group",
            "Enviroment/Tru Den Group",
            "Enviroment/CauCauNho",
            "Enviroment/CauCauNho (1)",
            "Enviroment/Lan Can Group",
            "Enviroment/Tree Decor - Upper map",
            "Enviroment/GachToOng",
            "Enviroment/GachToOng (1)",
            "Enviroment/HoNuoc",
            "Enviroment/stone (1)",
            "Enviroment/MB_Nenbatquai",
            "Enviroment/MB_Nenbatquai (1)"
        }
    };

    private static readonly string[][] RuntimeEnvironmentLateBatches =
    {
        new[]
        {
            "Enviroment/khu_trung_bay_vat_pham",
            "Enviroment/khu_trung_bay_vat_pham (1)",
            "Enviroment/khu_trung_bay_vat_pham (2)",
            "Enviroment/khu_trung_bay_vat_pham (3)"
        },
        new[]
        {
            "Enviroment/Khu Co Hoc 1",
            "Enviroment/Khu Co Hoc 1 (1)"
        },
        new[]
        {
            "Enviroment/Upper House",
            "Enviroment/House",
            "Enviroment/Nha_T1",
            "Enviroment/Nha_T1 (1)",
            "Enviroment/Nha_T1 (2)",
            "Enviroment/Nha_T1 (3)"
        },
        new[]
        {
            "Enviroment/CongT2",
            "Enviroment/sold3_waterfall_high",
            "Enviroment/VFX_Water_Surface_Calm_02"
        }
    };

    private static readonly string[] InitialSharedUiPrefabPaths =
    {
        CanvasLoginPrefabPath,
        "Assets/Shaders/Prefabs_UI/Phong Tuyen SInh/PTS_ObjectManager.prefab",
        "Assets/Shaders/Prefabs_UI/Minimap_New/Plot Area UI.prefab",
        "Assets/Shaders/Prefabs_UI/Thanh Toan/Canvas Payment UI Canvas.prefab",
        "Assets/Shaders/Prefabs_UI/Minimap_New/Big Map UI.prefab",
        "Assets/Shaders/Prefabs_UI/Login_Popup/Login Popup UI.prefab",
        "Assets/Shaders/Prefabs_UI/Login_Popup/Success Login Popup UI Variant.prefab",
        "Assets/Shaders/Prefabs_UI/Login_Popup/Warning Login Popup UI Variant.prefab",
        "Assets/Shaders/Prefabs_UI/Login_Popup/Failed Login Popup UI Variant.prefab",
        "Assets/Shaders/Prefabs_UI/Minimap_New/Course Area UI.prefab",
        "Assets/Shaders/Prefabs_UI/Minimap_New/Find Course/Course Semi 3D UI.prefab"
    };

    private static readonly string[][] OrderedLateHierarchyBatches =
    {
        new[]
        {
            "Enviroment/Tuong_Thanh"
        },
        new[]
        {
            "Enviroment/CongTrong"
        },
        new[]
        {
            "Enviroment/MB_VachDa"
        },
        new[]
        {
            "Enviroment/CongT2"
        },
        new[]
        {
            "Enviroment/ToaChanhDien_3toa"
        },
        new[]
        {
            "Enviroment/Upper House"
        },
        new[]
        {
            "Enviroment/House"
        },
        new[]
        {
            "Enviroment/Nha_T1",
            "Enviroment/Nha_T1 (1)",
            "Enviroment/Nha_T1 (2)",
            "Enviroment/Nha_T1 (3)"
        },
        new[]
        {
            "Enviroment/Khu Co Hoc 1",
            "Enviroment/Khu Co Hoc 1 (1)"
        },
        new[]
        {
            "Enviroment/khu_trung_bay_vat_pham",
            "Enviroment/khu_trung_bay_vat_pham (1)",
            "Enviroment/khu_trung_bay_vat_pham (2)",
            "Enviroment/khu_trung_bay_vat_pham (3)"
        },
        new[]
        {
            "Enviroment/Mot Goc Khuon Vien",
            "Enviroment/Mot Goc Khuon Vien (1)"
        },
        new[]
        {
            "Enviroment/Bon Cay Sanh",
            "Enviroment/Bon Cay Sanh (1)",
            "Enviroment/Cay Co Bon Hoa",
            "Enviroment/Cay Co Bon Hoa (1)",
            "Enviroment/CayCauLon"
        },
        new[]
        {
            "Enviroment/BonHoaNho",
            "Enviroment/BonHoaNho (1)",
            "Enviroment/BonHoaNho (2)",
            "Enviroment/BonHoaNho (3)",
            "Enviroment/caygameS",
            "Enviroment/caygameS (1)",
            "Enviroment/caygameS (2)",
            "Enviroment/caygameS (3)"
        },
        new[]
        {
            "Enviroment/KMDG (1)",
            "Enviroment/KMDG (3)"
        },
        new[]
        {
            "Enviroment/ChoiNho",
            "Enviroment/Hoa Sen Group",
            "Enviroment/Tru Den Group",
            "Enviroment/CauCauNho",
            "Enviroment/CauCauNho (1)",
            "Enviroment/Lan Can Group"
        },
        new[]
        {
            "Enviroment/Tree Decor - Upper map",
            "Enviroment/GachToOng",
            "Enviroment/GachToOng (1)",
            "Enviroment/HoNuoc",
            "Enviroment/stone (1)",
            "Enviroment/MB_Nenbatquai",
            "Enviroment/MB_Nenbatquai (1)"
        },
        new[]
        {
            "Enviroment/sold3_waterfall_high",
            "Enviroment/VFX_Water_Surface_Calm_02"
        }
    };

    [MenuItem("Tools/Addressables/Regenerate Cloud_New Scene Group + Build Addressables")]
    public static void RegenerateCloudNewSceneGroupAndBuildAddressablesMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Regenerate Bundle_NewScene + Build",
                "This regenerates Assets/Scenes/Bundle_NewScene, refreshes the Cloud_New Scene group, then builds Addressables.",
                "Regenerate + Build",
                "Cancel"))
        {
            return;
        }

        RegenerateCloudNewSceneGroupAndBuildAddressables();
    }

    public static void RegenerateCloudNewSceneGroupAndBuildAddressables()
    {
        RegenerateCloudNewSceneGroup();
        AddressableAssetSettings.BuildPlayerContent();
        CreateCatalogAliasesForActiveBuildTarget();
        DeleteUnreferencedServerDataBundlesForActiveBuildTarget();
        Debug.Log("[NewSceneSplit] Regenerated Cloud_New Scene group and built Addressables.");
    }

    public static void LogNewSceneDownloadBudget()
    {
        Addressables.InitializeAsync().WaitForCompletion();

        LogDownloadBudgetForKey(GeneratedMainAddress);
        LogDownloadBudgetForKey("New Scene Late 01");
        LogDownloadBudgetForKey(MainLabel);
        LogDownloadBudgetForKey(LateLabel);
    }

    private static void LogDownloadBudgetForKey(object key)
    {
        var sizeHandle = Addressables.GetDownloadSizeAsync(key);
        long bytes = sizeHandle.WaitForCompletion();
        Addressables.Release(sizeHandle);
        Debug.Log("[NewSceneSplit] Download budget key=" + key + ", size=" + FormatBytes(bytes));
    }

    public static void RegenerateCloudNewSceneGroup()
    {
        EnsureReportDirectory();
        CleanupGeneratedAddressableOutputs(false);
        EnsureAssetFolder(GeneratedSceneDirectory);

        if (!AssetDatabase.CopyAsset(AuthoringScenePath, GeneratedMainScenePath))
            throw new InvalidOperationException("Failed to copy authoring scene to generated path: " + GeneratedMainScenePath);

        AssetDatabase.ImportAsset(GeneratedMainScenePath);

        Scene generatedMainScene = EditorSceneManager.OpenScene(GeneratedMainScenePath, OpenSceneMode.Single);
        RemoveLoaderObject(generatedMainScene);
        RemoveGeneratedOnlyDiscardRoots(generatedMainScene);
        ApplyGeneratedSceneOptimizations(generatedMainScene);
        ConfigureInitialSceneShell(generatedMainScene);

        List<string> generatedScenePaths = new List<string> { GeneratedMainScenePath };
        List<string> lateSceneKeys = new List<string>();
        StringBuilder splitReport = new StringBuilder();
        splitReport.AppendLine(
            "scene_key\tgameobject_path\tlocal_position\tlocal_rotation\tlocal_scale\tworld_position\tworld_rotation\tsibling_index");

        int nextLateIndex = 1;
        CreateFirstEntryReadyScene(ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);

        foreach (string[] batch in OrderedLateHierarchyBatches)
            SplitHierarchyPathBatch(generatedMainScene, batch, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);

        SplitRemainingEnvironmentVisualContent(generatedMainScene, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);
        SplitTerrainContent(generatedMainScene, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);
        SplitAutomaticRootCandidates(generatedMainScene, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);

        EnsureLateSceneLoader(generatedMainScene, lateSceneKeys);
        BakeAstarGraphForGeneratedScenes(generatedMainScene, lateSceneKeys);

        if (!EditorSceneManager.SaveScene(generatedMainScene, GeneratedMainScenePath))
            throw new InvalidOperationException("Failed to save generated main scene: " + GeneratedMainScenePath);

        PatchGeneratedMainSceneYaml();
        AssetDatabase.ImportAsset(GeneratedMainScenePath, ImportAssetOptions.ForceUpdate);

        NormalizeInitialLoginPrefabForAddressables();

        List<string> initialUiDependencyPaths = CollectInitialUiDependencyPaths();
        SharedGeneratedDependencySelection sharedGeneratedDependencyPaths =
            CollectSharedGeneratedDependencyPaths(generatedScenePaths, initialUiDependencyPaths);

        RegisterGeneratedAddressables(
            generatedScenePaths,
            lateSceneKeys,
            initialUiDependencyPaths,
            sharedGeneratedDependencyPaths);

        string splitReportPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            Path.Combine(ReportDirectory, "regenerate_cloud_new_scene.tsv"));
        File.WriteAllText(splitReportPath, splitReport.ToString());
        Debug.Log("[NewSceneSplit] Saved transform report: " + splitReportPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[NewSceneSplit] Regenerated Cloud_New Scene group. Authoring scene was not modified. Late scenes: "
                  + string.Join(", ", lateSceneKeys));
    }

    private static void BakeAstarGraphForGeneratedScenes(Scene generatedMainScene, IReadOnlyList<string> lateSceneKeys)
    {
        if (!generatedMainScene.IsValid() || !generatedMainScene.isLoaded)
            throw new InvalidOperationException("Generated main scene must be loaded before baking A* graph.");

        if (lateSceneKeys == null || lateSceneKeys.Count == 0)
            throw new InvalidOperationException("Cannot bake A* graph because no generated late scenes exist.");

        List<Scene> openedScenes = new List<Scene>();
        AstarPath previousActive = AstarPath.active;

        try
        {
            EnsureGeneratedLateScenesLoaded(lateSceneKeys, openedScenes);
            EditorSceneManager.SetActiveScene(generatedMainScene);
            Physics.SyncTransforms();
            Physics2D.SyncTransforms();

            AstarPath astar = FindAstarPathInScene(generatedMainScene);

            if (astar == null)
                throw new InvalidOperationException("Cannot bake A* graph because generated main scene has no AstarPath/Pathfinder object.");

            if (astar.data == null)
                throw new InvalidOperationException("Cannot bake A* graph because AstarPath.data is missing.");

            AstarPath.active = astar;
            astar.data.FindGraphTypes();

            if (astar.data.graphs == null || astar.data.graphs.Length == 0 || astar.data.graphTypes == null)
                astar.data.DeserializeGraphs();

            if (astar.data.graphs == null || astar.data.graphs.Length == 0)
                throw new InvalidOperationException("Cannot bake A* graph because no graph settings were found on the generated Pathfinder.");

            astar.data.UpdateShortcuts();

            float lastProgressUpdate = 0f;

            foreach (Pathfinding.Progress progress in astar.ScanAsync())
            {
                if (Time.realtimeSinceStartup - lastProgressUpdate > 0.2f)
                {
                    EditorUtility.DisplayProgressBar(
                        "New Scene A* Bake",
                        progress.description,
                        Mathf.Clamp01(progress.progress));
                    lastProgressUpdate = Time.realtimeSinceStartup;
                }
            }

            Pathfinding.Serialization.SerializeSettings serializeSettings = new Pathfinding.Serialization.SerializeSettings
            {
                nodes = true
            };

            uint checksum;
            byte[] bakedGraphData = astar.data.SerializeGraphs(serializeSettings, out checksum);

            if (bakedGraphData == null || bakedGraphData.Length == 0)
                throw new InvalidOperationException("A* graph bake produced empty serialized data.");

            astar.data.SetData(bakedGraphData);
            astar.scanOnStartup = false;
            DisableRuntimeAstarRescan(generatedMainScene);

            int nodeCount = 0;
            astar.data.GetNodes(_ => nodeCount++);

            EditorUtility.SetDirty(astar);
            EditorUtility.SetDirty(astar.gameObject);
            EditorSceneManager.MarkSceneDirty(generatedMainScene);

            Debug.Log("[NewSceneSplit] Baked A* graph into generated main scene. lateScenes="
                      + lateSceneKeys.Count
                      + ", graphs="
                      + astar.data.graphs.Count(graph => graph != null)
                      + ", nodes="
                      + nodeCount.ToString(CultureInfo.InvariantCulture)
                      + ", bytes="
                      + FormatBytes(bakedGraphData.Length)
                      + ", checksum="
                      + checksum.ToString(CultureInfo.InvariantCulture)
                      + ", scanOnStartup=false, runtimeRescan=false");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AstarPath.active = previousActive;

            for (int i = 0; i < openedScenes.Count; i++)
            {
                Scene scene = openedScenes[i];

                if (scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void EnsureGeneratedLateScenesLoaded(IReadOnlyList<string> lateSceneKeys, List<Scene> openedScenes)
    {
        for (int i = 0; i < lateSceneKeys.Count; i++)
        {
            string sceneKey = lateSceneKeys[i];
            string scenePath = GetGeneratedLateScenePath(sceneKey);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
                throw new FileNotFoundException("Generated late scene is missing before A* bake: " + scenePath);

            Scene loadedScene = SceneManager.GetSceneByName(sceneKey);

            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                if (!string.Equals(NormalizeAssetPath(loadedScene.path), NormalizeAssetPath(scenePath), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("A loaded scene named '"
                                                        + sceneKey
                                                        + "' does not match generated late scene path: "
                                                        + loadedScene.path);
                }

                continue;
            }

            Scene openedScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            if (!openedScene.IsValid() || !openedScene.isLoaded)
                throw new InvalidOperationException("Failed to load generated late scene for A* bake: " + scenePath);

            openedScenes.Add(openedScene);
        }
    }

    private static string GetGeneratedLateScenePath(string sceneKey)
    {
        return GeneratedSceneDirectory + "/" + sceneKey + ".unity";
    }

    private static string NormalizeAssetPath(string assetPath)
    {
        return string.IsNullOrEmpty(assetPath)
            ? string.Empty
            : assetPath.Replace('\\', '/');
    }

    private static AstarPath FindAstarPathInScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            AstarPath astar = root.GetComponentInChildren<AstarPath>(true);

            if (astar != null)
                return astar;
        }

        return null;
    }

    private static void DisableRuntimeAstarRescan(Scene generatedMainScene)
    {
        AddressableAdditiveSceneLoader loader = null;

        foreach (GameObject root in generatedMainScene.GetRootGameObjects())
        {
            loader = root.GetComponentInChildren<AddressableAdditiveSceneLoader>(true);

            if (loader != null)
                break;
        }

        if (loader == null)
            return;

        SerializedObject serialized = new SerializedObject(loader);
        SerializedProperty rescanProperty = serialized.FindProperty("rescanAstarAfterLateScenes");

        if (rescanProperty != null)
        {
            rescanProperty.boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loader);
        }
    }

    public static void RestoreAuthoringSceneAndCleanGeneratedAssets()
    {
        EnsureReportDirectory();

        Scene mainScene = EditorSceneManager.OpenScene(AuthoringScenePath, OpenSceneMode.Single);
        RemoveLoaderObject(mainScene);

        Dictionary<string, Queue<string>> restorePathsByScene = LoadRestorePathsByScene();
        int restoredCount = RestoreLegacyLateScenesIntoAuthoringScene(mainScene, restorePathsByScene);

        if (!EditorSceneManager.SaveScene(mainScene, AuthoringScenePath))
            throw new InvalidOperationException("Failed to save restored authoring scene: " + AuthoringScenePath);

        CleanupGeneratedAddressableOutputs(true);
        RegisterAuthoringSceneAsAddressable();
        SetCloudSharedPacking(BundledAssetGroupSchema.BundlePackingMode.PackTogether);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[NewSceneSplit] Restored authoring scene and cleaned generated assets. Objects restored: " + restoredCount);
    }

    private static void SplitAutomaticRootCandidates(
        Scene generatedMainScene,
        ref int nextLateIndex,
        List<string> lateSceneKeys,
        List<string> generatedScenePaths,
        StringBuilder splitReport)
    {
        List<RootInfo> candidates = AnalyzeRoots(generatedMainScene)
            .Where(r => r.MoveCandidate)
            .OrderByDescending(r => r.EstimatedBytes)
            .ToList();

        foreach (List<RootInfo> batch in BuildBatches(candidates))
        {
            List<GameObject> objects = batch
                .Where(r => r.GameObject != null)
                .Select(r => r.GameObject)
                .ToList();

            CreateLateSceneFromObjects(objects, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);
        }
    }

    private static void ConfigureInitialSceneShell(Scene scene)
    {
        GameObject boxLoad = FindByHierarchyPath(scene, BoxLoadObjectName);

        if (boxLoad == null)
        {
            Debug.LogWarning("[NewSceneSplit] boxLoad was not found in generated scene. Mobile late-load cover will be unavailable.");
            return;
        }

        if (boxLoad.activeSelf)
            boxLoad.SetActive(false);

        EditorUtility.SetDirty(boxLoad);
    }

    private static void SplitRemainingEnvironmentVisualContent(
        Scene generatedMainScene,
        ref int nextLateIndex,
        List<string> lateSceneKeys,
        List<string> generatedScenePaths,
        StringBuilder splitReport)
    {
        GameObject environment = FindByHierarchyPath(generatedMainScene, EnvironmentRootPath);

        if (environment == null)
            return;

        List<RootInfo> candidates = environment.transform
            .Cast<Transform>()
            .Select(t => BuildRootInfo(t.gameObject))
            .Where(info => ShouldMoveEnvironmentChild(info, out _))
            .OrderByDescending(info => info.EstimatedBytes)
            .ToList();

        foreach (List<RootInfo> batch in BuildBatches(candidates))
        {
            List<GameObject> objects = batch
                .Where(info => info.GameObject != null)
                .Select(info => info.GameObject)
                .ToList();

            CreateLateSceneFromObjects(objects, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);
        }
    }

    private static bool ShouldMoveEnvironmentChild(RootInfo info, out string reason)
    {
        if (info.GameObject == null)
        {
            reason = "missing object";
            return false;
        }

        string hierarchyPath = GetHierarchyPath(info.GameObject);

        if (IsInitialMainHierarchyPath(hierarchyPath) || WouldMoveInitialMainHierarchy(info.GameObject))
        {
            reason = "initial shell";
            return false;
        }

        string normalizedName = Normalize(info.Name);

        if (CriticalNameFragments.Any(fragment => normalizedName.Contains(fragment)))
        {
            reason = "critical name";
            return false;
        }

        if (info.EstimatedBytes < MinEnvironmentChildLateBytes)
        {
            reason = "small";
            return false;
        }

        if (!info.HasRenderer)
        {
            reason = "no renderer";
            return false;
        }

        if (info.HasCriticalUnityComponent)
        {
            reason = "critical Unity component";
            return false;
        }

        if (!info.UsesLateAssetPath)
        {
            reason = "not visual-path content";
            return false;
        }

        reason = "remaining environment visual";
        return true;
    }

    private static void SplitTerrainContent(
        Scene generatedMainScene,
        ref int nextLateIndex,
        List<string> lateSceneKeys,
        List<string> generatedScenePaths,
        StringBuilder splitReport)
    {
        List<GameObject> terrainRoots = CollectTerrainMoveRoots(generatedMainScene);

        if (terrainRoots.Count == 0)
            return;

        List<GameObject> terrainBatch = new List<GameObject>(terrainRoots);
        AddGpuiTerrainManagersForTerrainRoots(generatedMainScene, terrainRoots, terrainBatch);

        CreateLateSceneFromObjects(terrainBatch, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);

        Debug.Log("[NewSceneSplit] Moved terrain content to late scenes: "
                  + string.Join(", ", terrainBatch.Select(GetHierarchyPath)));
    }

    private static List<GameObject> CollectTerrainMoveRoots(Scene scene)
    {
        List<GameObject> result = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
                AddTerrainMoveRoot(terrain.gameObject, scene, result);

            foreach (TerrainCollider terrainCollider in root.GetComponentsInChildren<TerrainCollider>(true))
                AddTerrainMoveRoot(terrainCollider.gameObject, scene, result);
        }

        return result
            .OrderBy(GetHierarchyPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddTerrainMoveRoot(GameObject terrainObject, Scene scene, List<GameObject> result)
    {
        if (terrainObject == null)
            return;

        GameObject moveRoot = GetTerrainMoveRoot(terrainObject, scene);

        if (moveRoot == null)
            return;

        string movePath = GetHierarchyPath(moveRoot);

        if (IsInitialMainHierarchyPath(movePath) || WouldMoveInitialMainHierarchy(moveRoot))
        {
            Debug.LogWarning("[NewSceneSplit] Terrain move skipped because it would include initial shell object: " + movePath);
            return;
        }

        for (int i = result.Count - 1; i >= 0; i--)
        {
            GameObject existing = result[i];

            if (existing == null)
            {
                result.RemoveAt(i);
                continue;
            }

            if (moveRoot.transform.IsChildOf(existing.transform))
                return;

            if (existing.transform.IsChildOf(moveRoot.transform))
                result.RemoveAt(i);
        }

        result.Add(moveRoot);
    }

    private static GameObject GetTerrainMoveRoot(GameObject terrainObject, Scene scene)
    {
        GameObject prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(terrainObject);

        if (IsValidTerrainMoveRoot(prefabRoot, scene))
            return prefabRoot;

        Transform current = terrainObject.transform;

        while (current.parent != null && current.parent.parent != null)
        {
            GameObject parent = current.parent.gameObject;

            if (WouldMoveInitialMainHierarchy(parent))
                break;

            current = current.parent;
        }

        return IsValidTerrainMoveRoot(current.gameObject, scene)
            ? current.gameObject
            : null;
    }

    private static void AddGpuiTerrainManagersForTerrainRoots(
        Scene scene,
        IReadOnlyList<GameObject> terrainRoots,
        List<GameObject> moveRoots)
    {
        if (terrainRoots == null || terrainRoots.Count == 0)
            return;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || !IsGpuiTerrainManagerComponent(component))
                    continue;

                if (!ReferencesAnyTerrainRoot(component, terrainRoots))
                    continue;

                GameObject managerRoot = GetGpuiManagerMoveRoot(component, scene);

                if (managerRoot == null)
                    continue;

                if (AddRelatedMoveRoot(moveRoots, managerRoot))
                {
                    Debug.Log("[NewSceneSplit] Moved GPUI terrain manager with terrain late scene: "
                              + GetHierarchyPath(managerRoot));
                }
            }
        }
    }

    private static bool IsGpuiTerrainManagerComponent(MonoBehaviour component)
    {
        Type componentType = component.GetType();
        string fullName = componentType.FullName ?? string.Empty;

        if (!fullName.StartsWith("GPUInstancerPro.", StringComparison.Ordinal))
            return false;

        SerializedObject serialized = new SerializedObject(component);
        return serialized.FindProperty("_gpuiTerrains") != null;
    }

    private static bool ReferencesAnyTerrainRoot(MonoBehaviour manager, IReadOnlyList<GameObject> terrainRoots)
    {
        SerializedObject serialized = new SerializedObject(manager);
        SerializedProperty terrainReferences = serialized.FindProperty("_gpuiTerrains");

        if (terrainReferences == null || !terrainReferences.isArray)
            return false;

        for (int i = 0; i < terrainReferences.arraySize; i++)
        {
            UnityEngine.Object reference = terrainReferences.GetArrayElementAtIndex(i).objectReferenceValue;
            GameObject referencedGameObject = GetReferencedGameObject(reference);

            if (referencedGameObject != null && IsUnderAnyRoot(referencedGameObject, terrainRoots))
                return true;
        }

        return false;
    }

    private static GameObject GetReferencedGameObject(UnityEngine.Object reference)
    {
        if (reference == null)
            return null;

        if (reference is GameObject gameObject)
            return gameObject;

        if (reference is Component component)
            return component.gameObject;

        return null;
    }

    private static bool IsUnderAnyRoot(GameObject gameObject, IReadOnlyList<GameObject> roots)
    {
        foreach (GameObject root in roots)
        {
            if (root == null)
                continue;

            if (gameObject == root || gameObject.transform.IsChildOf(root.transform))
                return true;
        }

        return false;
    }

    private static GameObject GetGpuiManagerMoveRoot(MonoBehaviour manager, Scene scene)
    {
        GameObject managerObject = manager.gameObject;

        if (managerObject == null || managerObject.scene != scene)
            return null;

        if (IsValidTerrainMoveRoot(managerObject, scene))
            return managerObject;

        Debug.LogWarning("[NewSceneSplit] GPUI terrain manager was kept in main scene because moving it would include initial shell object: "
                         + GetHierarchyPath(managerObject));
        return null;
    }

    private static bool AddRelatedMoveRoot(List<GameObject> moveRoots, GameObject moveRoot)
    {
        if (moveRoot == null)
            return false;

        string movePath = GetHierarchyPath(moveRoot);

        if (IsInitialMainHierarchyPath(movePath) || WouldMoveInitialMainHierarchy(moveRoot))
        {
            Debug.LogWarning("[NewSceneSplit] Related terrain move skipped because it would include initial shell object: " + movePath);
            return false;
        }

        for (int i = moveRoots.Count - 1; i >= 0; i--)
        {
            GameObject existing = moveRoots[i];

            if (existing == null)
            {
                moveRoots.RemoveAt(i);
                continue;
            }

            if (moveRoot.transform.IsChildOf(existing.transform))
                return false;

            if (existing.transform.IsChildOf(moveRoot.transform))
                moveRoots.RemoveAt(i);
        }

        moveRoots.Add(moveRoot);
        return true;
    }

    private static bool IsValidTerrainMoveRoot(GameObject go, Scene scene)
    {
        if (go == null)
            return false;

        if (go.scene != scene)
            return false;

        string path = GetHierarchyPath(go);
        return !IsInitialMainHierarchyPath(path) && !WouldMoveInitialMainHierarchy(go);
    }

    private static void SplitHierarchyPathBatch(
        Scene generatedMainScene,
        IReadOnlyList<string> hierarchyPaths,
        ref int nextLateIndex,
        List<string> lateSceneKeys,
        List<string> generatedScenePaths,
        StringBuilder splitReport)
    {
        List<GameObject> objects = new List<GameObject>();

        foreach (string hierarchyPath in hierarchyPaths)
        {
            GameObject go = FindByHierarchyPath(generatedMainScene, hierarchyPath);

            if (go == null)
            {
                Debug.LogWarning("[NewSceneSplit] Generated split object not found: " + hierarchyPath);
                continue;
            }

            string currentPath = GetHierarchyPath(go);

            if (IsInitialMainHierarchyPath(currentPath))
            {
                Debug.Log("[NewSceneSplit] Keeping initial shell object in main scene: " + currentPath);
                continue;
            }

            if (!objects.Contains(go))
                objects.Add(go);
        }

        CreateLateSceneFromObjects(objects, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);
    }

    private static void CreateFirstEntryReadyScene(
        ref int nextLateIndex,
        List<string> lateSceneKeys,
        List<string> generatedScenePaths,
        StringBuilder splitReport)
    {
        string sceneName = "New Scene Late " + nextLateIndex.ToString("00", CultureInfo.InvariantCulture);
        string scenePath = GeneratedSceneDirectory + "/" + sceneName + ".unity";
        nextLateIndex++;

        Scene markerScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject marker = new GameObject(FirstEntryReadyMarkerName);
        SceneManager.MoveGameObjectToScene(marker, markerScene);

        if (!EditorSceneManager.SaveScene(markerScene, scenePath))
            throw new InvalidOperationException("Failed to save generated first-entry marker scene: " + scenePath);

        AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);

        generatedScenePaths.Add(scenePath);
        lateSceneKeys.Add(sceneName);

        splitReport.Append(sceneName);
        splitReport.Append('\t');
        splitReport.Append(FirstEntryReadyMarkerName);
        splitReport.Append("\t\t\t\t\t\t");
        splitReport.AppendLine();
    }

    private static void CreateLateSceneFromObjects(
        List<GameObject> objects,
        ref int nextLateIndex,
        List<string> lateSceneKeys,
        List<string> generatedScenePaths,
        StringBuilder splitReport)
    {
        if (objects.Count == 0)
            return;

        string sceneName = "New Scene Late " + nextLateIndex.ToString("00", CultureInfo.InvariantCulture);
        string scenePath = GeneratedSceneDirectory + "/" + sceneName + ".unity";
        nextLateIndex++;

        Scene lateScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        foreach (GameObject go in objects)
        {
            string originalPath = GetHierarchyPath(go);
            TransformSnapshot transformSnapshot = CaptureTransformSnapshot(go.transform);
            List<TransformSnapshot> parentSnapshots = CaptureParentSnapshots(go.transform);

            // Detach without changing the world transform. The original local
            // transform is restored after the duplicate parent chain exists.
            go.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(go, lateScene);

            GameObject parent = FindOrCreateTransformHierarchy(lateScene, parentSnapshots);

            if (parent != null)
                go.transform.SetParent(parent.transform, false);

            ApplyObjectSnapshot(go, transformSnapshot);
            ApplyTransformSnapshot(go.transform, transformSnapshot);
            ApplyWorldTransformIfNeeded(go.transform, transformSnapshot);

            int clearedBatchingStaticCount = ClearBatchingStaticRecursive(parent != null ? parent : go);

            if (clearedBatchingStaticCount > 0)
            {
                Debug.Log("[NewSceneSplit] Disabled runtime static batching for late scene object '"
                          + originalPath
                          + "' render hierarchy count="
                          + clearedBatchingStaticCount);
            }

            splitReport.Append(sceneName);
            splitReport.Append('\t');
            splitReport.Append(EscapeTsv(originalPath));
            splitReport.Append('\t');
            splitReport.Append(FormatVector3(transformSnapshot.LocalPosition));
            splitReport.Append('\t');
            splitReport.Append(FormatQuaternion(transformSnapshot.LocalRotation));
            splitReport.Append('\t');
            splitReport.Append(FormatVector3(transformSnapshot.LocalScale));
            splitReport.Append('\t');
            splitReport.Append(FormatVector3(transformSnapshot.WorldPosition));
            splitReport.Append('\t');
            splitReport.Append(FormatQuaternion(transformSnapshot.WorldRotation));
            splitReport.Append('\t');
            splitReport.Append(transformSnapshot.SiblingIndex.ToString(CultureInfo.InvariantCulture));
            splitReport.AppendLine();
        }

        if (!EditorSceneManager.SaveScene(lateScene, scenePath))
            throw new InvalidOperationException("Failed to save generated late scene: " + scenePath);

        ScrubGeneratedSceneStaticBatchCacheYaml(scenePath);
        AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);

        generatedScenePaths.Add(scenePath);
        lateSceneKeys.Add(sceneName);

        Debug.Log("[NewSceneSplit] Saved late scene transforms. scene="
                  + sceneName
                  + ", objects="
                  + objects.Count.ToString(CultureInfo.InvariantCulture)
                  + ", path="
                  + scenePath);
    }

    private static List<TransformSnapshot> CaptureParentSnapshots(Transform transform)
    {
        List<TransformSnapshot> snapshots = new List<TransformSnapshot>();
        Transform current = transform.parent;

        while (current != null)
        {
            snapshots.Add(CaptureTransformSnapshot(current));
            current = current.parent;
        }

        snapshots.Reverse();
        return snapshots;
    }

    private static TransformSnapshot CaptureTransformSnapshot(Transform transform)
    {
        GameObject go = transform.gameObject;

        return new TransformSnapshot
        {
            Name = go.name,
            LocalPosition = transform.localPosition,
            LocalRotation = transform.localRotation,
            LocalScale = transform.localScale,
            WorldPosition = transform.position,
            WorldRotation = transform.rotation,
            SiblingIndex = transform.GetSiblingIndex(),
            ActiveSelf = go.activeSelf,
            Layer = go.layer,
            Tag = go.tag,
            StaticFlags = GameObjectUtility.GetStaticEditorFlags(go)
        };
    }

    private static GameObject FindOrCreateTransformHierarchy(Scene scene, IReadOnlyList<TransformSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0)
            return null;

        GameObject current = null;

        for (int i = 0; i < snapshots.Count; i++)
        {
            TransformSnapshot snapshot = snapshots[i];

            if (current == null)
            {
                current = scene.GetRootGameObjects()
                    .Where(go => string.Equals(go.name, snapshot.Name, StringComparison.Ordinal))
                    .OrderBy(go => Math.Abs(go.transform.GetSiblingIndex() - snapshot.SiblingIndex))
                    .FirstOrDefault();

                if (current == null)
                {
                    current = new GameObject(snapshot.Name);
                    SceneManager.MoveGameObjectToScene(current, scene);
                }
            }
            else
            {
                Transform child = current.transform.Cast<Transform>()
                    .Where(t => string.Equals(t.name, snapshot.Name, StringComparison.Ordinal))
                    .OrderBy(t => Math.Abs(t.GetSiblingIndex() - snapshot.SiblingIndex))
                    .FirstOrDefault();

                if (child == null)
                {
                    GameObject created = new GameObject(snapshot.Name);
                    created.transform.SetParent(current.transform, false);
                    child = created.transform;
                }

                current = child.gameObject;
            }

            ApplyObjectSnapshot(current, snapshot);
            ApplyTransformSnapshot(current.transform, snapshot);
            SetSiblingIndexSafe(current.transform, snapshot.SiblingIndex);
        }

        return current;
    }

    private static void ApplyObjectSnapshot(GameObject go, TransformSnapshot snapshot)
    {
        go.layer = snapshot.Layer;

        try
        {
            go.tag = snapshot.Tag;
        }
        catch (UnityException)
        {
            go.tag = "Untagged";
        }

        GameObjectUtility.SetStaticEditorFlags(go, snapshot.StaticFlags);
        go.SetActive(snapshot.ActiveSelf);
        EditorUtility.SetDirty(go);
    }

    private static void ApplyTransformSnapshot(Transform transform, TransformSnapshot snapshot)
    {
        transform.localPosition = snapshot.LocalPosition;
        transform.localRotation = snapshot.LocalRotation;
        transform.localScale = snapshot.LocalScale;
        EditorUtility.SetDirty(transform);
    }

    private static void ApplyWorldTransformIfNeeded(Transform transform, TransformSnapshot snapshot)
    {
        if (Approximately(transform.position, snapshot.WorldPosition)
            && Approximately(transform.rotation, snapshot.WorldRotation))
        {
            return;
        }

        // A recreated parent can have the same name but a different transform.
        // Keep the object at its authored world position in that case.
        transform.SetPositionAndRotation(snapshot.WorldPosition, snapshot.WorldRotation);
        EditorUtility.SetDirty(transform);
    }

    private static void SetSiblingIndexSafe(Transform transform, int siblingIndex)
    {
        if (transform == null || siblingIndex < 0)
            return;

        transform.SetSiblingIndex(Mathf.Min(siblingIndex, transform.parent != null
            ? transform.parent.childCount - 1
            : transform.root.gameObject.scene.GetRootGameObjects().Length - 1));
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude <= 0.000001f;
    }

    private static bool Approximately(Quaternion a, Quaternion b)
    {
        return Quaternion.Angle(a, b) <= 0.01f;
    }

    private static int ClearBatchingStaticRecursive(GameObject root)
    {
        int changedCount = 0;

        foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
        {
            GameObject go = transform.gameObject;
            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);

            if ((flags & StaticEditorFlags.BatchingStatic) == 0)
                continue;

            GameObjectUtility.SetStaticEditorFlags(go, flags & ~StaticEditorFlags.BatchingStatic);
            EditorUtility.SetDirty(go);
            changedCount++;
        }

        return changedCount;
    }

    private static int RestoreLegacyLateScenesIntoAuthoringScene(
        Scene mainScene,
        Dictionary<string, Queue<string>> restorePathsByScene)
    {
        int restoredCount = 0;
        string absoluteLateDirectory = ToAbsolutePath(LegacyLateSceneDirectory);

        if (!Directory.Exists(absoluteLateDirectory))
            return restoredCount;

        foreach (string absoluteScenePath in Directory.GetFiles(absoluteLateDirectory, "New Scene Late *.unity").OrderBy(p => p))
        {
            string scenePath = ToAssetPath(absoluteScenePath);
            string sceneKey = Path.GetFileNameWithoutExtension(scenePath);
            Scene lateScene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            Queue<string> restorePaths = restorePathsByScene.TryGetValue(sceneKey, out Queue<string> queue)
                ? queue
                : new Queue<string>();

            foreach (GameObject root in lateScene.GetRootGameObjects().ToList())
            {
                string originalPath = restorePaths.Count > 0 ? restorePaths.Dequeue() : root.name;
                MoveObjectBackToAuthoringScene(mainScene, root, originalPath);
                restoredCount++;
            }

            EditorSceneManager.CloseScene(lateScene, false);
        }

        return restoredCount;
    }

    private static void MoveObjectBackToAuthoringScene(Scene mainScene, GameObject go, string originalPath)
    {
        TransformSnapshot transformSnapshot = CaptureTransformSnapshot(go.transform);
        go.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(go, mainScene);

        string parentPath = GetParentPath(originalPath);

        if (string.IsNullOrEmpty(parentPath))
            return;

        GameObject parent = FindOrCreateByHierarchyPath(mainScene, parentPath);
        go.transform.SetParent(parent.transform, false);
        ApplyTransformSnapshot(go.transform, transformSnapshot);
        ApplyWorldTransformIfNeeded(go.transform, transformSnapshot);
    }

    private static Dictionary<string, Queue<string>> LoadRestorePathsByScene()
    {
        Dictionary<string, Queue<string>> pathsByScene = new Dictionary<string, Queue<string>>(StringComparer.OrdinalIgnoreCase);

        LoadRestoreReport(pathsByScene, "split.tsv", 0, 2);
        LoadRestoreReport(pathsByScene, "split_additional.tsv", 0, 1);
        LoadRestoreReport(pathsByScene, "split_planned_environment.tsv", 0, 1);
        LoadRestoreReport(pathsByScene, "split_runtime_environment.tsv", 0, 1);

        return pathsByScene;
    }

    private static void LoadRestoreReport(
        Dictionary<string, Queue<string>> pathsByScene,
        string reportFileName,
        int sceneKeyColumn,
        int pathColumn)
    {
        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), ReportDirectory, reportFileName);

        if (!File.Exists(reportPath))
            return;

        foreach (string line in File.ReadLines(reportPath).Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] columns = line.Split('\t');

            if (columns.Length <= Math.Max(sceneKeyColumn, pathColumn))
                continue;

            string sceneKey = columns[sceneKeyColumn];
            string hierarchyPath = columns[pathColumn];

            if (!pathsByScene.TryGetValue(sceneKey, out Queue<string> queue))
            {
                queue = new Queue<string>();
                pathsByScene.Add(sceneKey, queue);
            }

            queue.Enqueue(hierarchyPath);
        }
    }

    private static void NormalizeInitialLoginPrefabForAddressables()
    {
        GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasLoginPrefabPath);

        if (prefabAsset == null)
        {
            Debug.LogWarning("[NewSceneSplit] Cannot normalize CanvasLogin deeplink config. Prefab not found: " + CanvasLoginPrefabPath);
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(CanvasLoginPrefabPath);
        bool changed = false;

        try
        {
            foreach (LmsDeepLinkAuthUI deepLinkAuthUI in prefabRoot.GetComponentsInChildren<LmsDeepLinkAuthUI>(true))
            {
                changed |= SetIfDifferent(ref deepLinkAuthUI.xheroScheme, "xhero");
                changed |= SetIfDifferent(ref deepLinkAuthUI.xheroHost, XHeroDeepLinkHost);
                changed |= SetIfDifferent(ref deepLinkAuthUI.xheroAndroidHost, XHeroDeepLinkHost);
                changed |= SetIfDifferent(ref deepLinkAuthUI.xheroIosHost, XHeroDeepLinkHost);
                changed |= SetIfDifferent(ref deepLinkAuthUI.xheroPath, "");
                changed |= SetIfDifferent(ref deepLinkAuthUI.xheroAndroidPackageName, XHeroAndroidPackageName);
                changed |= SetIfDifferent(ref deepLinkAuthUI.androidStorePackageName, XHeroAndroidPackageName);
            }

            if (!changed)
                return;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, CanvasLoginPrefabPath);
            AssetDatabase.ImportAsset(CanvasLoginPrefabPath, ImportAssetOptions.ForceUpdate);
            Debug.Log("[NewSceneSplit] Normalized CanvasLogin XHero deeplink config before Addressables registration.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool SetIfDifferent(ref string value, string expected)
    {
        expected = expected ?? "";

        if (string.Equals(value ?? "", expected, StringComparison.Ordinal))
            return false;

        value = expected;
        return true;
    }

    private static List<string> CollectInitialUiDependencyPaths()
    {
        HashSet<string> dependencyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string prefabPath in InitialSharedUiPrefabPaths)
        {
            UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(prefabPath);

            if (prefab == null)
            {
                Debug.LogWarning("[NewSceneSplit] Initial UI prefab not found: " + prefabPath);
                continue;
            }

            dependencyPaths.Add(prefabPath);

            foreach (UnityEngine.Object dependency in EditorUtility.CollectDependencies(new[] { prefab }))
            {
                if (dependency == null)
                    continue;

                string dependencyPath = AssetDatabase.GetAssetPath(dependency);

                if (ShouldMoveInitialUiDependency(dependencyPath))
                    dependencyPaths.Add(dependencyPath);
            }
        }

        return dependencyPaths
            .Where(path => !AssetDatabase.IsValidFolder(path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static SharedGeneratedDependencySelection CollectSharedGeneratedDependencyPaths(
        List<string> generatedScenePaths,
        List<string> initialUiDependencyPaths)
    {
        HashSet<string> excludedPaths = new HashSet<string>(
            initialUiDependencyPaths ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        HashSet<string> mainSceneDependencies = generatedScenePaths.Count > 0
            ? new HashSet<string>(AssetDatabase.GetDependencies(generatedScenePaths[0], true), StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Dictionary<string, int> sceneReferenceCounts =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (string scenePath in generatedScenePaths)
        {
            HashSet<string> sceneDependencies = new HashSet<string>(
                AssetDatabase.GetDependencies(scenePath, true),
                StringComparer.OrdinalIgnoreCase);

            foreach (string dependencyPath in sceneDependencies)
            {
                if (!ShouldMoveSharedGeneratedDependency(dependencyPath, excludedPaths))
                    continue;

                if (!sceneReferenceCounts.TryGetValue(dependencyPath, out int count))
                    count = 0;

                sceneReferenceCounts[dependencyPath] = count + 1;
            }
        }

        List<SharedDependencyInfo> sharedDependencies = sceneReferenceCounts
            .Where(pair => pair.Value >= 2)
            .Select(pair => new SharedDependencyInfo(pair.Key, pair.Value, GetAssetFileBytes(pair.Key)))
            .Where(info => ShouldPromoteSharedDependency(info))
            .OrderByDescending(info => info.DuplicateSavingsBytes)
            .ThenByDescending(info => info.AssetBytes)
            .ThenBy(info => info.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<SharedDependencyInfo> initialSharedDependencies = sharedDependencies
            .Where(info => mainSceneDependencies.Contains(info.AssetPath))
            .ToList();

        List<SharedDependencyInfo> lateOnlySharedDependencies = sharedDependencies
            .Where(info => !mainSceneDependencies.Contains(info.AssetPath))
            .ToList();

        SharedGeneratedDependencySelection selection = new SharedGeneratedDependencySelection();
        selection.InitialSharedPaths.AddRange(initialSharedDependencies.Select(info => info.AssetPath));
        selection.LateOnlySharedPaths.AddRange(lateOnlySharedDependencies.Select(info => info.AssetPath));

        List<string> sharedPaths = sharedDependencies
            .Select(info => info.AssetPath)
            .ToList();

        long totalBytes = sharedPaths.Sum(GetAssetFileBytes);
        long estimatedDuplicateSavingsBytes = sharedDependencies.Sum(info => info.DuplicateSavingsBytes);
        long initialSharedBytes = initialSharedDependencies.Sum(info => info.AssetBytes);
        long lateOnlySharedBytes = lateOnlySharedDependencies.Sum(info => info.AssetBytes);

        Debug.Log("[NewSceneSplit] Shared generated dependencies: "
                  + sharedPaths.Count
                  + " assets, approx "
                  + FormatBytes(totalBytes)
                  + ", estimated duplicate savings "
                  + FormatBytes(estimatedDuplicateSavingsBytes)
                  + ". Initial-required: "
                  + selection.InitialSharedPaths.Count
                  + " assets / "
                  + FormatBytes(initialSharedBytes)
                  + ". Late-only: "
                  + selection.LateOnlySharedPaths.Count
                  + " assets / "
                  + FormatBytes(lateOnlySharedBytes));

        WriteSharedDependencyReport(sharedDependencies, "shared_generated_dependencies.tsv");
        WriteSharedDependencyReport(initialSharedDependencies, "shared_initial_dependencies.tsv");
        WriteSharedDependencyReport(lateOnlySharedDependencies, "shared_late_only_dependencies.tsv");

        return selection;
    }

    private static void WriteSharedDependencyReport(List<SharedDependencyInfo> sharedDependencies, string fileName)
    {
        StringBuilder report = new StringBuilder();
        report.AppendLine("asset_path\treference_count\tasset_bytes\testimated_duplicate_savings_bytes");

        foreach (SharedDependencyInfo info in sharedDependencies)
        {
            report.Append(EscapeTsv(info.AssetPath));
            report.Append('\t');
            report.Append(info.ReferenceCount.ToString(CultureInfo.InvariantCulture));
            report.Append('\t');
            report.Append(info.AssetBytes.ToString(CultureInfo.InvariantCulture));
            report.Append('\t');
            report.Append(info.DuplicateSavingsBytes.ToString(CultureInfo.InvariantCulture));
            report.AppendLine();
        }

        File.WriteAllText(
            Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(ReportDirectory, fileName)),
            report.ToString());
    }

    private static bool ShouldPromoteSharedDependency(SharedDependencyInfo info)
    {
        if (info.AssetBytes <= 0L)
            return false;

        return info.AssetBytes >= SharedDependencyMinBytes
               || info.DuplicateSavingsBytes >= SharedDependencyMinDuplicateSavingsBytes;
    }

    private static void RegisterGeneratedAddressables(
        List<string> generatedScenePaths,
        List<string> lateSceneKeys,
        List<string> initialUiDependencyPaths,
        SharedGeneratedDependencySelection sharedGeneratedDependencyPaths)
    {
        AddressableAssetSettings settings = GetAddressableSettings();
        ConfigureGlobalAddressableRuntimeSettings(settings);

        AddressableAssetGroup mainGroup = GetOrCreateGroup(settings, MainGroupName);
        ConfigureRemoteGroup(settings, mainGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel);
        SetCloudSharedPacking(BundledAssetGroupSchema.BundlePackingMode.PackSeparately);

        settings.AddLabel(CloudLabel, false);
        settings.AddLabel(MainLabel, false);
        settings.AddLabel(LateLabel, false);
        settings.AddLabel(InitialLabel, false);
        settings.AddLabel(SharedLabel, false);
        settings.AddLabel(LateSharedLabel, false);
        RemoveStaleLateNumberedLabels(settings, lateSceneKeys.Count);

        RemoveEntries(mainGroup, entry =>
            string.Equals(entry.address, GeneratedMainAddress, StringComparison.OrdinalIgnoreCase)
            || entry.address.StartsWith("New Scene Late ", StringComparison.OrdinalIgnoreCase)
            || entry.labels.Contains(CloudLabel)
            || entry.labels.Contains(MainLabel)
            || entry.labels.Contains(LateLabel)
            || entry.labels.Contains(InitialLabel)
            || entry.labels.Contains(SharedLabel)
            || entry.labels.Contains(LateSharedLabel)
            || IsGeneratedAssetPath(AssetDatabase.GUIDToAssetPath(entry.guid)));

        for (int i = 0; i < generatedScenePaths.Count; i++)
        {
            string scenePath = generatedScenePaths[i];
            string sceneAddress = i == 0 ? GeneratedMainAddress : lateSceneKeys[i - 1];
            AddressableAssetEntry entry = CreateOrMoveEntry(settings, mainGroup, scenePath, sceneAddress);
            ClearGeneratedEntryLabels(entry);

            if (i == 0)
            {
                entry.SetLabel(MainLabel, true, false, true);
            }
            else
            {
                string numberedLabel = LateLabel + "_" + i.ToString("00", CultureInfo.InvariantCulture);
                settings.AddLabel(numberedLabel, false);
                entry.SetLabel(LateLabel, true, false, true);
                entry.SetLabel(numberedLabel, true, false, true);
            }
        }

        foreach (string assetPath in initialUiDependencyPaths)
        {
            AddressableAssetEntry previousEntry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
            string previousAddress = previousEntry != null ? previousEntry.address : null;
            AddressableAssetEntry entry = CreateOrMoveEntry(
                settings,
                mainGroup,
                assetPath,
                string.IsNullOrEmpty(previousAddress) ? assetPath : previousAddress);

            ClearGeneratedEntryLabels(entry);
            entry.SetLabel(InitialLabel, true, false, true);
        }

        RegisterSharedGeneratedDependencyEntries(
            settings,
            mainGroup,
            sharedGeneratedDependencyPaths.InitialSharedPaths,
            SharedLabel);

        RegisterSharedGeneratedDependencyEntries(
            settings,
            mainGroup,
            sharedGeneratedDependencyPaths.LateOnlySharedPaths,
            LateSharedLabel);

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
    }

    private static void RegisterSharedGeneratedDependencyEntries(
        AddressableAssetSettings settings,
        AddressableAssetGroup mainGroup,
        IEnumerable<string> assetPaths,
        string label)
    {
        foreach (string assetPath in assetPaths)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrEmpty(guid))
                continue;

            if (settings.FindAssetEntry(guid) != null)
                continue;

            AddressableAssetEntry entry = CreateOrMoveEntry(settings, mainGroup, assetPath, assetPath);
            ClearGeneratedEntryLabels(entry);
            entry.SetLabel(label, true, false, true);
        }
    }

    private static void ClearGeneratedEntryLabels(AddressableAssetEntry entry)
    {
        if (entry == null)
            return;

        foreach (string label in entry.labels.ToList())
        {
            if (IsGeneratedNewSceneLabel(label))
                entry.SetLabel(label, false, false, true);
        }
    }

    private static bool IsGeneratedNewSceneLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
            return false;

        return string.Equals(label, CloudLabel, StringComparison.OrdinalIgnoreCase)
               || string.Equals(label, MainLabel, StringComparison.OrdinalIgnoreCase)
               || string.Equals(label, LateLabel, StringComparison.OrdinalIgnoreCase)
               || string.Equals(label, InitialLabel, StringComparison.OrdinalIgnoreCase)
               || string.Equals(label, SharedLabel, StringComparison.OrdinalIgnoreCase)
               || string.Equals(label, LateSharedLabel, StringComparison.OrdinalIgnoreCase)
               || label.StartsWith(LateLabel + "_", StringComparison.OrdinalIgnoreCase);
    }

    private static void RemoveStaleLateNumberedLabels(AddressableAssetSettings settings, int currentLateSceneCount)
    {
        if (settings == null)
            return;

        string numberedPrefix = LateLabel + "_";
        List<string> labels = settings.GetLabels();

        foreach (string label in labels.ToList())
        {
            if (string.IsNullOrEmpty(label)
                || !label.StartsWith(numberedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string numberText = label.Substring(numberedPrefix.Length);

            if (!int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int labelIndex))
                continue;

            if (labelIndex <= currentLateSceneCount)
                continue;

            settings.RemoveLabel(label, false);
        }
    }

    private static void RegisterAuthoringSceneAsAddressable()
    {
        AddressableAssetSettings settings = GetAddressableSettings();
        ConfigureGlobalAddressableRuntimeSettings(settings);

        AddressableAssetGroup mainGroup = GetOrCreateGroup(settings, MainGroupName);
        ConfigureRemoteGroup(settings, mainGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogether);

        settings.AddLabel(CloudLabel, false);
        settings.AddLabel(MainLabel, false);

        string authoringGuid = AssetDatabase.AssetPathToGUID(AuthoringScenePath);
        RemoveEntries(mainGroup, entry =>
            !string.Equals(entry.guid, authoringGuid, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(entry.address, GeneratedMainAddress, StringComparison.OrdinalIgnoreCase)
                || entry.address.StartsWith("New Scene Late ", StringComparison.OrdinalIgnoreCase)
                || entry.labels.Contains(CloudLabel)
                || entry.labels.Contains(MainLabel)
                || entry.labels.Contains(LateLabel)
                || entry.labels.Contains(InitialLabel)
                || entry.labels.Contains(SharedLabel)
                || entry.labels.Contains(LateSharedLabel)
                || IsGeneratedAssetPath(AssetDatabase.GUIDToAssetPath(entry.guid))));

        AddressableAssetEntry authoringEntry = settings.CreateOrMoveEntry(authoringGuid, mainGroup, false, false);
        authoringEntry.address = GeneratedMainAddress;
        ClearGeneratedEntryLabels(authoringEntry);
        authoringEntry.SetLabel(MainLabel, true, false, true);

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, authoringEntry, true, true);
    }

    private static AddressableAssetEntry CreateOrMoveEntry(
        AddressableAssetSettings settings,
        AddressableAssetGroup group,
        string assetPath,
        string address)
    {
        string guid = AssetDatabase.AssetPathToGUID(assetPath);

        if (string.IsNullOrEmpty(guid))
            throw new InvalidOperationException("Missing guid for addressable asset: " + assetPath);

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
        entry.address = address;

        return entry;
    }

    private static void CleanupGeneratedAddressableOutputs(bool deleteLegacyLateScenes)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings != null)
        {
            foreach (AddressableAssetGroup group in settings.groups.Where(IsGeneratedOrLegacyGroup).ToList())
            {
                string groupPath = AssetDatabase.GetAssetPath(group);
                settings.RemoveGroup(group);

                if (!string.IsNullOrEmpty(groupPath))
                    AssetDatabase.DeleteAsset(groupPath);
            }
        }

        DeleteGeneratedSchemaAssets();
        DeleteAssetIfExists(GeneratedRootDirectory);
        DeleteAssetIfExists(LegacyGeneratedSceneDirectory);

        if (deleteLegacyLateScenes)
            DeleteAssetIfExists(LegacyLateSceneDirectory);
    }

    private static void DeleteGeneratedSchemaAssets()
    {
        string schemasDirectory = ToAbsolutePath("Assets/AddressableAssetsData/AssetGroups/Schemas");

        if (!Directory.Exists(schemasDirectory))
            return;

        foreach (string path in Directory.GetFiles(schemasDirectory, LegacyLateGroupPrefix + "*", SearchOption.TopDirectoryOnly))
            DeleteAssetIfExists(ToAssetPath(path));

        foreach (string path in Directory.GetFiles(schemasDirectory, LegacyInitialUiGroupName + "*", SearchOption.TopDirectoryOnly))
            DeleteAssetIfExists(ToAssetPath(path));
    }

    private static bool IsGeneratedOrLegacyGroup(AddressableAssetGroup group)
    {
        if (group == null)
            return false;

        return group.Name.StartsWith(LegacyLateGroupPrefix, StringComparison.Ordinal)
               || string.Equals(group.Name, LegacyInitialUiGroupName, StringComparison.Ordinal)
               || group.Name.StartsWith(MainGroupName + "/Late", StringComparison.Ordinal);
    }

    private static void SetCloudSharedPacking(BundledAssetGroupSchema.BundlePackingMode bundleMode)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
            return;

        AddressableAssetGroup sharedGroup = settings.FindGroup("Cloud_Shared");

        if (sharedGroup == null)
            return;

        BundledAssetGroupSchema bundled = sharedGroup.GetSchema<BundledAssetGroupSchema>();

        if (bundled == null)
            return;

        bundled.BundleMode = bundleMode;
        EditorUtility.SetDirty(bundled);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
    }

    private static void ApplyGeneratedSceneOptimizations(Scene scene)
    {
        Debug.Log("[NewSceneSplit] Preserve scene mode: material and render settings optimizations are disabled.");
    }

    private static void PatchGeneratedMainSceneYaml()
    {
        Debug.Log("[NewSceneSplit] Preserve scene mode: YAML skybox/occlusion patch is disabled.");
        ScrubGeneratedSceneStaticBatchCacheYaml(GeneratedMainScenePath);
    }

    private static void ScrubGeneratedSceneStaticBatchCacheYaml(string scenePath)
    {
        if (string.IsNullOrWhiteSpace(scenePath))
            return;

        string absolutePath = ToAbsolutePath(scenePath);

        if (!File.Exists(absolutePath))
            return;

        string yaml = File.ReadAllText(absolutePath);
        string patched = Regex.Replace(
            yaml,
            @"(?m)^  m_StaticBatchInfo:\r?\n    firstSubMesh: -?\d+\r?\n    subMeshCount: -?\d+",
            "  m_StaticBatchInfo:\n    firstSubMesh: 0\n    subMeshCount: 0");

        patched = Regex.Replace(
            patched,
            @"(?m)^  m_StaticBatchRoot: \{fileID: -?\d+\}",
            "  m_StaticBatchRoot: {fileID: 0}");

        if (!string.Equals(yaml, patched, StringComparison.Ordinal))
            File.WriteAllText(absolutePath, patched);
    }

    private static void RemoveEntries(AddressableAssetGroup group, Func<AddressableAssetEntry, bool> predicate)
    {
        foreach (AddressableAssetEntry entry in group.entries.Where(predicate).ToList())
            group.RemoveAssetEntry(entry);
    }

    private static bool IsGeneratedAssetPath(string assetPath)
    {
        return !string.IsNullOrEmpty(assetPath)
               && (assetPath.StartsWith(GeneratedRootDirectory + "/", StringComparison.OrdinalIgnoreCase)
                   || assetPath.StartsWith(LegacyGeneratedSceneDirectory + "/", StringComparison.OrdinalIgnoreCase)
                   || assetPath.StartsWith(LegacyLateSceneDirectory + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static List<RootInfo> AnalyzeRoots(Scene scene)
    {
        List<RootInfo> roots = new List<RootInfo>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            RootInfo info = BuildRootInfo(root);
            info.MoveCandidate = ShouldMoveRoot(info, out string reason);
            info.Reason = reason;
            roots.Add(info);
        }

        return roots
            .OrderByDescending(r => r.EstimatedBytes)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static RootInfo BuildRootInfo(GameObject root)
    {
        HashSet<string> dependencyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long estimatedBytes = 0L;

        UnityEngine.Object[] dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { root });

        foreach (UnityEngine.Object dependency in dependencies)
        {
            if (dependency == null)
                continue;

            string path = AssetDatabase.GetAssetPath(dependency);

            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
                continue;

            if (!dependencyPaths.Add(path))
                continue;

            estimatedBytes += GetAssetFileBytes(path);
        }

        Component[] components = root.GetComponentsInChildren<Component>(true);
        bool hasRenderer = components.Any(c => c is Renderer);
        bool hasTerrain = components.Any(c => c is Terrain || c is TerrainCollider);
        bool hasCriticalUnityComponent = components.Any(IsCriticalUnityComponent);
        bool hasRuntimeMonoBehaviour = components.Any(HasRuntimeMonoBehaviour);
        bool usesLateAssetPath = dependencyPaths.Any(IsLateAssetPath);

        return new RootInfo
        {
            GameObject = root,
            Name = root.name,
            EstimatedBytes = estimatedBytes,
            DependencyPaths = dependencyPaths.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList(),
            HasRenderer = hasRenderer,
            HasTerrain = hasTerrain,
            HasCriticalUnityComponent = hasCriticalUnityComponent,
            HasRuntimeMonoBehaviour = hasRuntimeMonoBehaviour,
            UsesLateAssetPath = usesLateAssetPath
        };
    }

    private static bool ShouldMoveRoot(RootInfo info, out string reason)
    {
        if (info.GameObject == null)
        {
            reason = "missing root";
            return false;
        }

        string normalizedName = Normalize(info.Name);
        string hierarchyPath = GetHierarchyPath(info.GameObject);

        if (IsInitialMainHierarchyPath(hierarchyPath))
        {
            reason = "initial shell";
            return false;
        }

        if (CriticalNameFragments.Any(fragment => normalizedName.Contains(fragment)))
        {
            reason = "critical name";
            return false;
        }

        if (info.EstimatedBytes < MinCandidateBytes)
        {
            reason = "small";
            return false;
        }

        if (!info.HasRenderer)
        {
            reason = "no renderer";
            return false;
        }

        if (info.HasTerrain)
        {
            reason = "terrain kept in main scene";
            return false;
        }

        if (info.HasCriticalUnityComponent)
        {
            reason = "critical Unity component";
            return false;
        }

        if (info.HasRuntimeMonoBehaviour)
        {
            reason = "runtime script";
            return false;
        }

        if (!info.UsesLateAssetPath)
        {
            reason = "not model-path content";
            return false;
        }

        reason = "large visual model";
        return true;
    }

    private static List<List<RootInfo>> BuildBatches(List<RootInfo> candidates)
    {
        List<List<RootInfo>> batches = new List<List<RootInfo>>();
        List<long> batchBytes = new List<long>();

        foreach (RootInfo candidate in candidates)
        {
            int bestIndex = -1;
            long bestRemaining = long.MaxValue;

            for (int i = 0; i < batches.Count; i++)
            {
                long nextBytes = batchBytes[i] + candidate.EstimatedBytes;

                if (nextBytes > MaxGroupBytes)
                    continue;

                long remaining = MaxGroupBytes - nextBytes;

                if (remaining < bestRemaining)
                {
                    bestRemaining = remaining;
                    bestIndex = i;
                }
            }

            if (bestIndex < 0)
            {
                batches.Add(new List<RootInfo> { candidate });
                batchBytes.Add(candidate.EstimatedBytes);
            }
            else
            {
                batches[bestIndex].Add(candidate);
                batchBytes[bestIndex] += candidate.EstimatedBytes;
            }
        }

        return batches;
    }

    private static void EnsureLateSceneLoader(Scene scene, List<string> lateSceneKeys)
    {
        GameObject loaderObject = scene
            .GetRootGameObjects()
            .FirstOrDefault(go => go.name == LoaderObjectName);

        if (loaderObject == null)
            loaderObject = new GameObject(LoaderObjectName);

        SceneManager.MoveGameObjectToScene(loaderObject, scene);

        AddressableAdditiveSceneLoader loader = loaderObject.GetComponent<AddressableAdditiveSceneLoader>();

        if (loader == null)
            loader = loaderObject.AddComponent<AddressableAdditiveSceneLoader>();

        SerializedObject serialized = new SerializedObject(loader);
        serialized.FindProperty("loadOnStart").boolValue = true;
        serialized.FindProperty("initialDelaySeconds").floatValue = 0f;
        serialized.FindProperty("delayBetweenScenesSeconds").floatValue = 0f;
        serialized.FindProperty("loadScenesDirectly").boolValue = true;
        serialized.FindProperty("maxConcurrentSceneLoads").intValue = 3;
        serialized.FindProperty("loadCachedScenesWithoutDelay").boolValue = true;
        serialized.FindProperty("cachedMaxConcurrentSceneLoads").intValue = 4;
        serialized.FindProperty("cachedDelayBetweenScenesSeconds").floatValue = 0f;
        serialized.FindProperty("cachedDependencyCheckTimeoutSeconds").floatValue = 3f;
        serialized.FindProperty("downloadDependenciesTogether").boolValue = false;
        serialized.FindProperty("loadSceneAsSoonAsDependenciesReady").boolValue = true;
        serialized.FindProperty("showBlockingOverlayUntilLoaded").boolValue = false;
        serialized.FindProperty("blockingOverlayText").stringValue = "Đang dựng thế giới...";
        serialized.FindProperty("minimumOverlaySeconds").floatValue = 0.1f;
        serialized.FindProperty("controlBoxLoad").boolValue = true;
        serialized.FindProperty("boxLoadObjectName").stringValue = BoxLoadObjectName;
        serialized.FindProperty("boxLoadOnlyOnMobile").boolValue = true;
        serialized.FindProperty("showBoxLoadOnlyWhenDependenciesMissing").boolValue = true;
        serialized.FindProperty("rescanAstarAfterLateScenes").boolValue = true;
        serialized.FindProperty("astarRescanDelaySeconds").floatValue = 0.1f;

        SerializedProperty sceneKeysProperty = serialized.FindProperty("sceneKeys");
        sceneKeysProperty.arraySize = lateSceneKeys.Count;

        for (int i = 0; i < lateSceneKeys.Count; i++)
            sceneKeysProperty.GetArrayElementAtIndex(i).stringValue = lateSceneKeys[i];

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loaderObject);
    }

    private static void CreateCatalogAliasesForActiveBuildTarget()
    {
        string outputDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "ServerData",
            EditorUserBuildSettings.activeBuildTarget.ToString());

        if (!Directory.Exists(outputDirectory))
        {
            Debug.LogWarning("[NewSceneSplit] Addressables output directory not found: " + outputDirectory);
            return;
        }

        string versionedJson = Directory
            .GetFiles(outputDirectory, "catalog_*.json", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        string versionedHash = Directory
            .GetFiles(outputDirectory, "catalog_*.hash", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(versionedJson) || string.IsNullOrEmpty(versionedHash))
        {
            Debug.LogWarning("[NewSceneSplit] Versioned catalog files not found in: " + outputDirectory);
            return;
        }

        string catalogDirectory = Path.GetDirectoryName(versionedJson);

        if (string.IsNullOrEmpty(catalogDirectory))
        {
            Debug.LogWarning("[NewSceneSplit] Cannot resolve catalog directory for: " + versionedJson);
            return;
        }

        File.Copy(versionedJson, Path.Combine(catalogDirectory, "catalog.json"), true);
        File.Copy(versionedHash, Path.Combine(catalogDirectory, "catalog.hash"), true);

        Debug.Log("[NewSceneSplit] Updated catalog aliases from "
                  + Path.GetFileName(versionedJson)
                  + " and "
                  + Path.GetFileName(versionedHash));
    }

    private static void DeleteUnreferencedServerDataBundlesForActiveBuildTarget()
    {
        string outputDirectory = Path.Combine(
            Directory.GetCurrentDirectory(),
            "ServerData",
            EditorUserBuildSettings.activeBuildTarget.ToString());

        string catalogPath = Path.Combine(outputDirectory, "catalog.json");

        if (!File.Exists(catalogPath))
        {
            Debug.LogWarning("[NewSceneSplit] Cannot clean ServerData bundles because catalog.json is missing: " + catalogPath);
            return;
        }

        string catalogJson = File.ReadAllText(catalogPath);
        HashSet<string> referencedBundles = new HashSet<string>(
            catalogJson.Split('"')
                .Where(token => token.IndexOf(".bundle", StringComparison.OrdinalIgnoreCase) >= 0)
                .Select(GetFileNameFromCatalogToken)
                .Where(fileName => !string.IsNullOrEmpty(fileName)),
            StringComparer.OrdinalIgnoreCase);

        int deletedCount = 0;
        long deletedBytes = 0L;

        foreach (string bundlePath in Directory.GetFiles(outputDirectory, "*.bundle", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(bundlePath);

            if (referencedBundles.Contains(fileName))
                continue;

            FileInfo fileInfo = new FileInfo(bundlePath);
            deletedBytes += fileInfo.Length;
            fileInfo.Delete();
            deletedCount++;
        }

        Debug.Log("[NewSceneSplit] Cleaned stale ServerData bundles: "
                  + deletedCount
                  + " files, "
                  + FormatBytes(deletedBytes));
    }

    private static string GetFileNameFromCatalogToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return "";

        token = token.Replace('\\', '/');

        int queryIndex = token.IndexOf('?');

        if (queryIndex >= 0)
            token = token.Substring(0, queryIndex);

        int slashIndex = token.LastIndexOf('/');

        token = slashIndex >= 0 ? token.Substring(slashIndex + 1) : token;

        int bundleIndex = token.IndexOf(".bundle", StringComparison.OrdinalIgnoreCase);

        if (bundleIndex < 0)
            return "";

        return token.Substring(0, bundleIndex + ".bundle".Length);
    }

    private static void RemoveLoaderObject(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects().Where(go => go.name == LoaderObjectName).ToList())
            UnityEngine.Object.DestroyImmediate(root);
    }

    private static void RemoveGeneratedOnlyDiscardRoots(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects().ToList())
        {
            if (!GeneratedOnlyDiscardRootNames.Any(name => string.Equals(root.name, name, StringComparison.Ordinal)))
                continue;

            Debug.Log("[NewSceneSplit] Removed generated-only discard root from main scene: " + root.name);
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static void ConfigureRemoteGroup(
        AddressableAssetSettings settings,
        AddressableAssetGroup group,
        BundledAssetGroupSchema.BundlePackingMode bundleMode)
    {
        BundledAssetGroupSchema bundled = group.GetSchema<BundledAssetGroupSchema>();

        if (bundled == null)
            bundled = group.AddSchema<BundledAssetGroupSchema>();

        bundled.BuildPath.SetVariableByName(settings, "Remote.BuildPath");
        bundled.LoadPath.SetVariableByName(settings, "Remote.LoadPath");
        bundled.BundleMode = bundleMode;
        bundled.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
        bundled.IncludeInBuild = true;
        bundled.UseAssetBundleCache = true;
        bundled.UseAssetBundleCrc = true;
        bundled.UseAssetBundleCrcForCachedBundles = true;
        EditorUtility.SetDirty(bundled);

        ContentUpdateGroupSchema contentUpdate = group.GetSchema<ContentUpdateGroupSchema>();

        if (contentUpdate == null)
            contentUpdate = group.AddSchema<ContentUpdateGroupSchema>();

        contentUpdate.StaticContent = false;
        EditorUtility.SetDirty(contentUpdate);
        EditorUtility.SetDirty(group);
    }

    private static AddressableAssetSettings GetAddressableSettings()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

        if (settings == null)
            throw new InvalidOperationException("AddressableAssetSettingsDefaultObject.Settings is null.");

        return settings;
    }

    private static void ConfigureGlobalAddressableRuntimeSettings(AddressableAssetSettings settings)
    {
        if (settings == null)
            return;

        if (settings.MaxConcurrentWebRequests == MaxConcurrentWebRequests)
            return;

        settings.MaxConcurrentWebRequests = MaxConcurrentWebRequests;
        EditorUtility.SetDirty(settings);
        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);

        Debug.Log("[NewSceneSplit] Addressables MaxConcurrentWebRequests set to "
                  + MaxConcurrentWebRequests);
    }

    private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName)
    {
        AddressableAssetGroup group = settings.FindGroup(groupName);

        if (group != null)
            return group;

        return settings.CreateGroup(
            groupName,
            false,
            false,
            false,
            null,
            typeof(BundledAssetGroupSchema),
            typeof(ContentUpdateGroupSchema));
    }

    private static bool IsCriticalUnityComponent(Component component)
    {
        return component is Camera
               || component is AudioListener
               || component is Canvas
               || component is EventSystem
               || component is Light
               || component is ReflectionProbe
               || component is UnityEngine.AI.NavMeshAgent;
    }

    private static bool HasRuntimeMonoBehaviour(Component component)
    {
        MonoBehaviour monoBehaviour = component as MonoBehaviour;

        if (monoBehaviour == null)
            return false;

        MonoScript script = MonoScript.FromMonoBehaviour(monoBehaviour);

        if (script == null)
            return true;

        Type scriptClass = script.GetClass();

        if (scriptClass == null)
            return true;

        string className = scriptClass.FullName ?? scriptClass.Name;

        return className.IndexOf("GeneratedColliderMarker", StringComparison.Ordinal) < 0;
    }

    private static bool IsLateAssetPath(string path)
    {
        return LateAssetPathPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldMoveInitialUiDependency(string path)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.StartsWith("Assets/Scenes/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("Assets/Scripts/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("Assets/AddressableAssetsData/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return path.StartsWith("Assets/UI_XHeroLMS/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("Assets/Shaders/Prefabs_UI/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("Assets/Prefabs_UI/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("Assets/TextMesh Pro/", StringComparison.OrdinalIgnoreCase)
               || path.StartsWith("Assets/Fonts/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldMoveSharedGeneratedDependency(string path, HashSet<string> excludedPaths)
    {
        if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (excludedPaths != null && excludedPaths.Contains(path))
            return false;

        if (AssetDatabase.IsValidFolder(path))
            return false;

        if (path.StartsWith(GeneratedRootDirectory + "/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("Assets/Scenes/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("Assets/Scripts/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("Assets/Editor/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("Assets/AddressableAssetsData/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        string extension = Path.GetExtension(path);

        if (string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".asmdef", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".unity", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static long GetAssetFileBytes(string assetPath)
    {
        string absolutePath = ToAbsolutePath(assetPath);

        if (File.Exists(absolutePath))
            return new FileInfo(absolutePath).Length;

        return 0L;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024L * 1024L)
            return (bytes / (1024f * 1024f)).ToString("0.##", CultureInfo.InvariantCulture) + " MB";

        if (bytes >= 1024L)
            return (bytes / 1024f).ToString("0.##", CultureInfo.InvariantCulture) + " KB";

        return bytes.ToString(CultureInfo.InvariantCulture) + " B";
    }

    private static string ToAbsolutePath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
    }

    private static string ToAssetPath(string absolutePath)
    {
        string projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/').TrimEnd('/');
        string normalized = Path.GetFullPath(absolutePath).Replace('\\', '/');

        if (!normalized.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
            return normalized;

        return normalized.Substring(projectRoot.Length + 1);
    }

    private static void EnsureAssetFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];

            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);

            current = next;
        }
    }

    private static void EnsureReportDirectory()
    {
        Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), ReportDirectory));
    }

    private static void DeleteAssetIfExists(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath) || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);
    }

    private static GameObject FindByHierarchyPath(Scene scene, string hierarchyPath)
    {
        string[] parts = hierarchyPath.Split('/');

        if (parts.Length == 0)
            return null;

        GameObject current = scene.GetRootGameObjects()
            .FirstOrDefault(go => string.Equals(go.name, parts[0], StringComparison.Ordinal));

        if (current == null)
            return null;

        for (int i = 1; i < parts.Length; i++)
        {
            Transform child = current.transform.Cast<Transform>()
                .FirstOrDefault(t => string.Equals(t.name, parts[i], StringComparison.Ordinal));

            if (child == null)
                return null;

            current = child.gameObject;
        }

        return current;
    }

    private static GameObject FindOrCreateByHierarchyPath(Scene scene, string hierarchyPath)
    {
        string[] parts = hierarchyPath.Split('/');

        if (parts.Length == 0)
            throw new ArgumentException("Hierarchy path is empty.", nameof(hierarchyPath));

        GameObject current = scene.GetRootGameObjects()
            .FirstOrDefault(go => string.Equals(go.name, parts[0], StringComparison.Ordinal));

        if (current == null)
        {
            current = new GameObject(parts[0]);
            SceneManager.MoveGameObjectToScene(current, scene);
        }

        for (int i = 1; i < parts.Length; i++)
        {
            Transform child = current.transform.Cast<Transform>()
                .FirstOrDefault(t => string.Equals(t.name, parts[i], StringComparison.Ordinal));

            if (child == null)
            {
                GameObject created = new GameObject(parts[i]);
                created.transform.SetParent(current.transform, false);
                child = created.transform;
            }

            current = child.gameObject;
        }

        return current;
    }

    private static string GetHierarchyPath(GameObject go)
    {
        Stack<string> names = new Stack<string>();
        Transform current = go.transform;

        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static bool IsInitialMainHierarchyPath(string hierarchyPath)
    {
        if (string.IsNullOrEmpty(hierarchyPath))
            return false;

        for (int i = 0; i < InitialMainHierarchyPaths.Length; i++)
        {
            string initialPath = InitialMainHierarchyPaths[i];

            if (string.Equals(hierarchyPath, initialPath, StringComparison.OrdinalIgnoreCase))
                return true;

            if (hierarchyPath.StartsWith(initialPath + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool WouldMoveInitialMainHierarchy(GameObject root)
    {
        if (root == null)
            return false;

        string rootPath = GetHierarchyPath(root);

        for (int i = 0; i < InitialMainHierarchyPaths.Length; i++)
        {
            string initialPath = InitialMainHierarchyPaths[i];

            if (string.Equals(rootPath, initialPath, StringComparison.OrdinalIgnoreCase))
                return true;

            if (initialPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string GetParentPath(string hierarchyPath)
    {
        int index = hierarchyPath.LastIndexOf('/');

        if (index < 0)
            return "";

        return hierarchyPath.Substring(0, index);
    }

    private static string EscapeTsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string FormatVector3(Vector3 value)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:R},{1:R},{2:R}",
            value.x,
            value.y,
            value.z);
    }

    private static string FormatQuaternion(Quaternion value)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0:R},{1:R},{2:R},{3:R}",
            value.x,
            value.y,
            value.z,
            value.w);
    }

    private static string Normalize(string value)
    {
        return (value ?? "").ToLowerInvariant().Replace(" ", "");
    }

    private sealed class RootInfo
    {
        public GameObject GameObject;
        public string Name;
        public long EstimatedBytes;
        public List<string> DependencyPaths;
        public bool HasRenderer;
        public bool HasTerrain;
        public bool HasCriticalUnityComponent;
        public bool HasRuntimeMonoBehaviour;
        public bool UsesLateAssetPath;
        public bool MoveCandidate;
        public string Reason;
    }

    private sealed class SharedDependencyInfo
    {
        public SharedDependencyInfo(string assetPath, int referenceCount, long assetBytes)
        {
            AssetPath = assetPath;
            ReferenceCount = referenceCount;
            AssetBytes = assetBytes;
        }

        public string AssetPath { get; }
        public int ReferenceCount { get; }
        public long AssetBytes { get; }
        public long DuplicateSavingsBytes => AssetBytes * Math.Max(0, ReferenceCount - 1);
    }

    private sealed class SharedGeneratedDependencySelection
    {
        public readonly List<string> InitialSharedPaths = new List<string>();
        public readonly List<string> LateOnlySharedPaths = new List<string>();
    }

    private sealed class TransformSnapshot
    {
        public string Name;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public Vector3 WorldPosition;
        public Quaternion WorldRotation;
        public int SiblingIndex;
        public bool ActiveSelf;
        public int Layer;
        public string Tag;
        public StaticEditorFlags StaticFlags;
    }
}
