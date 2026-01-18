using System;
using UnityEngine;

public class PlayerMarker : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private RectTransform marker;
    [SerializeField] private float normalizeValue = 180;

    private void Awake()
    {
        marker = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        SyncRotation();
    }

    private void SyncRotation()
    {
        if (player == null)
        {
            Debug.LogError("This player is null",gameObject);
            return;
        }
        float yRotation = player.transform.eulerAngles.y;
        yRotation -= normalizeValue;
        marker.transform.eulerAngles = new Vector3(0, 0, -yRotation);
        // Debug.Log("Y Rotation: "+yRotation);
    }
}
