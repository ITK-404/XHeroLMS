using UnityEngine;

public class VideoPlayPauseButtonHover : HoverButtonBase
{
    [SerializeField] private VideoPlayerControllerPro videoPlayer;

    [SerializeField] private Sprite playSprite;
    [SerializeField] private Sprite playSpriteHover;

    [SerializeField] private Sprite pauseSprite;
    [SerializeField] private Sprite pauseSpriteHover;

    private bool isPlaying = true;
    private void Awake()
    {
        videoPlayer.OnPlayStateChanged.AddListener(VideoPlayer_OnPlayStateChanged);
        HandleSprite(isPlaying);
    }

    private void OnDestroy()
    {
        videoPlayer.OnPlayStateChanged.RemoveListener(VideoPlayer_OnPlayStateChanged);
    }

    private void VideoPlayer_OnPlayStateChanged(bool isPlay)
    {
        isPlaying = isPlay;
        HandleSprite(isPlaying);
    }

    public void HandleSprite(bool isPlaying)
    {
        if (isPlaying)
        {
            normalImg.sprite = pauseSprite;
            hoverImg.sprite = pauseSpriteHover;
        }
        else
        {
            normalImg.sprite = playSprite;
            hoverImg.sprite = playSpriteHover;
        }
    }
}
