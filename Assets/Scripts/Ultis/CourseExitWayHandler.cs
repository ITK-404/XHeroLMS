using System;
using UnityEngine;

public class CourseExitWayHandler : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject target;
    [SerializeField] private RectTransform leftSign;
    [SerializeField] private RectTransform rightSign;
    [SerializeField] private RectTransform findTargetSign;
    [SerializeField] private GameObject container;
    [Header("Other")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject player;
    
    private bool isShowing = true;

    // lay vi tri item o tren screen space
    private void LateUpdate()
    {
        if (!isShowing)
            return;
        var targetPos = target.transform.position;
        var cameraPos = player.transform.position;
        
        var screenPosition = playerCamera.WorldToScreenPoint(targetPos);

        findTargetSign.position = screenPosition;
        if (IsPositionInsideScreen(screenPosition) && screenPosition.z > 0)
        {
            // Debug.Log("Target is inside screen");
            HideAll();
        }
        else
        {
            // Debug.Log("Target is outside screen");
            
            Vector3 directionToTarget = targetPos - cameraPos;
            directionToTarget.Normalize();
            
            // Nếu dot > 0 là bên phải, < 0 là bên trái
            float dot = Vector3.Dot(playerCamera.transform.right, directionToTarget);

            bool isLeft = dot < 0;
            
            ShowIndicator(isLeft);
        }
    }

    private void HideAll()
    {
        leftSign.gameObject.SetActive(false);
        rightSign.gameObject.SetActive(false);
    }

    
    void ShowIndicator(bool isLeft) {
        leftSign.gameObject.SetActive(isLeft);
        rightSign.gameObject.SetActive(!isLeft);
    }

    private bool IsPositionInsideScreen(Vector2 screenPosition)
    {
        return screenPosition.x >= 0 && screenPosition.x <= Screen.width && screenPosition.y >= 0 &&
               screenPosition.y <= Screen.height;
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
        isShowing = true;
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
        isShowing = false;
    }
}