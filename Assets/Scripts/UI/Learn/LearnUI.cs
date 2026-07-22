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
    public Action<bool> onCourseListShow;
    private void Awake()
    {
        if (returnBtn != null)
            returnBtn.onClick.AddListener(ClickReturnBtn);
        toggleLessonScrollView.OnToggleOff.AddListener(OnToggleHide);
        toggleLessonScrollView.OnToggleOn.AddListener(OnToggleShow);
    }

    private void Start()
    {
        toggleLessonScrollView.ChangeState(ToggleBaseUI.State.DeActive);
        scrollView.gameObject.SetActive(false);
        Hide();
    }

    private void OnDestroy()
    {
        if (returnBtn != null)
            returnBtn.onClick.RemoveListener(ClickReturnBtn);

        if (toggleLessonScrollView != null)
        {
            toggleLessonScrollView.OnToggleOff.RemoveListener(OnToggleHide);
            toggleLessonScrollView.OnToggleOn.RemoveListener(OnToggleShow);
        }
    }

    private void ClickReturnBtn()
    {
        OnClickReturnBtn?.Invoke();
    }

    public void OnToggleShow()
    {
        if (scrollView != null) scrollView.SetActive(true);
        onCourseListShow?.Invoke(true);
    }

    public void OnToggleHide()
    {
        if (scrollView != null) scrollView.SetActive(false);
        onCourseListShow?.Invoke(false);
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
    }

    public bool GetCourseToggleIsOn()
    {
        return toggleLessonScrollView.currentState == ToggleBaseUI.State.Active;
    }
}
