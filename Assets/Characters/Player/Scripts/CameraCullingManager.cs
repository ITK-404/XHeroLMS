using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public class CameraCullingManager : MonoBehaviour
{
    [Header("References")]
    public Camera targetCamera;
    [Tooltip("Directional Light chính để tham chiếu ShadowDistance (không bắt buộc)")]
    public Light mainDirectionalLight;

    [Header("Distance Bands (m)")]
    [Tooltip("<= Near: LOD0, Visible")]
    public float nearDistance = 40f;
    [Tooltip("<= Mid: LOD1, Visible")]
    public float midDistance = 80f;
    [Tooltip("<= Far: LOD2, Visible (nếu nằm trong frustum) / ShadowsOnly (nếu ngoài frustum)")]
    public float farDistance = 120f;
    [Tooltip(">  Max: Hidden (disable hoàn toàn)")]
    public float maxRenderDistance = 160f;

    [Header("Layers")]
    [Tooltip("Những layer KHÔNG bao giờ bị ẩn (ví dụ Landmark/Terrain lớn).")]
    public LayerMask neverHideLayers;
    [Tooltip("Những layer được phép chuyển sang ShadowsOnly khi ngoài frustum.")]
    public LayerMask allowShadowsOnlyLayers = ~0; // mặc định cho phép tất cả

    [Header("Anti-Aliasing & Post")]
    public bool enableFXAA = true;
    public bool enableMSAA = false;
    [Range(2, 8)] public int msaaSamples = 2; // sẽ clamp về 2/4/8
    public bool enableMotionBlur = false;
    public float motionIntensity = 0.2f;

    [Header("Adaptive Performance (optional)")]
    public bool adaptiveFps = false;
    public int targetFPS = 120;
    public float renderScaleMin = 0.5f;
    public float renderScaleMax = 1.0f;
    public float renderScaleStep = 0.05f;
    public float fpsCheckInterval = 1.0f;

    [Header("Smart Focus Quality")]
    [Tooltip("Tự động buff chất lượng trong vùng nhìn camera")]
    public bool focusBoost = true;

    [Tooltip("RenderScale cực nét khi đang nhìn trực tiếp vào vật thể (1.0 = gốc)")]
    [Range(0.5f, 1.5f)] public float focusQualityScale = 1.15f;

    [Tooltip("RenderScale mặc định khi nhìn chung quanh")]
    [Range(0.3f, 1.0f)] public float baseQualityScale = 0.9f;

    [Tooltip("Thời gian nội suy giữa base và focus (giây)")]
    public float focusSmoothTime = 0.5f;
    [Header("Per-band RenderScale")]
    [Tooltip("Scale khi có vật thể đang thấy trong near band")]
    [Range(0.1f, 1.5f)] public float nearBandScale = 1.0f;
    [Tooltip("Scale khi chỉ còn vật thể mid band")]
    [Range(0.1f, 1.0f)] public float midBandScale  = 0.7f;
    [Tooltip("Scale khi chỉ còn vật thể far band")]
    [Range(0.05f, 1.0f)] public float farBandScale  = 0.2f;
    public LayerMask exemptLayers;
    // ==== internal ====
    struct Tracked
    {
        public Renderer rend;
        public LODGroup lod;
        public ShadowCastingMode originalShadowMode;
        public int layer;
    }

    private readonly List<Tracked> tracked = new();
    private BoundingSphere[] spheres;
    private CullingGroup cullingGroup;

    private float fpsTimer, deltaTime;
    private float _adaptiveScale = 1f;
    private float currentFocusScale = 1f;
    private float focusVelocity;

    // precomputed sqr distances
    private float dNear2, dMid2, dFar2, dMax2;

    // buffer cho QueryIndices
    private int[] _visibleIdxBuffer = new int[256];

    // cleanup định kỳ
    private float _cleanupTimer = 0f;
    public float cleanupInterval = 5f;

    void Awake()
    {
        if (!targetCamera) targetCamera = Camera.main;
        if (!targetCamera)
        {
            Debug.LogWarning("[CameraCullingManager] Không tìm thấy camera.");
            enabled = false;
            return;
        }
        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            exemptLayers |= (1 << playerLayer);

        targetCamera.useOcclusionCulling = true;

        // thu thập renderer + LODGroup một lần
        // var renders = FindObjectsOfType<Renderer>(true);
        #if UNITY_2023_1_OR_NEWER
        var renders = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        #else
        var renders = FindObjectsOfType<Renderer>(true);
        #endif
        tracked.Capacity = renders.Length;
        foreach (var r in renders)
        {
            if (!r || r is ParticleSystemRenderer) continue; // bỏ qua particle (thường tự cull)
            if (((1 << r.gameObject.layer) & exemptLayers.value) != 0) continue;
            var t = new Tracked
            {
                rend = r,
                lod = r.GetComponentInParent<LODGroup>(),
                originalShadowMode = r.shadowCastingMode,
                layer = r.gameObject.layer
            };
            tracked.Add(t);
        }

        // bounding spheres
        spheres = new BoundingSphere[tracked.Count];
        for (int i = 0; i < tracked.Count; i++)
        {
            var b = tracked[i].rend.bounds;
            spheres[i] = new BoundingSphere(b.center, b.extents.magnitude);
        }

        // set culling group
        cullingGroup = new CullingGroup();
        cullingGroup.targetCamera = targetCamera;
        cullingGroup.SetBoundingSpheres(spheres);
        cullingGroup.SetBoundingSphereCount(tracked.Count);

        // thiết lập dải khoảng cách (band 0/1/2/3)
        cullingGroup.SetDistanceReferencePoint(targetCamera.transform);
        cullingGroup.SetBoundingDistances(new float[] { nearDistance, midDistance, farDistance, maxRenderDistance });

        cullingGroup.onStateChanged += OnCullingStateChange;

        // cache sqr distances
        dNear2 = nearDistance * nearDistance;
        dMid2 = midDistance * midDistance;
        dFar2 = farDistance * farDistance;
        dMax2 = maxRenderDistance * maxRenderDistance;

        // guard: far <= shadowDistance để ShadowsOnly còn ý nghĩa
        float shadowDist = QualitySettings.shadowDistance;
        if (mainDirectionalLight && mainDirectionalLight.shadows != LightShadows.None)
            shadowDist = Mathf.Max(shadowDist, mainDirectionalLight.shadowNearPlane + 1f);
        if (farDistance > shadowDist) farDistance = shadowDist;

        ApplyAAAndPost();
        SetupOptionalLayerCullDistances();
    }

    void Update()
    {
        // dọn renderer null mỗi X giây
        _cleanupTimer += Time.unscaledDeltaTime;
        if (_cleanupTimer >= Mathf.Max(1f, cleanupInterval))
        {
            _cleanupTimer = 0f;
            CompactTracked();
        }
    }

    void LateUpdate()
    {
        if (cullingGroup == null) return;

        // cập nhật tâm sphere theo bounds hiện tại (đối tượng có thể di chuyển)
        for (int i = 0; i < tracked.Count; i++)
        {
            var r = tracked[i].rend;
            if (!r) continue;

            LocalBoundsToWorldSphere(r, out var c, out var rad);
            spheres[i].position = c;
            spheres[i].radius = rad;
        }

        // --- Adaptive scale ---
        if (adaptiveFps) UpdateAdaptiveScale();

        // --- Focus scale (chỉ tính toán currentFocusScale) ---
        if (focusBoost && targetCamera) UpdateFocusScaleFast();
        else currentFocusScale = Mathf.SmoothDamp(currentFocusScale, 1f, ref focusVelocity, focusSmoothTime);

        // --- GỘP scale và gọi ResizeBuffers đúng 1 lần ---
        float finalScale = Mathf.Clamp(_adaptiveScale * (focusBoost ? currentFocusScale : 1f),
                                       renderScaleMin, renderScaleMax);
        ScalableBufferManager.ResizeBuffers(finalScale, finalScale);
    }

    void OnDisable()
    {
        DisposeCullingGroupSafe();

        // trả renderer về mặc định
        for (int i = 0; i < tracked.Count; i++)
        {
            var t = tracked[i];
            if (!t.rend) continue;
            t.rend.shadowCastingMode = t.originalShadowMode;
            if (t.lod) t.lod.ForceLOD(-1);
            if (!t.rend.enabled) t.rend.enabled = true;
        }
        ScalableBufferManager.ResizeBuffers(1f, 1f);
    }

    void OnDestroy()
    {
        DisposeCullingGroupSafe();
    }

    void OnApplicationQuit()
    {
        ScalableBufferManager.ResizeBuffers(1f, 1f);
        DisposeCullingGroupSafe();
    }

    void DisposeCullingGroupSafe()
    {
        if (cullingGroup != null)
        {
            try
            {
                cullingGroup.onStateChanged -= OnCullingStateChange;
            }
            catch { /* ignore nếu đã null/đã gỡ */ }

            try
            {
                cullingGroup.Dispose();
            }
            catch { /* phòng sự cố khi dispose 2 lần */ }

            cullingGroup = null;
        }
    }

    // ======== Culling logic qua callback ========
    void OnCullingStateChange(CullingGroupEvent ev)
    {
        int i = ev.index;
        if (i < 0 || i >= tracked.Count) return;
        var t = tracked[i];
        var r = t.rend;
        if (!r) return;

        if (((1 << t.layer) & exemptLayers.value) != 0) return;

        // band (0=near, 1=mid, 2=far, 3=max+)
        int band = ev.currentDistance;

        // visible trong FRUSTUM?
        bool inFrustum = ev.isVisible;

        // landmark/whitelist: KHÔNG bao giờ Hidden
        bool neverHide = ((1 << t.layer) & neverHideLayers.value) != 0;

        // khoảng cách thực (sqr) để quyết định nhanh
        // float d2 = (r.transform.position - targetCamera.transform.position).sqrMagnitude;
        // float d2 = r.bounds.SqrDistance(targetCamera.transform.position);
        var s = spheres[i];
        float d = Vector3.Distance(targetCamera.transform.position, s.position) - s.radius;
        float d2 = (d <= 0f) ? 0f : d * d;

        // Nếu vượt max distance và không thuộc whitelist -> Hidden
        if (!neverHide && d2 > dMax2)
        {
            SetHidden(r, t.lod);
            return;
        }

        // Nếu đang nằm trong frustum (isVisible) -> Visible, LOD theo band
        if (inFrustum)
        {
            SetVisible(r, t.originalShadowMode, t.lod, band);
            return;
        }

        // Ngoài frustum nhưng còn trong khoảng farDistance:
        if (d2 <= dFar2)
        {
            // Nếu layer cho phép và object vốn có bóng -> ShadowsOnly
            if (AllowShadowsOnly(t.layer) && IsShadowMeaningful(t.originalShadowMode))
            {
                SetShadowsOnly(r, t.lod);
            }
            else
            {
                SetVisible(r, t.originalShadowMode, t.lod, 2); // ép LOD xa nhất

            }
            return;
        }

        // nếu whitelist -> ShadowsOnly, nếu không -> Hidden
        if (neverHide)
        {
            SetVisible(r, t.originalShadowMode, t.lod, 0); // ép LOD0 hoặc -1 tuỳ bạn
            return;
        }
        SetHidden(r, t.lod);
    }

    bool AllowShadowsOnly(int layer)
    {
        return ((1 << layer) & allowShadowsOnlyLayers.value) != 0;
    }

    // ======== ShadowOnly / Hidden ========
static void SetVisible(Renderer r, ShadowCastingMode original, LODGroup lod, int band)
{
    if (r == null) return;

    if (!r.enabled) r.enabled = true;

    // Nếu "Visible" mà original lại là ShadowsOnly, ta fallback thành On để chắc chắn có mesh.
    var visibleShadowMode = (original == ShadowCastingMode.ShadowsOnly)
                                ? ShadowCastingMode.On
                                : original;

    if (r.shadowCastingMode != visibleShadowMode)
        r.shadowCastingMode = visibleShadowMode;

    if (lod != null)
    {
        int lodIndex = band switch { 0 => 0, 1 => 1, 2 => 2, _ => -1 };
        // CLAMP LOD (xem mục #2)
        lodIndex = ClampLodIndex(lod, lodIndex);
        lod.ForceLOD(lodIndex);
    }
}
static int ClampLodIndex(LODGroup lod, int wanted)
{
    if (lod == null) return -1;
    var lods = lod.GetLODs();
    int max = lods != null ? lods.Length - 1 : -1;
    if (wanted < 0) return -1;              // auto
    if (max < 0)   return -1;              // không có LOD -> để auto
    return Mathf.Clamp(wanted, 0, max);
}
static void SetShadowsOnly(Renderer r, LODGroup lod)
{
    if (!r.enabled) r.enabled = true;
    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;

    if (lod)
    {
        int idx = ClampLodIndex(lod, 2); // “xa nhất”
        lod.ForceLOD(idx);
    }
}
static void SetHidden(Renderer r, LODGroup lod)
{
    if (r.enabled) r.enabled = false; // tắt hẳn -> không vẽ bóng

    if (lod)
    {
        int idx = ClampLodIndex(lod, 2); // clamp theo số LOD thực
        lod.ForceLOD(idx);
    }
}

    // ======== AA / Post / URP Dynamic Resolution ========
    [SerializeField] private Volume globalPostVolume;

    void ApplyAAAndPost()
    {
        // Lấy URP Asset hiện tại
        var urpAsset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
    if (urpAsset != null) {
        var p = typeof(UniversalRenderPipelineAsset).GetProperty("supportsDynamicResolution");
        bool dr = p != null && (bool)p.GetValue(urpAsset, null);
        Debug.Log($"[URP] Dynamic Resolution: {(dr ? "ENABLED" : "DISABLED")} (URP Asset)");
    }
        // MSAA: nên set trên URP Asset (URP bỏ qua QualitySettings.antiAliasing)
        int s = Mathf.Clamp(msaaSamples, 2, 8);
        s = (s <= 2) ? 2 : (s <= 4) ? 4 : 8;

        if (urpAsset != null)
        {
            try
            {
                // Nhiều phiên bản URP expose msaaSampleCount (int)
                urpAsset.msaaSampleCount = enableMSAA ? s : 1; // 1 = off
            }
            catch { /* có bản không cho set runtime -> bỏ qua */ }
        }
        else
        {
            // fallback (không hại gì nếu URP bỏ qua)
            QualitySettings.antiAliasing = enableMSAA ? s : 0;
        }

        // Camera AA & PostProcess
        if (targetCamera.TryGetComponent(out UniversalAdditionalCameraData urpCam))
        {
            bool useFxaaNow = enableFXAA && !enableMSAA;
            urpCam.antialiasing = useFxaaNow
                ? AntialiasingMode.FastApproximateAntialiasing
                : AntialiasingMode.None;

            // Luôn cho phép post; việc có hiệu ứng hay không do Volume quyết định
            urpCam.renderPostProcessing = true;
        }

        // Global Volume cho Motion Blur (tạo một lần)
        EnsureGlobalVolume();

        // Cấu hình Motion Blur trong Volume
        if (globalPostVolume && globalPostVolume.profile)
        {
            if (!globalPostVolume.profile.TryGet(out MotionBlur mb))
                mb = globalPostVolume.profile.Add<MotionBlur>(true);

            if (enableMotionBlur)
            {
                mb.active = true;
                mb.intensity.Override(motionIntensity);
                mb.clamp.Override(0.04f);
            }
            else
            {
                mb.active = false;
            }
        }

        // Dynamic Resolution: cảnh báo nếu chưa bật trong URP Asset
        if (urpAsset != null)
        {
            bool drEnabled = false;
            try
            {
                // API tên khác nhau giữa các phiên bản; thử bắt “supportsDynamicResolution”
                var prop = typeof(UniversalRenderPipelineAsset).GetProperty("supportsDynamicResolution");
                if (prop != null)
                {
                    drEnabled = (bool)prop.GetValue(urpAsset, null);
                }
            }
            catch { /* ignore */ }

            if (!drEnabled)
            {
                Debug.LogWarning("[URP] Dynamic Resolution đang TẮT trong URP Asset. " +
                                "Hãy bật 'Dynamic Resolution' và chọn upscaler (FSR/Auto). " +
                                "Nếu không, ResizeBuffers() sẽ không giúp tăng FPS.");
            }
        }
    }

    // Tạo/giữ 1 Global Volume (PostProcess) dùng chung – tránh add/remove mỗi lần
    void EnsureGlobalVolume()
    {
        if (globalPostVolume != null) return;

        var go = new GameObject("[Global Post Volume - CullingMgr]");
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(targetCamera ? targetCamera.transform : null, false);

        globalPostVolume = go.AddComponent<Volume>();
        globalPostVolume.isGlobal = true;
        globalPostVolume.priority = 10f;
        globalPostVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
    }

    // layer cull distances cơ bản (Unity sẽ cull sớm theo layer)
    void SetupOptionalLayerCullDistances()
    {
        float[] dists = new float[32];
        for (int l = 0; l < 32; l++) dists[l] = maxRenderDistance;
        targetCamera.layerCullDistances = dists;
        targetCamera.layerCullSpherical = true;
    }

    // ======== Adaptive render scale (không gọi ResizeBuffers trực tiếp) ========
    void UpdateAdaptiveScale()
    {
        // EMA để đo FPS
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fpsTimer += Time.deltaTime;

        if (fpsTimer >= fpsCheckInterval)
        {
            float fps = 1f / Mathf.Max(0.0001f, deltaTime);
            if (fps < targetFPS - 5)
                _adaptiveScale = Mathf.Max(renderScaleMin, _adaptiveScale - renderScaleStep);
            else if (fps > targetFPS + 10)
                _adaptiveScale = Mathf.Min(renderScaleMax, _adaptiveScale + renderScaleStep);

            fpsTimer = 0f;
        }
    }
    // ======== Focus boost: chọn renderScale theo band nhìn thấy (near=1.0, mid=0.7, far=0.2) ========
    void UpdateFocusScaleFast()
    {
        if (cullingGroup == null) { SmoothFocusTo(baseQualityScale); return; }

        int count = cullingGroup.QueryIndices(true, _visibleIdxBuffer, 0);
        if (count == _visibleIdxBuffer.Length)
        {
            // mở rộng và query lại
            _visibleIdxBuffer = new int[_visibleIdxBuffer.Length * 2];
            count = cullingGroup.QueryIndices(true, _visibleIdxBuffer, 0);
        }
        if (count <= 0) { SmoothFocusTo(baseQualityScale); return; }

        // Tìm band nhỏ nhất (ưu tiên near -> mid -> far) trong các đối tượng visible
        int minBand = int.MaxValue;
        Vector3 camPos = targetCamera.transform.position;

        for (int k = 0; k < count; k++)
        {
            int idx = _visibleIdxBuffer[k];
            if (idx < 0 || idx >= tracked.Count) continue;

            var r = tracked[idx].rend;
            if (!r || !r.enabled) continue;

            if (((1 << tracked[idx].layer) & exemptLayers.value) != 0) continue;

            // Phân loại band dựa trên khoảng cách theo bounds (gần nhất đến camera)
            float d2 = r.bounds.SqrDistance(camPos);
            int band = (d2 <= dNear2) ? 0 : (d2 <= dMid2) ? 1 : (d2 <= dFar2) ? 2 : 3;

            if (band < minBand) minBand = band;
            if (minBand == 0) break; // đã gặp near -> tối ưu, thoát luôn
        }

        float targetScale;
        switch (minBand)
        {
            case 0: targetScale = nearBandScale; break; // near
            case 1: targetScale = midBandScale;  break; // mid
            case 2: targetScale = farBandScale;  break; // far
            default: targetScale = baseQualityScale;    // không có gì trong near/mid/far
                break;
        }

        SmoothFocusTo(targetScale);
    }

    void SmoothFocusTo(float targetScale)
    {
        currentFocusScale = Mathf.SmoothDamp(currentFocusScale, targetScale, ref focusVelocity, focusSmoothTime);
    }

    // ======== Dọn renderer null + rebuild spheres ========
    void CompactTracked()
    {
        bool changed = false;
        for (int i = tracked.Count - 1; i >= 0; i--)
        {
            if (!tracked[i].rend)
            {
                tracked.RemoveAt(i);
                changed = true;
            }
        }
        if (changed && cullingGroup != null)
        {
            spheres = new BoundingSphere[tracked.Count];
            for (int i = 0; i < tracked.Count; i++)
            {
                var b = tracked[i].rend.bounds;
                spheres[i] = new BoundingSphere(b.center, b.extents.magnitude);
            }
            cullingGroup.SetBoundingSpheres(spheres);
            cullingGroup.SetBoundingSphereCount(tracked.Count);
        }
    }

    static bool IsShadowMeaningful(ShadowCastingMode original)
    {
        // Chỉ meaningful nếu ban đầu có bóng
        return original != ShadowCastingMode.Off;
    }
    static void LocalBoundsToWorldSphere(Renderer r, out Vector3 center, out float radius)
    {
        var lb = r.localBounds;                 // tồn tại ngay cả khi r.enabled == false
        var t = r.transform;
        center = t.TransformPoint(lb.center);
        // scale extents theo lossyScale
        var e = lb.extents;
        var s = t.lossyScale;
        var worldExtents = new Vector3(Mathf.Abs(e.x * s.x), Mathf.Abs(e.y * s.y), Mathf.Abs(e.z * s.z));
        radius = Mathf.Max(0.01f, worldExtents.magnitude); // tránh radius = 0
    }
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!targetCamera) return;
        Vector3 p = targetCamera.transform.position;
        Gizmos.color = Color.green; Gizmos.DrawWireSphere(p, nearDistance);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(p, midDistance);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(p, farDistance);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(p, maxRenderDistance);
    }
#endif
}
