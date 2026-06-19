using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.Rendering;

namespace VertexField.VoluSmokeFX
{
    #if UNITY_EDITOR
    using UnityEditor;
    using System.IO;
    #endif

    [CreateAssetMenu(fileName = "VoluSmokePreset", menuName = "VertexField/VoluSmoke Preset")]
    public class VoluSmokePreset : ScriptableObject
    {
        [Header("Preset Info")]
        public string presetName = "Default";
        [TextArea(2, 4)] public string description = "No description";
        public Texture2D previewImage;

        [Header("Transform")]
        [HideInInspector] public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public Vector3 scale = Vector3.one;

        [Header("Material")]
        public Material voluSmokeMaterial;
        public bool hasMaterial = false;




        [System.Serializable]
        public class MaterialSnapshot
        {
            public string shaderName;
            public Color color;
            public List<TextureProperty> textures = new List<TextureProperty>();
            public List<FloatProperty> floats = new List<FloatProperty>();
            public List<ColorProperty> colors = new List<ColorProperty>();
            public List<VectorProperty> vectors = new List<VectorProperty>();
            public int renderQueue;
            public string[] keywords;
        }
        [System.Serializable] public class TextureProperty { public string name; public Texture texture; public Vector2 offset; public Vector2 scale; }
        [System.Serializable] public class FloatProperty { public string name; public float value; }
        [System.Serializable] public class ColorProperty { public string name; public Color value; }
        [System.Serializable] public class VectorProperty { public string name; public Vector4 value; }

        public MaterialSnapshot materialSnapshot = new MaterialSnapshot();

        [Header("Mesh Structure")]
        public int gridResolution = 10;
        public float planeSize = 5f;
        public int stackLayers = 20;
        public float layerSpacing = 0.1f;
        public bool enableCrop = false;
        public bool hideCropHandles = false;
        public float cropLeft = 0f;
        public float cropRight = 0f;
        public float cropForward = 0f;
        public float cropBack = 0f;
        public Vector3 cropPivotOffset = Vector3.zero;

        [Header("Layer Scaling")]
        public bool enableLayerScaling = false;
        public VoluSmokeMeshGenerator.ScalingMode scalingMode = VoluSmokeMeshGenerator.ScalingMode.Linear;
        public float scaleReduction = 0.9f;
        public float minScale = 0.1f;
        public float maxScale = 1f;
        public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 0);
        public VoluSmokeMeshGenerator.ScaleCurvePreset scaleCurvePreset = VoluSmokeMeshGenerator.ScaleCurvePreset.Custom;

        [Header("Opacity & Alpha")]
        public float sphereRadius = 2f;
        public AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        public float centerOpacity = 1f;
        public float edgeOpacity = 0f;
        public bool enableEdgeAlphaGradient = false;
        public float edgeAlphaGradient = 0.5f;
        public float edgeGradientWidth = 0.15f;
        public AnimationCurve edgeGradientCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Appearance")]
        public Vector3 sphereCenter = Vector3.zero;
        public float noiseAmount = 0f;
        public float noiseScale = 1f;
        public bool enableLayerColorGradient = false;
        [GradientUsage(true)]
        public Gradient layerColorGradient = VoluSmokeMeshGenerator.CreateDefaultLayerGradient();
        public bool smoothNormals = true;
        [FormerlySerializedAs("normalFlattening")]
        [Range(0f, 1f)] public float planarNormalWeight = 0.8f;
        [FormerlySerializedAs("normalSpherical")]
        [Range(0f, 1f)] public float inwardBubbleStrength = 0.3f;
        [FormerlySerializedAs("normalSphericalOffset")]
        public Vector3 bubbleCenterOffset = Vector3.zero;





        public void SaveFromGenerator(VoluSmokeMeshGenerator generator, string name)
        {
            presetName = name;


            rotation = generator.transform.rotation;
            scale = generator.transform.localScale;


            var renderer = generator.GetComponent<MeshRenderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                var matOnRenderer = renderer.sharedMaterial;

    #if UNITY_EDITOR
                string matPath = AssetDatabase.GetAssetPath(matOnRenderer);
                bool isAsset = !string.IsNullOrEmpty(matPath);
    #else
                bool isAsset = false;
    #endif

                CaptureMaterialSnapshot(matOnRenderer);


                if (isAsset)
                {
                    voluSmokeMaterial = matOnRenderer;
                    hasMaterial = true;
                }
                else
                {

                    voluSmokeMaterial = null;
                    hasMaterial = false;
                }
            }
            else
            {
                voluSmokeMaterial = null;
                hasMaterial = false;
                materialSnapshot = new MaterialSnapshot();
            }


            gridResolution = generator.gridResolution;
            planeSize = generator.planeSize;
            stackLayers = generator.stackLayers;
            layerSpacing = generator.layerSpacing;
            enableCrop = generator.enableCrop;
            hideCropHandles = generator.hideCropHandles;
            cropLeft = generator.cropLeft;
            cropRight = generator.cropRight;
            cropForward = generator.cropForward;
            cropBack = generator.cropBack;
            cropPivotOffset = enableCrop ? generator.CurrentCropPivotOffset : Vector3.zero;


            enableEdgeAlphaGradient = generator.enableEdgeAlphaGradient;
            enableLayerScaling = generator.enableLayerScaling;
            scalingMode = generator.scalingMode;
            scaleReduction = generator.scaleReduction;
            minScale = generator.minScale;
            maxScale = generator.maxScale;
            scaleCurve = new AnimationCurve(generator.scaleCurve.keys);
            scaleCurvePreset = generator.scaleCurvePreset;


            sphereRadius = generator.sphereRadius;
            falloffCurve = new AnimationCurve(generator.falloffCurve.keys);
            centerOpacity = generator.centerOpacity;
            edgeOpacity = generator.edgeOpacity;
            edgeAlphaGradient = generator.edgeAlphaGradient;
            edgeGradientWidth = generator.edgeGradientWidth;
            edgeGradientCurve = new AnimationCurve(generator.edgeGradientCurve.keys);


            sphereCenter = generator.sphereCenter;
            noiseAmount = generator.noiseAmount;
            noiseScale = generator.noiseScale;
            enableLayerColorGradient = generator.enableLayerColorGradient;
            if (layerColorGradient == null)
                layerColorGradient = VoluSmokeMeshGenerator.CreateDefaultLayerGradient();
            var generatorGradient = generator.layerColorGradient ?? VoluSmokeMeshGenerator.CreateDefaultLayerGradient();
            VoluSmokeMeshGenerator.CopyGradient(layerColorGradient, generatorGradient);
            smoothNormals = generator.smoothNormals;
            planarNormalWeight = generator.planarNormalWeight;
            inwardBubbleStrength = generator.inwardBubbleStrength;
            bubbleCenterOffset = generator.bubbleCenterOffset;
        }

        public void ApplyToGenerator(VoluSmokeMeshGenerator generator)
        {

            generator.transform.rotation = rotation;
            generator.transform.localScale = scale;


            var renderer = generator.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                Material matToApply = null;

                if (voluSmokeMaterial != null)
                {
                    matToApply = new Material(voluSmokeMaterial);

                    if (!string.IsNullOrEmpty(materialSnapshot?.shaderName))
                    {
                        var sh = Shader.Find(materialSnapshot.shaderName);
                        if (sh != null && matToApply.shader != sh) matToApply.shader = sh;
                    }
                }
                else if (!string.IsNullOrEmpty(materialSnapshot?.shaderName))
                {
                    var sh = Shader.Find(materialSnapshot.shaderName);
                    if (sh != null) matToApply = new Material(sh);
                }

                if (matToApply != null)
                {
                    ApplyMaterialSnapshot(matToApply);
                    renderer.sharedMaterial = matToApply;
                }
            }


            generator.gridResolution = gridResolution;
            generator.planeSize = planeSize;
            generator.stackLayers = stackLayers;
            generator.layerSpacing = layerSpacing;
            generator.enableCrop = enableCrop;
            generator.hideCropHandles = hideCropHandles;
            generator.cropLeft = cropLeft;
            generator.cropRight = cropRight;
            generator.cropForward = cropForward;
            generator.cropBack = cropBack;
            generator.ClampCropValues();
            Vector3 desiredPivot = Vector3.zero;
            if (enableCrop)
            {
                desiredPivot = cropPivotOffset;
                if (desiredPivot == Vector3.zero && (cropLeft > 0f || cropRight > 0f || cropForward > 0f || cropBack > 0f))
                {
                    desiredPivot = generator.CalculateCropData().pivotOffset;
                }
            }
            generator.SetCropPivotOffset(desiredPivot);


            generator.enableLayerScaling = enableLayerScaling;
            generator.scalingMode = scalingMode;
            generator.scaleReduction = scaleReduction;
            generator.minScale = minScale;
            generator.maxScale = maxScale;
            var presetCurve = VoluSmokeMeshGenerator.CreateScaleCurvePreset(scaleCurvePreset);
            if (scaleCurvePreset != VoluSmokeMeshGenerator.ScaleCurvePreset.Custom && presetCurve != null)
            {
                generator.scaleCurvePreset = scaleCurvePreset;
                if (generator.scaleCurve == null) generator.scaleCurve = new AnimationCurve();
                VoluSmokeMeshGenerator.CopyCurveData(generator.scaleCurve, presetCurve);
            }
            else
            {
                if (generator.scaleCurve == null) generator.scaleCurve = new AnimationCurve();
                VoluSmokeMeshGenerator.CopyCurveData(generator.scaleCurve, scaleCurve);
                generator.scaleCurvePreset = VoluSmokeMeshGenerator.ScaleCurvePreset.Custom;
            }


            generator.sphereRadius = sphereRadius;
            generator.falloffCurve = new AnimationCurve(falloffCurve.keys);
            generator.centerOpacity = centerOpacity;
            generator.edgeOpacity = edgeOpacity;
            generator.enableEdgeAlphaGradient = enableEdgeAlphaGradient;
            generator.edgeAlphaGradient = edgeAlphaGradient;
            generator.edgeGradientWidth = edgeGradientWidth;
            generator.edgeGradientCurve = new AnimationCurve(edgeGradientCurve.keys);


            generator.sphereCenter = sphereCenter;
            generator.noiseAmount = noiseAmount;
            generator.noiseScale = noiseScale;
            generator.enableLayerColorGradient = enableLayerColorGradient;
            if (generator.layerColorGradient == null)
                generator.layerColorGradient = VoluSmokeMeshGenerator.CreateDefaultLayerGradient();
            var presetGradient = layerColorGradient ?? VoluSmokeMeshGenerator.CreateDefaultLayerGradient();
            VoluSmokeMeshGenerator.CopyGradient(generator.layerColorGradient, presetGradient);
            generator.smoothNormals = smoothNormals;
            generator.planarNormalWeight = planarNormalWeight;
            generator.inwardBubbleStrength = inwardBubbleStrength;
            generator.bubbleCenterOffset = bubbleCenterOffset;

    #if UNITY_EDITOR
            generator.EditorSetLastAppliedPreset(this);
    #endif
        }

        void CaptureMaterialSnapshot(Material mat)
        {
            if (mat == null) return;

            if (materialSnapshot == null) materialSnapshot = new MaterialSnapshot();
            materialSnapshot.shaderName = (mat.shader != null) ? mat.shader.name : string.Empty;
            materialSnapshot.renderQueue = mat.renderQueue;
            materialSnapshot.keywords = mat.shaderKeywords;


            materialSnapshot.textures.Clear();
            materialSnapshot.floats.Clear();
            materialSnapshot.colors.Clear();
            materialSnapshot.vectors.Clear();

            if (mat.HasProperty("_Color"))
                materialSnapshot.color = mat.GetColor("_Color");

    #if UNITY_EDITOR
            var shader = mat.shader;
            if (shader != null)
            {
                int count = shader.GetPropertyCount();
                for (int i = 0; i < count; i++)
                {
                    string prop = shader.GetPropertyName(i);
                    var type = shader.GetPropertyType(i);
                    try
                    {
                        switch (type)
                        {
                            case ShaderPropertyType.Color:
                                if (mat.HasProperty(prop))
                                    materialSnapshot.colors.Add(new ColorProperty { name = prop, value = mat.GetColor(prop) });
                                break;

                            case ShaderPropertyType.Vector:
                                if (mat.HasProperty(prop))
                                    materialSnapshot.vectors.Add(new VectorProperty { name = prop, value = mat.GetVector(prop) });
                                break;

                            case ShaderPropertyType.Float:
                            case ShaderPropertyType.Range:
                                if (mat.HasProperty(prop))
                                    materialSnapshot.floats.Add(new FloatProperty { name = prop, value = mat.GetFloat(prop) });
                                break;

                            case ShaderPropertyType.Texture:
                                if (mat.HasProperty(prop))
                                {
                                    var t = mat.GetTexture(prop);
                                    if (t != null)
                                    {
                                        materialSnapshot.textures.Add(new TextureProperty
                                        {
                                            name = prop,
                                            texture = t,
                                            offset = mat.GetTextureOffset(prop),
                                            scale = mat.GetTextureScale(prop)
                                        });
                                    }
                                }
                                break;
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"VoluSmokePreset: Could not capture property '{prop}': {e.Message}");
                    }
                }
            }
    #endif
        }

        void ApplyMaterialSnapshot(Material mat)
        {
            if (mat == null || materialSnapshot == null) return;


            if (!string.IsNullOrEmpty(materialSnapshot.shaderName))
            {
                var sh = Shader.Find(materialSnapshot.shaderName);
                if (sh != null && mat.shader != sh) mat.shader = sh;
            }

            mat.renderQueue = materialSnapshot.renderQueue;
            if (materialSnapshot.keywords != null)
                mat.shaderKeywords = materialSnapshot.keywords;

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", materialSnapshot.color);

            foreach (var c in materialSnapshot.colors)
                if (mat.HasProperty(c.name)) mat.SetColor(c.name, c.value);

            foreach (var v in materialSnapshot.vectors)
                if (mat.HasProperty(v.name)) mat.SetVector(v.name, v.value);

            foreach (var f in materialSnapshot.floats)
                if (mat.HasProperty(f.name)) mat.SetFloat(f.name, f.value);

            foreach (var t in materialSnapshot.textures)
                if (mat.HasProperty(t.name) && t.texture != null)
                {
                    mat.SetTexture(t.name, t.texture);
                    mat.SetTextureOffset(t.name, t.offset);
                    mat.SetTextureScale(t.name, t.scale);
                }
        }

    #if UNITY_EDITOR




        public const string PresetFolder = "Assets/VertexField/VoluSmoke FX/Presets";

        public static void EnsurePresetFolder()
        {
            var parts = PresetFolder.Replace("\\", "/").Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private const string PREF_GUID = "VertexField_VoluSmokePreset_StartingGUID";
        private const string PREF_LOCK = "VertexField_VoluSmokePreset_ProtectionEnabled";

        public static bool ProtectionEnabled
        {
            get => EditorPrefs.GetBool(PREF_LOCK, true);
            set => EditorPrefs.SetBool(PREF_LOCK, value);
        }

        public static string StartingGuid
        {
            get => EditorPrefs.GetString(PREF_GUID, string.Empty);
            private set => EditorPrefs.SetString(PREF_GUID, value ?? string.Empty);
        }

        public static VoluSmokePreset LoadDefaultPreset()
        {
            var guid = StartingGuid;
            if (string.IsNullOrEmpty(guid)) return null;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<VoluSmokePreset>(path);
        }

        public bool IsStartingPresetInstance()
        {
            string path = AssetDatabase.GetAssetPath(this);
            string guid = AssetDatabase.AssetPathToGUID(path);
            return !string.IsNullOrEmpty(guid) && guid == StartingGuid;
        }

        public static void MarkAsStarting(VoluSmokePreset preset, bool enableProtection)
        {
            if (preset == null) { StartingGuid = string.Empty; return; }

            string path = AssetDatabase.GetAssetPath(preset);
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("Selected VoluSmokePreset is not an asset on disk.");
                return;
            }

            StartingGuid = AssetDatabase.AssetPathToGUID(path);
            ProtectionEnabled = enableProtection;
            Debug.Log($"Starting VoluSmoke Preset set to '{preset.name}'. Protection {(ProtectionEnabled ? "ON" : "OFF")}\n{path}");
        }

        private class StartingPresetProtection : AssetModificationProcessor
        {
            static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
            {
                if (!ProtectionEnabled) return AssetDeleteResult.DidNotDelete;

                var guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid) && guid == StartingGuid)
                {
                    EditorUtility.DisplayDialog("Protected Preset",
                        "This preset is marked as the Starting VoluSmoke Preset and cannot be deleted while protection is ON.\n\n" +
                        "Select another starting preset or toggle protection OFF.",
                        "OK");
                    return AssetDeleteResult.FailedDelete;
                }
                return AssetDeleteResult.DidNotDelete;
            }

            static AssetMoveResult OnWillMoveAsset(string oldPath, string newPath)
            {
                if (!ProtectionEnabled) return AssetMoveResult.DidNotMove;

                var guid = AssetDatabase.AssetPathToGUID(oldPath);
                if (!string.IsNullOrEmpty(guid) && guid == StartingGuid)
                {
                    EditorUtility.DisplayDialog("Protected Preset",
                        "This preset is marked as the Starting VoluSmoke Preset and cannot be moved/renamed while protection is ON.\n\n" +
                        "Select another starting preset or toggle protection OFF.",
                        "OK");
                    return AssetMoveResult.FailedMove;
                }
                return AssetMoveResult.DidNotMove;
            }
        }

        private class PresetFolderEnforcer : AssetModificationProcessor
        {
            static void OnWillCreateAsset(string path)
            {
                if (!path.EndsWith(".asset.meta")) return;
                EditorApplication.delayCall += () =>
                {
                    string assetPath = path.Substring(0, path.Length - 5);
                    var preset = AssetDatabase.LoadAssetAtPath<VoluSmokePreset>(assetPath);
                    if (preset == null) return;

                    EnsurePresetFolder();

                    string normalized = assetPath.Replace("\\", "/");
                    if (!normalized.StartsWith(PresetFolder + "/"))
                    {
                        string sourceFolder = Path.GetDirectoryName(assetPath).Replace("\\", "/");
                        string fileName = Path.GetFileName(assetPath);
                        string target = AssetDatabase.GenerateUniqueAssetPath($"{PresetFolder}/{fileName}");
                        var err = AssetDatabase.MoveAsset(assetPath, target);
                        if (!string.IsNullOrEmpty(err))
                        {
                            Debug.LogError($"Failed moving preset to '{PresetFolder}': {err}");
                        }
                        else
                        {
                            Debug.Log($"VoluSmokePreset moved to: {target}");

                            if (!string.IsNullOrEmpty(sourceFolder) && AssetDatabase.IsValidFolder(sourceFolder))
                            {
                                string[] remainingAssets = AssetDatabase.FindAssets("", new[] { sourceFolder });
                                if (remainingAssets.Length == 0)
                                {
                                    bool deleted = AssetDatabase.DeleteAsset(sourceFolder);
                                    if (deleted) Debug.Log($"Deleted empty folder: {sourceFolder}");
                                }
                            }
                        }
                    }
                };
            }
        }

        [CustomEditor(typeof(VoluSmokePreset))]
        private class VoluSmokePresetInspector : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                EditorGUILayout.Space(8);

                var preset = (VoluSmokePreset)target;
                bool isStarting = preset.IsStartingPresetInstance();

                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField("Starting Preset", EditorStyles.boldLabel);

                    bool newIsStarting = EditorGUILayout.ToggleLeft("This is the Starting Preset", isStarting);

                    using (new EditorGUI.DisabledScope(!newIsStarting))
                    {
                        bool newProt = EditorGUILayout.ToggleLeft("Protection ON (lock delete/move)", ProtectionEnabled);
                        if (newProt != ProtectionEnabled) ProtectionEnabled = newProt;
                    }

                    if (newIsStarting != isStarting)
                    {
                        if (newIsStarting) MarkAsStarting(preset, ProtectionEnabled);
                        else if (isStarting) StartingGuid = string.Empty;
                    }

                    if (GUILayout.Button("Ping Current Starting Preset"))
                    {
                        var def = LoadDefaultPreset();
                        if (def != null) { Selection.activeObject = def; EditorGUIUtility.PingObject(def); }
                        else EditorUtility.DisplayDialog("Starting Preset", "No starting preset set.", "OK");
                    }

                    if (GUILayout.Button("Ensure Preset Folder Exists"))
                        EnsurePresetFolder();

                    if (GUILayout.Button("Rebuild Material Snapshot (from Asset or Renderer)"))
                        RebuildSnapshotMenu(preset);
                }
            }
        }


        void OnValidate()
        {
            if (planarNormalWeight < 0f) planarNormalWeight = 0f;
            else if (planarNormalWeight > 1f) planarNormalWeight = 1f;
            if (inwardBubbleStrength < 0f) inwardBubbleStrength = 0f;
            else if (inwardBubbleStrength > 1f) inwardBubbleStrength = 1f;

            if (materialSnapshot == null) materialSnapshot = new MaterialSnapshot();

            if (string.IsNullOrEmpty(materialSnapshot.shaderName) && voluSmokeMaterial != null)
            {
                CaptureMaterialSnapshot(voluSmokeMaterial);
    #if UNITY_EDITOR
                EditorUtility.SetDirty(this);
    #endif
            }
        }

        [ContextMenu("Rebuild Material Snapshot")]
        void RebuildSnapshotContext()
        {
    #if UNITY_EDITOR
            RebuildSnapshotMenu(this);
    #endif
        }

        static void RebuildSnapshotMenu(VoluSmokePreset preset)
        {
            if (preset == null) return;


            if (preset.voluSmokeMaterial != null)
            {
                preset.CaptureMaterialSnapshot(preset.voluSmokeMaterial);
                Debug.Log($"VoluSmokePreset: snapshot rebuilt from material asset '{preset.voluSmokeMaterial.name}'.", preset);
            }
            else
            {

                var gen = Object.FindFirstObjectByType<VoluSmokeMeshGenerator>();
                if (gen != null && gen.TryGetComponent<MeshRenderer>(out var mr) && mr.sharedMaterial != null)
                {
                    preset.CaptureMaterialSnapshot(mr.sharedMaterial);
                    Debug.Log($"VoluSmokePreset: snapshot rebuilt from renderer material '{mr.sharedMaterial.name}'.", preset);
                }
                else
                {
                    Debug.LogWarning("VoluSmokePreset: no source material found to rebuild snapshot.", preset);
                }
            }
            EditorUtility.SetDirty(preset);
            AssetDatabase.SaveAssets();
        }
    #endif
    }
}
