using UnityEngine;

[ExecuteAlways]
public class SimpleImpostorBillboard : MonoBehaviour
{
    public Transform visualTransform;
    public Renderer targetRenderer;
    public Camera targetCamera;
    public bool billboardToCamera = true;
    public bool lockYAxis = false;
    public Vector2 worldSize = Vector2.one;
    public float sizeMultiplier = 1f;

    private void Reset()
    {
        targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
            visualTransform = targetRenderer.transform;
    }

    private void OnEnable()
    {
        Apply();
    }

    private void OnValidate()
    {
        Apply();
    }

    private void LateUpdate()
    {
        Apply();
    }

    private void Apply()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();

        if (visualTransform == null && targetRenderer != null)
            visualTransform = targetRenderer.transform;

        if (visualTransform == null)
            return;

        visualTransform.localScale = new Vector3(
            Mathf.Max(worldSize.x, 0.0001f) * sizeMultiplier,
            Mathf.Max(worldSize.y, 0.0001f) * sizeMultiplier,
            1f);

        if (!billboardToCamera)
            return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;

#if UNITY_EDITOR
        if (cam == null && !Application.isPlaying && UnityEditor.SceneView.lastActiveSceneView != null)
            cam = UnityEditor.SceneView.lastActiveSceneView.camera;
#endif

        if (cam == null)
            return;

        Vector3 dir = cam.transform.position - visualTransform.position;
        if (lockYAxis)
            dir.y = 0f;

        if (dir.sqrMagnitude <= 0.000001f)
            return;

        dir.Normalize();
        Vector3 up = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.96f ? transform.forward : Vector3.up;
        visualTransform.rotation = Quaternion.LookRotation(dir, up);
    }
}
