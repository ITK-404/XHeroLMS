using UnityEngine;

public class LetterButtonUI : MonoBehaviour
{
    [SerializeField] private bool isHaveNotify = false;
    [SerializeField] private ShakeNotification _shakeNotification;

    private void OnEnable()
    {
        if (isHaveNotify)
        {
            _shakeNotification.StartShake();
        }
        else
        {
            _shakeNotification.StopShake();
        }
    }
}