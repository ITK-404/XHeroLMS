using System;
using UnityEngine;
using UnityEngine.UI;

public class LearnUI : MonoBehaviour
{
    public GameObject container;

    public Button returnBtn;

    public Action OnClickReturnBtn;
    public GameObject scrollView;
    public ToggleBaseUI toggleLessonScrollView;
    private void Awake()
    {
        returnBtn.onClick.AddListener(ClickReturnBtn);
        toggleLessonScrollView.btn.onClick.AddListener(toggleLessonScrollView.Toggle);
        toggleLessonScrollView.OnToggleOff.AddListener(() =>{scrollView.gameObject.SetActive(false);});
        toggleLessonScrollView.OnToggleOn.AddListener(() =>{scrollView.gameObject.SetActive(true);});
        Hide();
    }
    
    private void OnDestroy()
    {
        returnBtn.onClick.RemoveListener(ClickReturnBtn);
        toggleLessonScrollView.btn.onClick.RemoveListener(toggleLessonScrollView.Toggle);
        toggleLessonScrollView.OnToggleOff.RemoveListener(() =>{scrollView.gameObject.SetActive(false);});
        toggleLessonScrollView.OnToggleOn.RemoveListener(() =>{scrollView.gameObject.SetActive(true);});
        
    }

    private void ClickReturnBtn()
    {
        OnClickReturnBtn?.Invoke();
    }

    public void Show()
    {
        Debug.Log("Show Learn UI");
        container.gameObject.SetActive(true);
        
    }

    public void Hide()
    {
        Debug.Log("Hide Learn UI");
        container.gameObject.SetActive(false);
        toggleLessonScrollView.ChangeState(ToggleBaseUI.State.DeActive);
    }
}
