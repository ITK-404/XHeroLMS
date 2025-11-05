using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;                 // WebUtility.HtmlDecode
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamUIController : MonoBehaviour
{
    [Header("Input JSON (optional)")]
    [Tooltip("File JSON dump: { status, data:{ duration, questions:[ ... ] } }")]
    public TextAsset rawExamJson;

    [Header("Where to spawn")]
    public Transform content;               // nơi spawn câu hỏi + đáp án

    [Header("Prefabs")]
    public TMP_Text prefabCauHoi;           // prefab chỉ có TMP_Text
    public AnswerButton prefabCauTraLoi;    // prefab AnswerButton của bạn

    [Header("Buttons & Timer")]
    public Button btnBack;
    public Button btnNext;
    public Button btnNopBai;
    public TMP_Text textDemNguoc;

    [Header("Options")]
    public bool autoStart = true;
    public string timeFormat = "{0:00}:{1:00}"; // mm:ss

    // ===== runtime =====
    private ExamPaper paper;                // type sẵn có trong project của bạn
    private int currentIndex = 0;
    private int durationSeconds = 0;
    private Coroutine timerCo;

    // chọn của user: key = questionId, value = set index đáp án đã chọn
    private readonly Dictionary<string, HashSet<int>> selectedMap = new();

    // giữ danh sách item option đã spawn để thao tác bật/tắt nhanh
    private readonly List<AnswerButton> spawnedOptions = new();

    void Awake()
    {
        if (btnBack)   btnBack.onClick.AddListener(OnBack);
        if (btnNext)   btnNext.onClick.AddListener(OnNext);
        if (btnNopBai) btnNopBai.onClick.AddListener(OnSubmit);
    }

    void Start()
    {
        if (autoStart) InitAndRender();
    }

    [ContextMenu("Init & Render")]
    public void InitAndRender()
    {
        // 1) đọc JSON để lấy duration + mảng questions (nếu bạn nạp ở nơi khác thì bỏ qua)
        string json = rawExamJson ? rawExamJson.text : null;
        int durFromJson;
        string questionsJson = ExtractQuestionsArray(json, out durFromJson);
        durationSeconds = Mathf.Max(0, durFromJson);

        // 2) Ưu tiên ExamSession.Current nếu bạn đã parse sẵn ở nơi khác
        if (ExamSession.Current != null && ExamSession.Current.Count > 0)
        {
            paper = ExamSession.Current;
        }
        else
        {
            if (!string.IsNullOrEmpty(questionsJson))
            {
                // ❗ Fallback parse trực tiếp (không cần ExamParser)
                paper = FallbackParseToPaper(questionsJson);
                Debug.Log($"[ExamUI] Fallback parsed questions: {paper?.Count ?? 0}, duration={durationSeconds}s");
            }
            else
            {
                Debug.LogWarning("[ExamUI] No questionsJson extracted.");
                paper = new ExamPaper();
            }
        }

        // 3) render
        currentIndex = 0;
        RenderCurrentQuestion();

        if (timerCo != null) StopCoroutine(timerCo);
        timerCo = StartCoroutine(TimerCountdown());
    }

    // ===== Render =====
    void ClearContent()
    {
        if (!content) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        spawnedOptions.Clear();
    }

    void RenderCurrentQuestion()
    {
        if (paper == null || paper.questions == null || paper.questions.Count == 0)
        {
            ClearContent();
            SpawnQuestionText("(Không có câu hỏi)");
            UpdateNavButtons();
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, paper.questions.Count - 1);
        var q = paper.questions[currentIndex];

        ClearContent();

        // Câu hỏi
        SpawnQuestionText($"{currentIndex + 1}. {q.title}");

        // Loại: SINGLE / MULTI / khác
        switch (q.type)
        {
            case ExamQuestionType.SINGLE_CHOICE:
            case ExamQuestionType.MULTIPLE_CHOICE:
                RenderOptions(q);
                break;
            default:
                SpawnQuestionText($"(Type {q.type} chưa hỗ trợ UI – sẽ cập nhật sau)");
                break;
        }

        UpdateNavButtons();
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
            string optText = CleanOptionText(q.options[i]); // làm sạch <p>...</p>
            var item = Instantiate(prefabCauTraLoi, content);
            spawnedOptions.Add(item);

            // đổi skin theo loại
            if (isSingle) item.ActiveSingleChoice();
            else          item.ActiveMultipleChoice();

            bool isOn = picked.Contains(i);
            item.SetText(optText);
            item.ActiveSelect(isOn);

            int optionIndex = i;
            item.OnSelectButton = (btn) =>
            {
                if (isSingle)
                {
                    // tắt hết cái khác
                    foreach (var other in spawnedOptions)
                        if (other != btn) other.ActiveSelect(false);

                    picked.Clear();
                    bool turnOn = !btn.value; // toggle
                    btn.ActiveSelect(turnOn);
                    if (turnOn) picked.Add(optionIndex);
                }
                else
                {
                    // MULTI: đảo trạng thái phần tử được click
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

    void UpdateNavButtons()
    {
        if (btnBack) btnBack.interactable = currentIndex > 0;
        if (btnNext) btnNext.interactable = paper != null && currentIndex < paper.Count - 1;
    }

    // ===== Buttons =====
    void OnBack()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            RenderCurrentQuestion();
        }
    }

    void OnNext()
    {
        if (paper != null && currentIndex < paper.Count - 1)
        {
            currentIndex++;
            RenderCurrentQuestion();
        }
    }

    void OnSubmit()
    {
        Debug.Log($"[Exam] Submit. Questions answered: {selectedMap.Count}");
    }

    // ===== Timer =====
    IEnumerator TimerCountdown()
    {
        int remain = durationSeconds;
        while (true)
        {
            if (textDemNguoc)
            {
                int mm = Mathf.Max(0, remain) / 60;
                int ss = Mathf.Max(0, remain) % 60;
                textDemNguoc.text = string.Format(timeFormat, mm, ss);
            }
            if (remain <= 0) { OnSubmit(); yield break; }
            yield return new WaitForSeconds(1f);
            remain--;
        }
    }

    // ===== Helpers =====
    // Bóc duration và mảng questions từ JSON gốc:
    // { "status": true, "data": { "duration": 1800, "questions": [ ... ] } }
    string ExtractQuestionsArray(string raw, out int durationSec)
    {
        durationSec = 0;
        if (string.IsNullOrEmpty(raw))
        {
            if (ExamSession.Current != null) return "[]";
            return null;
        }

        try
        {
            // duration
            var durMatch = Regex.Match(raw, @"""duration""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
            if (durMatch.Success) int.TryParse(durMatch.Groups[1].Value, out durationSec);

            // questions array
            var key = "\"questions\"";
            int i = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i < 0) return null;

            int s = raw.IndexOf('[', i);
            if (s < 0) return null;

            int depth = 0;
            for (int p = s; p < raw.Length; p++)
            {
                if (raw[p] == '[') depth++;
                else if (raw[p] == ']')
                {
                    depth--;
                    if (depth == 0) return raw.Substring(s, p - s + 1);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ExamUI] ExtractQuestionsArray fail: " + ex.Message);
        }
        return null;
    }

    // Xoá HTML, đặc biệt <p>...</p> -> chỉ giữ nội dung
    string CleanOptionText(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        // thay <br> thành newline
        html = Regex.Replace(html, @"<\s*br\s*/?>", "\n", RegexOptions.IgnoreCase);
        // bỏ các thẻ p — giữ nội dung
        html = Regex.Replace(html, @"</?\s*p\s*>", "", RegexOptions.IgnoreCase);
        // bỏ mọi tag còn lại
        html = Regex.Replace(html, @"<[^>]+>", "");
        // decode entity & trim
        return WebUtility.HtmlDecode(html).Trim();
    }

    // ===== Fallback JSON parse (không phụ thuộc ExamParser) =====
    [Serializable]
    private class QuestionRaw
    {
        public string _id;
        public string title;
        public string type;                 // "SINGLE_CHOICE" | "MULTIPLE_CHOICE" | ...
        public List<string> answers;        // ["<p>...</p>", ...]
    }

    [Serializable]
    private class QuestionsWrapper
    {
        public List<QuestionRaw> questions; // dùng khi bọc {"questions":[...]}
    }

    // Nhận chuỗi mảng "[ {...}, {...} ]" hoặc đã bọc {"questions":[...]}
    private ExamPaper FallbackParseToPaper(string questionsJson)
    {
        string wrapped = questionsJson.TrimStart().StartsWith("[")
            ? "{\"questions\":" + questionsJson + "}"
            : questionsJson;

        var wrapper = JsonUtility.FromJson<QuestionsWrapper>(wrapped);
        var result = new ExamPaper { questions = new List<ExamQuestion>() };

        if (wrapper?.questions == null) return result;

        foreach (var q in wrapper.questions)
        {
            var eq = new ExamQuestion
            {
                id    = string.IsNullOrEmpty(q._id) ? Guid.NewGuid().ToString() : q._id,
                title = q.title ?? "",
                type  = ToType(q.type),
                options = new List<string>()
            };

            if (q.answers != null)
                foreach (var a in q.answers)
                    eq.options.Add(CleanOptionText(a));

            result.questions.Add(eq);
        }
        return result;
    }

    private ExamQuestionType ToType(string t)
    {
        if (string.Equals(t, "SINGLE_CHOICE", StringComparison.OrdinalIgnoreCase))
            return ExamQuestionType.SINGLE_CHOICE;
        if (string.Equals(t, "MULTIPLE_CHOICE", StringComparison.OrdinalIgnoreCase))
            return ExamQuestionType.MULTIPLE_CHOICE;

        // các loại khác tạm coi là SINGLE để hiển thị được
        return ExamQuestionType.SINGLE_CHOICE;
    }
}
