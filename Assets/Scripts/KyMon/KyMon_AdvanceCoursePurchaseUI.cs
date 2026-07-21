using System;
using UnityEngine;
using UnityEngine.UI;

public class KyMon_AdvanceCoursePurchaseUI : MonoBehaviour
{
    [SerializeField] private KyMon_BuyButtonHandleUI buyButtonHandleUI;
    [SerializeField] private KyMon_AdvanceCourseElementUI courseListToggle;
    [SerializeField] private KyMon_AdvanceCourseElementUI courseRegisterToggle;
    [SerializeField] private Transform courseContentList;
    private Toggle[] toggleElements;

    public KyMon_BuyButtonHandleUI ButtonHandle => buyButtonHandleUI;

    private void Awake()
    {
        if (courseContentList)
        {
            toggleElements = courseContentList.GetComponentsInChildren<Toggle>();
        }

        foreach (var element in toggleElements)
        {
            element.onValueChanged.AddListener(CheckingCourseState);
        }

        courseListToggle.OnSelectStateChanged += CheckingCourseState;
        courseRegisterToggle.OnSelectStateChanged += CheckingCourseState;
    }

    private void OnDestroy()
    {
        foreach (var element in toggleElements)
        {
            element.onValueChanged.RemoveListener(CheckingCourseState);
        }

        courseListToggle.OnSelectStateChanged -= CheckingCourseState;
        courseRegisterToggle.OnSelectStateChanged -= CheckingCourseState;
    }

    private bool IsHaveAnyElementSelect()
    {
        if (toggleElements.Length == 0) return false;
        foreach (var element in toggleElements)
        {
            if (element.isOn)
            {
                return true;
            }
        }

        return false;
    }

    private void CheckingCourseState(bool state)
    {
        bool isCourseListSelect = courseListToggle.IsOn();
        bool isCourseRegisterSelect = courseRegisterToggle.IsOn();

        if (isCourseListSelect && IsHaveAnyElementSelect() && isCourseRegisterSelect)
        {
            // Show Register
            buyButtonHandleUI.ShowRegisterButton();
            return;
        }

        if (isCourseListSelect && IsHaveAnyElementSelect())
        {
            buyButtonHandleUI.ShowRegisterButton();
            return;
        }

        if (isCourseRegisterSelect)
        {
            buyButtonHandleUI.ShowBuyButton();
            return;
        }

        buyButtonHandleUI.HideBothButtons();
    }
}