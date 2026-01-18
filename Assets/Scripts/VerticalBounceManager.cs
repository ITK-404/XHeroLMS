using System.Collections.Generic;
using UnityEngine;

public class VerticalBounceManager : MonoBehaviour
{
    [Header("Global Settings")]
    [SerializeField] private float amplitude = 0.2f;
    [SerializeField] private float speed = 3f;

    [Header("Items")]
    [SerializeField] private List<VerticalBounce> items = new();

    public bool canMove;
    void Awake()
    {
        // Auto collect nếu quên kéo tay
        if (items.Count == 0)
            items.AddRange(GetComponentsInChildren<VerticalBounce>());
    }

    void Update()
    {
        if (!TutorialHandler.Instance.IsPlayedBefore())
        {
            return;
        }
        float offsetY = Mathf.Sin(Time.time * speed) * amplitude;

        for (int i = 0; i < items.Count; i++)
        {
            var t = items[i];
            if (!t) continue;

            t.transform.position = t.startPos+ Vector3.up * offsetY;
        }    }
}