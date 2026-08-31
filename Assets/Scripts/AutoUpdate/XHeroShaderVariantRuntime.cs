using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mobile-friendly shader warmup. No scene component or Inspector setup is needed.
/// The generated project collection and the GPUI collection are warmed before the
/// boot flow is allowed to enter the main world.
/// </summary>
public static class XHeroShaderVariantRuntime
{
    private const string GeneratedCollectionResourcesPath =
        "XHeroShaderWarmup/AutomaticShaderVariants";

    private const string GpuiCollectionResourcesPath = "GPUIShaderVariantCollection";
    private const float MobileWarmupTimeoutSeconds = 90f;

    private static readonly List<ShaderVariantCollection> s_warmedCollections =
        new List<ShaderVariantCollection>();

    private static bool s_bootstrapStarted;

    public static bool IsWarmupComplete { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (s_bootstrapStarted)
            return;

        s_bootstrapStarted = true;

        GameObject gameObject = new GameObject("[XHero Shader Warmup]");
        gameObject.hideFlags = HideFlags.HideInHierarchy;
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<XHeroShaderVariantWarmupRunner>();
    }

    internal static IEnumerator WarmupRoutine()
    {
        float startTime = Time.realtimeSinceStartup;
        List<ShaderVariantCollection> collections = LoadRequiredCollections();
        int warmedCount = 0;
        int totalVariants = 0;

        foreach (ShaderVariantCollection collection in collections)
        {
            if (collection == null || s_warmedCollections.Contains(collection))
                continue;

            totalVariants += collection.variantCount;

            if (Application.isMobilePlatform || Application.isEditor)
            {
                yield return WarmupMobileCollection(collection);
                s_warmedCollections.Add(collection);
                warmedCount++;
                continue;
            }

            try
            {
                if (!collection.isWarmedUp && collection.variantCount > 0)
                    collection.WarmUp();

                s_warmedCollections.Add(collection);
                warmedCount++;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[XHeroShaderVariants] Warmup failed for '{collection.name}': {exception}");
            }
        }

        IsWarmupComplete = true;

        Log(
            $"Warmup complete. platform={Application.platform}, " +
            $"collections={warmedCount}, variants={totalVariants}, " +
            $"duration={Time.realtimeSinceStartup - startTime:0.00}s");
    }

    private static IEnumerator WarmupMobileCollection(ShaderVariantCollection collection)
    {
        if (collection.variantCount == 0 || collection.isWarmedUp)
            yield break;

        int variantsPerFrame =
            Application.platform == RuntimePlatform.IPhonePlayer ? 12 : 24;
        float deadline = Time.realtimeSinceStartup + MobileWarmupTimeoutSeconds;
        bool complete = false;

        while (!complete && Time.realtimeSinceStartup < deadline)
        {
            try
            {
                complete = collection.WarmUpProgressively(variantsPerFrame);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[XHeroShaderVariants] Progressive warmup failed for '{collection.name}': {exception}");
                yield break;
            }

            if (!complete)
                yield return null;
        }

        if (!complete)
        {
            LogWarning(
                $"Mobile shader warmup timed out for '{collection.name}'. " +
                $"warmed={collection.warmedUpVariantCount}/{collection.variantCount}");
        }
    }

    private static List<ShaderVariantCollection> LoadRequiredCollections()
    {
        var collections = new List<ShaderVariantCollection>(2);

        ShaderVariantCollection generated =
            Resources.Load<ShaderVariantCollection>(GeneratedCollectionResourcesPath);

        if (generated != null)
            collections.Add(generated);
        else
            LogWarning("Generated shader variant collection is missing from Resources.");

        ShaderVariantCollection gpui =
            Resources.Load<ShaderVariantCollection>(GpuiCollectionResourcesPath);

        if (gpui != null && !collections.Contains(gpui))
            collections.Add(gpui);

        return collections;
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

internal sealed class XHeroShaderVariantWarmupRunner : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return XHeroShaderVariantRuntime.WarmupRoutine();
    }
}
