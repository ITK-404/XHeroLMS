using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class VideoContainer : MonoBehaviour
{
    public RawImage videoContainer;
    public Transform container;

    public void Show()
    {
        Debug.Log($"Show {gameObject.name}",gameObject);
        container.gameObject.SetActive(true);
    }
    
    public void Hide()
    {
        Debug.Log($"Hide {gameObject.name}",gameObject);
        container.gameObject.SetActive(false);
    }
}