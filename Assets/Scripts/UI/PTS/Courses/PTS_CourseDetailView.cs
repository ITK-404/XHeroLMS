using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum CourseDetailSection
{
    Intro,
    Brief,
    Detail,
    None
}

public class PTS_CourseSectionBase : MonoBehaviour
{
    public CourseDetailSection Current;

    public virtual void Show()
    {
        
    }

    public virtual void Hide()
    {
        
    }
}


public class PTS_CourseDetailView : MonoBehaviour
{
    public static PTS_CourseDetailView Instance;
    [Header("Views")]
    [SerializeField] private PTS_BackgroundWrapper background;
    [SerializeField] private PTS_CourseDetailManager detail;
    [SerializeField] private PTS_CourseInforManager infor;
    [SerializeField] private PTS_CourseListManager intro;
    [Header("Settings")] 
    [SerializeField]private PTS_ButtonGroupHandle btnGroupHandle;

    [SerializeField] private Button btnReturn;
    private List<PTS_CourseSectionBase> sectionBases = new();
    private Stack<CourseDetailSection> simple_history = new();
    private CourseDetailSection Current = CourseDetailSection.None;
    private void Awake()
    {
        Instance = this;
        
        // show intro
        sectionBases.Add(detail);
        sectionBases.Add(infor);
        sectionBases.Add(intro);
        // first view
        ShowIntroView();
        btnReturn.onClick.AddListener(GoBackward);
    }

    private void OnDestroy()
    {
        btnReturn.onClick.RemoveListener(GoBackward);
    }

    private void GoBackward()
    {
       
    }

    public void Request(CourseDetailSection section)
    {
        foreach (var item in sectionBases)
        {
            if (item.Current == section)
            {
                item.Show();
            }
            else
            {
                item.Hide();                
            }
        }
    }

    public void ShowBriefView(string courseID)
    {
        btnGroupHandle.TryShow(PTS_ButtonGroupHandle.State.Brief);
        Request(CourseDetailSection.Brief);
        background.Switch(PTS_Image.Courses);
    }

    public void ShowDetailView()
    {
        btnGroupHandle.TryShow(PTS_ButtonGroupHandle.State.Detail);
        Request(CourseDetailSection.Detail);
        background.Switch(PTS_Image.Detail);
    }

    public void ShowIntroView()
    {
        btnGroupHandle.TryShow(PTS_ButtonGroupHandle.State.None);
        Request(CourseDetailSection.Intro);

        
        background.Switch(PTS_Image.Courses);
        
    }
}