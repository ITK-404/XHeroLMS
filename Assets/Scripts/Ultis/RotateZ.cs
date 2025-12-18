using UnityEngine;

public class RotateZ : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    
    void Update()
    {
        // Xoay theo trục Z
        transform.Rotate(0, 0, speed * Time.deltaTime);
    }
}
