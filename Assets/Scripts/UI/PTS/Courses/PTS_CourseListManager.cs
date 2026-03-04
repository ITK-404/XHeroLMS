using System;
using UnityEngine;

public class PTS_CourseListManager : PTS_CourseSectionBase
{
    [SerializeField] private GameObject courseGroup;
    [SerializeField] private GameObject courseFilerGroup;
    [SerializeField] private GameObject leftSide;
    public override void Show()
    {
        courseGroup.gameObject.SetActive(true);
        courseFilerGroup.gameObject.SetActive(true);
        leftSide.gameObject.SetActive(true);
    }

    public override void Hide()
    {
        courseGroup.gameObject.SetActive(false);
        courseFilerGroup.gameObject.SetActive(false);
        leftSide.gameObject.SetActive(false);
    }
}