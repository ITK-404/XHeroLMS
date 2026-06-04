using System;
using UnityEngine;

public class OverlayHoleHandler : MonoBehaviour
{
    private static readonly int HoleRect = Shader.PropertyToID("_HoleRect");
    
    [SerializeField] private Material overlayMaterial;

    [SerializeField] private bool isOverlayCanvas = true;
    [SerializeField] private Canvas canvas;

    [SerializeField] private GameObject container;

    private void Awake()
    {
        ResetHole();
    }

    private void ResetHole()
    {
        overlayMaterial.SetVector("_HoleRect", new Vector4(0f,0f,0f,0f));
    }


    public void Show(RectTransform target)
    {
        UpdateHole(target);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.gameObject.SetActive(false);
    }

    void UpdateHole(RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 bl, tr;

        if (isOverlayCanvas)
        {
            // Screen Space - Overlay: world corners đã là screen coords
            bl = corners[0];
            tr = corners[2];
        }
        else
        {
            // Screen Space - Camera: cần convert qua camera của canvas
            var cam = canvas.worldCamera;
            bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        }

        overlayMaterial.SetVector("_HoleRect", new Vector4(bl.x, bl.y, tr.x, tr.y));
    }
}
