using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExamTitleManager : MonoBehaviour
{
    [Header("Header UI")]
    // public TMP_Text textQuestionCounter; // "01/30"

    public Button btnBatDau;
    public TMP_Text textExamTitle;
    public TMP_Text textTotalQuestions;
    public TMP_Text textTotalDuration;
    public TMP_Text textPassNeed;

    public TMP_Text textDemNguoc;
    // public Image multiple_hint;

    // public GameObject correctCheck;
    // public GameObject inCorrectCheck;

    public string timeFormat = "{0:00}:{1:00}";

    private ExamUIController _examUIController;

    // [Header("Auth")]
    // public bool useTokenFromStore = true;

    // public string overrideAccessToken = "";

    // private int _examUIController.currentIndex = 0;

    private void Awake()
    {
        _examUIController = GetComponent<ExamUIController>();
    }

    public void UpdateHeaderInfo()
    {
        int total = _examUIController.Paper?.Count ?? 0;

        if (textExamTitle) textExamTitle.text = string.IsNullOrEmpty(_examUIController.examTitle) ? "Bài thi" : _examUIController.examTitle;

        if (textTotalQuestions) textTotalQuestions.text = $"{total}";

        if (textTotalDuration)
        {
            int mm = Mathf.Max(0, _examUIController.DurationScends) / 60;
            int ss = Mathf.Max(0, _examUIController.DurationScends) % 60;
            textTotalDuration.text = $"{string.Format(timeFormat, mm, ss)}";
        }

        if (textPassNeed)
        {
            int need = Mathf.CeilToInt(total * (_examUIController.passPointPercent / 100f));
            textPassNeed.text = $"{need}/{total}";
        }
    }

    // ===================== BẮT ĐẦU THI =====================
    public void BeginExam()
    {
        if (_examUIController.examStarted) return;
        if (_examUIController.Paper == null || _examUIController.Paper.Count == 0)
        {
            Debug.LogWarning("[ExamUI] Chưa có dữ liệu câu hỏi. Không thể bắt đầu.");
            return;
        }

        _examUIController.examStarted = true;
        _examUIController.currentIndex = 0;

        _examUIController.RenderCurrentQuestion();

        if (_examUIController.timerCo != null) StopCoroutine(_examUIController.timerCo);
        if (_examUIController.DurationScends > 0) _examUIController.timerCo = StartCoroutine(TimerCountdown());
    }

    public IEnumerator TimerCountdown()
    {
        int remain = _examUIController.DurationScends;
        _examUIController._elapsedSeconds = 0;

        while (true)
        {
            if (textDemNguoc)
            {
                int mm = Mathf.Max(0, remain) / 60;
                int ss = Mathf.Max(0, remain) % 60;
                textDemNguoc.text = string.Format(timeFormat, mm, ss);
            }

            if (_examUIController.DurationScends <= 0)
            {
                // không giới hạn: chỉ tăng elapsed và chờ user nộp
                _examUIController._elapsedSeconds++;
                yield return new WaitForSeconds(1f);
                continue;
            }

            if (remain <= 0)
            {
                // Hết giờ -> cưỡng chế nộp
                StartCoroutine(_examUIController.SubmitExamCoroutine(timeUp: true));
                yield break;
            }

            yield return new WaitForSeconds(1f);
            _examUIController._elapsedSeconds++;
            remain--;
        }
    }
}
