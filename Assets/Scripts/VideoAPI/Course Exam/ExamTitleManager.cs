using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamTitleManager : MonoBehaviour
{
    [Header("Header UI")]
    public Button btnBatDau;
    public TMP_Text textExamTitle;
    public TMP_Text textTotalQuestions;
    public TMP_Text textTotalDuration;
    public TMP_Text textPassNeed;

    [Header("Timer UI")]
    public TMP_Text textDemNguoc;
    public string timeFormat = "{0:00}:{1:00}";

    [Header("1-minute Warning")]
    [Tooltip("Object sẽ hiện trong 5s khi còn 1 phút")]
    public GameObject objectImage;
    [Tooltip("Màu cảnh báo khi còn <= 60s")]
    public Color warningColor = Color.red;

    private ExamUIController _examUIController;

    private bool _oneMinuteWarningTriggered = false;
    private Coroutine _warningCo;

    private void Awake()
    {
        _examUIController = GetComponent<ExamUIController>();
    }

    public void UpdateHeaderInfo()
    {
        int total = _examUIController.Paper?.Count ?? 0;

        if (textExamTitle)
            textExamTitle.text = string.IsNullOrEmpty(_examUIController.examTitle) ? "Bài thi" : _examUIController.examTitle;

        if (textTotalQuestions)
            textTotalQuestions.text = $"{total}";

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

        // Reset trạng thái cảnh báo
        _oneMinuteWarningTriggered = false;
        if (_warningCo != null) { StopCoroutine(_warningCo); _warningCo = null; }
        if (objectImage) objectImage.SetActive(false);

        _examUIController.examStarted = true;
        _examUIController.currentIndex = 0;

        _examUIController.RenderCurrentQuestion();

        if (_examUIController.timerCo != null)
            StopCoroutine(_examUIController.timerCo);

        _examUIController.timerCo = StartCoroutine(TimerCountdown());
    }

    public IEnumerator TimerCountdown()
    {
        int remain = _examUIController.DurationScends;
        // int remain = 70;
        _examUIController._elapsedSeconds = 0;

        while (true)
        {
            // Render đồng hồ
            if (textDemNguoc)
            {
                int mm = Mathf.Max(0, remain) / 60;
                int ss = Mathf.Max(0, remain) % 60;
                textDemNguoc.text = string.Format(timeFormat, mm, ss);
            }

            // Nếu có giới hạn: kiểm tra mốc 1 phút
            if (_examUIController.DurationScends > 0)
            {
                TryTriggerOneMinuteWarning(remain);

                if (remain <= 0)
                {
                    // Hết giờ -> cưỡng chế nộp
                    StartCoroutine(_examUIController.SubmitExamCoroutine(timeUp: true));
                    yield break;
                }
            }

            yield return new WaitForSeconds(1f);
            _examUIController._elapsedSeconds++;
            if (_examUIController.DurationScends > 0) remain--;
        }
    }

    /// <summary>
    /// Kích hoạt cảnh báo khi còn 1 phút.
    /// </summary>
    private void TryTriggerOneMinuteWarning(int remainSeconds)
    {
        if (_oneMinuteWarningTriggered) return;
        if (remainSeconds > 60) return;
        if (remainSeconds < 0) return;

        // Chỉ đổi màu khi còn 1p
        if (textDemNguoc) textDemNguoc.color = warningColor;

        // Hiện objectImage 5s
        if (objectImage)
        {
            if (_warningCo != null) StopCoroutine(_warningCo);
            _warningCo = StartCoroutine(ShowWarningObject5s());
        }

        _oneMinuteWarningTriggered = true;
    }

    private IEnumerator ShowWarningObject5s()
    {
        objectImage.SetActive(true);
        yield return new WaitForSeconds(5f);
        objectImage.SetActive(false);
        _warningCo = null;
    }
}
