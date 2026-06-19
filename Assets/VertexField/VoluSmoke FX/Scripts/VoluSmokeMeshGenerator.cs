using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace VertexField.VoluSmokeFX
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    public class VoluSmokeMeshGenerator : MonoBehaviour
    {
        [Header("MESH STRUCTURE")]
        [Range(2, 100)] public int gridResolution = 10;
        [Range(0.1f, 10f)] public float planeSize = 5f;
        [Range(2, 100)] public int stackLayers = 20;
        [Range(0.01f, 1f)] public float layerSpacing = 0.1f;

        [Header("CROP")]
        public bool enableCrop = false;
        [Range(0f, 0.49f)] public float cropLeft = 0f;
        [Range(0f, 0.49f)] public float cropRight = 0f;
        [Range(0f, 0.49f)] public float cropForward = 0f;
        [Range(0f, 0.49f)] public float cropBack = 0f;
        [SerializeField, HideInInspector] private Vector3 cropPivotOffset = Vector3.zero;
        public bool hideCropHandles = false;

        [Header("LAYER SCALING")]
        public bool enableLayerScaling = false;
        public enum ScalingMode { Linear, Exponential, Curve, Stepped }
        public ScalingMode scalingMode = ScalingMode.Linear;
        [Range(0.01f, 1f)] public float scaleReduction = 0.9f;
        [Range(0.1f, 2f)] public float minScale = 0.1f;
        [Range(0.5f, 2f)] public float maxScale = 1f;
        public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 0);
        public enum ScaleCurvePreset
        {
            Custom,
            TaperedTop,
            TaperedBottom,
            Uniform,
            BulgedMid,
            Hourglass,
            Dome,
            Anvil
        }
        public ScaleCurvePreset scaleCurvePreset = ScaleCurvePreset.TaperedTop;

        [Header("OPACITY & ALPHA")]
        [Range(0.1f, 10f)] public float sphereRadius = 2f;
        public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        [Range(0f, 1f)] public float centerOpacity = 1f;
        [Range(0f, 1f)] public float edgeOpacity = 0f;
        public bool enableEdgeAlphaGradient = false;
        [Range(0f, 1f)] public float edgeAlphaGradient = 0.5f;
        [Range(0.01f, 0.5f)] public float edgeGradientWidth = 0.15f;
        public AnimationCurve edgeGradientCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("APPEARANCE")]
        public Vector3 sphereCenter = Vector3.zero;
        [Range(0f, 1f)] public float noiseAmount = 0f;
        [Range(0.1f, 10f)] public float noiseScale = 1f;
        public bool enableLayerColorGradient = false;
        [GradientUsage(true)]
        public Gradient layerColorGradient = CreateDefaultLayerGradient();

        [Header("NORMALS")]
        public bool smoothNormals = true;
        [FormerlySerializedAs("normalFlattening")]
        [Range(0f, 1f)] public float planarNormalWeight = 0.8f;
        [FormerlySerializedAs("normalSpherical")]
        [Range(0f, 1f)] public float inwardBubbleStrength = 0.3f;
        [FormerlySerializedAs("normalSphericalOffset")]
        public Vector3 bubbleCenterOffset = Vector3.zero;

        [Header("EDITOR OPTIONS")]
        public bool showGizmos = false;
        public bool autoUpdate = true;

        [Header("DEBUG")]
        public bool debugShowPolygons = false;
        public bool debugShowWire = true;
        public bool debugShowNormals = false;
        public bool debugShowTriIndices = false;
        public bool debugShowVertIndices = false;
        [Range(1, 50000)] public int debugMaxTriangles = 5000;
        [Range(0.01f, 1f)] public float debugNormalScale = 0.1f;
        public Color debugWireColor = new Color(0f, 0f, 0f, 0.8f);
        public Color debugNormalColor = new Color(1f, 0f, 1f, 0.9f);
        public Color debugTriIndexColor = new Color(1f, 0.5f, 0f, 0.95f);
        public Color debugVertIndexColor = new Color(0f, 0.6f, 1f, 0.95f);

        [SerializeField, HideInInspector] private VoluSmokePreset lastAppliedPreset;
        public VoluSmokePreset LastAppliedPreset => lastAppliedPreset;

        private MeshFilter meshFilter;
        private Mesh generatedMesh;

        private List<Vector3> vertices = new List<Vector3>();
        private List<int> triangles = new List<int>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<Color> colors = new List<Color>();
        private List<Vector3> normals = new List<Vector3>();

        private const float MaxCropRatio = 0.49f;
        private const float MaxCropCombined = 0.98f;

        public struct CropData
        {
            public float baseMinX;
            public float baseMaxX;
            public float baseMinZ;
            public float baseMaxZ;
            public float localMinX;
            public float localMaxX;
            public float localMinZ;
            public float localMaxZ;
            public float minU;
            public float maxU;
            public float minV;
            public float maxV;
            public Vector3 pivotOffset;
        }

    #if UNITY_EDITOR
        private bool scheduledRebuild = false;
    #endif

    #if UNITY_EDITOR
        public void EditorSetLastAppliedPreset(VoluSmokePreset preset)
        {
            if (lastAppliedPreset == preset) return;

            lastAppliedPreset = preset;
            EditorUtility.SetDirty(this);

            if (PrefabUtility.IsPartOfPrefabInstance(this))
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
    #endif

        void Reset()
        {
            lastAppliedPreset = null;
            cropPivotOffset = Vector3.zero;
            hideCropHandles = false;
            InitializeComponents();
            GenerateMesh();
            DisableShadowCasting();
        }

        void Awake() { InitializeComponents(); DisableShadowCasting(); }

        void Start()
        {
            if (Application.isPlaying)
            {
                GenerateMesh();
                DisableShadowCasting();
            }
        }

        void OnEnable()
        {
            InitializeComponents();
            if (!Application.isPlaying) GenerateMesh();
            DisableShadowCasting();
    #if UNITY_EDITOR
            EditorApplication.delayCall -= OnEditorDelayedRebuild;
            EditorApplication.delayCall += OnEditorDelayedRebuild;
    #endif
        }

        void OnDisable()
        {
    #if UNITY_EDITOR
            EditorApplication.delayCall -= OnEditorDelayedRebuild;
            scheduledRebuild = false;
    #endif
        }

    #if UNITY_EDITOR
        void OnValidate()
        {
            planarNormalWeight = Mathf.Clamp01(planarNormalWeight);
            inwardBubbleStrength = Mathf.Clamp01(inwardBubbleStrength);

            if (autoUpdate)
            {
                scheduledRebuild = true;
                EditorApplication.delayCall -= OnEditorDelayedRebuild;
                EditorApplication.delayCall += OnEditorDelayedRebuild;
            }

            if (layerColorGradient == null)
                layerColorGradient = CreateDefaultLayerGradient();
        }

        private void OnEditorDelayedRebuild()
        {
            if (!scheduledRebuild) return;
            scheduledRebuild = false;
            if (this == null) return;
            InitializeComponents();
            GenerateMesh();
            DisableShadowCasting();
        }
    #endif

        void InitializeComponents()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();
            if (GetComponent<MeshRenderer>() == null) gameObject.AddComponent<MeshRenderer>();
        }

        void DisableShadowCasting()
        {
            var renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        public void GenerateMesh()
        {
            InitializeComponents();

            if (generatedMesh == null)
            {
                generatedMesh = new Mesh();
                generatedMesh.name = "Generated VoluSmoke Mesh";
                generatedMesh.MarkDynamic();
            }
            else
            {
                generatedMesh.Clear(true);
            }

            CreateStackedMesh();

            if (meshFilter != null) meshFilter.sharedMesh = generatedMesh;

            DisableShadowCasting();
        }

        public void ClearMesh()
        {
            if (generatedMesh != null)
            {
                if (Application.isPlaying) Destroy(generatedMesh);
                else DestroyImmediate(generatedMesh);
                generatedMesh = null;
            }
            if (meshFilter != null) meshFilter.sharedMesh = null;
        }

        public void ClampCropValues()
        {
            ClampCropPair(ref cropLeft, ref cropRight);
            ClampCropPair(ref cropBack, ref cropForward);
        }

        private static void ClampCropPair(ref float first, ref float second)
        {
            first = Mathf.Clamp(first, 0f, MaxCropRatio);
            second = Mathf.Clamp(second, 0f, MaxCropRatio);

            float sum = first + second;
            if (sum > MaxCropCombined && sum > 0f)
            {
                float scale = MaxCropCombined / sum;
                first *= scale;
                second *= scale;
            }
        }

        public CropData CalculateCropData()
        {
            CropData data = new CropData();


            float left = Mathf.Clamp(cropLeft, 0f, MaxCropRatio);
            float right = Mathf.Clamp(cropRight, 0f, MaxCropRatio);
            float forward = Mathf.Clamp(cropForward, 0f, MaxCropRatio);
            float back = Mathf.Clamp(cropBack, 0f, MaxCropRatio);

            float sumX = left + right;
            if (sumX > MaxCropCombined && sumX > 0f)
            {
                float scale = MaxCropCombined / sumX;
                left *= scale;
                right *= scale;
            }

            float sumZ = forward + back;
            if (sumZ > MaxCropCombined && sumZ > 0f)
            {
                float scale = MaxCropCombined / sumZ;
                forward *= scale;
                back *= scale;
            }

            float baseHalf = planeSize * 0.5f;
            float baseMinX = -baseHalf + (enableCrop ? left * planeSize : 0f);
            float baseMaxX = baseHalf - (enableCrop ? right * planeSize : 0f);
            float baseMinZ = -baseHalf + (enableCrop ? back * planeSize : 0f);
            float baseMaxZ = baseHalf - (enableCrop ? forward * planeSize : 0f);

            float pivotX = enableCrop ? (baseMinX + baseMaxX) * 0.5f : 0f;
            float pivotZ = enableCrop ? (baseMinZ + baseMaxZ) * 0.5f : 0f;

            data.baseMinX = baseMinX;
            data.baseMaxX = baseMaxX;
            data.baseMinZ = baseMinZ;
            data.baseMaxZ = baseMaxZ;
            data.localMinX = baseMinX - pivotX;
            data.localMaxX = baseMaxX - pivotX;
            data.localMinZ = baseMinZ - pivotZ;
            data.localMaxZ = baseMaxZ - pivotZ;
            data.minU = enableCrop ? left : 0f;
            data.maxU = enableCrop ? 1f - right : 1f;
            data.minV = enableCrop ? back : 0f;
            data.maxV = enableCrop ? 1f - forward : 1f;
            data.pivotOffset = enableCrop ? new Vector3(pivotX, 0f, pivotZ) : Vector3.zero;

            return data;
        }

        void ApplyCropPivot(Vector3 targetOffset)
        {
            if (!enableCrop) targetOffset = Vector3.zero;

            if ((targetOffset - cropPivotOffset).sqrMagnitude <= 1e-10f)
            {
                cropPivotOffset = targetOffset;
                return;
            }


            Vector3 worldDelta = transform.TransformVector(targetOffset - cropPivotOffset);
            transform.position += worldDelta;
            cropPivotOffset = targetOffset;
        }

        public Vector3 CurrentCropPivotOffset => cropPivotOffset;

        public void SetCropPivotOffset(Vector3 offset)
        {
            cropPivotOffset = offset;
        }

        void BuildNormals()
        {
            int vertexCount = vertices.Count;
            EnsureVectorListSize(normals, vertexCount);

            Vector3 planeNormal = Vector3.up;
            float flatten = Mathf.Clamp01(planarNormalWeight);
            float sphericalStrength = Mathf.Clamp01(inwardBubbleStrength);
            Vector3 sphereOffset = bubbleCenterOffset;
            float baseRadius = Mathf.Max(0.0001f, sphereRadius);

            for (int i = 0; i < vertexCount; i++)
            {
                Vector3 vertexPos = vertices[i];
                Vector3 offset = vertexPos - (sphereCenter + sphereOffset);
                float distance = offset.magnitude;

                Vector3 tangent = new Vector3(offset.x, 0f, offset.z);
                float tangentLen = tangent.magnitude;
                if (tangentLen <= 1e-6f)
                {
                    tangent = Vector3.forward;
                    tangentLen = 1f;
                }
                tangent /= tangentLen;

                Vector3 bitangent = Vector3.Cross(planeNormal, tangent);
                if (bitangent.sqrMagnitude <= 1e-6f) bitangent = Vector3.right;
                bitangent.Normalize();

                Vector3 radialDir = distance > 1e-6f ? offset / distance : planeNormal;

                // Build tangent-space normal components
                float radialComponent = (1f - flatten) * sphericalStrength;
                float verticalComponent = Mathf.Lerp(1f, 0f, radialComponent);
                Vector3 normalTS = new Vector3(radialComponent, 0f, Mathf.Max(0.0001f, verticalComponent));
                normalTS = normalTS.normalized;

                Vector3 normalOS = tangent * normalTS.x + bitangent * normalTS.y + planeNormal * normalTS.z;

                if (sphericalStrength > 0f)
                {
                    float normalizedDistance = Mathf.Clamp01(distance / baseRadius);
                    float inwardBlend = Mathf.Pow(1f - normalizedDistance, 2f);
                    normalOS = Vector3.Slerp(normalOS, radialDir, inwardBlend * sphericalStrength);

                    float rimStart = 0.8f;
                    if (normalizedDistance > rimStart)
                    {
                        float rim = Mathf.Clamp01((normalizedDistance - rimStart) / (1f - rimStart));
                        Vector3 inverted = -radialDir;
                        normalOS = Vector3.Slerp(normalOS, inverted, rim * sphericalStrength);
                    }
                }

                if (normalOS.sqrMagnitude < 1e-10f) normalOS = planeNormal;
                normals[i] = normalOS.normalized;
            }
        }

        void EnsureVectorListSize(List<Vector3> list, int targetCount)
        {
            if (list.Count < targetCount)
            {
                int start = list.Count;
                list.Capacity = Mathf.Max(list.Capacity, targetCount);
                for (int i = start; i < targetCount; i++)
                    list.Add(Vector3.zero);
            }
            else if (list.Count > targetCount)
            {
                list.RemoveRange(targetCount, list.Count - targetCount);
            }
        }

        void CreateStackedMesh()
        {
            vertices.Clear();
            triangles.Clear();
            uvs.Clear();
            colors.Clear();
            normals.Clear();

            if (layerColorGradient == null)
                layerColorGradient = CreateDefaultLayerGradient();

            ClampCropValues();
            CropData crop = CalculateCropData();
            bool layerColoringEnabled = enableLayerColorGradient && layerColorGradient != null;

            int vpp = gridResolution * gridResolution;
            float totalHeight = (stackLayers - 1) * layerSpacing;
            float bottomY = -totalHeight * 0.5f;

            for (int layer = 0; layer < stackLayers; layer++)
            {
                float layerY = bottomY + layer * layerSpacing;
                float layerProgress = (stackLayers > 1) ? (float)layer / (stackLayers - 1) : 0f;
                int baseVertexIndex = layer * vpp;
                float layerScale = GetLayerScale(layerProgress, layer);
                float scaledMinX = crop.localMinX * layerScale;
                float scaledMaxX = crop.localMaxX * layerScale;
                float scaledMinZ = crop.localMinZ * layerScale;
                float scaledMaxZ = crop.localMaxZ * layerScale;
                float spanX = scaledMaxX - scaledMinX;
                float spanZ = scaledMaxZ - scaledMinZ;
                float stepX = (gridResolution > 1) ? spanX / (gridResolution - 1) : 0f;
                float stepZ = (gridResolution > 1) ? spanZ / (gridResolution - 1) : 0f;
                Color gradientColor = Color.white;
                if (layerColoringEnabled)
                    gradientColor = layerColorGradient.Evaluate(layerProgress);

                for (int z = 0; z < gridResolution; z++)
                {
                    for (int x = 0; x < gridResolution; x++)
                    {
                        float xPos = scaledMinX + x * stepX;
                        float zPos = scaledMinZ + z * stepZ;
                        Vector3 vertexPos = new Vector3(xPos, layerY, zPos);
                        float normalizedU = (gridResolution > 1) ? (float)x / (gridResolution - 1) : 0f;
                        float normalizedV = (gridResolution > 1) ? (float)z / (gridResolution - 1) : 0f;
                        float u = Mathf.Lerp(crop.minU, crop.maxU, normalizedU);
                        float v = Mathf.Lerp(crop.minV, crop.maxV, normalizedV);
                        float alpha = CalculateSphereAlpha(vertexPos);

                        if (enableEdgeAlphaGradient)
                        {
                            float edgeAlphaBoost = CalculateEdgeAlphaBoost(normalizedU, normalizedV, layerProgress);
                            alpha = Mathf.Clamp01(alpha + edgeAlphaBoost);
                        }

                        if (noiseAmount > 0f)
                        {
                            float n1 = Mathf.PerlinNoise(vertexPos.x * noiseScale + 100f, vertexPos.z * noiseScale + 100f);
                            float n2 = Mathf.PerlinNoise(n1 * 10f, layerY * noiseScale + 13.37f);
                            alpha = Mathf.Lerp(alpha, alpha * n2, noiseAmount);
                        }

                        alpha = Mathf.Clamp01(alpha);
                        vertices.Add(vertexPos);
                        uvs.Add(new Vector2(u, v));
                        Color vertexColor = layerColoringEnabled
                            ? new Color(gradientColor.r, gradientColor.g, gradientColor.b, alpha)
                            : new Color(1f, 1f, 1f, alpha);
                        colors.Add(vertexColor);
                    }
                }

                for (int z = 0; z < gridResolution - 1; z++)
                {
                    for (int x = 0; x < gridResolution - 1; x++)
                    {
                        int bottomLeft = baseVertexIndex + z * gridResolution + x;
                        int bottomRight = bottomLeft + 1;
                        int topLeft = bottomLeft + gridResolution;
                        int topRight = topLeft + 1;

                        triangles.Add(bottomLeft);
                        triangles.Add(topLeft);
                        triangles.Add(topRight);
                        triangles.Add(bottomLeft);
                        triangles.Add(topRight);
                        triangles.Add(bottomRight);
                    }
                }
            }

            if (vertices.Count > 65535) generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            else generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;

            BuildNormals();

            generatedMesh.SetVertices(vertices);
            generatedMesh.SetTriangles(triangles, 0);
            generatedMesh.SetUVs(0, uvs);
            generatedMesh.SetColors(colors);
            generatedMesh.SetNormals(normals);
            generatedMesh.RecalculateTangents();
            generatedMesh.RecalculateBounds();

            if (Application.isPlaying) generatedMesh.MarkDynamic();

            ApplyCropPivot(crop.pivotOffset);
        }

        float GetLayerScale(float layerProgress, int layerIndex)
        {
            if (!enableLayerScaling) return 1f;
            float layerScale = 1f;
            switch (scalingMode)
            {
                case ScalingMode.Linear:
                    layerScale = Mathf.Lerp(maxScale, minScale, layerProgress);
                    break;
                case ScalingMode.Exponential:
                    layerScale = maxScale * Mathf.Pow(scaleReduction, layerIndex);
                    layerScale = Mathf.Max(layerScale, minScale);
                    break;
                case ScalingMode.Curve:
                    float curveValue = Mathf.Clamp01(scaleCurve.Evaluate(layerProgress));
                    layerScale = Mathf.Lerp(minScale, maxScale, curveValue);
                    break;
                case ScalingMode.Stepped:
                    int steps = 5;
                    float stepSize = 1f / steps;
                    float steppedProgress = Mathf.Floor(layerProgress / stepSize) * stepSize;
                    layerScale = Mathf.Lerp(maxScale, minScale, steppedProgress);
                    break;
            }
            return Mathf.Clamp(layerScale, Mathf.Min(minScale, maxScale), Mathf.Max(minScale, maxScale));
        }

        public void ApplyScaleCurvePreset(ScaleCurvePreset preset)
        {
            scaleCurvePreset = preset;
            if (preset == ScaleCurvePreset.Custom) return;

            AnimationCurve presetCurve = CreateScaleCurvePreset(preset);
            if (presetCurve != null)
            {
                if (scaleCurve == null) scaleCurve = new AnimationCurve();
                CopyCurveData(scaleCurve, presetCurve);
            }
        }

        public static AnimationCurve CreateScaleCurvePreset(ScaleCurvePreset preset)
        {
            switch (preset)
            {
                case ScaleCurvePreset.TaperedTop:
                    return BuildCurve(new Vector2(0f, 1f), new Vector2(1f, 0f));
                case ScaleCurvePreset.TaperedBottom:
                    return BuildCurve(new Vector2(0f, 0.25f), new Vector2(1f, 1f));
                case ScaleCurvePreset.Uniform:
                    return BuildCurve(new Vector2(0f, 0.75f), new Vector2(1f, 0.75f));
                case ScaleCurvePreset.BulgedMid:
                    return BuildCurve(new Vector2(0f, 0.35f), new Vector2(0.4f, 0.95f), new Vector2(0.6f, 0.95f), new Vector2(1f, 0.35f));
                case ScaleCurvePreset.Hourglass:
                    return BuildCurve(new Vector2(0f, 1f), new Vector2(0.5f, 0.2f), new Vector2(1f, 1f));
                case ScaleCurvePreset.Dome:
                    return BuildCurve(new Vector2(0f, 1f), new Vector2(0.35f, 1f), new Vector2(1f, 0.35f));
                case ScaleCurvePreset.Anvil:
                    return BuildCurve(new Vector2(0f, 0.3f), new Vector2(0.25f, 0.85f), new Vector2(0.7f, 0.85f), new Vector2(1f, 0.2f));
                default:
                    return null;
            }
        }

        static AnimationCurve BuildCurve(params Vector2[] points)
        {
            if (points == null || points.Length == 0)
                return new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

            Keyframe[] keys = new Keyframe[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                Vector2 p = points[i];
                keys[i] = new Keyframe(Mathf.Clamp01(p.x), Mathf.Clamp01(p.y));
            }

            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) curve.SmoothTangents(i, 0.5f);
            return curve;
        }

        public static void CopyCurveData(AnimationCurve destination, AnimationCurve source)
        {
            if (destination == null || source == null) return;
            destination.keys = source.keys;
            destination.preWrapMode = source.preWrapMode;
            destination.postWrapMode = source.postWrapMode;
        }

        public static Gradient CreateDefaultLayerGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
    #if UNITY_2020_1_OR_NEWER
            gradient.mode = GradientMode.Blend;
    #endif
            return gradient;
        }

        public static void CopyGradient(Gradient destination, Gradient source)
        {
            if (destination == null || source == null) return;
            destination.SetKeys(source.colorKeys, source.alphaKeys);
    #if UNITY_2020_1_OR_NEWER
            destination.mode = source.mode;
    #endif
        }

        float CalculateEdgeAlphaBoost(float u, float v, float layerProgress)
        {
            float distU = Mathf.Abs(u - 0.5f);
            float distV = Mathf.Abs(v - 0.5f);
            float maxDist = Mathf.Max(distU, distV);
            float edgeInfluence = Mathf.Clamp01((maxDist - (0.5f - edgeGradientWidth)) / edgeGradientWidth);
            edgeInfluence = edgeGradientCurve.Evaluate(edgeInfluence);
            return edgeInfluence * layerProgress * edgeAlphaGradient;
        }

        float CalculateSphereAlpha(Vector3 vertexPos)
        {
            float distance = Vector3.Distance(vertexPos, sphereCenter);
            float normalized = Mathf.Clamp01(distance / sphereRadius);
            float falloff = falloffCurve.Evaluate(normalized);
            return Mathf.Lerp(edgeOpacity, centerOpacity, falloff);
        }

        void OnDrawGizmos()
        {
            if (!showGizmos) return;
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.TransformPoint(sphereCenter), sphereRadius * transform.lossyScale.x);
            float totalHeight = (stackLayers - 1) * layerSpacing;
            if (enableCrop && !hideCropHandles)
            {
                Gizmos.color = new Color(1, 1, 0, 0.5f);
                var crop = CalculateCropData();
                float widthX = Mathf.Abs(crop.baseMaxX - crop.baseMinX);
                float depthZ = Mathf.Abs(crop.baseMaxZ - crop.baseMinZ);
                Vector3 size = new Vector3(widthX, totalHeight, depthZ);
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
                Gizmos.DrawWireCube(sphereCenter, size);
                Gizmos.matrix = oldMatrix;
            }
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(sphereCenter), 0.1f);
        }

        void OnDrawGizmosSelected()
        {
            if (!showGizmos) return;
            float totalHeight = (stackLayers - 1) * layerSpacing;
            if (enableCrop && !hideCropHandles)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
                float bottomY = -totalHeight * 0.5f;
                var crop = CalculateCropData();
                for (int i = 0; i < stackLayers; i++)
                {
                    float layerY = bottomY + i * layerSpacing;
                    float progress = (stackLayers > 1) ? (float)i / (stackLayers - 1) : 0f;
                    float scale = GetLayerScale(progress, i);
                    Vector3 pos = transform.TransformPoint(new Vector3(0, layerY, 0));
                    Matrix4x4 oldMatrix = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(pos, transform.rotation, transform.lossyScale);
                    float widthX = Mathf.Abs(crop.localMaxX - crop.localMinX) * scale;
                    float depthZ = Mathf.Abs(crop.localMaxZ - crop.localMinZ) * scale;
                    Gizmos.DrawWireCube(Vector3.zero, new Vector3(widthX, 0.01f, depthZ));
                    Gizmos.matrix = oldMatrix;
                }
            }
    #if UNITY_EDITOR
            if (debugShowPolygons) DrawMeshDebug();
    #endif
        }

    #if UNITY_EDITOR
        void DrawMeshDebug()
        {
            Mesh m = generatedMesh != null ? generatedMesh : (meshFilter ? meshFilter.sharedMesh : null);
            if (m == null) return;
            var verts = m.vertices;
            var tris = m.triangles;
            int triCount = tris.Length / 3;
            int drawTriCount = Mathf.Min(triCount, debugMaxTriangles);
            var localToWorld = transform.localToWorldMatrix;
            if (debugShowVertIndices)
            {
                Handles.color = debugVertIndexColor;
                for (int i = 0; i < verts.Length; i++) Handles.Label(localToWorld.MultiplyPoint3x4(verts[i]), i.ToString());
            }
            for (int t = 0; t < drawTriCount; t++)
            {
                int i0 = tris[t * 3];
                int i1 = tris[t * 3 + 1];
                int i2 = tris[t * 3 + 2];
                Vector3 v0 = localToWorld.MultiplyPoint3x4(verts[i0]);
                Vector3 v1 = localToWorld.MultiplyPoint3x4(verts[i1]);
                Vector3 v2 = localToWorld.MultiplyPoint3x4(verts[i2]);
                if (debugShowWire)
                {
                    Handles.color = debugWireColor;
                    Handles.DrawLine(v0, v1);
                    Handles.DrawLine(v1, v2);
                    Handles.DrawLine(v2, v0);
                }
                if (debugShowNormals)
                {
                    Vector3 center = (v0 + v1 + v2) / 3f;
                    Vector3 n = Vector3.Cross(v1 - v0, v2 - v0).normalized;
                    Handles.color = debugNormalColor;
                    Handles.DrawLine(center, center + n * debugNormalScale);
                }
                if (debugShowTriIndices)
                {
                    Vector3 center = (v0 + v1 + v2) / 3f;
                    Handles.color = debugTriIndexColor;
                    Handles.Label(center, $"tri {t}");
                }
            }
        }
    #endif

        void OnDestroy()
        {
    #if UNITY_EDITOR
            EditorApplication.delayCall -= OnEditorDelayedRebuild;
            scheduledRebuild = false;
    #endif
            if (generatedMesh != null)
            {
                if (Application.isPlaying) Destroy(generatedMesh);
                else DestroyImmediate(generatedMesh);
                generatedMesh = null;
            }
        }
    }

    [System.Serializable]
    public class VoluSmokeShaderSetup : MonoBehaviour
    {
        public Material voluSmokeMaterial;
        public Texture2D noiseTexture;
        public Color tintColor = Color.white;
        [Range(0f, 2f)] public float alphaPower = 1f;

        private Material instanceMaterial;
        private MeshRenderer meshRenderer;

        void Start() => SetupShader();

        public void SetupShader()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null && voluSmokeMaterial != null)
            {
                if (instanceMaterial != null)
                {
                    if (Application.isPlaying) Destroy(instanceMaterial);
                    else DestroyImmediate(instanceMaterial);
                }
                instanceMaterial = new Material(voluSmokeMaterial);
                if (noiseTexture != null) instanceMaterial.SetTexture("_NoiseTex", noiseTexture);
                instanceMaterial.SetColor("_TintColor", tintColor);
                instanceMaterial.SetFloat("_AlphaPower", alphaPower);
                instanceMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                instanceMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                instanceMaterial.SetInt("_ZWrite", 0);
                instanceMaterial.DisableKeyword("_ALPHATEST_ON");
                instanceMaterial.EnableKeyword("_ALPHABLEND_ON");
                instanceMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                instanceMaterial.renderQueue = 3000;
                meshRenderer.sharedMaterial = instanceMaterial;
            }
        }

        void OnDestroy()
        {
            if (instanceMaterial != null)
            {
                if (Application.isPlaying) Destroy(instanceMaterial);
                else DestroyImmediate(instanceMaterial);
            }
        }
    }
}
