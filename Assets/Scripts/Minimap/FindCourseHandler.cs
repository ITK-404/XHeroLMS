using System;
using UnityEngine;

public class FindCourseHandler : MonoBehaviour
{
    [SerializeField] private FindCourseTypeOptionUI[] courseTpeList;
    [SerializeField] private GameObject container;

    private void Awake()
    {
        courseTpeList = GetComponentsInChildren<FindCourseTypeOptionUI>();
        Hide();
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

}