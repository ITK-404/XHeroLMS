using System;
using UnityEngine;
using UnityEngine.UI;

public class PTS_WorldSpaceUI : WorldSpaceUI
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private Button btn;

    public static Action OnClickButtonEvent;
    
    protected override void Awake()
    {
        base.Awake();
        btn.onClick.AddListener(OnClickButton);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnClickButton);
    }

    private void OnClickButton()
    {
        OnClickButtonEvent?.Invoke();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        // Guard: need a camera to face
        if (wrapperCamera == null)
            return;

        // Only rotate when this UI is in world-space. For screen-space UI the RectTransform
        // is positioned using screen coordinates and rotation should be controlled by the canvas.
        if (isWorldSpaceUI)
        {
            // Create a billboard that faces the camera but stays upright to prevent distortion/tilt.
            Vector3 dir = wrapperCamera.transform.position - transform.position;
            dir.y = 0f; // keep the UI upright (no pitch)
            if (dir.sqrMagnitude > 0.0001f)
            {
                // Look towards the camera, then flip 180deg so the front faces the camera if needed.
                transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 180f, 0f);
            }
        }

        HandleFollowTarget();
    }

    protected override Vector3 GetTargetPosition()
    {
        return target.transform.position + offset;
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void SetOffset(Vector3 offset)
    {
        this.offset = offset;
    }

}