using UnityEngine;

public class RotateLeftRightCamera : MonoBehaviour
{
    [SerializeField] private float percent = 0.1f;
    [SerializeField] private float lerpSpeed = 5;
    [SerializeField] private float left = -0.5f;
    [SerializeField] private float right = 0.5f;
    public float vertical = 0;
    
}