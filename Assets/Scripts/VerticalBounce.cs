using UnityEngine;

public class VerticalBounce : MonoBehaviour
{
    public Vector3 startPos;

    void Awake()
    {
        startPos = transform.position;
    }
}