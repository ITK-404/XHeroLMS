using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelatedCoursesView : MonoBehaviour
{
    [SerializeField] private Button clickCourseBtn;
    [SerializeField] private TextMeshProUGUI courseTitle;
    [SerializeField] private TextMeshProUGUI viewCount;
    [SerializeField] private TextMeshProUGUI customerCount;
    [SerializeField] string courseID;

    private void Awake()
    {
        if(clickCourseBtn)
            clickCourseBtn.onClick.AddListener(OnShowCourse);
    }

    private void OnDestroy()
    {
        if(clickCourseBtn)
            clickCourseBtn.onClick.RemoveListener(OnShowCourse);
    }

    private void OnShowCourse()
    {
        // for test
        LoadingUI.Show(3);
    }
}
