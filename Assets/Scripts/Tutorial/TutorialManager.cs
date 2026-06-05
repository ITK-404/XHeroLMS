using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[Serializable]
public class TutorialStep
{
    public string step_ID;
    public bool isShowingUI = false;
    public Transform popupUI;

    public bool CanShowUI()
    {
        // kiểm tra nếu isShowingUI là true và popupUI khác null thì trả về true
        if (isShowingUI)
        {
            if (popupUI == null)
            {
                Debug.Log("Popup đang rỗng");
            }
            return popupUI != null;
        }
        return false;
    }

}


public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public GameObject tutorialPrefab;
    private GameObject currentTutorial;
    // Assign the Canvas that contains the UI (set in inspector)
    public Canvas uiCanvas;

    private void Awake()
    {
        Instance = this;
    }
    public void ShowTutorial(TutorialBase button)
    {
        if (button.isUI)
        {
            Debug.Log($"Tutorial Manager is true");
            UpdateCurrentTutorialAtButton(button.GetComponent<RectTransform>());
        }
        else
        {
            Debug.Log($"Tutorial Manager is false");
            ShowTutorialAtWorldPosition(button.transform.position);
        }
    }


    private void UpdateCurrentTutorialAtButton(RectTransform buttonRect)
    {
        if (uiCanvas == null || tutorialPrefab == null)
            return;

        var canvasRect = uiCanvas.GetComponent<RectTransform>();
        if (canvasRect == null || buttonRect == null)
            return;

        if (currentTutorial != null)
            Destroy(currentTutorial);

        currentTutorial = Instantiate(tutorialPrefab, uiCanvas.transform, false);

        var currentRect = currentTutorial.GetComponent<RectTransform>();
        if (currentRect == null)
            return;

        Camera cam = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, buttonRect.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out localPoint);

        currentRect.anchoredPosition = localPoint;
    }

    public bool ShowTutorialAtWorldPosition(Vector3 worldPosition, Vector2 canvasLocalOffset = default)
    {
        if (uiCanvas == null)
        {
            Debug.LogWarning("TutorialManager.ShowTutorialAtWorldPosition: uiCanvas is null.");
            return false;
        }

        if (tutorialPrefab == null)
        {
            Debug.LogWarning("TutorialManager.ShowTutorialAtWorldPosition: tutorialPrefab is null.");
            return false;
        }

        var canvasRect = uiCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogWarning("TutorialManager.ShowTutorialAtWorldPosition: Canvas RectTransform missing.");
            return false;
        }

        // Choose camera to project world point to screen
        Camera worldToScreenCam = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? Camera.main : (uiCanvas.worldCamera != null ? uiCanvas.worldCamera : Camera.main);
        if (worldToScreenCam == null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            Debug.LogWarning("ShowTutorialAtWorldPosition: No camera available for ScreenSpaceCamera/WorldSpace canvas.");
            return false;
        }

        Vector3 screenPoint3 = worldToScreenCam != null ? worldToScreenCam.WorldToScreenPoint(worldPosition) : RectTransformUtility.WorldToScreenPoint(null, worldPosition);

        // If point is behind the camera, don't show (avoid inverted positions)
        if (screenPoint3.z <= 0f)
        {
            // Hide previous tutorial if any
            if (currentTutorial != null)
                Destroy(currentTutorial);
            Debug.Log("ShowTutorialAtWorldPosition: world position is behind the camera.");
            return false;
        }

        // Destroy previous instance
        if (currentTutorial != null)
            Destroy(currentTutorial);

        currentTutorial = Instantiate(tutorialPrefab, uiCanvas.transform, false);
        var currentRect = currentTutorial.GetComponent<RectTransform>();
        if (currentRect == null)
        {
            Debug.LogWarning("TutorialManager.ShowTutorialAtWorldPosition: instantiated prefab has no RectTransform.");
            return false;
        }

        // Ensure layout/size is up-to-date (important when prefab uses layout components)
        LayoutRebuilder.ForceRebuildLayoutImmediate(currentRect);

        // Convert screen point to canvas local point.
        Vector2 screenPoint = new Vector2(screenPoint3.x, screenPoint3.y);
        Camera camForRect = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : worldToScreenCam;
        Vector2 localPoint;
        bool valid = RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, camForRect, out localPoint);
        if (!valid)
        {
            // fallback: center popup
            localPoint = Vector2.zero;
        }

        // Apply optional offset
        localPoint += canvasLocalOffset;

        // Clamp so popup stays inside canvas rect (consider popup size)
        Rect canvasBounds = canvasRect.rect;
        Rect popupRect = currentRect.rect;

        float halfWidth = popupRect.width * 0.5f;
        float halfHeight = popupRect.height * 0.5f;

        float minX = canvasBounds.xMin + halfWidth;
        float maxX = canvasBounds.xMax - halfWidth;
        float minY = canvasBounds.yMin + halfHeight;
        float maxY = canvasBounds.yMax - halfHeight;

        // If popup is larger than canvas, just center it
        if (minX > maxX) localPoint.x = 0f;
        else localPoint.x = Mathf.Clamp(localPoint.x, minX, maxX);

        if (minY > maxY) localPoint.y = 0f;
        else localPoint.y = Mathf.Clamp(localPoint.y, minY, maxY);

        currentRect.anchoredPosition = localPoint;
        return true;
    }

    public void Clear()
    {
        if (currentTutorial != null)
            Destroy(currentTutorial);
    }
}