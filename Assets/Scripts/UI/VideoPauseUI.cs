using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.CullingGroup;

public class VideoPauseUI : MonoBehaviour
{
    public VideoPlayerControllerPro videoPlayerControllerPro;
    public Image background;
    public Image pauseIcon;
    private void Awake()
    {
        background.gameObject.SetActive(false);
        pauseIcon.gameObject.SetActive(false);
        videoPlayerControllerPro.OnPlayStateChanged.AddListener(OnPlayVideoChanged);
    }

    private void OnDestroy()
    {
        videoPlayerControllerPro.OnPlayStateChanged.RemoveListener(OnPlayVideoChanged);
    }

    private void OnPlayVideoChanged(bool OnStateChanged)
    {
        Debug.Log("Trạng thái play video thay đổi: " + OnStateChanged);
        background.gameObject.SetActive(!OnStateChanged);
        pauseIcon.gameObject.SetActive(!OnStateChanged);
    }
}
