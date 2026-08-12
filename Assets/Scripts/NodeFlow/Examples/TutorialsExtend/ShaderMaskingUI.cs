using System;
using UnityEngine;
using UnityEngine.UI;

public class ShaderMaskingUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    
    [SerializeField] private RectTransform maskRect;
    [SerializeField] private RectTransform focusRectForTest;
    [SerializeField] private Camera canvasCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Material material;
    private static readonly int FocusRectID = Shader.PropertyToID("_FocusRect");
    
    private void Awake()
    {
        if (canvas == null) return;
        
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            canvasCamera = canvas.worldCamera;
        }

    }

    private Vector3[] corners = new Vector3[4];

    public void SetTarget(RectTransform targetRect)
    {
        focusRectForTest = targetRect;  
        maskRect.gameObject.SetActive(true);
    }

    public void ClearTargetAndTurnOff()
    {
        focusRectForTest = null;
        maskRect.gameObject.SetActive(false);
    }

    
    private void Update()
    {
        
    }

    private void LateUpdate()
    {
        if (focusRectForTest == null) return;
        focusRectForTest.GetWorldCorners(corners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            corners[0]
        );

        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(
            canvasCamera,
            corners[2]
        );

        Vector2 minUV = new Vector2(bottomLeft.x / Screen.width, bottomLeft.y / Screen.height);
        Vector2 maxUV = new Vector2(topRight.x / Screen.width, topRight.y / Screen.height);
        
        material.SetVector(FocusRectID, new Vector4(minUV.x, minUV.y, maxUV.x, maxUV.y));
      
        Debug.Log($"Min UV {minUV}");
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}