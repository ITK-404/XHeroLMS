using System;
using UnityEngine;
using UnityEngine.UI;

public class ExamReturn: MonoBehaviour
{
    public Button btnReturn;

    public ExamUIController examUIController;
    public GameObject ExamResult;
    public GameObject ExamCanvas;
    public GameObject SubmitExam;
    public static Action ExamReturnAction;
    void Start()
    {
        btnReturn.onClick.AddListener(() =>
        {
            ExamResultReviewPanel.FlagContinue = true;
        });
    }

    void Update()
    {
        if (ExamResultReviewPanel.FlagContinue)
        {
            ExamResultReviewPanel.FlagContinue = false;

            // Reset toàn bộ bài thi (state + UI + timer)
            examUIController.RestartExam();

            // Mở giao diện thi
            ExamResult.SetActive(false);
            ExamCanvas.SetActive(true);
            SubmitExam.SetActive(false);
            
            ExamReturnAction?.Invoke();
        }
    }
}
