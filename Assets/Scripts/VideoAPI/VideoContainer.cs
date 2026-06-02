using UnityEngine;
using UnityEngine.UI;

public class VideoContainer : MonoBehaviour
{
    [Header("Video")]
    public RawImage videoContainer;
    public Transform container;

    [Header("Document")]
    public FrameDocumentUI frameDocumentUI;

    public void Show()
    {
        ShowVideo();
    }

    public void ShowVideo()
    {
        Debug.Log($"Show Video {gameObject.name}", gameObject);

        if (container != null)
            container.gameObject.SetActive(true);

        if (videoContainer != null)
            videoContainer.gameObject.SetActive(true);

        if (frameDocumentUI != null)
            frameDocumentUI.Hide();
    }

    public void ShowDocument(string documentUrl)
    {
        Debug.Log($"Show Document {gameObject.name} | url={documentUrl}", gameObject);

        if (container != null)
            container.gameObject.SetActive(true);

        if (videoContainer != null)
            videoContainer.gameObject.SetActive(false);

        if (frameDocumentUI != null)
            frameDocumentUI.ShowDocument(documentUrl);
        else
            Debug.LogWarning($"[VideoContainer] Missing FrameDocumentUI on {gameObject.name}");
    }

    public void Hide()
    {
        Debug.Log($"Hide {gameObject.name}", gameObject);

        if (container != null)
            container.gameObject.SetActive(false);

        if (videoContainer != null)
            videoContainer.gameObject.SetActive(false);

        if (frameDocumentUI != null)
            frameDocumentUI.Hide();
    }
}