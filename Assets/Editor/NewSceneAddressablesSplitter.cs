using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class NewSceneAddressablesSplitter
{
    private const string AuthoringScenePath = "Assets/Scenes/New Scene.unity";
    private const string GeneratedSceneDirectory = "Assets/Scenes/New Scene_AddressableGenerated";
    private const string GeneratedMainScenePath = GeneratedSceneDirectory + "/New Scene Addressable.unity";
    private const string GeneratedLiteWindowMaterialPath = GeneratedSceneDirectory + "/SG_WindowCubemap Material Lite.mat";
    private const string LegacyLateSceneDirectory = "Assets/Scenes/New Scene_AddressableLate";
    private const string ReportDirectory = "Library/NewSceneAddressableSplit";
    private const string LoaderObjectName = "[New Scene Late Content Loader]";
    private const string OriginalWindowMaterialPath = "Assets/HDRI_Captures/SG_WindowCubemap Material.mat";

    private const string MainGroupName = "Cloud_New Scene";
    private const string LegacyLateGroupPrefix = "Cloud_New Scene_Late_";
    private const string LegacyInitialUiGroupName = "Cloud_New Scene_Initial_UI";
    private const string GeneratedMainAddress = "New Scene";
    private const string CloudLabel = "cloud";
    private const string LateLabel = "new_scene_late";
    private const string InitialLabel = "new_scene_initial";

    private const long MaxGroupBytes = 100L * 1024L * 1024L;
    private const long MinCandidateBytes = 2L * 1024L * 1024L;

    private static readonly string[] LateAssetPathPrefixes =
    {
        "Assets/Models_LMS/",
        "Assets/Models_LMS_Mobile/",
        "Assets/GD_SanhTruoc/",
        "Assets/Prefabs/Models/",
        "Assets/mountain/",
        "Assets/khutrungbayvatpham_TTB/",
        "Assets/Ky Mon Don Giap/"
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

    private static readonly string[] AdditionalLateHierarchyPaths =
    {
        "Enviroment/ToaChanhDien_3toa",
        "Enviroment/MB_Nen_Sau (1)"
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
        "Assets/Shaders/Prefabs_UI/Minimap_New/Plot Area UI.prefab",
        "Assets/Shaders/Prefabs_UI/Thanh Toan/Canvas Payment UI Canvas.prefab",
        "Assets/Shaders/Prefabs_UI/Minimap_New/Big Map UI.prefab",
        "Assets/Shaders/Prefabs_UI/Login_Popup/Warning Login Popup UI Variant.prefab",
        "Assets/Shaders/Prefabs_UI/Login_Popup/Failed Login Popup UI Variant.prefab",
        "Assets/Shaders/Prefabs_UI/Minimap_New/Course Area UI.prefab",
        "Assets/Shaders/Prefabs_UI/Minimap_New/Find Course/Course Semi 3D UI.prefab"
    };

    [MenuItem("Tools/Addressables/Regenerate Cloud_New Scene Group")]
    public static void RegenerateCloudNewSceneGroupMenu()
    {
        if (!EditorUtility.DisplayDialog(
                "Regenerate Cloud_New Scene",
                "This keeps Assets/Scenes/New Scene.unity intact, creates generated addressable split scenes from a copy, and refreshes the Cloud_New Scene group.",
                "Regenerate",
                "Cancel"))
        {
            return;
        }

        RegenerateCloudNewSceneGroup();
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
        ApplyGeneratedSceneOptimizations(generatedMainScene);

        List<string> generatedScenePaths = new List<string> { GeneratedMainScenePath };
        List<string> lateSceneKeys = new List<string>();
        StringBuilder splitReport = new StringBuilder();
        splitReport.AppendLine("scene_key\tgameobject_path");

        int nextLateIndex = 1;
        SplitAutomaticRootCandidates(generatedMainScene, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);
        SplitHierarchyPathBatch(generatedMainScene, AdditionalLateHierarchyPaths, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);

        foreach (string[] batch in PlannedEnvironmentLateBatches)
            SplitHierarchyPathBatch(generatedMainScene, batch, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);

        foreach (string[] batch in RuntimeEnvironmentLateBatches)
            SplitHierarchyPathBatch(generatedMainScene, batch, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);

        EnsureLateSceneLoader(generatedMainScene, lateSceneKeys);

        if (!EditorSceneManager.SaveScene(generatedMainScene, GeneratedMainScenePath))
            throw new InvalidOperationException("Failed to save generated main scene: " + GeneratedMainScenePath);

        List<string> initialUiDependencyPaths = CollectInitialUiDependencyPaths();
        RegisterGeneratedAddressables(generatedScenePaths, lateSceneKeys, initialUiDependencyPaths);

        File.WriteAllText(
            Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(ReportDirectory, "regenerate_cloud_new_scene.tsv")),
            splitReport.ToString());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[NewSceneSplit] Regenerated Cloud_New Scene group. Authoring scene was not modified. Late scenes: "
                  + string.Join(", ", lateSceneKeys));
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

            if (!objects.Contains(go))
                objects.Add(go);
        }

        CreateLateSceneFromObjects(objects, ref nextLateIndex, lateSceneKeys, generatedScenePaths, splitReport);
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
            bool activeInHierarchy = go.activeInHierarchy;

            go.transform.SetParent(null, true);

            if (!activeInHierarchy)
                go.SetActive(false);

            SceneManager.MoveGameObjectToScene(go, lateScene);
            splitReport.Append(sceneName);
            splitReport.Append('\t');
            splitReport.Append(EscapeTsv(originalPath));
            splitReport.AppendLine();
        }

        if (!EditorSceneManager.SaveScene(lateScene, scenePath))
            throw new InvalidOperationException("Failed to save generated late scene: " + scenePath);

        generatedScenePaths.Add(scenePath);
        lateSceneKeys.Add(sceneName);
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
        go.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(go, mainScene);

        string parentPath = GetParentPath(originalPath);

        if (string.IsNullOrEmpty(parentPath))
            return;

        GameObject parent = FindOrCreateByHierarchyPath(mainScene, parentPath);
        go.transform.SetParent(parent.transform, true);
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

    private static void RegisterGeneratedAddressables(
        List<string> generatedScenePaths,
        List<string> lateSceneKeys,
        List<string> initialUiDependencyPaths)
    {
        AddressableAssetSettings settings = GetAddressableSettings();
        AddressableAssetGroup mainGroup = GetOrCreateGroup(settings, MainGroupName);
        ConfigureRemoteGroup(settings, mainGroup, BundledAssetGroupSchema.BundlePackingMode.PackSeparately);
        SetCloudSharedPacking(BundledAssetGroupSchema.BundlePackingMode.PackSeparately);

        settings.AddLabel(CloudLabel, false);
        settings.AddLabel(LateLabel, false);
        settings.AddLabel(InitialLabel, false);

        RemoveEntries(mainGroup, entry =>
            string.Equals(entry.address, GeneratedMainAddress, StringComparison.OrdinalIgnoreCase)
            || entry.address.StartsWith("New Scene Late ", StringComparison.OrdinalIgnoreCase)
            || entry.labels.Contains(LateLabel)
            || entry.labels.Contains(InitialLabel)
            || IsGeneratedAssetPath(AssetDatabase.GUIDToAssetPath(entry.guid)));

        for (int i = 0; i < generatedScenePaths.Count; i++)
        {
            string scenePath = generatedScenePaths[i];
            string sceneAddress = i == 0 ? GeneratedMainAddress : lateSceneKeys[i - 1];
            AddressableAssetEntry entry = CreateOrMoveEntry(settings, mainGroup, scenePath, sceneAddress);
            entry.SetLabel(CloudLabel, true, false, true);

            if (i > 0)
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

            entry.SetLabel(CloudLabel, true, false, true);
            entry.SetLabel(InitialLabel, true, false, true);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
    }

    private static void RegisterAuthoringSceneAsAddressable()
    {
        AddressableAssetSettings settings = GetAddressableSettings();
        AddressableAssetGroup mainGroup = GetOrCreateGroup(settings, MainGroupName);
        ConfigureRemoteGroup(settings, mainGroup, BundledAssetGroupSchema.BundlePackingMode.PackTogether);

        settings.AddLabel(CloudLabel, false);

        string authoringGuid = AssetDatabase.AssetPathToGUID(AuthoringScenePath);
        RemoveEntries(mainGroup, entry =>
            !string.Equals(entry.guid, authoringGuid, StringComparison.OrdinalIgnoreCase)
            && (string.Equals(entry.address, GeneratedMainAddress, StringComparison.OrdinalIgnoreCase)
                || entry.address.StartsWith("New Scene Late ", StringComparison.OrdinalIgnoreCase)
                || entry.labels.Contains(LateLabel)
                || entry.labels.Contains(InitialLabel)
                || IsGeneratedAssetPath(AssetDatabase.GUIDToAssetPath(entry.guid))));

        AddressableAssetEntry authoringEntry = settings.CreateOrMoveEntry(authoringGuid, mainGroup, false, false);
        authoringEntry.address = GeneratedMainAddress;
        authoringEntry.SetLabel(CloudLabel, true, false, true);

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
        DeleteAssetIfExists(GeneratedSceneDirectory);

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
        Material originalMaterial = AssetDatabase.LoadAssetAtPath<Material>(OriginalWindowMaterialPath);
        Material liteMaterial = GetOrCreateGeneratedLiteWindowMaterial();

        if (originalMaterial == null || liteMaterial == null)
            return;

        int replacedCount = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != originalMaterial)
                        continue;

                    materials[i] = liteMaterial;
                    changed = true;
                    replacedCount++;
                }

                if (!changed)
                    continue;

                renderer.sharedMaterials = materials;
                EditorUtility.SetDirty(renderer);
            }
        }

        if (replacedCount > 0)
            Debug.Log("[NewSceneSplit] Replaced generated-only heavy window material refs: " + replacedCount);
    }

    private static Material GetOrCreateGeneratedLiteWindowMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(GeneratedLiteWindowMaterialPath);

        if (material == null)
        {
            if (!AssetDatabase.CopyAsset(OriginalWindowMaterialPath, GeneratedLiteWindowMaterialPath))
            {
                Debug.LogWarning("[NewSceneSplit] Failed to create generated lite material from: " + OriginalWindowMaterialPath);
                return null;
            }

            AssetDatabase.ImportAsset(GeneratedLiteWindowMaterialPath);
            material = AssetDatabase.LoadAssetAtPath<Material>(GeneratedLiteWindowMaterialPath);
        }

        if (material == null)
            return null;

        if (material.HasProperty("_RoomCube"))
            material.SetTexture("_RoomCube", null);

        EditorUtility.SetDirty(material);

        return material;
    }

    private static void RemoveEntries(AddressableAssetGroup group, Func<AddressableAssetEntry, bool> predicate)
    {
        foreach (AddressableAssetEntry entry in group.entries.Where(predicate).ToList())
            group.RemoveAssetEntry(entry);
    }

    private static bool IsGeneratedAssetPath(string assetPath)
    {
        return !string.IsNullOrEmpty(assetPath)
               && (assetPath.StartsWith(GeneratedSceneDirectory + "/", StringComparison.OrdinalIgnoreCase)
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
        serialized.FindProperty("delayBetweenScenesSeconds").floatValue = 0.1f;
        serialized.FindProperty("downloadDependenciesTogether").boolValue = true;
        serialized.FindProperty("loadSceneAsSoonAsDependenciesReady").boolValue = true;

        SerializedProperty sceneKeysProperty = serialized.FindProperty("sceneKeys");
        sceneKeysProperty.arraySize = lateSceneKeys.Count;

        for (int i = 0; i < lateSceneKeys.Count; i++)
            sceneKeysProperty.GetArrayElementAtIndex(i).stringValue = lateSceneKeys[i];

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(loaderObject);
    }

    private static void RemoveLoaderObject(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects().Where(go => go.name == LoaderObjectName).ToList())
            UnityEngine.Object.DestroyImmediate(root);
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

    private static long GetAssetFileBytes(string assetPath)
    {
        string absolutePath = ToAbsolutePath(assetPath);

        if (File.Exists(absolutePath))
            return new FileInfo(absolutePath).Length;

        return 0L;
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
}
