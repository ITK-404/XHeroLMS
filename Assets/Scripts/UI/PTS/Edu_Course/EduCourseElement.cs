using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EduCourseElement : MonoBehaviour
{
    [SerializeField] private Image courseImg;
    [SerializeField] private TextMeshProUGUI courseTitle;
    [SerializeField] private TextMeshProUGUI courseDate;
    [SerializeField] private TextMeshProUGUI coursSeatTmp;
    [SerializeField] private Button goToDetailBtn;
    [SerializeField] private CourseTagHandle courseTag;
    [SerializeField] private UnityEvent OnChangeViewClicked;
    private void GoToDetail()
    {
        OnChangeViewClicked?.Invoke();
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