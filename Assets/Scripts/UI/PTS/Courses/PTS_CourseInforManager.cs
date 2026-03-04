using UnityEngine;

public class PTS_CourseInforManager : PTS_CourseSectionBase
{
    [SerializeField] private GameObject videoScreen;
    [SerializeField] private GameObject fullScreen;
    [SerializeField] private GameObject navigationGroup;
    [SerializeField] private GameObject leftSide;
    public override void Show()
    {
        videoScreen.gameObject.SetActive(true);
        navigationGroup.gameObject.SetActive(true);
        leftSide.gameObject.SetActive(true);
    }

    public override void Hide()
    {
        videoScreen.gameObject.SetActive(false);
        fullScreen.gameObject.SetActive(false);
        navigationGroup.gameObject.SetActive(false);
        leftSide.gameObject.SetActive(false);
    }
}