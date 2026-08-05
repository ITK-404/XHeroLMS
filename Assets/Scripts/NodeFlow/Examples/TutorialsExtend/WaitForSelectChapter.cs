using UnityEngine;

public class WaitForSelectCourse : TutorialStepBehaviour
{
    private enum SelectState
    {
        None,
        WaitForChapter,
        WaitForLesson,
        Completed
    }

    [SerializeField] private LessonUI validLesson;
    [SerializeField] private ChapterUI validChapter;

    private CourseListView courseListView;
    private SelectState currentState;

    private void Awake()
    {
        courseListView = FindFirstObjectByType<CourseListView>();
    }

    public override void Enter(CutsceneContext context = null)
    {
        base.Enter(context);

        currentState = SelectState.None;

        if (!TrySetupTarget())
        {
            currentState = SelectState.Completed;
            return;
        }

        ChangeState(SelectState.WaitForChapter);
    }

    public override void Exit(CutsceneContext context = null)
    {
        base.Exit(context);

        currentState = SelectState.None;
    }

    public override bool IsCompleted()
    {
        TickState();

        return currentState == SelectState.Completed;
    }

    private bool TrySetupTarget()
    {
        if (courseListView == null)
        {
            Debug.LogError(
                $"[{nameof(WaitForSelectCourse)}] CourseListView not found.",
                this
            );

            return false;
        }

        var lessons = courseListView.VideoLessons;

        if (lessons == null || lessons.Count == 0)
        {
            Debug.LogError(
                $"[{nameof(WaitForSelectCourse)}] No lessons found.",
                this
            );

            return false;
        }

        validLesson = lessons[0];

        if (validLesson == null)
        {
            Debug.LogError(
                $"[{nameof(WaitForSelectCourse)}] Valid lesson is null.",
                this
            );

            return false;
        }

        validChapter = validLesson.chapterUI;

        if (validChapter == null)
        {
            Debug.LogError(
                $"[{nameof(WaitForSelectCourse)}] Valid chapter is null.",
                this
            );

            return false;
        }

        return true;
    }

    private void TickState()
    {
        switch (currentState)
        {
            case SelectState.WaitForChapter:
                CheckChapterSelection();
                break;

            case SelectState.WaitForLesson:
                CheckLessonSelection();
                break;
        }
    }

    private void CheckChapterSelection()
    {
        if (ChapterUIManager.Instance.currentChapter != validChapter)
        {
            return;
        }

        ChangeState(SelectState.WaitForLesson);
    }

    private void CheckLessonSelection()
    {
        if (courseListView.CurrentLesson != validLesson)
        {
            return;
        }

        ChangeState(SelectState.Completed);
    }

    private void ChangeState(SelectState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        ExitState(currentState);

        currentState = newState;

        EnterState(currentState);
    }

    private void EnterState(SelectState state)
    {
        switch (state)
        {
            case SelectState.WaitForChapter:
                string chapterDescription = $"Nhấn vào để xem các bài học bên trong";
                Focus(validChapter.GetComponent<RectTransform>(), chapterDescription);
                break;

            case SelectState.WaitForLesson:
                string lessonDescription = $"Nhấn vào để học";
                Focus(validLesson.GetComponent<RectTransform>(),lessonDescription);
                break;

            case SelectState.Completed:
                ClearFocus();
                break;
        }
    }

    private void ExitState(SelectState state)
    {
        // Hiện tại chưa cần xử lý.
        // Sau này có animation hoặc cleanup riêng cho từng state thì thêm ở đây.
    }

    private void Focus(RectTransform target,string description)
    {
        if (target == null)
        {
            Debug.LogError(
                $"[{nameof(WaitForSelectCourse)}] Focus target is null.",
                this
            );

            currentState = SelectState.Completed;
            return;
        }

        ClassTutorialFlow.Instance.SetInteractZone(target);
        FocusHandManager.Instance.SetToTargetRect(target, description);
    }

    private void ClearFocus()
    {
        ClassTutorialFlow.Instance.ClearZone();
        FocusHandManager.Instance.Hide();
    }
}