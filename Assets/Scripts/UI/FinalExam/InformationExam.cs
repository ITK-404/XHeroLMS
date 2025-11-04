using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InformationExam : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI examQuestionCountTMP;
    [SerializeField] private TextMeshProUGUI examTimerCountTMP;
    [SerializeField] private TextMeshProUGUI examCorrectCountTMP;
    [SerializeField] private Transform container;
    [SerializeField] private Button startBtn;

    public Action OnStartButtonClick;
    
    private void Awake()
    {
        startBtn.onClick.AddListener(OnClickStartBtn);
    }

    private void OnDestroy()
    {
        startBtn.onClick.RemoveListener(OnClickStartBtn);
    }

    private void OnClickStartBtn()
    {
        OnStartButtonClick?.Invoke();
    }

    public void Show() => container.gameObject.SetActive(true);
    public void Hide() => container.gameObject.SetActive(false);
    
    public void SetExamData(ExamData examData)
    {
        int.TryParse(examData.passPointPercent, out var percent);
        int.TryParse(examData.count, out var count);
        int correctCount = count * percent;

        examQuestionCountTMP.text = count.ToString();

        if (int.TryParse(examData.duration, out var totalSeconds))
        {
            if (totalSeconds < 0) totalSeconds = 0;
            int minutes = totalSeconds / 60;
            examTimerCountTMP.text = $"{minutes} Phút";
        }
        else
        {
            examTimerCountTMP.text = examData.duration ?? string.Empty;
        }

        examCorrectCountTMP.text = correctCount.ToString();
    }
}