using UnityEngine;

public class PTS_CourseInforManager : PTS_CourseSectionBase
{
    [SerializeField] private VideoControllerTest videoControllerTest;
    public override void Show()
    {
        ActiveList(true);
        videoControllerTest.ShowViewA();
    }

    public override void Hide()
    {
        ActiveList(false);
        videoControllerTest.GetComponent<VideoPlayerCore>().Stop();
    }
}