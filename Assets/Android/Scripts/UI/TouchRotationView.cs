using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class TouchRotationView : MonoBehaviour
{
    [FormerlySerializedAs("rectTransform")] [SerializeField] private RectTransform rotationView;
    [SerializeField] private bool isLooking;
    [SerializeField] private int touchID;
    [SerializeField] private bool blockRotationWhenPointerOverUI = true;
    
    [SerializeField] private List<RectTransform> includeList = new();

    public static bool IsLooking;
    
    public static Vector2 deltaGlobal;
    private readonly List<RaycastResult> uiRaycastResults = new(16);
    private PointerEventData uiPointerEventData;
    private EventSystem uiPointerEventSystem;

    private void Awake()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        ResetState();
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        ResetState();
    }
    
    private void OnApplicationPause(bool pauseStatus)
    {
        // Xử lý khi app bị pause (về màn hình chính)
        if (pauseStatus)
        {
            ResetState();
        }
    }

    private void ResetState()
    {
        deltaGlobal = Vector2.zero;
        isLooking = false;
        IsLooking = false;
        touchID = -1;
    }

    private void Update()
    {
        if (GameplayLock.IsLocked(GameplayLockTarget.Camera))
        {
            ResetState();
            return;
        }

        if (TutorialHandler.Instance != null)
        {
            if (!TutorialHandler.Instance.IsPlayedBefore())
            {
                return;
            }
        }
        var activeTouches = EnhancedTouch.activeTouches;

        if (activeTouches.Count == 0)
        {
            ResetState();
            return;
        }

        if (activeTouches.Count >= 3)
        {
            ResetState();
            return;
        }

        foreach (var touch in activeTouches)
        {
            switch (touch.phase)
            {
                case UnityEngine.InputSystem.TouchPhase.None:
                    break;
                case UnityEngine.InputSystem.TouchPhase.Began:
                    Check(touch);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                    Move(touch);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                    End(touch);
                    break;
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    ResetState();
                    break;
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    Static(touch);
                    break;
                default:
                    break;
            }
        }

        IsLooking = isLooking;
    }


    private void Check(EnhancedTouch touch)
    {
        if (isLooking == true) return;

        if (IsBlockedByOverlayUI(touch.screenPosition))
        {
            ResetState();
            return;
        }

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rotationView,
            touch.screenPosition,
            null,
            out localPoint
        );

        if (IsTouchToNotInteractZone(touch))
        {
            return;
        }

        if (rotationView.rect.Contains(localPoint))
        {
            Debug.Log("Touched inside image!");
            isLooking = true;
            touchID = touch.touchId;

        }
    }

    private bool IsTouchToNotInteractZone(EnhancedTouch touch)
    {
        foreach (var item in includeList)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                item,
                touch.screenPosition,
                null,
                out localPoint
            );


            if (item.rect.Contains(localPoint))
            {
                return true;
            }
        }

        return false;
    }

    private void Move(EnhancedTouch touch)
    {
        if (isLooking && touch.touchId == touchID)
        {
            if (IsBlockedByOverlayUI(touch.screenPosition))
            {
                ResetState();
                return;
            }

            deltaGlobal = touch.delta;
        }
    }
    private void End(EnhancedTouch touch)
    {
        if (isLooking && touch.touchId == touchID)
        {
            ResetState();
        }
    }
    private void Static(EnhancedTouch touch)
    {
        if (isLooking && touch.touchId == touchID)
        {
            if (IsBlockedByOverlayUI(touch.screenPosition))
            {
                ResetState();
                return;
            }

            deltaGlobal = Vector2.zero;
        }
    }

    private bool IsBlockedByOverlayUI(Vector2 screenPosition)
    {
        if (!blockRotationWhenPointerOverUI)
            return false;

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        if (uiPointerEventData == null || uiPointerEventSystem != eventSystem)
        {
            uiPointerEventData = new PointerEventData(eventSystem);
            uiPointerEventSystem = eventSystem;
        }

        uiPointerEventData.Reset();
        uiPointerEventData.position = screenPosition;

        uiRaycastResults.Clear();
        eventSystem.RaycastAll(uiPointerEventData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            RaycastResult result = uiRaycastResults[i];
            if (result.gameObject == null || !result.gameObject.activeInHierarchy)
                continue;

            if (result.module as GraphicRaycaster == null)
                continue;

            if (IsAllowedInputControl(result.gameObject.transform))
                continue;

            return true;
        }

        return false;
    }

    private bool IsAllowedInputControl(Transform target)
    {
        if (target == null)
            return false;

        if (rotationView != null && (target == rotationView || target.IsChildOf(rotationView)))
            return true;

        foreach (RectTransform item in includeList)
        {
            if (item == null)
                continue;

            if (target == item || target.IsChildOf(item))
                return true;
        }

        return false;
    }
}
