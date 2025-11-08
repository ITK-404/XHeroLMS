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

public class ExamQuestionManager : MonoBehaviour
{
    [Header("Where to spawn")]
    public Transform content;

    [Header("Prefabs")]
    public TMP_Text prefabCauHoi;

    public AnswerButton prefabCauTraLoi;

    // Prefab chứa TMP_InputField để nhập bài tự luận
    public TMP_InputField prefabCauTraLoiTuLuan;

    [Header("Buttons & Timer")]
    public TMP_Text textQuestionCounter; // "01/30"
    public Button btnBack;

    public Button btnNext;
    public Button btnNopBai;
    // public TMP_Text textDemNguoc;

    public Image multiple_hint;
    public bool getWithCorrectAnswer = true; // khi GET kết quả

    // ===== submit state =====
    bool _isSubmitting = false;
    // int _examUIController._elapsedSeconds = 0; 
    
    private ExamUIController _examUIController;

    private readonly Dictionary<string, HashSet<int>> selectedMap = new();
    private readonly List<AnswerButton> spawnedOptions = new();

    private readonly Dictionary<string, string> essayMap = new();

    // private int _examUIController.currentIndex = 0;

    private void Awake()
    {
        _examUIController = GetComponent<ExamUIController>();
    }
    
    public void ClearContent()
    {
        if (!content) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        spawnedOptions.Clear();
    }

    public void ShowNoQuestion()
    {
        ClearContent();
        SpawnQuestionText("(Không có câu hỏi)");
        UpdateNavButtons();
        UpdateQuestionCounter();
        _examUIController.UpdateHeaderInfo();
    }

    public void RenderCurrentQuestion()
    {
        if (!_examUIController.examStarted) return;

        if (_examUIController.Paper == null || _examUIController.Paper.questions == null || _examUIController.Paper.questions.Count == 0)
        {
            ShowNoQuestion();
            return;
        }

        _examUIController.currentIndex = Mathf.Clamp(_examUIController.currentIndex, 0, _examUIController.Paper.questions.Count - 1);
        var q = _examUIController.Paper.questions[_examUIController.currentIndex];

        ClearContent();
        SpawnQuestionText($"{_examUIController.currentIndex + 1}. {q.title}");

        switch (q.type)
        {
            case ExamQuestionType.SINGLE_CHOICE:
            case ExamQuestionType.MULTIPLE_CHOICE:
                RenderOptions(q);

                // multiple_hint.gameObject.SetActive(q.type == ExamQuestionType.MULTIPLE_CHOICE);

                break;
            case ExamQuestionType.ESSAY:
                RenderEssay(q);
                break;
            default:
                SpawnQuestionText($"(Type {q.type} chưa hỗ trợ UI – sẽ cập nhật sau)");
                break;
        }

        UpdateNavButtons();
        UpdateQuestionCounter();
    }


    void RenderOptions(ExamQuestion q)
    {
        if (q.options == null || q.options.Count == 0)
        {
            SpawnQuestionText("(Không có đáp án)");
            return;
        }

        if (!selectedMap.ContainsKey(q.id))
            selectedMap[q.id] = new HashSet<int>();
        var picked = selectedMap[q.id];

        bool isSingle = (q.type == ExamQuestionType.SINGLE_CHOICE);

        for (int i = 0; i < q.options.Count; i++)
        {
            string optText = ExamFormat.CleanOptionText(q.options[i]);
            var item = Instantiate(prefabCauTraLoi, content);
            spawnedOptions.Add(item);

            if (isSingle) item.ActiveSingleChoice();
            else item.ActiveMultipleChoice();

            bool isOn = picked.Contains(i);
            item.SetText(optText);
            item.ActiveSelect(isOn);

            int optionIndex = i;
            item.OnSelectButton = (btn) =>
            {
                if (isSingle)
                {
                    foreach (var other in spawnedOptions)
                        if (other != btn)
                            other.ActiveSelect(false);

                    picked.Clear();
                    bool turnOn = !btn.value;
                    btn.ActiveSelect(turnOn);
                    if (turnOn) picked.Add(optionIndex);
                }
                else
                {
                    bool turnOn = !btn.value;
                    btn.ActiveSelect(turnOn);
                    if (turnOn) picked.Add(optionIndex);
                    else picked.Remove(optionIndex);
                }
            };
        }
    }

    TMP_Text SpawnQuestionText(string text)
    {
        var t = Instantiate(prefabCauHoi, content);
        t.text = text ?? "";
        return t;
    }

    public void UpdateNavButtons()
    {
        bool canNav = _examUIController.examStarted && _examUIController.Paper != null && _examUIController.Paper.Count > 0;
        if (btnBack) btnBack.interactable = canNav && _examUIController.currentIndex > 0;
        if (btnNext) btnNext.interactable = canNav && _examUIController.currentIndex < _examUIController.Paper.Count - 1;
        if (btnNopBai) btnNopBai.interactable = canNav;
    }

    public void OnBack()
    {
        if (!_examUIController.examStarted) return;
        if (_examUIController.Paper == null || _examUIController.currentIndex <= 0) return;
        _examUIController.currentIndex--;
        RenderCurrentQuestion();
    }

    public void OnNext()
    {
        if (!_examUIController.examStarted) return;
        if (_examUIController.Paper == null || _examUIController.currentIndex >= _examUIController.Paper.Count - 1) return;
        _examUIController.currentIndex++;
        RenderCurrentQuestion();
    }

    public void OnSubmit()
    {
        if (_isSubmitting) return;
        StartCoroutine(SubmitExamCoroutine(timeUp: false));
    }

    [Serializable]
    public class ResultItem
    {
        public string questionId;
        public List<string> result;
    }

    [Serializable]
    public class SubmitBody
    {
        public string examId;
        public List<ResultItem> results = new List<ResultItem>();
        public int timeSpent; // giây
    }
    SubmitBody BuildSubmitBody(string examId)
    {
        var body = new SubmitBody
        {
            examId = examId,
            timeSpent = Mathf.Max(0, _examUIController._elapsedSeconds)
        };

        if (_examUIController.Paper?.questions == null) return body;

        foreach (var q in _examUIController.Paper.questions)
        {
            var item = new ResultItem { questionId = q.id, result = new List<string>() };

            switch (q.type)
            {
                case ExamQuestionType.SINGLE_CHOICE:
                case ExamQuestionType.MULTIPLE_CHOICE:
                    if (selectedMap.TryGetValue(q.id, out var picked))
                    {
                        foreach (var idx in picked)
                        {
                            if (q.options != null && idx >= 0 && idx < q.options.Count)
                            {
                                // Nếu API cần chỉ số: item.result.Add(idx.ToString());
                                // Nếu API cần text (theo ví dụ swagger): dùng text sạch
                                var txt = ExamFormat.CleanOptionText(q.options[idx]);
                                item.result.Add(txt ?? "");
                            }
                        }
                    }
                    break;

                case ExamQuestionType.ESSAY:
                    if (essayMap.TryGetValue(q.id, out var essay) && !string.IsNullOrWhiteSpace(essay))
                        item.result.Add(essay);
                    break;
            }

            if (item.result == null) item.result = new List<string>();
            body.results.Add(item);
        }

        return body;
    }
    string BuildSubmitUrl(string courseId)
    {
        string baseUrl = _examUIController.GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return null;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        return baseUrl + "lms/result-exam/" + courseId;
    }

    string BuildGetResultUrl(string courseId, bool withCorrect)
    {
        var url = BuildSubmitUrl(courseId);
        if (string.IsNullOrEmpty(url)) return null;
        if (withCorrect) url += "?mode=show_correct_answer";
        return url;
    }
    [Header("Logging / JSON export")]
    public bool saveJsonToFile = true;
    // public string jsonFolderName = "ExamLogs";
    // public bool prettyPrintJson = true;

    public IEnumerator SubmitExamCoroutine(bool timeUp)
    {
        if (!_examUIController.TryGetIds(out var examId, out var courseId))
        {
            Debug.LogError("[ExamUI] Submit failed: thiếu examId/courseId.");
            yield break;
        }

        if (_isSubmitting) yield break;
        _isSubmitting = true;

        _examUIController.ShowLoading(true);
        btnNopBai?.gameObject.SetActive(false);

        var body = BuildSubmitBody(examId);
        string json = JsonUtility.ToJson(body);
        if (_examUIController.debugVerbose) Debug.Log("[ExamUI] Submit JSON: " + json);

        if (saveJsonToFile)
        {
            var name = $"submit_{examId}_course_{_examUIController.overrideCourseId}";
            if (string.IsNullOrEmpty(_examUIController.overrideCourseId) && _examUIController.TryGetIds(out _, out var cid)) name = $"submit_{examId}_course_{cid}";
            _examUIController.SaveJsonToFile(name, json);
        }

        // PUT /lms/result-exam/{courseId}
        string submitUrl = BuildSubmitUrl(courseId);
        if (string.IsNullOrEmpty(submitUrl))
        {
            Debug.LogError("[ExamUI] Submit failed: không build được URL.");
            _isSubmitting = false;
            _examUIController.ShowLoading(false);
            yield break;
        }

        string token = _examUIController.GetAccessToken();

        using (var req = new UnityWebRequest(submitUrl, "PUT"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = Mathf.CeilToInt(Mathf.Max(1f, _examUIController.requestTimeout));
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);

            if (_examUIController.debugVerbose) Debug.Log("[ExamUI] PUT " + submitUrl);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif
            if (hasErr)
            {
                Debug.LogError($"[ExamUI] Submit ERROR: {req.responseCode} {req.error}\n{req.downloadHandler?.text}");
                _isSubmitting = false;
                _examUIController.ShowLoading(false);
                yield break;
            }
        }

        if (_examUIController.debugVerbose) Debug.Log("[ExamUI] Submit OK.");

        // GET /lms/result-exam/{courseId}?mode=show_correct_answer
        string getUrl = BuildGetResultUrl(courseId, getWithCorrectAnswer);
        if (!string.IsNullOrEmpty(getUrl))
        {
            using (var getReq = UnityWebRequest.Get(getUrl))
            {
                getReq.timeout = Mathf.CeilToInt(Mathf.Max(1f, _examUIController.requestTimeout));
                if (!string.IsNullOrEmpty(token))
                    getReq.SetRequestHeader("Authorization", "Bearer " + token);

                if (_examUIController.debugVerbose) Debug.Log("[ExamUI] GET " + getUrl);
                yield return getReq.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool hasErr2 = getReq.result != UnityWebRequest.Result.Success;
#else
                bool hasErr2 = getReq.isNetworkError || getReq.isHttpError;
#endif
                if (hasErr2)
                {
                    Debug.LogError($"[ExamUI] Get Result ERROR: {getReq.responseCode} {getReq.error}");
                }
                else
                {
                    var resultJson = getReq.downloadHandler?.text ?? "";
                    Debug.Log("[ExamUI] Result JSON:\n" + resultJson);
                    if (saveJsonToFile)
                    {
                        var name = $"result_{examId}_course_{courseId}";
                        _examUIController.SaveJsonToFile(name, resultJson);
                    }
                }
            }
        }

        _examUIController.ShowLoading(false);
        _isSubmitting = false;

        // Khoá điều hướng sau khi nộp (tuỳ chọn)
        btnBack?.gameObject.SetActive(false);
        btnNext?.gameObject.SetActive(false);
        btnNopBai?.gameObject.SetActive(false);
    }
    void RenderEssay(ExamQuestion q)
    {
        // Hướng dẫn nho nhỏ (tuỳ chọn)
        SpawnQuestionText("(Nhập câu trả lời của bạn bên dưới)");

        if (prefabCauTraLoiTuLuan == null)
        {
            SpawnQuestionText("(Thiếu prefabCauTraLoiTuLuan)");
            return;
        }

        // Khởi tạo input
        var input = Instantiate(prefabCauTraLoiTuLuan, content);

        // Điền lại nội dung nếu đã lưu trước đó
        if (essayMap.TryGetValue(q.id, out var saved))
            input.text = saved;
        else
            input.text = "";

        // Lắng nghe thay đổi để lưu realtime
        input.onValueChanged.RemoveAllListeners();
        input.onValueChanged.AddListener((val) =>
        {
            essayMap[q.id] = val ?? "";
        });
    }
        public void UpdateQuestionCounter()
    {
        if (!textQuestionCounter) return;

        int total = _examUIController.Paper?.Count ?? 0;
        if (total <= 0)
        {
            textQuestionCounter.text = "00/00";
            return;
        }

        int current = _examUIController.examStarted ? Mathf.Clamp(_examUIController.currentIndex + 1, 1, total) : 0;
        int width = total.ToString().Length;
        string left = current.ToString().PadLeft(width, '0');
        string right = total.ToString().PadLeft(width, '0');
        textQuestionCounter.text = $"{left}/{right}";
    }
}
