using UnityEngine;
using UnityEngine.UI;

public class ChapterReviewCourseUI : ChapterBaseUI
{
    public Color selectColor;
    public Color unSelectColor;
    public Button background;
    public CourseReviewUI courseReviewUI;

    [SerializeField] private Image iconChapter;
    [SerializeField] private Sprite finalExamSprite;
    protected override void Awake()
    {
        base.Awake();
        if (background != null)
            background.onClick.AddListener(Toggle);

        // ensure default color when created
        if (titleName != null)
            titleName.color = unSelectColor;
        
        UnHighlight();
        ShowActiveUI(false);
    }

    protected override void OnDestroy()
    {
        if (background != null)
            background.onClick.RemoveListener(Toggle);
        base.OnDestroy();
    }

    public void Highlight()
    {
        if (titleName != null) titleName.color = selectColor;
    }

    public void UnHighlight()
    {
        if (titleName != null) titleName.color = unSelectColor;
    }
    
    public virtual void Toggle()
    {
        if (courseReviewUI != null)
        {
            courseReviewUI.Select(this);
            return;
        }

        if (isOpen)
        {
            ToggleOff();
        }
        else
        {
            ToggleOn();
        }
    }

    public void ShowFinalExam()
    {
        iconChapter.sprite = finalExamSprite;
    }
}