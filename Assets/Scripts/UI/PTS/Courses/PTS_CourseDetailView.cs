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
    [SerializeField] private GameObject container;
    [SerializeField] private PTS_BackgroundWrapper background;
    [SerializeField] private PTS_CourseDetailManager detail;
    [SerializeField] private PTS_CourseInforManager infor;
    [SerializeField] private PTS_CourseListManager intro;

    [Header("Settings")]
    [SerializeField] private PTS_ButtonGroupHandle btnGroupHandle;
    [SerializeField] private Button btnReturn;

    private readonly List<PTS_CourseSectionBase> sectionBases = new();
    private readonly Stack<CourseDetailSection> simpleHistory = new();
    public Action OnEnterNoneView;
    private CourseDetailSection current = CourseDetailSection.None;

    private void Awake()
    {
        Instance = this;

        sectionBases.Add(detail);
        sectionBases.Add(infor);
        sectionBases.Add(intro);

        btnReturn.onClick.AddListener(GoBackward);
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
                break;

            case CourseDetailSection.Brief:
                btnGroupHandle.TryShow(PTS_ButtonGroupHandle.State.Brief);
                background.Switch(PTS_Image.Courses);
                break;

            case CourseDetailSection.Detail:
                btnGroupHandle.TryShow(PTS_ButtonGroupHandle.State.Detail);
                background.Switch(PTS_Image.Detail);
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

    public void ShowBriefView(string courseID)
    {
        Show();
        NavigateTo(CourseDetailSection.Brief);
    }

    public void ShowDetailView()
    {
        Show();
        NavigateTo(CourseDetailSection.Detail);
    }

    public void ShowIntroView()
    {
        Show();
        NavigateTo(CourseDetailSection.Intro);
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}