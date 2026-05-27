using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace VertexField.VoluSmokeFX
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class DayNightCycle : MonoBehaviour
    {
        [Header("References")]
        public Light sun;
        public Light moon;
        public ReflectionProbe dayProbe;
        public ReflectionProbe nightProbe;
        public Material skyboxMaterial;

        [Header("Runtime Options")]
        public bool assignDefaultReflection = true;
        public bool playCycle = true;

        [Header("Time")]
        [Range(0f, 24f)]
        public float timeOfDay = 12f;
        public float dayLengthSeconds = 120f;

        [Header("Sun and Moon")]
        [Range(0f, 360f)]
        public float sunAzimuth = 170f;
        [Range(0f, 90f)]
        public float maxSunElevation = 60f;
        [Range(0f, 30f)]
        public float twilightElevation = 6f;
        public float sunIntensityMultiplier = 1f;
        public float moonIntensityMultiplier = 1f;
        public Gradient sunColor = DefaultSunColor();
        public Gradient moonColor = DefaultMoonColor();
        public AnimationCurve sunIntensity = DefaultSunIntensity();
        public AnimationCurve moonIntensity = DefaultMoonIntensity();

        [Header("Environment")]
        public Gradient ambientColor = DefaultAmbientColor();
        public Gradient fogColor = DefaultFogColor();
        public AnimationCurve reflectionBlend = DefaultReflectionBlend();
        public float probeIntensityMultiplier = 1f;

        float lastAppliedTime = -1f;
        enum RenderPipelineType { BuiltIn, URP, HDRP, Custom }
        RenderPipelineType pipelineType = RenderPipelineType.BuiltIn;
        RenderPipelineAsset cachedPipelineAsset;
        bool IsBuiltInPipeline => pipelineType == RenderPipelineType.BuiltIn;
        bool IsHDRPPipeline => pipelineType == RenderPipelineType.HDRP;
        bool UsesRenderSettingsSkybox => pipelineType != RenderPipelineType.HDRP;

        [Header("HDRP Lighting")]
        [Tooltip("Lux value applied to the sun when the intensity curve evaluates to 1 in HDRP.")]
        public float hdrpSunLuxAtPeak = 120000f;
        [Tooltip("Lux value applied to the moon when the intensity curve evaluates to 1 in HDRP.")]
        public float hdrpMoonLuxAtPeak = 0.25f;

        static bool hdrpReflectionInitialized = false;
        static Type hdAdditionalLightDataType;
        static PropertyInfo hdLightUnitProperty;
        static PropertyInfo hdIntensityProperty;
        static object hdLightUnitLuxValue;
        static readonly int SkyTintId = Shader.PropertyToID("_SkyTint");
        static readonly int TintId = Shader.PropertyToID("_Tint");

        void Reset()
        {
            if (sun == null)
            {
                Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID);
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i].type == LightType.Directional)
                    {
                        sun = lights[i];
                        break;
                    }
                }
            }

            if (moon == null)
            {
                GameObject moonGO = new GameObject("Moon Light");
                moonGO.transform.SetParent(transform, false);
                Light moonLight = moonGO.AddComponent<Light>();
                moonLight.type = LightType.Directional;
                moonLight.intensity = 0.1f;
                moonLight.shadows = LightShadows.Soft;
                moon = moonLight;
            }
        }

        void OnEnable()
        {
            DetectRenderPipeline();
            lastAppliedTime = -1f;
            UpdateCycle();
        }

        void OnValidate()
        {
            DetectRenderPipeline();
            lastAppliedTime = -1f;
            UpdateCycle();
        }

        void Awake() => DetectRenderPipeline();

        void Update()
        {
            if (Application.isPlaying && playCycle && dayLengthSeconds > 0f)
            {
                float deltaHours = Time.deltaTime * 24f / Mathf.Max(dayLengthSeconds, 0.0001f);
                timeOfDay = Mathf.Repeat(timeOfDay + deltaHours, 24f);
            }

            UpdateCycle();
        }

        void UpdateCycle()
        {
            DetectRenderPipeline();

            float normalizedTime = Mathf.Repeat(timeOfDay / 24f, 1f);
            if (Mathf.Approximately(normalizedTime, lastAppliedTime) && !Application.isPlaying)
                return;

            lastAppliedTime = normalizedTime;

            Vector3 sunDirection = CalculateSunDirection(normalizedTime);
            Vector3 moonDirection = -sunDirection;

            float twilightFactor = Mathf.Sin(twilightElevation * Mathf.Deg2Rad);
            float sunElevationFactor = Mathf.Clamp01((Vector3.Dot(sunDirection, Vector3.up) + twilightFactor) / (1f + twilightFactor));
            float moonElevationFactor = Mathf.Clamp01((Vector3.Dot(moonDirection, Vector3.up) + twilightFactor) / (1f + twilightFactor));

            ApplyLight(sun, -sunDirection, sunColor, sunIntensity, sunIntensityMultiplier, normalizedTime, sunElevationFactor, true);
            ApplyLight(moon, -moonDirection, moonColor, moonIntensity, moonIntensityMultiplier, normalizedTime, moonElevationFactor, false);

            Light dominantLight = DetermineDominantLight();
            if (dominantLight != null)
                RenderSettings.sun = dominantLight;

            ApplyEnvironment(normalizedTime);
            UpdateReflectionProbes(normalizedTime);
        }

        Vector3 CalculateSunDirection(float normalizedTime)
        {
            float longitude = (normalizedTime * 360f) - 180f;
            float elevation = Mathf.Clamp(Mathf.Sin(longitude * Mathf.Deg2Rad) * maxSunElevation, -maxSunElevation, maxSunElevation);
            Quaternion azimuthRotation = Quaternion.Euler(0f, sunAzimuth, 0f);
            Quaternion elevationRotation = Quaternion.Euler(elevation, 0f, 0f);
            Vector3 direction = azimuthRotation * elevationRotation * Vector3.forward;
            return direction.normalized;
        }

        Light DetermineDominantLight()
        {
            if (sun == null) return moon;
            if (moon == null) return sun;
            return sun.intensity >= moon.intensity ? sun : moon;
        }

        void ApplyEnvironment(float normalizedTime)
        {
            Color ambient = ambientColor.Evaluate(normalizedTime);
            Color fog = fogColor.Evaluate(normalizedTime);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
            RenderSettings.fogColor = fog;
            RenderSettings.fog = true;

            if (skyboxMaterial != null)
            {
                if (UsesRenderSettingsSkybox && RenderSettings.skybox != skyboxMaterial)
                    RenderSettings.skybox = skyboxMaterial;

                if (skyboxMaterial.HasProperty(SkyTintId))
                    skyboxMaterial.SetColor(SkyTintId, ambient);
                else if (skyboxMaterial.HasProperty(TintId))
                    skyboxMaterial.SetColor(TintId, ambient);
            }
        }

        void UpdateReflectionProbes(float normalizedTime)
        {
            if (dayProbe == null && nightProbe == null)
                return;

            float blend = Mathf.Clamp01(reflectionBlend.Evaluate(normalizedTime));

            if (dayProbe != null)
                dayProbe.intensity = Mathf.Lerp(1f, 0f, blend) * probeIntensityMultiplier;
            if (nightProbe != null)
                nightProbe.intensity = Mathf.Lerp(0f, 1f, blend) * probeIntensityMultiplier;

            if (assignDefaultReflection && !Application.isPlaying)
            {
                if (blend < 0.5f && dayProbe != null && dayProbe.bakedTexture != null)
                {
                    RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                    RenderSettings.customReflectionTexture = dayProbe.bakedTexture;
                }
                else if (nightProbe != null && nightProbe.bakedTexture != null)
                {
                    RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                    RenderSettings.customReflectionTexture = nightProbe.bakedTexture;
                }
                else if (!IsHDRPPipeline)
                {
                    RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
                }
            }
        }

        void ApplyLight(Light targetLight, Vector3 forward, Gradient colorGradient, AnimationCurve intensityCurve, float intensityMultiplier, float normalizedTime, float elevationFactor, bool isSun)
        {
            if (targetLight == null) return;

            targetLight.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            targetLight.color = colorGradient.Evaluate(normalizedTime);
            float evaluated = Mathf.Max(0f, intensityCurve.Evaluate(normalizedTime) * intensityMultiplier * elevationFactor);

            if (IsHDRPPipeline)
                ApplyHdrpLightIntensity(targetLight, evaluated, isSun);
            else
                targetLight.intensity = evaluated;
        }

        void ApplyHdrpLightIntensity(Light targetLight, float normalizedIntensity, bool isSun)
        {
            float lux = Mathf.Max(0f, normalizedIntensity) * (isSun ? hdrpSunLuxAtPeak : hdrpMoonLuxAtPeak);
            targetLight.intensity = lux;

            if (hdAdditionalLightDataType == null) return;

            var additionalData = targetLight.GetComponent(hdAdditionalLightDataType) ?? targetLight.gameObject.AddComponent(hdAdditionalLightDataType);
            try
            {
                if (hdLightUnitProperty != null && hdLightUnitLuxValue != null)
                    hdLightUnitProperty.SetValue(additionalData, hdLightUnitLuxValue);

                if (hdIntensityProperty != null)
                    hdIntensityProperty.SetValue(additionalData, lux);
                else
                {
                    MethodInfo setIntensity = hdAdditionalLightDataType.GetMethod("SetIntensity", new[] { typeof(float) });
                    if (setIntensity != null)
                        setIntensity.Invoke(additionalData, new object[] { lux });
                }
            }
            catch
            {
                // Intentionally swallow reflection errors so the script still runs on other pipelines.
            }
        }

        void DetectRenderPipeline()
        {
            var asset = GraphicsSettings.currentRenderPipeline;
            if (asset == cachedPipelineAsset && pipelineType != RenderPipelineType.Custom)
                return;

            cachedPipelineAsset = asset;
            pipelineType = ResolvePipelineType(asset);

            if (pipelineType == RenderPipelineType.HDRP)
                EnsureHdrpReflection();
        }

        RenderPipelineType ResolvePipelineType(RenderPipelineAsset asset)
        {
            if (asset == null)
                return RenderPipelineType.BuiltIn;

            string typeName = asset.GetType().FullName;
            if (!string.IsNullOrEmpty(typeName))
            {
                typeName = typeName.ToLowerInvariant();
                if (typeName.Contains("highdefinition"))
                    return RenderPipelineType.HDRP;
                if (typeName.Contains("universal") || typeName.Contains("lightweight"))
                    return RenderPipelineType.URP;
            }

            return RenderPipelineType.Custom;
        }

        void EnsureHdrpReflection()
        {
            if (hdrpReflectionInitialized)
                return;

            hdrpReflectionInitialized = true;

            const string hdrpAssembly = "Unity.RenderPipelines.HighDefinition.Runtime";
            hdAdditionalLightDataType = Type.GetType($"UnityEngine.Rendering.HighDefinition.HDAdditionalLightData, {hdrpAssembly}");
            if (hdAdditionalLightDataType == null)
                return;

            hdIntensityProperty = hdAdditionalLightDataType.GetProperty("intensity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            hdLightUnitProperty = hdAdditionalLightDataType.GetProperty("lightUnit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (hdLightUnitProperty != null)
            {
                Type lightUnitType = hdLightUnitProperty.PropertyType;
                try
                {
                    hdLightUnitLuxValue = Enum.Parse(lightUnitType, "Lux");
                }
                catch
                {
                    hdLightUnitLuxValue = null;
                }
            }
        }

        static AnimationCurve DefaultReflectionBlend()
        {
            return new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.2f, 0.5f),
                new Keyframe(0.25f, 0f),
                new Keyframe(0.75f, 0f),
                new Keyframe(0.8f, 0.5f),
                new Keyframe(1f, 1f)
            );
        }

        static Gradient DefaultSunColor()
        {
            Gradient g = new Gradient();
            g.colorKeys = new[]
            {
                new GradientColorKey(new Color(0.05f, 0.08f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.45f, 0.2f), 0.25f),
                new GradientColorKey(new Color(1f, 0.98f, 0.9f), 0.5f),
                new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.75f),
                new GradientColorKey(new Color(0.05f, 0.08f, 0.2f), 1f)
            };
            g.alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            return g;
        }

        static AnimationCurve DefaultSunIntensity()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.05f),
                new Keyframe(0.25f, 0.6f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.6f),
                new Keyframe(1f, 0.05f)
            );
        }

        static Gradient DefaultMoonColor()
        {
            Gradient g = new Gradient();
            g.colorKeys = new[]
            {
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0f),
                new GradientColorKey(new Color(0.9f, 0.95f, 1f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.9f, 1f), 1f)
            };
            g.alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            return g;
        }

        static AnimationCurve DefaultMoonIntensity()
        {
            return new AnimationCurve(
                new Keyframe(0f, 0.6f),
                new Keyframe(0.25f, 0.1f),
                new Keyframe(0.5f, 0.05f),
                new Keyframe(0.75f, 0.1f),
                new Keyframe(1f, 0.6f)
            );
        }

        static Gradient DefaultAmbientColor()
        {
            Gradient g = new Gradient();
            g.colorKeys = new[]
            {
                new GradientColorKey(new Color(0.02f, 0.03f, 0.06f), 0f),
                new GradientColorKey(new Color(0.8f, 0.5f, 0.3f), 0.25f),
                new GradientColorKey(new Color(0.7f, 0.85f, 1f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.5f, 0.25f), 0.75f),
                new GradientColorKey(new Color(0.02f, 0.03f, 0.06f), 1f)
            };
            g.alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            return g;
        }

        static Gradient DefaultFogColor()
        {
            Gradient g = new Gradient();
            g.colorKeys = new[]
            {
                new GradientColorKey(new Color(0.01f, 0.02f, 0.04f), 0f),
                new GradientColorKey(new Color(0.6f, 0.45f, 0.4f), 0.25f),
                new GradientColorKey(new Color(0.7f, 0.8f, 0.95f), 0.5f),
                new GradientColorKey(new Color(0.6f, 0.35f, 0.3f), 0.75f),
                new GradientColorKey(new Color(0.01f, 0.02f, 0.04f), 1f)
            };
            g.alphaKeys = new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            return g;
        }
    }

}
