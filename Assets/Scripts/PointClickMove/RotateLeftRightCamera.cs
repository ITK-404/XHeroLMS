using UnityEngine;

public class RotateLeftRightCamera : MonoBehaviour
{
    [SerializeField] private float percent = 0.1f;
    [SerializeField] private float lerpSpeed = 5;
    [SerializeField] private float left = -0.5f;
    [SerializeField] private float right = 0.5f;
    public float vertical = 0;
    private void LateUpdate()
    {
        var inputMousePos = Input.mousePosition;
        var screenWidth = Screen.width;
        var screenHeigh = Screen.height;

        var offset = screenWidth * percent;
        float xPos = inputMousePos.x;
        if (xPos > 0 && xPos < offset)
        {
            vertical = Mathf.Lerp(vertical, left, Time.deltaTime * lerpSpeed);
        }
        else if (xPos < screenWidth && xPos > screenWidth - offset)
        {
            vertical = Mathf.Lerp(vertical, right, Time.deltaTime * lerpSpeed);
        }
        else
        {
            vertical = 0;
        }
    }
}