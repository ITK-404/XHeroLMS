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
    [SerializeField] protected List<GameObject> openingList = new();
    
    protected void ActiveList(bool state)
    {
        foreach (var item in openingList)
        {
            item.gameObject.SetActive(state);
        }
    }
    
    public virtual void Show()
    {
    }

    public virtual void Hide()
    {
    }
}


public class PTS_BaseView : UIView
{
    
    public Action OnEnterNoneView;
    [Header("Settings")]
    [SerializeField] protected PTS_ButtonGroupHandle btnGroupHandle;
    [SerializeField] protected Button btnReturn;

    public virtual void ShowDefault()
    {
        
    }
}
public class PTS_CourseDetailView : PTS_BaseView
{

    [Header("Views")]
    [SerializeField] private PTS_BackgroundWrapper background;
    [SerializeField] private PTS_CourseDetailManager detail;
    [SerializeField] private PTS_CourseInforManager infor;
    [SerializeField] private PTS_CourseListManager intro;
    [SerializeField] private PTS_CourseTitle title;

    private readonly List<PTS_CourseSectionBase> sectionBases = new();
    private readonly Stack<CourseDetailSection> simpleHistory = new();
    
    private CourseDetailSection current = CourseDetailSection.None;

    protected override void Awake()
    {
        base.Awake();

        btnGroupHandle.GoToDetailClickEvent = ShowDetailView;
        
        sectionBases.Add(detail);
        sectionBases.Add(infor);
        sectionBases.Add(intro);
        
        title.gameObject.SetActive(false);
        btnReturn.onClick.AddListener(GoBackward);
        NavigateTo(CourseDetailSection.Intro);
        Hide();
    }

    private void OnDestroy()
    {
        btnReturn.onClick.RemoveListener(GoBackward);
    }

    private void GoBackward()
    {
        if (simpleHistory.Count > 0)
        {
            var previous = simpleHistory.Pop();
            NavigateTo(previous, false);
            Debug.Log($"[Course View] Pop {previous}");
            
            return;
        }

        Hide();
        current = CourseDetailSection.None;
        OnEnterNoneView?.Invoke();
      
    }

    private void NavigateTo(CourseDetailSection target, bool saveHistory = true)
    {
        if (current == target)
            return;

        if (saveHistory && current != CourseDetailSection.None)
        {
            Debug.Log($"[Course View] Push {target}");
            simpleHistory.Push(current);
        }

        current = target;
        Request(target);
        ApplyVisualBySection(target);
    }

    private void ApplyVisualBySection(CourseDetailSection section)
    {
        switch (section)
        {
            case CourseDetailSection.Intro:
                btnGroupHandle.TryShow(PTS_ButtonGroupHandle.State.None);
                background.Switch(PTS_Image.Courses);
                title.gameObject.SetActive(false);
                break;

            case CourseDetailSection.Brief:
                btnGroupHandle.TryShow(PTS_ButtonGroupHandle.State.Brief);
                background.Switch(PTS_Image.Courses);
                title.gameObject.SetActive(true);
                break;

            case CourseDetailSection.Detail:
                btnGroupHandle.TryShow(PTS_ButtonGroupHandle.State.Detail);
                background.Switch(PTS_Image.Detail);
                title.gameObject.SetActive(true);
                break;
        }
    }

    private void Request(CourseDetailSection section)
    {
        foreach (var item in sectionBases)
        {
            if (item.Current == section)
                item.Show();
            else
                item.Hide();
        }
    }

    public void ShowBriefView()
    {
        Debug.Log("Show brief view");
        Show();
        NavigateTo(CourseDetailSection.Brief);
    }

    public void ShowDetailView()
    {
        Debug.Log("Show detail view");
        Show();
        NavigateTo(CourseDetailSection.Detail);
    }

    public override void ShowDefault()
    {
        base.ShowDefault();
        simpleHistory.Clear();
        NavigateTo(CourseDetailSection.Intro);
    }
}