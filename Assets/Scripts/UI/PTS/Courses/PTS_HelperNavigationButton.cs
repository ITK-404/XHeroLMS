using System;
using UnityEngine;

public class PTS_HelperNavigationButton : MonoBehaviour
{
    [SerializeField] private CourseDetailSection section;
    public void NavigationCourseID()
    {
        var view = PTS_ViewManager.Instance.Current.GetComponent<PTS_CourseDetailView>();

        if (view == null)
        {
            return;
        }
        
        switch (section)
        {
            case CourseDetailSection.Brief:
                view.ShowBriefView();
                break;
            case CourseDetailSection.Detail:
                view.ShowDetailView();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}