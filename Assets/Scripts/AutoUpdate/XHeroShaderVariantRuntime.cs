using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads and warms every ShaderVariantCollection stored in Resources before the
/// first scene starts. It is intentionally static so no scene or prefab setup is
/// required. The collection references are retained for the lifetime of the app.
/// </summary>
public static class XHeroShaderVariantRuntime
{
    private static readonly List<ShaderVariantCollection> s_warmedCollections =
        new List<ShaderVariantCollection>();

    private static bool s_warmupStarted;

    public static bool IsWarmupComplete { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WarmupBeforeFirstScene()
    {
        if (s_warmupStarted)
            return;

        s_warmupStarted = true;
        ShaderVariantCollection[] collections = Resources.LoadAll<ShaderVariantCollection>("");

        if (collections == null || collections.Length == 0)
        {
            LogWarning("No ShaderVariantCollection found in Resources.");
            IsWarmupComplete = true;
            return;
        }

        int warmedCount = 0;

        foreach (ShaderVariantCollection collection in collections)
        {
            if (collection == null || s_warmedCollections.Contains(collection))
                continue;

            try
            {
                if (collection.variantCount > 0 && !collection.isWarmedUp)
                    collection.WarmUp();

                s_warmedCollections.Add(collection);
                warmedCount++;
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"[XHeroShaderVariants] Warmup failed for '{collection.name}': {exception}");
            }
        }

        IsWarmupComplete = true;
        Log("Warmup complete. collections=" + warmedCount);
    }

    private static void Log(string message)
    {
        if (Application.isEditor || Debug.isDebugBuild)
            Debug.Log("[XHeroShaderVariants] " + message);
    }

    private static void LogWarning(string message)
    {
        if (Application.isEditor || Debug.isDebugBuild)
            Debug.LogWarning("[XHeroShaderVariants] " + message);
    }
}
