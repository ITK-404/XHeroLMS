using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace XHero.Rendering
{
    [Serializable]
    public sealed class XHeroShaderRuntimeUsageReport
    {
        public int version = 1;
        public string generatedUtc;
        public string platform;
        public string graphicsDevice;
        public string graphicsDeviceType;
        public string renderPipeline;
        public float durationSeconds;
        public int scanCount;
        public int maxActiveRendererCount;
        public int materialObservations;
        public int invalidMaterialObservations;
        public XHeroShaderRuntimeVariantUsage[] variants;
    }

    [Serializable]
    public sealed class XHeroShaderRuntimeVariantUsage
    {
        public string shader;
        public string passType;
        public string[] keywords;
        public int observations;
    }

    /// <summary>
    /// Captures active renderer shader/keyword combinations in Editor and
    /// Development Builds. Reports are consumed automatically by the editor
    /// collection builder on the next refresh/build.
    /// </summary>
    public sealed class XHeroShaderRuntimeUsageRecorder : MonoBehaviour
    {
        private const float CaptureIntervalSeconds = 5f;
        private const float ReportFlushIntervalSeconds = 30f;

        private readonly Dictionary<string, XHeroShaderRuntimeVariantUsage> _variants =
            new Dictionary<string, XHeroShaderRuntimeVariantUsage>(StringComparer.Ordinal);

        private float _nextCaptureTime;
        private float _nextFlushTime;
        private float _startTime;
        private int _scanCount;
        private int _maxActiveRendererCount;
        private int _materialObservations;
        private int _invalidMaterialObservations;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isEditor && !Debug.isDebugBuild)
                return;

            if (FindAnyObjectByType<XHeroShaderRuntimeUsageRecorder>() != null)
                return;

            GameObject gameObject = new GameObject("[XHero Shader Usage Recorder]");
            gameObject.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(gameObject);
            gameObject.AddComponent<XHeroShaderRuntimeUsageRecorder>();
        }

        private void Awake()
        {
            _startTime = Time.realtimeSinceStartup;
            _nextCaptureTime = 0f;
            _nextFlushTime = ReportFlushIntervalSeconds;
        }

        private void Update()
        {
            if (Time.unscaledTime >= _nextCaptureTime)
            {
                _nextCaptureTime = Time.unscaledTime + CaptureIntervalSeconds;
                CaptureActiveRenderers();
            }

            if (Time.unscaledTime >= _nextFlushTime)
            {
                _nextFlushTime = Time.unscaledTime + ReportFlushIntervalSeconds;
                SaveReport();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                SaveReport();
        }

        private void OnApplicationQuit()
        {
            SaveReport();
        }

        private void CaptureActiveRenderers()
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            int activeRendererCount = 0;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                activeRendererCount++;
                Material[] materials = renderer.sharedMaterials;

                foreach (Material material in materials)
                {
                    _materialObservations++;

                    if (material == null || material.shader == null || !material.shader.isSupported)
                    {
                        _invalidMaterialObservations++;
                        continue;
                    }

                    string[] keywords = NormalizeKeywords(material.shaderKeywords);
                    RecordVariant(material.shader, PassType.ScriptableRenderPipeline, keywords);
                    RecordVariant(
                        material.shader,
                        PassType.ScriptableRenderPipelineDefaultUnlit,
                        keywords);
                }
            }

            _scanCount++;
            _maxActiveRendererCount = Mathf.Max(_maxActiveRendererCount, activeRendererCount);
        }

        private void RecordVariant(Shader shader, PassType passType, string[] keywords)
        {
            string key = shader.name + "|" + (int)passType + "|" + string.Join(";", keywords);

            if (_variants.TryGetValue(key, out XHeroShaderRuntimeVariantUsage usage))
            {
                usage.observations++;
                return;
            }

            _variants.Add(key, new XHeroShaderRuntimeVariantUsage
            {
                shader = shader.name,
                passType = passType.ToString(),
                keywords = keywords,
                observations = 1
            });
        }

        private void SaveReport()
        {
            if (_scanCount == 0)
                return;

            try
            {
                string path = GetReportPath();
                string directory = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var report = new XHeroShaderRuntimeUsageReport
                {
                    generatedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    platform = Application.platform.ToString(),
                    graphicsDevice = SystemInfo.graphicsDeviceName,
                    graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                    renderPipeline = GraphicsSettings.currentRenderPipeline != null
                        ? GraphicsSettings.currentRenderPipeline.GetType().Name
                        : "Built-in",
                    durationSeconds = Time.realtimeSinceStartup - _startTime,
                    scanCount = _scanCount,
                    maxActiveRendererCount = _maxActiveRendererCount,
                    materialObservations = _materialObservations,
                    invalidMaterialObservations = _invalidMaterialObservations,
                    variants = _variants.Values
                        .OrderBy(item => item.shader, StringComparer.Ordinal)
                        .ThenBy(item => item.passType, StringComparer.Ordinal)
                        .ThenBy(item => string.Join(";", item.keywords), StringComparer.Ordinal)
                        .ToArray()
                };

                string temporaryPath = path + ".tmp";
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(report, true));

                try
                {
                    if (File.Exists(path))
                        File.Replace(temporaryPath, path, null);
                    else
                        File.Move(temporaryPath, path);
                }
                catch (IOException)
                {
                    // Some mobile filesystems do not implement Replace reliably.
                    if (File.Exists(path))
                        File.Delete(path);

                    File.Move(temporaryPath, path);
                }

                Log(
                    $"Usage report saved. path={path}, variants={report.variants.Length}, " +
                    $"invalid={report.invalidMaterialObservations}, scans={report.scanCount}");
            }
            catch (Exception exception)
            {
                LogWarning("Usage report save failed: " + exception.Message);
            }
        }

        private static string GetReportPath()
        {
            string root = Application.isEditor
                ? Directory.GetParent(Application.dataPath).FullName
                : Application.persistentDataPath;

            return Path.Combine(
                root,
                "XHeroShaderWarmup",
                "RuntimeUsage_" + Application.platform + ".json");
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

        private static void Log(string message)
        {
            if (Application.isEditor || Debug.isDebugBuild)
                Debug.Log("[XHeroShaderUsage] " + message);
        }

        private static void LogWarning(string message)
        {
            if (Application.isEditor || Debug.isDebugBuild)
                Debug.LogWarning("[XHeroShaderUsage] " + message);
        }
    }
}
