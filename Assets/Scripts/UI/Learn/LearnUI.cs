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
        if (returnBtn != null)
            returnBtn.onClick.AddListener(ClickReturnBtn);

        if (toggleLessonScrollView != null)
        {
            toggleLessonScrollView.OnToggleOff.AddListener(OnToggleHide);
            toggleLessonScrollView.OnToggleOn.AddListener(OnToggleShow);
        }
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

    private void OnToggleShow()
    {
        if (scrollView != null) scrollView.SetActive(true);
    }

    private void OnToggleHide()
    {
        if (scrollView != null) scrollView.SetActive(false);
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
