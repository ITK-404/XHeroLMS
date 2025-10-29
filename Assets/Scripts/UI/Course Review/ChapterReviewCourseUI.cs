using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterReviewCourseUI : MonoBehaviour
{
    public TextMeshProUGUI titleName;   
    public Color selectColor;
    public Color unSelectColor;
    public Button background;
    public GameObject lessonContainer;
    public CourseReviewUI courseReviewUI;
    public Button toggleOpenBtn;
    public Button toggleOffBtn;

    private bool isOpen;
    private void Awake()
    {
        
        toggleOpenBtn.onClick.AddListener(ToggleOn);
        toggleOffBtn.onClick.AddListener(ToggleOff);
        background.onClick.AddListener(SelectChapter);
    }

    private void OnDestroy()
    {
        background.onClick.RemoveListener(SelectChapter);
    }

    private void SelectChapter()
    {
        courseReviewUI.Select(this);
    }

    public void Highlight()
    {
        titleName.color = selectColor;
    }

    public void UnHighlight()
    {
        titleName.color = unSelectColor;
    }
    
    private void ToggleOn()
    {
        SelectChapter();
        Debug.Log("Toggle on");
        isOpen = true;
        lessonContainer.gameObject.SetActive(isOpen);
        toggleOpenBtn.gameObject.SetActive(false);
        toggleOffBtn.gameObject.SetActive(true);
    }

    private void ToggleOff()
    {
        Debug.Log("Toggle off");
        isOpen = false;
        lessonContainer.gameObject.SetActive(isOpen);
        toggleOpenBtn.gameObject.SetActive(true);
        toggleOffBtn.gameObject.SetActive(false);
    }
}