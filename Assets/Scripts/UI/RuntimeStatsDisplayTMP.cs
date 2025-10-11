using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class RuntimeStatsDisplayTMP : MonoBehaviour
{
    [Header("TMP Texts (assign all 4)")]
    public TextMeshProUGUI vertsText;
    public TextMeshProUGUI trisText;
    public TextMeshProUGUI batchesText;
    public TextMeshProUGUI fpsText;

    [Header("Update Rates")]
    public float geoScanInterval = 0.5f;
    [Range(0f, 0.99f)] public float fpsSmoothing = 0.9f;

    [Header("Counting Options")]
    public Camera statCamera;
    public bool preferEditorStats = true;
    public bool skipDisabledRenderers = true;

    [Header("Label Formatting")]
    [Tooltip("Khoảng trắng giữa nhãn và giá trị")]
    public string labelSep = ": ";
    [Tooltip("Độ rộng cột nhãn để canh thẳng hàng (0 = không canh)")]
    public int labelColumnWidth = 8;
    [Tooltip("Viết hoa nhãn")]
    public bool upperCaseLabels = true;

    [Tooltip("Dùng frustum của camera (nếu có) để đếm chính xác những gì đang thấy")]
    public bool useCameraFrustum = true;

    float geoTimer;
    float smoothedFps;

    void OnEnable()
    {
        geoTimer = Mathf.Max(0.1f, geoScanInterval);
        smoothedFps = 0f;
        RecountGeometry();
        UpdateUIBatches();
    }

    void Update()
    {
        // FPS (smoothed)
        float dt = Time.unscaledDeltaTime;
        float instantFps = (dt > 1e-6f) ? 1f / dt : 0f;
        smoothedFps = Mathf.Lerp(smoothedFps, instantFps, 1f - fpsSmoothing);
        if (fpsText) SetLabeled(fpsText, "fps", smoothedFps.ToString("F1")); // dễ thấy thay đổi hơn

        // Geometry (đếm theo chu kỳ)
        geoTimer -= Time.unscaledDeltaTime;
        if (geoTimer <= 0f)
        {
            geoTimer = Mathf.Max(0.1f, geoScanInterval);
            RecountGeometry();
        }

        // Batches
        UpdateUIBatches();
    }

    // Chọn 1 camera an toàn để dùng
    static Camera PickAnyCamera(Camera prefer = null)
    {
        if (prefer && prefer.isActiveAndEnabled) return prefer;

        var m = Camera.main;
        if (m && m.isActiveAndEnabled) return m;

        // Duyệt tất cả camera đang bật
        int count = Camera.allCamerasCount;
        if (count > 0)
        {
            var buffer = new Camera[count];
            Camera.GetAllCameras(buffer);
            for (int i = 0; i < buffer.Length; i++)
            {
                var c = buffer[i];
                if (c && c.isActiveAndEnabled) return c;
            }
        }
        return null;
    }

    void RecountGeometry()
    {
#if UNITY_EDITOR
        if (preferEditorStats)
        {
            if (vertsText) SetLabeled(vertsText, "verts", FormatK(UnityStats.vertices));
            if (trisText)  SetLabeled(trisText,  "tris",  FormatK(UnityStats.triangles));
            return;
        }
#endif
        // --- Runtime path ---
        Camera cam = PickAnyCamera(statCamera);

        // Nếu không có camera hoặc tắt frustum -> đếm toàn scene
        if (!useCameraFrustum || !cam)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!cam && useCameraFrustum)
                Debug.LogWarning("[RuntimeStatsDisplayTMP] No camera found. Falling back to CountSceneNoCamera.");
#endif
            CountSceneNoCamera();
            return;
        }

        var planes = GeometryUtility.CalculateFrustumPlanes(cam);
        long verts = 0, tris = 0;

        // MeshFilter
#if UNITY_2023_1_OR_NEWER
        var allMF = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var allMF = Object.FindObjectsOfType<MeshFilter>();
#endif
        for (int i = 0; i < allMF.Length; i++)
        {
            var mf = allMF[i];
            if (!mf) continue;

            var r = mf.GetComponent<Renderer>();
            if (!r) continue;
            if (skipDisabledRenderers && !r.enabled) continue;

            // ngoài frustum thì bỏ
            if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;

            var mesh = mf.sharedMesh; if (!mesh) continue;
            verts += mesh.vertexCount;
            int sub = mesh.subMeshCount;
            for (int s = 0; s < sub; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                tris += mesh.GetIndexCount(s) / 3;
            }
        }

        // SkinnedMeshRenderer
#if UNITY_2023_1_OR_NEWER
        var allSMR = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var allSMR = Object.FindObjectsOfType<SkinnedMeshRenderer>();
#endif
        for (int i = 0; i < allSMR.Length; i++)
        {
            var smr = allSMR[i];
            if (!smr) continue;
            if (skipDisabledRenderers && !smr.enabled) continue;

            if (!GeometryUtility.TestPlanesAABB(planes, smr.bounds)) continue;

            var mesh = smr.sharedMesh; if (!mesh) continue;
            verts += mesh.vertexCount;
            int sub = mesh.subMeshCount;
            for (int s = 0; s < sub; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                tris += mesh.GetIndexCount(s) / 3;
            }
        }

        if (verts == 0 && tris == 0)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("[RuntimeStatsDisplayTMP] Frustum count returned 0. Falling back to CountSceneNoCamera.");
#endif
            CountSceneNoCamera();
            return;
        }

        if (vertsText) SetLabeled(vertsText, "verts", FormatK(verts));
        if (trisText)  SetLabeled(trisText,  "tris",  FormatK(tris));
    }

    void CountSceneNoCamera()
    {
        long verts = 0, tris = 0;

#if UNITY_2023_1_OR_NEWER
        var allMF = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var allMF = Object.FindObjectsOfType<MeshFilter>();
#endif
        foreach (var mf in allMF)
        {
            if (!mf) continue;

            var r = mf.GetComponent<Renderer>();
            if (!r) continue;
            if (skipDisabledRenderers && !r.enabled) continue;

            var mesh = mf.sharedMesh; if (!mesh) continue;

            verts += mesh.vertexCount;
            int sub = mesh.subMeshCount;
            for (int s = 0; s < sub; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                tris += mesh.GetIndexCount(s) / 3;
            }
        }

#if UNITY_2023_1_OR_NEWER
        var allSMR = Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        var allSMR = Object.FindObjectsOfType<SkinnedMeshRenderer>();
#endif
        foreach (var smr in allSMR)
        {
            if (!smr) continue;
            if (skipDisabledRenderers && !smr.enabled) continue;

            var mesh = smr.sharedMesh; if (!mesh) continue;

            verts += mesh.vertexCount;
            int sub = mesh.subMeshCount;
            for (int s = 0; s < sub; s++)
            {
                if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                tris += mesh.GetIndexCount(s) / 3;
            }
        }

        if (vertsText) SetLabeled(vertsText, "verts", FormatK(verts));
        if (trisText)  SetLabeled(trisText,  "tris",  FormatK(tris));
    }

    void UpdateUIBatches()
    {
        if (!batchesText) return;
#if UNITY_EDITOR
        SetLabeled(batchesText, "batches", UnityStats.batches.ToString("n0"));
#else
        SetLabeled(batchesText, "batches", "N/A"); // không có UnityStats trong build
#endif
    }

    void SetLabeled(TextMeshProUGUI tmp, string label, string value)
    {
        if (!tmp) return;
        if (upperCaseLabels) label = label.ToUpperInvariant();
        if (labelColumnWidth > 0)
            tmp.text = $"{label.PadRight(labelColumnWidth)}{labelSep}{value}";
        else
            tmp.text = $"{label}{labelSep}{value}";
    }

    static string FormatK(long v)
    {
        if (v >= 1_000_000) return (v / 1_000_000f).ToString("0.00") + "M";
        if (v >= 1_000)     return (v / 1_000f).ToString("0.0") + "K";
        return v.ToString("n0");
    }
}
