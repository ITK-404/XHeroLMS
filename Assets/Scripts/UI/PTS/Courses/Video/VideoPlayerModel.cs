using System;

[Serializable]
public class VideoPlayerModel
{
    public string VideoUrl;
    public string BannerUrl;
    public float Volume = 1f;
    public float Duration;
    public float CurrentTime;
    public bool IsPlaying;
    public bool IsPrepared;
}