using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Development-only watchdog for the purple-material failure mode. It reports
/// the exact renderer/material/shader that is unsupported or missing, instead of
/// guessing from the screen color after the fact.
/// </summary>
public sealed class XHeroShaderRuntimeWatchdog : MonoBehaviour
{
    private const float AuditIntervalSeconds = 15f;

    private readonly HashSet<string> _reportedIssues = new HashSet<string>();
    private float _nextAuditTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForDevelopmentBuilds()
    {
        if (!Application.isEditor && !Debug.isDebugBuild)
            return;

        if (FindAnyObjectByType<XHeroShaderRuntimeWatchdog>() != null)
            return;

        GameObject gameObject = new GameObject("[XHero Shader Watchdog]");
        gameObject.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(gameObject);
        gameObject.AddComponent<XHeroShaderRuntimeWatchdog>();
    }

    private void Start()
    {
        _nextAuditTime = 0f;
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextAuditTime)
            return;

        _nextAuditTime = Time.unscaledTime + AuditIntervalSeconds;
        AuditLoadedRenderers();
    }

    private void AuditLoadedRenderers()
    {
        Renderer[] renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                string shaderName = material != null && material.shader != null
                    ? material.shader.name
                    : "<null>";

                bool invalidMaterial = material == null;
                bool invalidShader = material != null &&
                                     (material.shader == null || !material.shader.isSupported);

                if (!invalidMaterial && !invalidShader)
                    continue;

                string issueKey = renderer.GetInstanceID() + ":" + i + ":" + shaderName;
                if (!_reportedIssues.Add(issueKey))
                    continue;

                Debug.LogError(
                    $"[XHeroShaderVariants] Invalid runtime material. " +
                    $"renderer='{renderer.name}', material='{(material ? material.name : "<null>")}', " +
                    $"shader='{shaderName}', shaderSupported=" +
                    (material != null && material.shader != null && material.shader.isSupported));
            }
        }
    }
}
