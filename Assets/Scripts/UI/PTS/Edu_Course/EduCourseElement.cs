using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EduCourseElement : MonoBehaviour
{
    [SerializeField] private Image courseImg;
    [SerializeField] private TextMeshProUGUI courseTitle;
    [SerializeField] private TextMeshProUGUI courseDate;
    [SerializeField] private TextMeshProUGUI coursSeatTmp;
    [SerializeField] private Button goToDetailBtn;
    [SerializeField] private CourseTagHandle courseTag;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackSprite;

    private string _courseId;
    private Coroutine _loadImageRoutine;

    private void Awake()
    {
        if (goToDetailBtn != null)
            goToDetailBtn.onClick.AddListener(GoToDetail);
    }

    private void OnDestroy()
    {
        if (goToDetailBtn != null)
            goToDetailBtn.onClick.RemoveListener(GoToDetail);

        if (_loadImageRoutine != null)
            StopCoroutine(_loadImageRoutine);
    }

    public void Setup(CourseModels.CourseLite data)
    {
        if (data == null) return;

        _courseId = data._id;

        if (courseTitle != null)
            courseTitle.text = string.IsNullOrWhiteSpace(data.title) ? "Khóa học" : data.title;

        if (courseDate != null)
        {
            string startDateText = GetFirstStartDateText(data.courseStartDate);
            courseDate.text = string.IsNullOrWhiteSpace(startDateText)
                ? "Chưa có lịch khai giảng"
                : startDateText;
        }

        if (coursSeatTmp != null)
        {
            int learners = data.learners > 0 ? data.learners : data.totalStudent;
            coursSeatTmp.text = learners.ToString();
        }

        if (courseTag != null)
        {
        }

        if (_loadImageRoutine != null)
            StopCoroutine(_loadImageRoutine);

        if (courseImg != null)
        {
            courseImg.sprite = fallbackSprite;

            if (!string.IsNullOrWhiteSpace(data.image))
                _loadImageRoutine = StartCoroutine(LoadImage(data.image));
        }
    }

    private void GoToDetail()
    {
        if (PTS_CourseOpeningView.Instance != null)
        {
            PTS_CourseOpeningView.Instance.ShowCourseInformation();
        }
    }

    private string GetFirstStartDateText(List<CourseModels.CourseStartDateItem> dates)
    {
        if (dates == null || dates.Count == 0)
            return "";

        var first = dates[0];
        if (first == null || first.start == null)
            return "";

        int day = first.start.day;
        int month = first.start.month;
        int year = first.start.year;

        if (day <= 0 || month <= 0 || year <= 0)
            return "";

        return $"{day:00}/{month:00}/{year}";
    }

    private IEnumerator LoadImage(string url)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                _loadImageRoutine = null;
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(req);
            if (texture == null)
            {
                _loadImageRoutine = null;
                yield break;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            if (courseImg != null)
                courseImg.sprite = sprite;
        }

        _loadImageRoutine = null;
    }
}