using UnityEngine;
using UnityEngine.UI;

public class ExamNavigationElement : MonoBehaviour
{
    public Button button;

    public ExamNavigation examNavigation;
    private RectTransform rectTransform;
 

    public void ActiveItemInNavigationBar()
    {
        Debug.Log("exam navigation active");
        if (examNavigation == null)
        {
            Debug.Log("Exam navigation is null");
            return;
        }
        examNavigation.CenterOnItem(rectTransform);
    }
}