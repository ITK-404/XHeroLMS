using UnityEngine;
using UnityEngine.EventSystems;

public class HoverPreviewChapterLearningUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private ChapterReviewCourseUI parentUI;
    private void Awake()
    {
        parentUI = GetComponentInParent<ChapterReviewCourseUI>();
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!parentUI.isOpen)
        {
            parentUI.activeGroup.gameObject.SetActive(true);
            parentUI.Highlight();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!parentUI.isOpen)
        {
            parentUI.activeGroup.gameObject.SetActive(false);
            parentUI.UnHighlight();
        }
    }
}