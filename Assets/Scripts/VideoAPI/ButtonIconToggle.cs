using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ButtonIconToggle : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnPlay;
    public Button btnFullscreen;

    [Header("Play Button Icons")]
    public Image playImage;
    public Sprite playSprite1;
    public Sprite playSprite2;

    [Header("Fullscreen Button Icons")]
    public Image fullscreenImage;
    public Sprite fullscreenSprite1;
    public Sprite fullscreenSprite2;
    
    bool isPlaying = false;
    bool isFullscreen = false;

    VideoPlayerControllerPro playerCtrl;

    void Start()
    {
        if (btnPlay) btnPlay.onClick.AddListener(OnPlayClicked);
        if (btnFullscreen) btnFullscreen.onClick.AddListener(OnFullscreenClicked);

        // Set icon ban đầu
        if (playImage && playSprite1) playImage.sprite = playSprite1;
        if (fullscreenImage && fullscreenSprite1) fullscreenImage.sprite = fullscreenSprite1;

        playerCtrl = FindAnyObjectByType<VideoPlayerControllerPro>();
if (playerCtrl)
{
    playerCtrl.OnPlayStateChanged.AddListener(UpdatePlayIcon);
    playerCtrl.OnFullscreenChanged.AddListener(UpdateFullscreenIcon);
}
    }

    void OnPlayClicked()
    {
        isPlaying = !isPlaying;
        if (playImage)
            playImage.sprite = isPlaying ? playSprite2 : playSprite1;
    }

    void OnFullscreenClicked()
    {
        isFullscreen = !isFullscreen;
        if (fullscreenImage)
            fullscreenImage.sprite = isFullscreen ? fullscreenSprite2 : fullscreenSprite1;
    }

    void UpdatePlayIcon(bool isPlaying)
{
    this.isPlaying = isPlaying;
    if (playImage)
        playImage.sprite = isPlaying ? playSprite2 : playSprite1;
}

void UpdateFullscreenIcon(bool isFullscreen)
{
    this.isFullscreen = isFullscreen;
    if (fullscreenImage)
        fullscreenImage.sprite = isFullscreen ? fullscreenSprite2 : fullscreenSprite1;
}

}
