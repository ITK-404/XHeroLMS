using UnityEngine;

public class UIFollowFirstChairCheckPoint : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;

    private RectTransform rectTransform;
    private ChairCheckPoint target;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        if (worldCamera == null) worldCamera = Camera.main;
    }

    public void SetTarget(ChairCheckPoint newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null || worldCamera == null) return;

        Vector3 screenPos = worldCamera.WorldToScreenPoint(target.transform.position + worldOffset);
        rectTransform.position = screenPos;
    }
}