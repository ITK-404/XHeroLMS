using UnityEngine;
using UnityEngine.UI;

public class ExamNavigationElement : MonoBehaviour
{
    public Button button;

    public ExamNavigation examNavigation;
    private RectTransform rectTransform;
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        button.onClick.AddListener(OnSelectItem);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnSelectItem);
    }

    private void OnSelectItem()
    {
        if (examNavigation == null)
        {
            Debug.Log("Exam navigation is null");
            return;
        }
        examNavigation.CenterOnItem(rectTransform);
    }
}