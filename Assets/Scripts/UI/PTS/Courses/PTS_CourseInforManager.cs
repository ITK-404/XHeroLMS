using UnityEngine;

public class PTS_CourseInforManager : PTS_CourseSectionBase
{
    public override void Show()
    {
        ActiveList(true);
    }

    public override void Hide()
    {
        ActiveList(false);
    }
}