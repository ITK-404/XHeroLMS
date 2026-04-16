using UnityEngine;
using UnityEngine.UI;

public class CourseIntroVideoView : VideoViewBase
{
    [SerializeField] private Button playPauseBtn;
    [SerializeField] private VideoPlayPauseButtonHover videoPlayPauseButtonHover;
    [SerializeField] private VideoTimelineControl timeline;
    [SerializeField] private VideoVolumeControl volume;

    protected override void OnEnable()
    {
        base.OnEnable();
        playPauseBtn.onClick.AddListener(OnClickPlayPause);
        if (timeline != null) timeline.OnSeekRequested += Core.Seek;
        if (volume != null)   volume.OnVolumeChanged   += Core.SetVolume;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        playPauseBtn.onClick.RemoveListener(OnClickPlayPause);
        if (timeline != null) timeline.OnSeekRequested -= Core.Seek;
        if (volume != null)   volume.OnVolumeChanged   -= Core.SetVolume;
    }

    protected override void HandleStateChanged(VideoPlayerModel model)
    {
        if (videoPlayPauseButtonHover != null)
            videoPlayPauseButtonHover.VideoPlayer_OnPlayStateChanged(model.IsPlaying);

        timeline?.UpdateState(model.CurrentTime, model.Duration);
        volume?.UpdateState(model.Volume);
    }

    private void OnClickPlayPause()
    {
        if (Core.GetCurrentModel().IsPlaying) Core.Pause();
        else Core.Resume();
    }
}