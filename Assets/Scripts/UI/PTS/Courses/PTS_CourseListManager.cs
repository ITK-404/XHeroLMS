using System;
using System.Collections.Generic;
using UnityEngine;

public class PTS_CourseListManager : PTS_CourseSectionBase
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