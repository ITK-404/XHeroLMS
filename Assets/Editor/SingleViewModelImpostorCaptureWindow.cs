#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public sealed class SingleViewModelImpostorCaptureWindow : EditorWindow
{
private const string DefaultOutputFolder = "Assets/SingleViewImpostor/Generated";
private const string ShaderName = "Universal Render Pipeline/Unlit";
private const int CaptureLayer = 31;

    private GameObject _sourceModel;
    private string _outputFolder = DefaultOutputFolder;
    private int _textureSize = 2048;
    private float _padding = 1.12f;
    private int _cropPaddingPixels = 24;
    private bool _forceTwoSidedMaterials = true;
    private bool _disableScriptsOnClone = true;
    private bool _billboardToCamera = true;
    private bool _lockYAxis = false;
    private bool _useAlphaClip = false;

    // Tool-camera view state. Capture uses these values directly.
    private float _orbitYaw = 0f;
    private float _orbitPitch = 10f;
    private float _previewZoom = 1f;

    private Color _ambientColor = new Color(0.72f, 0.72f, 0.72f, 1f);
    private float _directionalLightIntensity = 1.1f;
    private Vector3 _directionalLightEuler = new Vector3(45f, -35f, 0f);
    private Color _previewBackground = new Color(0.18f, 0.18f, 0.18f, 1f);

    private Scene _previewScene;
    private GameObject _previewClone;
    private Camera _toolCamera;
    private Light _toolLight;
    private RenderTexture _previewTexture;
    private readonly List<Material> _previewTempMaterials = new List<Material>();
    private Bounds _previewBounds;
    private bool _previewDirty = true;
    private int _lastSourceInstanceId;
    private Vector2 _settingsScroll;

    [MenuItem("Tools/Impostor/Capture Selected Model -> Single View Flat")]
    public static void Open()
    {
        SingleViewModelImpostorCaptureWindow window = GetWindow<SingleViewModelImpostorCaptureWindow>("Single View Impostor");
        window.minSize = new Vector2(560f, 700f);
        window.TryUseSelection();
    }

    private void OnEnable()
    {
        EditorApplication.update += Repaint;
        EnsurePreviewScene();
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
        CleanupPreviewObjects();
        ReleasePreviewTexture();
        ClosePreviewScene();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Single View Flat Impostor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tool này không lấy SceneView/MainCamera nữa. Nó dùng 1 camera nội bộ trong cửa sổ preview. Xoay model ngay trong preview, nhấn Capture là chụp đúng góc đang nhìn thấy.",
            MessageType.Info);

        EditorGUI.BeginChangeCheck();

        _sourceModel = (GameObject)EditorGUILayout.ObjectField("Source Model", _sourceModel, typeof(GameObject), true);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Use Selection", GUILayout.Height(24f)))
            {
                TryUseSelection();
                MarkPreviewDirty();
            }

            if (GUILayout.Button("Reset View", GUILayout.Height(24f)))
            {
                SetToolView(0f, 10f, 1f);
            }

            if (GUILayout.Button("Fit Preview", GUILayout.Height(24f)))
            {
                _previewZoom = 1f;
                Repaint();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Front +Z", GUILayout.Height(23f))) SetToolView(0f, 0f, 1f);
            if (GUILayout.Button("Back -Z", GUILayout.Height(23f))) SetToolView(180f, 0f, 1f);
            if (GUILayout.Button("Right +X", GUILayout.Height(23f))) SetToolView(90f, 0f, 1f);
            if (GUILayout.Button("Left -X", GUILayout.Height(23f))) SetToolView(-90f, 0f, 1f);
            if (GUILayout.Button("Top +Y", GUILayout.Height(23f))) SetToolView(0f, 89f, 1f);
            if (GUILayout.Button("Bottom -Y", GUILayout.Height(23f))) SetToolView(0f, -89f, 1f);
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _orbitYaw = EditorGUILayout.Slider("Tool Camera Yaw", _orbitYaw, -180f, 180f);
            if (GUILayout.Button("0", GUILayout.Width(28f))) _orbitYaw = 0f;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            _orbitPitch = EditorGUILayout.Slider("Tool Camera Pitch", _orbitPitch, -89f, 89f);
            if (GUILayout.Button("0", GUILayout.Width(28f))) _orbitPitch = 0f;
        }

        _previewZoom = EditorGUILayout.Slider("Preview Zoom", _previewZoom, 0.35f, 3f);

        DrawPreviewArea();

        _settingsScroll = EditorGUILayout.BeginScrollView(_settingsScroll);

        using (new EditorGUILayout.HorizontalScope())
        {
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (GUILayout.Button("...", GUILayout.Width(34f)))
            {
                string picked = EditorUtility.OpenFolderPanel("Output Folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(picked) && picked.StartsWith(Application.dataPath))
                    _outputFolder = "Assets" + picked.Substring(Application.dataPath.Length).Replace('\\', '/');
            }
        }

        _textureSize = EditorGUILayout.IntPopup("Texture Size", _textureSize,
            new[] { "1024", "2048", "4096", "8192" },
            new[] { 1024, 2048, 4096, 8192 });

        _padding = EditorGUILayout.Slider("Camera Padding", _padding, 1.0f, 1.8f);
        _cropPaddingPixels = EditorGUILayout.IntSlider("Crop Padding Pixels", _cropPaddingPixels, 0, 128);
        _forceTwoSidedMaterials = EditorGUILayout.Toggle("Force Two Sided Clone", _forceTwoSidedMaterials);
        _disableScriptsOnClone = EditorGUILayout.Toggle("Disable Scripts On Clone", _disableScriptsOnClone);
        _billboardToCamera = EditorGUILayout.Toggle("Prefab Billboard To Camera", _billboardToCamera);
        _lockYAxis = EditorGUILayout.Toggle("Lock Y Axis", _lockYAxis);
        _useAlphaClip = EditorGUILayout.Toggle("Material Alpha Clip", _useAlphaClip);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Preview / Capture Lighting", EditorStyles.boldLabel);
        _ambientColor = EditorGUILayout.ColorField("Ambient Color", _ambientColor);
        _directionalLightIntensity = EditorGUILayout.Slider("Directional Intensity", _directionalLightIntensity, 0f, 3f);
        _directionalLightEuler = EditorGUILayout.Vector3Field("Directional Euler", _directionalLightEuler);
        _previewBackground = EditorGUILayout.ColorField("Preview Background", _previewBackground);

        if (EditorGUI.EndChangeCheck())
            MarkPreviewDirty();

        EditorGUILayout.Space(10);

        GUI.enabled = _sourceModel != null;
        if (GUILayout.Button("Capture What Tool Camera Sees", GUILayout.Height(36f)))
            CaptureCurrentToolView();
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    private void DrawPreviewArea()
    {
        EditorGUILayout.Space(6);
        Rect rect = GUILayoutUtility.GetRect(10f, 350f, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, _previewBackground);

        HandlePreviewInput(rect);

        if (_sourceModel == null)
        {
            DrawCenteredLabel(rect, "Kéo model/prefab vào Source Model để xem trực tiếp");
            return;
        }

        if (Event.current.type == EventType.Repaint)
        {
            EnsurePreviewObjects();
            RenderPreview(rect.width, rect.height);
        }

        if (_previewTexture != null)
            GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit, false);
        else
            DrawCenteredLabel(rect, "Không render được preview. Kiểm tra model có Renderer không.");

        DrawPreviewOverlay(rect);
    }

    private void HandlePreviewInput(Rect rect)
    {
        Event e = Event.current;
        if (!rect.Contains(e.mousePosition))
            return;

        if (e.type == EventType.MouseDrag && e.button == 0)
        {
            _orbitYaw += e.delta.x * 0.35f;
            _orbitPitch -= e.delta.y * 0.35f;
            _orbitYaw = Mathf.Repeat(_orbitYaw + 180f, 360f) - 180f;
            _orbitPitch = Mathf.Clamp(_orbitPitch, -89f, 89f);
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.ScrollWheel)
        {
            _previewZoom *= 1f + e.delta.y * 0.06f;
            _previewZoom = Mathf.Clamp(_previewZoom, 0.35f, 3f);
            e.Use();
            Repaint();
        }
    }

    private void DrawPreviewOverlay(Rect rect)
    {
        Rect box = new Rect(rect.x + 8f, rect.y + 8f, 390f, 62f);
        EditorGUI.DrawRect(box, new Color(0f, 0f, 0f, 0.55f));
        GUI.Label(new Rect(box.x + 8f, box.y + 4f, box.width - 16f, 18f), "Preview trực tiếp - chưa ghi file", EditorStyles.whiteLabel);
        GUI.Label(new Rect(box.x + 8f, box.y + 22f, box.width - 16f, 18f), "Kéo chuột trái để xoay | Lăn chuột để zoom", EditorStyles.whiteLabel);
        GUI.Label(new Rect(box.x + 8f, box.y + 40f, box.width - 16f, 18f), "Capture sẽ dùng đúng góc Tool Camera này", EditorStyles.whiteLabel);
    }

    private void DrawCenteredLabel(Rect rect, string text)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;
        GUI.Label(rect, text, style);
    }

    private void TryUseSelection()
    {
        if (Selection.activeGameObject != null)
            _sourceModel = Selection.activeGameObject;
        else if (Selection.activeObject is GameObject)
            _sourceModel = (GameObject)Selection.activeObject;
    }

    private void SetToolView(float yaw, float pitch, float zoom)
    {
        _orbitYaw = yaw;
        _orbitPitch = Mathf.Clamp(pitch, -89f, 89f);
        _previewZoom = Mathf.Clamp(zoom, 0.35f, 3f);
        Repaint();
    }

    private void MarkPreviewDirty()
    {
        _previewDirty = true;
        Repaint();
    }

    private void EnsurePreviewScene()
    {
        if (_previewScene.IsValid())
            return;

        _previewScene = EditorSceneManager.NewPreviewScene();
    }

    private void ClosePreviewScene()
    {
        if (!_previewScene.IsValid())
            return;

        EditorSceneManager.ClosePreviewScene(_previewScene);
        _previewScene = default;
    }

    private void EnsurePreviewObjects()
    {
        EnsurePreviewScene();

        int sourceId = _sourceModel != null ? _sourceModel.GetInstanceID() : 0;
        if (!_previewDirty && _previewClone != null && _toolCamera != null && _toolLight != null && _lastSourceInstanceId == sourceId)
            return;

        CleanupPreviewObjects();

        if (_sourceModel == null)
            return;

        _lastSourceInstanceId = sourceId;
        _previewClone = CreateIsolatedClone(_sourceModel, _previewTempMaterials);
        if (_previewClone == null)
            return;

        ForceLodZero(_previewClone);
        ForceSkinnedMeshVisible(_previewClone);

        if (!TryGetRendererBounds(_previewClone, out _previewBounds))
        {
            CleanupPreviewObjects();
            return;
        }

        _toolCamera = CreateToolCamera();
        _toolLight = CreateToolLight();
        _previewDirty = false;
    }

    private GameObject CreateIsolatedClone(GameObject source, List<Material> tempMaterials)
    {
        EnsurePreviewScene();

        GameObject clone = Object.Instantiate(source);
        clone.name = MakeSafeFileName(source.name) + "_ToolPreviewClone";
        SceneManager.MoveGameObjectToScene(clone, _previewScene);

        SetHideFlagsRecursively(clone, HideFlags.HideAndDontSave);
        SetLayerRecursively(clone, CaptureLayer);

        if (!clone.activeSelf)
            clone.SetActive(true);

        if (_disableScriptsOnClone)
            DisableBehavioursOnClone(clone);

        if (_forceTwoSidedMaterials)
            MakeCloneMaterialsTwoSided(clone, tempMaterials);

        NormalizeCloneToOrigin(clone, Vector3.zero);
        return clone;
    }

    private void CleanupPreviewObjects()
    {
        for (int i = 0; i < _previewTempMaterials.Count; i++)
            if (_previewTempMaterials[i] != null)
                Object.DestroyImmediate(_previewTempMaterials[i]);
        _previewTempMaterials.Clear();

        if (_previewClone != null) Object.DestroyImmediate(_previewClone);
        if (_toolCamera != null) Object.DestroyImmediate(_toolCamera.gameObject);
        if (_toolLight != null) Object.DestroyImmediate(_toolLight.gameObject);

        _previewClone = null;
        _toolCamera = null;
        _toolLight = null;
    }

    private void RenderPreview(float width, float height)
    {
        if (_toolCamera == null || _previewClone == null)
            return;

        int rtWidth = Mathf.Clamp(Mathf.RoundToInt(width), 256, 1600);
        int rtHeight = Mathf.Clamp(Mathf.RoundToInt(height), 180, 1000);
        EnsurePreviewTexture(rtWidth, rtHeight);

        Vector3 direction;
        Vector3 up;
        GetToolViewDirection(out direction, out up);
        Vector2 viewSize = CalculateViewSize(_previewBounds, direction, up);
        float aspect = rtWidth / Mathf.Max(1f, (float)rtHeight);
        SetupCameraForView(_toolCamera, _previewBounds, direction, up, viewSize, _padding, _previewZoom, aspect);

        _toolCamera.backgroundColor = _previewBackground;
        _toolLight.intensity = _directionalLightIntensity;
        _toolLight.transform.rotation = Quaternion.Euler(_directionalLightEuler);

        RenderSettingsSnapshot snapshot = ApplyTemporaryRenderSettings();
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevTarget = _toolCamera.targetTexture;

        try
        {
            _toolCamera.targetTexture = _previewTexture;
            RenderTexture.active = _previewTexture;
            GL.Clear(true, true, _previewBackground);
            _toolCamera.Render();
        }
        finally
        {
            _toolCamera.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            RestoreRenderSettings(snapshot);
        }
    }

    private void EnsurePreviewTexture(int width, int height)
    {
        if (_previewTexture != null && _previewTexture.width == width && _previewTexture.height == height)
            return;

        ReleasePreviewTexture();
        _previewTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
        {
            antiAliasing = 4,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "SingleView_ToolPreviewRT"
        };
        _previewTexture.Create();
    }

    private void ReleasePreviewTexture()
    {
        if (_previewTexture == null)
            return;

        _previewTexture.Release();
        Object.DestroyImmediate(_previewTexture);
        _previewTexture = null;
    }

    private void CaptureCurrentToolView()
    {
        if (_sourceModel == null)
        {
            Debug.LogError("[SingleViewImpostor] Source Model is null.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_outputFolder) || !_outputFolder.StartsWith("Assets"))
        {
            Debug.LogError("[SingleViewImpostor] Output folder must start with Assets.");
            return;
        }

        Shader shader = FindShader();
        if (shader == null)
        {
            Debug.LogError("[SingleViewImpostor] Cannot find shader: " + ShaderName);
            return;
        }

        EnsurePreviewObjects();
        if (_previewClone == null || _toolCamera == null || _toolLight == null)
        {
            Debug.LogError("[SingleViewImpostor] Preview clone/camera is not ready. Source may have no Renderer.");
            return;
        }

        EnsureFolder(_outputFolder);
        string safeName = MakeSafeFileName(_sourceModel.name);
        string sessionFolder = _outputFolder + "/" + safeName + "_SingleView_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        EnsureFolder(sessionFolder);

        try
        {
            Vector3 direction;
            Vector3 up;
            GetToolViewDirection(out direction, out up);

            Vector2 viewSize = CalculateViewSize(_previewBounds, direction, up);
            string texturePath = sessionFolder + "/" + safeName + "_SingleView.png";
            Vector2 croppedWorldSize = CaptureAndCropCurrentPreview(_toolCamera, _previewBounds, direction, up, viewSize, texturePath);

            AssetDatabase.Refresh();
            SetTextureImportSettings(texturePath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            string materialPath = sessionFolder + "/" + safeName + "_SingleView_Material.mat";
            Material material = CreateMaterial(shader, texture, materialPath);

            string prefabPath = sessionFolder + "/" + safeName + "_SingleView_Impostor.prefab";
            CreatePrefab(prefabPath, material, croppedWorldSize);

            Debug.Log(
                "[SingleViewImpostor] Done! Captured from Tool Camera view.\n" +
                "Folder: " + sessionFolder + "\n" +
                "Texture: " + texturePath + "\n" +
                "WorldSize: " + croppedWorldSize + "\n" +
                "Prefab: " + prefabPath);

            Object prefab = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
            if (prefab != null)
                Selection.activeObject = prefab;
        }
        finally
        {
            // Keep preview objects alive after capture so user can continue rotating/capturing.
            Repaint();
        }
    }

private Shader FindShader()
{
    Shader shader = Shader.Find(ShaderName);
    if (shader != null)
        return shader;

    AssetDatabase.Refresh();

    shader = Shader.Find(ShaderName);
    if (shader != null)
        return shader;

    return null;
}

    private Camera CreateToolCamera()
    {
        EnsurePreviewScene();

        GameObject go = new GameObject("SingleView_ToolOnlyCamera");
        go.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(go, _previewScene);

        Camera cam = go.AddComponent<Camera>();
        cam.enabled = false;
        cam.cameraType = CameraType.Game;
        cam.scene = _previewScene;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = _previewBackground;
        cam.cullingMask = 1 << CaptureLayer;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 10000f;
        cam.allowHDR = false;
        cam.allowMSAA = true;
        cam.useOcclusionCulling = false;
        return cam;
    }

    private Light CreateToolLight()
    {
        EnsurePreviewScene();

        GameObject go = new GameObject("SingleView_ToolOnlyDirectionalLight");
        go.hideFlags = HideFlags.HideAndDontSave;
        SceneManager.MoveGameObjectToScene(go, _previewScene);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = _directionalLightIntensity;
        light.color = Color.white;
        light.cullingMask = 1 << CaptureLayer;
        light.transform.rotation = Quaternion.Euler(_directionalLightEuler);
        return light;
    }

    private void GetToolViewDirection(out Vector3 direction, out Vector3 up)
    {
        Quaternion rot = Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
        direction = (rot * Vector3.forward).normalized;

        // Avoid LookRotation flipping when looking almost straight from top/bottom.
        if (_orbitPitch > 80f)
            up = Vector3.forward;
        else if (_orbitPitch < -80f)
            up = Vector3.back;
        else
            up = Vector3.up;
    }

    private void SetupCameraForView(Camera cam, Bounds bounds, Vector3 direction, Vector3 up, Vector2 viewSize, float padding, float zoom, float aspect)
    {
        aspect = Mathf.Max(0.01f, aspect);
        float radius = Mathf.Max(bounds.extents.magnitude, 0.1f);
        float distance = radius * 3f + 5f;

        cam.transform.position = bounds.center + direction.normalized * distance;
        cam.transform.rotation = Quaternion.LookRotation(bounds.center - cam.transform.position, up);
        cam.aspect = aspect;

        // OrthographicSize is vertical half-size. Fit both width and height.
        float verticalSize = Mathf.Max(viewSize.y, viewSize.x / aspect);
        cam.orthographicSize = verticalSize * 0.5f * padding * zoom;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = distance + radius * 6f + 10f;
    }

    private Vector2 CaptureAndCropCurrentPreview(Camera cam, Bounds bounds, Vector3 direction, Vector3 up, Vector2 viewSize, string outputPath)
    {
        const float captureAspect = 1f;
        SetupCameraForView(cam, bounds, direction, up, viewSize, _padding, _previewZoom, captureAspect);

        float fullWorldHeight = cam.orthographicSize * 2f;
        float fullWorldWidth = fullWorldHeight * captureAspect;

        RenderTexture rt = new RenderTexture(_textureSize, _textureSize, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
        {
            antiAliasing = 4,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "SingleView_CaptureRT"
        };

        Texture2D tex = new Texture2D(_textureSize, _textureSize, TextureFormat.RGBA32, false, false);
        RenderTexture prevActive = RenderTexture.active;
        RenderTexture prevTarget = cam.targetTexture;
        Color prevBackground = cam.backgroundColor;

        RenderSettingsSnapshot snapshot = ApplyTemporaryRenderSettings();
        try
        {
            rt.Create();
            cam.targetTexture = rt;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            RenderTexture.active = rt;
            GL.Clear(true, true, new Color(0f, 0f, 0f, 0f));
            cam.Render();

            tex.ReadPixels(new Rect(0, 0, _textureSize, _textureSize), 0, 0, false);
            tex.Apply(false, false);

            RectInt cropRect = FindAlphaCropRect(tex, _cropPaddingPixels);
            Texture2D cropped = CropTexture(tex, cropRect);
            cropped.Apply(false, false);
            File.WriteAllBytes(outputPath, cropped.EncodeToPNG());

            Vector2 worldSize = new Vector2(
                fullWorldWidth * cropRect.width / _textureSize,
                fullWorldHeight * cropRect.height / _textureSize);

            Object.DestroyImmediate(cropped);
            return worldSize;
        }
        finally
        {
            RestoreRenderSettings(snapshot);
            cam.backgroundColor = prevBackground;
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
        }
    }

    private struct RenderSettingsSnapshot
    {
        public AmbientMode AmbientMode;
        public Color AmbientLight;
        public bool Fog;
    }

    private RenderSettingsSnapshot ApplyTemporaryRenderSettings()
    {
        RenderSettingsSnapshot snapshot = new RenderSettingsSnapshot
        {
            AmbientMode = RenderSettings.ambientMode,
            AmbientLight = RenderSettings.ambientLight,
            Fog = RenderSettings.fog
        };

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = _ambientColor;
        RenderSettings.fog = false;
        return snapshot;
    }

    private void RestoreRenderSettings(RenderSettingsSnapshot snapshot)
    {
        RenderSettings.ambientMode = snapshot.AmbientMode;
        RenderSettings.ambientLight = snapshot.AmbientLight;
        RenderSettings.fog = snapshot.Fog;
    }

    private RectInt FindAlphaCropRect(Texture2D tex, int padding)
    {
        Color32[] pixels = tex.GetPixels32();
        int width = tex.width;
        int height = tex.height;

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                byte a = pixels[y * width + x].a;
                if (a <= 4)
                    continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
            return new RectInt(0, 0, width, height);

        minX = Mathf.Max(0, minX - padding);
        minY = Mathf.Max(0, minY - padding);
        maxX = Mathf.Min(width - 1, maxX + padding);
        maxY = Mathf.Min(height - 1, maxY + padding);

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private Texture2D CropTexture(Texture2D tex, RectInt rect)
    {
        Color[] pixels = tex.GetPixels(rect.x, rect.y, rect.width, rect.height);
        Texture2D cropped = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false, false);
        cropped.SetPixels(pixels);
        return cropped;
    }

private Material CreateMaterial(Shader shader, Texture2D texture, string materialPath)
{
    shader = Shader.Find("Universal Render Pipeline/Unlit");

    if (shader == null)
    {
        Debug.LogError("[SingleViewImpostor] Cannot find shader: Universal Render Pipeline/Unlit");
        return null;
    }

    Material material = new Material(shader)
    {
        name = Path.GetFileNameWithoutExtension(materialPath)
    };

    // Base Map
    if (material.HasProperty("_BaseMap"))
        material.SetTexture("_BaseMap", texture);

    // Fallback
    if (material.HasProperty("_MainTex"))
        material.SetTexture("_MainTex", texture);

    // Base Color
    if (material.HasProperty("_BaseColor"))
        material.SetColor("_BaseColor", Color.white);

    if (material.HasProperty("_Color"))
        material.SetColor("_Color", Color.white);

    // Alpha Clipping ON
    if (material.HasProperty("_AlphaClip"))
        material.SetFloat("_AlphaClip", 1f);

    if (material.HasProperty("_Cutoff"))
        material.SetFloat("_Cutoff", 0.05f);

    material.EnableKeyword("_ALPHATEST_ON");

    // Two sided
    if (material.HasProperty("_Cull"))
        material.SetFloat("_Cull", (float)CullMode.Off);

    material.renderQueue = (int)RenderQueue.AlphaTest;
    material.SetOverrideTag("RenderType", "TransparentCutout");

    Debug.Log("[SingleViewImpostor] Created material shader = " + material.shader.name);

    AssetDatabase.CreateAsset(material, materialPath);
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    return material;
}

    private void CreatePrefab(string prefabPath, Material material, Vector2 worldSize)
    {
        GameObject root = new GameObject(_sourceModel.name + "_SingleView_Impostor");
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Billboard_Quad";
        quad.transform.SetParent(root.transform, false);

        Collider collider = quad.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        SimpleImpostorBillboard billboard = root.AddComponent<SimpleImpostorBillboard>();
        billboard.targetRenderer = renderer;
        billboard.visualTransform = quad.transform;
        billboard.billboardToCamera = _billboardToCamera;
        billboard.lockYAxis = _lockYAxis;
        billboard.worldSize = worldSize;
        billboard.sizeMultiplier = 1f;

        try
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssetDatabase.SaveAssets();
    }

    private void DisableBehavioursOnClone(GameObject clone)
    {
        MonoBehaviour[] behaviours = clone.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
            if (behaviours[i] != null)
                behaviours[i].enabled = false;
    }

    private void ForceLodZero(GameObject clone)
    {
        LODGroup[] groups = clone.GetComponentsInChildren<LODGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] == null)
                continue;

            groups[i].enabled = true;
            groups[i].ForceLOD(0);
        }
    }

    private void ForceSkinnedMeshVisible(GameObject clone)
    {
        SkinnedMeshRenderer[] skins = clone.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] == null)
                continue;

            skins[i].updateWhenOffscreen = true;
            skins[i].forceMatrixRecalculationPerRender = true;
        }
    }

    private void MakeCloneMaterialsTwoSided(GameObject clone, List<Material> tempMaterials)
    {
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            Material[] shared = r.sharedMaterials;
            Material[] copied = new Material[shared.Length];

            for (int m = 0; m < shared.Length; m++)
            {
                Material source = shared[m];
                if (source == null)
                {
                    copied[m] = null;
                    continue;
                }

                Material copy = new Material(source);
                if (copy.HasProperty("_Cull")) copy.SetFloat("_Cull", 0f);
                if (copy.HasProperty("_CullMode")) copy.SetFloat("_CullMode", 0f);
                if (copy.HasProperty("_CullModeForward")) copy.SetFloat("_CullModeForward", 0f);

                copied[m] = copy;
                tempMaterials.Add(copy);
            }

            r.sharedMaterials = copied;
        }
    }

    private Vector2 CalculateViewSize(Bounds bounds, Vector3 cameraDirectionFromObject, Vector3 up)
    {
        Vector3 forward = -cameraDirectionFromObject.normalized;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.right;
        Vector3 viewUp = Vector3.Cross(forward, right).normalized;
        Vector3[] corners = GetBoundsCorners(bounds);

        float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
        float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 p = corners[i] - bounds.center;
            float x = Vector3.Dot(p, right);
            float y = Vector3.Dot(p, viewUp);
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
        }

        return new Vector2(Mathf.Max(0.0001f, maxX - minX), Mathf.Max(0.0001f, maxY - minY));
    }

    private void NormalizeCloneToOrigin(GameObject clone, Vector3 targetCenter)
    {
        Bounds bounds;
        if (TryGetRendererBounds(clone, out bounds))
            clone.transform.position += targetCenter - bounds.center;
    }

    private bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = new Bounds(root.transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || !r.enabled)
                continue;

            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return hasBounds;
    }

    private void SetLayerRecursively(GameObject root, int layer)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
            transforms[i].gameObject.layer = layer;
    }

    private void SetHideFlagsRecursively(GameObject root, HideFlags flags)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
            transforms[i].gameObject.hideFlags = flags;
    }

    private void SetTextureImportSettings(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath);
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = true;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = Mathf.Max(_textureSize, 8192);
        importer.SaveAndReimport();
    }

    private Vector3[] GetBoundsCorners(Bounds b)
    {
        Vector3 c = b.center;
        Vector3 e = b.extents;
        return new[]
        {
            c + new Vector3(-e.x, -e.y, -e.z), c + new Vector3(-e.x, -e.y,  e.z),
            c + new Vector3(-e.x,  e.y, -e.z), c + new Vector3(-e.x,  e.y,  e.z),
            c + new Vector3( e.x, -e.y, -e.z), c + new Vector3( e.x, -e.y,  e.z),
            c + new Vector3( e.x,  e.y, -e.z), c + new Vector3( e.x,  e.y,  e.z)
        };
    }

    private string MakeSafeFileName(string value)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            value = value.Replace(c, '_');
        return string.IsNullOrEmpty(value) ? "Model" : value;
    }

    private void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        if (!folder.StartsWith("Assets"))
        {
            Debug.LogError("[SingleViewImpostor] Folder must start with Assets: " + folder);
            return;
        }

        string current = "Assets";
        string relative = folder.Substring("Assets".Length).Trim('/');
        if (string.IsNullOrEmpty(relative))
            return;

        string[] parts = relative.Split('/');
        for (int i = 0; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
