using UnityEngine;

public class FogAlphaController : MonoBehaviour
{
    [Header("Shared Material")]
    public Material sharedMaterial;

    [Header("Alpha Settings")]
    [Range(0f, 1f)]
    public float targetAlpha = 0.6f;

    public float smoothTime = 3f; // càng lớn càng mượt

    private float currentAlpha;
    private float alphaVelocity;

    private static readonly int ColorID = Shader.PropertyToID("_Color");

    void Start()
    {
        if (sharedMaterial == null) return;

        currentAlpha = sharedMaterial.color.a;
    }

    void Update()
    {
        if (sharedMaterial == null) return;

        currentAlpha = Mathf.SmoothDamp(
            currentAlpha,
            targetAlpha,
            ref alphaVelocity,
            smoothTime
        );

        Color c = sharedMaterial.color;
        c.a = currentAlpha;
        sharedMaterial.color = c;
    }
}
