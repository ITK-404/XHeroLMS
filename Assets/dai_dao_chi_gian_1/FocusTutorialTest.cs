using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FocusTutorialTest : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private ScriptableRendererData data;
    private TutorialRenderFeature feature;
    
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        feature = GetFeature();
        Disable();
    }

    private TutorialRenderFeature GetFeature()
    {
        foreach (var f in data.rendererFeatures)
        {
            if (f is TutorialRenderFeature focusFeature)
                return focusFeature;
        }
        return null;
    }
    private void LateUpdate()
    {
        if (target == null) return;
        if (data == null) return;
        var viewPortPoint = mainCamera.ScreenToViewportPoint(target.position);
        var uvPos = new Vector2(viewPortPoint.x, viewPortPoint.y);
        feature.SetCenter(uvPos);
    }

    public void Enable()
    {
        feature?.SetActive(true);
    }

    public void Disable()
    {
        feature?.SetActive(false);
    }
}
