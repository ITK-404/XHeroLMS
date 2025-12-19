using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class TouchRotationView : MonoBehaviour
{
    [FormerlySerializedAs("rectTransform")] [SerializeField] private RectTransform rotationView;
    [SerializeField] private bool isLooking;
    [SerializeField] private int touchID;
    
    [SerializeField] private List<RectTransform> includeList = new();

    public static bool IsLooking;
    
    public static Vector2 deltaGlobal;
    private void Awake()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        deltaGlobal = Vector2.zero;
        IsLooking = false;
    }
    
    private void OnApplicationFocus(bool hasFocus)
    {
        deltaGlobal = Vector2.zero;
        IsLooking = false;
    }

    private void Update()
    {
        var activeTouches = EnhancedTouch.activeTouches;
        for (var i = 0; i < activeTouches.Count; ++i)
            //Debug.Log("Active touch: " + activeTouches[i]);

            if (activeTouches.Count > 0)
            {
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
                            Debug.Log("Cancel");
                            break;
                        case UnityEngine.InputSystem.TouchPhase.Stationary:
                            Static(touch);
                            break;
                        default:
                            break;
                    }
                }
            }

        IsLooking = isLooking;
    }


    private void Check(EnhancedTouch touch)
    {
        if (isLooking == true) return;
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
            deltaGlobal = touch.delta;
        }
    }
    private void End(EnhancedTouch touch)
    {
        if (isLooking && touch.touchId == touchID)
        {
            deltaGlobal = Vector2.zero;
            isLooking = false;
            touchID = -1;
        }
    }
    private void Static(EnhancedTouch touch)
    {
        if (isLooking && touch.touchId == touchID)
        {
            deltaGlobal = Vector2.zero;
        }
    }
}