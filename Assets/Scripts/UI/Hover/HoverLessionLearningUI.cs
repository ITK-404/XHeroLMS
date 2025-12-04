using UnityEngine;
using UnityEngine.EventSystems;

public class HoverLessionLearningUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private LessonUI lessonUI;
    private void Awake()
    {
        lessonUI = GetComponent<LessonUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!lessonUI.isSelect)
        {
            lessonUI.SetHover(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!lessonUI.isSelect)
        {
            lessonUI.SetHover(false);
        }
    }
}