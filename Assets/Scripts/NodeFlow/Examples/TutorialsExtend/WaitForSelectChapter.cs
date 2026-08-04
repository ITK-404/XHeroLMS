using System;
using UnityEngine;
using UnityEngine.UI;

public class WaitForSelectChapter : TutorialStepBehaviour
{
    private CourseListView courseListView;
    private bool isCompleted = false;
    [SerializeField] private LessonUI validLesson;
    [SerializeField] private ChapterUI validChapterUI;
    [SerializeField] private AutoParentUIElements autoParentUIElements;
    private void Awake()
    {
        courseListView = FindFirstObjectByType<CourseListView>();
    }

    private bool IsValidToOpen()
    {
        return false;
    }
    
    public override void Enter(CutsceneContext context = null)
    {
        base.Enter(context);
        var lessons = courseListView.VideoLessons;
        if (courseListView.VideoLessons == null || lessons.Count == 0)
        {
            isCompleted = true;
            Debug.LogError($"[WaitForSelectChapter] lesson is none");
            return;
        }

        validLesson = lessons[0];
        validChapterUI = validLesson.chapterUI;
        // validChapterUI.SelectThisChapter();

        Handle(validChapterUI.transform, false);
        autoParentUIElements.AddElement(validChapterUI.GetComponent<RectTransform>());
    }

    public override void Exit(CutsceneContext context = null)
    {
        base.Exit(context);
        Handle(validChapterUI.transform, true);
        autoParentUIElements.SetToOldParent();
    }

    private void Handle(Transform element, bool activeState)
    {
        var layoutGroup = element.parent.GetComponentInParent<VerticalLayoutGroup>();
        var contentSizeFiler = element.parent.GetComponentInParent<ContentSizeFitter>();

        layoutGroup.enabled = activeState;
        contentSizeFiler.enabled = activeState;
    }
    
    public override bool IsCompleted()
    {
        return isCompleted;
    }
}

public class WaitForSelectLesson : TutorialStepBehaviour
{
    public override bool IsCompleted()
    {
        throw new System.NotImplementedException();
    }
}