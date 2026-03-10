using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EduCourseElement : MonoBehaviour
{
    [SerializeField] private Image courseImg;
    [SerializeField] private TextMeshProUGUI courseTitle;
    [SerializeField] private TextMeshProUGUI courseDate;
    [SerializeField] private TextMeshProUGUI coursSeatTmp;
    [SerializeField] private Button goToDetailBtn;
    [SerializeField] private CourseTagHandle courseTag;

    private void GoToDetail()
    {
        PTS_CourseOpeningView.Instance.ShowCourseInformation();
    }

    private void Awake()
    {
        goToDetailBtn.onClick.AddListener(GoToDetail);   
    }

    private void OnDestroy()
    {
        goToDetailBtn.onClick.RemoveListener(GoToDetail);   
    }
}