#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Duy trì sự cập nhật cho bộ sưu tập biến thể shader (shader variant collection) của dự án mà không cần đến thành phần scene.
/// Các vật liệu (material) đóng vai trò là nguồn dữ liệu chuẩn xác nhất vì chúng lưu giữ các tổ hợp từ khóa
/// thực sự được dự án sử dụng, bao gồm cả các vật liệu nằm trong nội dung Addressable.
/// </summary>
[InitializeOnLoad]
public static class XHeroShaderVariantCollectionBuilder
{
    public const string CollectionAssetPath =
        "Assets/Resources/XHeroShaderWarmup/AutomaticShaderVariants.shadervariants";

    public const string CollectionResourcesPath =
        "XHeroShaderWarmup/AutomaticShaderVariants";

    private const string GraphicsSettingsAssetPath = "ProjectSettings/GraphicsSettings.asset";
    private const string GeneratedFolderPath = "Assets/Resources/XHeroShaderWarmup";
    private const string GeneratedAssetFileName = "AutomaticShaderVariants.shadervariants";
    private const string GeneratedAssetPath = GeneratedFolderPath + "/" + GeneratedAssetFileName;

    private static readonly PassType[] SurfacePassTypes =
    {
        PassType.ScriptableRenderPipeline,
        PassType.ScriptableRenderPipelineDefaultUnlit
    };

    private static bool s_rebuildQueued;
    private static bool s_isBuilding;

    static XHeroShaderVariantCollectionBuilder()
    {
        QueueRebuild();
    }

    [MenuItem("XHero/Rendering/Rebuild Shader Variants")]
    private static void RebuildFromMenu()
    {
        Rebuild("menu");
    }

    private static void QueueRebuild()
    {
        if (s_rebuildQueued)
            return;

        s_rebuildQueued = true;
        EditorApplication.delayCall += RebuildQueued;
    }

    private static void RebuildQueued()
    {
        s_rebuildQueued = false;
        EditorApplication.delayCall -= RebuildQueued;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            QueueRebuild();
            return;
        }

        Rebuild("automatic");
    }

    private static void Rebuild(string reason)
    {
        if (s_isBuilding || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        s_isBuilding = true;

        try
        {
            EnsureGeneratedFolder();

            ShaderVariantCollection collection =
                AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(GeneratedAssetPath);

            if (collection == null)
            {
                collection = new ShaderVariantCollection();
                AssetDatabase.CreateAsset(collection, GeneratedAssetPath);
            }

            collection.Clear();

            var addedVariants = new HashSet<string>(StringComparer.Ordinal);
            int materialCount = 0;
            int shaderCount = 0;

            string[] materialGuids = AssetDatabase.FindAssets("t:Material");
            foreach (string guid in materialGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

                foreach (UnityEngine.Object asset in assets)
                {
                    Material material = asset as Material;
                    if (material == null || material.shader == null)
                        continue;

                    materialCount++;
                    if (AddMaterialVariants(collection, material, addedVariants))
                        shaderCount++;
                }
            }

            AddAlwaysIncludedShaderVariants(collection, addedVariants);

            EditorUtility.SetDirty(collection);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(GeneratedAssetPath, ImportAssetOptions.ForceUpdate);
            RegisterAllShaderVariantCollectionsAsPreloaded(collection);

            Debug.Log(
                $"[XHeroShaderVariants] Rebuilt ({reason}). " +
                $"materials={materialCount}, shaders={shaderCount}, variants={collection.variantCount}.");
        }
        catch (Exception exception)
        {
            Debug.LogError("[XHeroShaderVariants] Automatic rebuild failed: " + exception);
        }
        finally
        {
            s_isBuilding = false;
        }
    }

    private static bool AddMaterialVariants(
        ShaderVariantCollection collection,
        Material material,
        HashSet<string> addedVariants)
    {
        string[] keywords = NormalizeKeywords(material.shaderKeywords);
        bool addedShader = false;

        foreach (PassType passType in SurfacePassTypes)
        {
            if (AddVariant(collection, material.shader, passType, keywords, addedVariants))
                addedShader = true;
        }

        return addedShader;
    }

    private static void AddAlwaysIncludedShaderVariants(
        ShaderVariantCollection collection,
        HashSet<string> addedVariants)
    {
        GraphicsSettings graphicsSettings =
            AssetDatabase.LoadAssetAtPath<GraphicsSettings>(GraphicsSettingsAssetPath);

        if (graphicsSettings == null)
            return;

        SerializedObject serializedGraphicsSettings = new SerializedObject(graphicsSettings);
        SerializedProperty alwaysIncludedShaders =
            serializedGraphicsSettings.FindProperty("m_AlwaysIncludedShaders");

        if (alwaysIncludedShaders == null)
            return;

        for (int i = 0; i < alwaysIncludedShaders.arraySize; i++)
        {
            Shader shader =
                alwaysIncludedShaders.GetArrayElementAtIndex(i).objectReferenceValue as Shader;

            if (shader == null)
                continue;

            foreach (PassType passType in SurfacePassTypes)
                AddVariant(collection, shader, passType, new string[0], addedVariants);
        }
    }

    private static bool AddVariant(
        ShaderVariantCollection collection,
        Shader shader,
        PassType passType,
        string[] keywords,
        HashSet<string> addedVariants)
    {
        if (shader == null)
            return false;

        string key = BuildVariantKey(shader, passType, keywords);
        if (!addedVariants.Add(key))
            return false;

        collection.Add(new ShaderVariantCollection.ShaderVariant
        {
            shader = shader,
            passType = passType,
            keywords = keywords
        });

        return true;
    }

    private static string BuildVariantKey(Shader shader, PassType passType, string[] keywords)
    {
        return shader.GetInstanceID() + "|" + (int)passType + "|" + string.Join(";", keywords);
    }

    private static string[] NormalizeKeywords(string[] keywords)
    {
        if (keywords == null || keywords.Length == 0)
            return new string[0];

        return keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(keyword => keyword, StringComparer.Ordinal)
            .ToArray();
    }

    private static void RegisterAllShaderVariantCollectionsAsPreloaded(
        ShaderVariantCollection generatedCollection)
    {
        GraphicsSettings graphicsSettings =
            AssetDatabase.LoadAssetAtPath<GraphicsSettings>(GraphicsSettingsAssetPath);

        if (graphicsSettings == null)
            return;

        string[] collectionGuids = AssetDatabase.FindAssets("t:ShaderVariantCollection");
        var collections = new List<ShaderVariantCollection>(collectionGuids.Length);

        foreach (string guid in collectionGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ShaderVariantCollection collection =
                AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(path);

            if (collection != null && !collections.Contains(collection))
                collections.Add(collection);
        }

        if (generatedCollection != null && !collections.Contains(generatedCollection))
            collections.Add(generatedCollection);

        SerializedObject serializedGraphicsSettings = new SerializedObject(graphicsSettings);
        SerializedProperty preloadedShaders =
            serializedGraphicsSettings.FindProperty("m_PreloadedShaders");

        if (preloadedShaders == null)
            return;

        bool changed = false;

        foreach (ShaderVariantCollection collection in collections)
        {
            bool alreadyRegistered = false;

            for (int i = 0; i < preloadedShaders.arraySize; i++)
            {
                if (preloadedShaders.GetArrayElementAtIndex(i).objectReferenceValue == collection)
                {
                    alreadyRegistered = true;
                    break;
                }
            }

            if (alreadyRegistered)
                continue;

            int newIndex = preloadedShaders.arraySize;
            preloadedShaders.InsertArrayElementAtIndex(newIndex);
            preloadedShaders.GetArrayElementAtIndex(newIndex).objectReferenceValue = collection;
            changed = true;
        }

        if (changed)
        {
            serializedGraphicsSettings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }

    private static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!AssetDatabase.IsValidFolder(GeneratedFolderPath))
            AssetDatabase.CreateFolder("Assets/Resources", "XHeroShaderWarmup");
    }

    private static bool IsShaderRelevant(string path)
    {
        if (string.IsNullOrEmpty(path) || path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            return false;

        if (path.Equals(GeneratedAssetPath, StringComparison.OrdinalIgnoreCase))
            return false;

        string extension = Path.GetExtension(path).ToLowerInvariant();
        return extension == ".mat" ||
               extension == ".shader" ||
               extension == ".shadergraph" ||
               extension == ".compute" ||
               extension == ".hlsl" ||
               extension == ".cginc" ||
               extension == ".prefab" ||
               extension == ".fbx" ||
               extension == ".unity";
    }

    private sealed class ShaderVariantAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (s_isBuilding)
                return;

            IEnumerable<string> allChangedPaths = importedAssets
                .Concat(deletedAssets)
                .Concat(movedAssets)
                .Concat(movedFromAssetPaths);

            if (allChangedPaths.Any(IsShaderRelevant))
                QueueRebuild();
        }
    }

    private sealed class ShaderVariantBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            Rebuild("build");
        }
    }
}
#endif
