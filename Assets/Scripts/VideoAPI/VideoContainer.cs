using UnityEngine;
using UnityEngine.UI;

public class VideoContainer : MonoBehaviour
{
    public RawImage videoContainer;
    public Transform container;

    public void Show()
    {
        Debug.Log($"Object {gameObject.name}",gameObject);
        container.gameObject.SetActive(true);
    }
    
    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}