using UnityEngine;

public interface IVideoView
{
    void OnTextureReady(RenderTexture rt);
    void OnStateChanged(VideoPlayerModel model);
    void OnBannerLoaded(Texture banner);
}