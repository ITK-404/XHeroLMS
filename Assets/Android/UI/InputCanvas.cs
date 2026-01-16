using System;
using UnityEngine;

public class InputCanvas : MonoBehaviour
{
    public GameObject container;
    private bool isWindow = false;
    private void Awake()
    {
        isWindow = false;
        
        #if UNITY_ANDROID || UNITY_IOS
        isWindow = true;
        #endif
#if UNITY_EDITOR
        isWindow = true; // giả lập mobile trong Editor
#endif
        if (isWindow)
        {
            Hide();
        }
    }

    public void Show()
    {
        if (isWindow) return;
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}
