using System;
using System.Collections;
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
    public GameObject objectImage;
    public Color warningColor = Color.red;

    private ExamUIController _examUIController;

    private bool _oneMinuteWarningTriggered = false;
    private Coroutine _warningCo;

    private void Awake()
    {
        _examUIController = GetComponent<ExamUIController>();
        if (_examUIController == null)
            Debug.LogError("[ExamTitleManager] Không tìm thấy ExamUIController.");
    }

    public void UpdateHeaderInfo()
    {
        if (_examUIController == null) return;

        int total = _examUIController.Paper?.Count ?? 0;
        int duration = Mathf.Max(0, _examUIController.DurationScends);
        int mm = duration / 60;
        int ss = duration % 60;
        int passPercent = Mathf.Max(0, _examUIController.passPointPercent);

        // ====== ALWAYS USE examName as the header title ======
        if (textExamTitle)
        {
            string headerTitle =
                !string.IsNullOrEmpty(_examUIController.examTitle)
                    ? _examUIController.examTitle
                    : _examUIController.examName;

            textExamTitle.text = headerTitle;
        }

        if (textTotalQuestions)
            textTotalQuestions.text = total.ToString();

        if (textTotalDuration)
            textTotalDuration.text = duration > 0
                ? string.Format(timeFormat, mm, ss)
                : "--:--";

        if (textPassNeed)
        {
            int need = Mathf.CeilToInt(total * (passPercent / 100f));
            textPassNeed.text = $"{need}/{total}";
        }

        if (textDemNguoc)
        {
            textDemNguoc.text = duration > 0
                ? string.Format(timeFormat, mm, ss)
                : "--:--";

            textDemNguoc.color = "812E11".ToColor();
        }

        Debug.Log(
            $"[TitleManager] Updated: total={total}, duration={duration}, pass={passPercent}, examName='{_examUIController.examName}'");
    }

    public void BeginExam()
    {
        if (_examUIController == null) return;
        if (_examUIController.examStarted) return;

        if (_examUIController.Paper == null || _examUIController.Paper.Count == 0)
        {
            Debug.LogWarning("[ExamUI] Không có dữ liệu câu hỏi.");
            return;
        }

        _oneMinuteWarningTriggered = false;
        if (_warningCo != null) { StopCoroutine(_warningCo); _warningCo = null; }
        if (objectImage) objectImage.SetActive(false);

        _examUIController.examStarted = true;
        _examUIController.currentIndex = 0;

        UpdateHeaderInfo();
        _examUIController.RenderCurrentQuestion();

        if (_examUIController.timerCo != null)
            StopCoroutine(_examUIController.timerCo);

        _examUIController.timerCo = StartCoroutine(TimerCountdown());
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

            if (_examUIController.DurationScends > 0)
            {
                TryTriggerOneMinuteWarning(remain);

                if (remain <= 0)
                {
                    StartCoroutine(_examUIController.SubmitExamCoroutine(timeUp: true));
                    yield break;
                }
            }

            yield return new WaitForSeconds(1f);
            _examUIController._elapsedSeconds++;
            remain--;
        }
    }

    private void TryTriggerOneMinuteWarning(int remainSeconds)
    {
        if (_oneMinuteWarningTriggered) return;
        if (remainSeconds > 60) return;
        if (remainSeconds < 0) return;

        if (textDemNguoc) textDemNguoc.color = warningColor;

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
