using UnityEngine;

public class FogFloatingMovement : MonoBehaviour
{
    [Header("Floating Settings")]
    public Vector3 amplitude = new Vector3(0.3f, 0.5f, 0.3f);
    public float speed = 0.5f;

    private Vector3 startPos;
    private float seed;


    void Start()
    {
        startPos = transform.position;
        seed = Random.Range(0f, 100f); // lệch phase cho từng object
    }

    void Update()
    {
        HandleFloatingMovement();
    }

    private void HandleFloatingMovement()
    {
        float t = Time.time * speed + seed;

        Vector3 offset = new Vector3(
            Mathf.Sin(t) * amplitude.x,
            Mathf.Sin(t * 1.27f) * amplitude.y,
            Mathf.Cos(t * 0.93f) * amplitude.z
        );

        transform.position = startPos + offset;
    }
}