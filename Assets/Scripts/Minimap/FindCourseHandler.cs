using System;
using UnityEngine;
using UnityEngine.UI;

public class FindCourseHandler : MonoBehaviour
{
    [SerializeField] private FindCourseTypeOptionUI[] courseTpeList;
    [SerializeField] private GameObject container;

    [SerializeField] Button closeBtn;
    [SerializeField] Button backgroundCloseBtn;
    public Action OnCloseFindCourseAction;
    private void Awake()
    {
        courseTpeList = GetComponentsInChildren<FindCourseTypeOptionUI>();
        closeBtn.onClick.AddListener(OnCloseBtn);
        backgroundCloseBtn.onClick.AddListener(OnCloseBtn);
        Hide();
    }

    private void OnDestroy()
    {
        closeBtn.onClick.RemoveListener(OnCloseBtn);
        backgroundCloseBtn.onClick.RemoveListener(OnCloseBtn);
    }

    private void Start()
    {
        courseTpeList[0].Toggle.isOn = true;
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
        // set first item is default active
        courseTpeList[0].Toggle.isOn = true;
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }

    private void OnCloseBtn()
    {
        OnCloseFindCourseAction?.Invoke();
    }

}