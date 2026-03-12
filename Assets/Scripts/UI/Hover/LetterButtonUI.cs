using System;
using UnityEngine;

public class LetterButtonUI : MonoBehaviour
{
    [SerializeField] private bool isHaveNotify = false;
    [SerializeField] private ShakeNotification _shakeNotification;
    [SerializeField] private GameObject dotNotify;

    private bool previousNotify = false;

    private void Awake()
    {
        UpdateNotify();
    }

    private void Update()
    {
        if (previousNotify != isHaveNotify)
        {
            UpdateNotify();
        }
    }

    private void UpdateNotify()
    {
        if (isHaveNotify)
        {
            _shakeNotification.StartShake();
            dotNotify.gameObject.SetActive(true);
        }
        else
        {
            _shakeNotification.StopShake();
            dotNotify.gameObject.SetActive(false);
        }

        previousNotify = isHaveNotify;
    }
}