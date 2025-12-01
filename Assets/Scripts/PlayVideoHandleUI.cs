using UnityEngine;
using UnityEngine.UI;

public class PlayVideoHandleUI : MonoBehaviour
{
    [SerializeField] PlayVideoOpenBook playVideoOpenBook;
    [SerializeField] Button skipButton;
    [SerializeField] RawImage rawImage;
    [SerializeField] private GameObject container;
    public Toggle autoSkipToggle;
    private void Awake()
    {   
        container.gameObject.SetActive(false);
        skipButton.onClick.AddListener(HideVideo);   
    }

    private void OnDestroy()
    {
        skipButton.onClick.RemoveListener(HideVideo);   
    }

    private void HideVideo()
    {
        playVideoOpenBook.Stop();
        Hide();
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    } 

    public void Hide()
    {
        container.gameObject.SetActive(false);
    } 
}