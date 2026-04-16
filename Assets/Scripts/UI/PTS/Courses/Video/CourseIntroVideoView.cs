using UnityEngine;
using UnityEngine.UI;

public class CourseIntroVideoView : VideoViewBase
{
    [SerializeField] private Button btnPlayPause;
    [SerializeField] private Slider sliderTime;
    // ...

    protected override void HandleStateChanged(VideoPlayerModel model)
    {
    }
}