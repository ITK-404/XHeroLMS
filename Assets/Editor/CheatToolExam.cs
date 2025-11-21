#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Cửa sổ cheat: tự gọi API /lms/result-exam/... để lấy đáp án,
/// cache toàn bộ câu hỏi + correctAnswer vào RAM,
/// hiển thị câu hiện tại + highlight đáp án đúng.
/// Đã có nút Clear cache để giải phóng RAM.
/// </summary>
public class CheatToolExam : EditorWindow
{
    // Auto find trong scene
    private ExamQuestionManager _manager;
    private ExamUIController    _ui;

    // Cache đáp án: questionId -> normalized correct texts
    private readonly Dictionary<string, HashSet<string>> _correctTextMap =
        new Dictionary<string, HashSet<string>>();

    // Cache raw question
    private List<QuestionNode> _cachedQuestions;

    private bool            _answersLoaded;
    private bool            _fetchStarted;
    private UnityWebRequest _req;
    private string          _status = "Chưa tải đáp án từ API.";

    // =========== DTO theo các schema khác nhau ===========

    [Serializable]
    private class QuestionNode {
        public string       _id;
        public List<string> correctAnswer;
    }

    [Serializable] private class ExamNode       { public List<QuestionNode> questions; }
    [Serializable] private class ResultExamNode { public ExamNode exam; }
    [Serializable] private class DataNode       { public ResultExamNode resultExam; }
    [Serializable] private class RootNode       { public bool status; public DataNode data; }

    [Serializable] private class AltData1       { public ExamNode exam; }
    [Serializable] private class AltRoot1       { public bool status; public AltData1 data; }

    [Serializable] private class AltData2       { public List<QuestionNode> questions; }
    [Serializable] private class AltRoot2       { public bool status; public AltData2 data; }

    [MenuItem("Window/Exam/Auto Cheat Tool (API)")]
    private static void Open()
    {
        var win = GetWindow<CheatToolExam>("Exam Cheat");
        win.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
        ClearCache(); // đóng cửa sổ là xóa cache để free RAM
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Auto Exam Cheat Tool (API)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Chỉ dùng được khi đang Play.", MessageType.Info);
            return;
        }

        AutoFindManager();
        if (_manager == null)
        {
            EditorGUILayout.HelpBox("Không tìm thấy ExamQuestionManager trong scene.", MessageType.Warning);
            return;
        }

        _ui = _manager._examUIController;
        if (_ui == null || _ui.Paper == null || _ui.Paper.questions == null || _ui.Paper.questions.Count == 0)
        {
            EditorGUILayout.HelpBox("ExamUIController chưa load đề (Paper null hoặc không có câu hỏi).", MessageType.Warning);
            return;
        }

        // Đảm bảo đã gọi API lấy đáp án (tự động, không cần input)
        EnsureAnswersLoaded();

        // Status
        EditorGUILayout.HelpBox(_status, _answersLoaded ? MessageType.None : MessageType.Info);
        EditorGUILayout.Space();

        // Chỉ show nút clear khi đã có cache
        using (new EditorGUI.DisabledScope(!_answersLoaded))
        {
            if (GUILayout.Button("Clear Cache (Free RAM)"))
            {
                ClearCache();
                _status = "Đã xóa cache khỏi RAM.";
            }
        }

        EditorGUILayout.Space();
        DrawCurrentQuestion();
    }

    // Tự tìm manager trong scene lần đầu
    private void AutoFindManager()
    {
        if (_manager != null) return;
#if UNITY_2023_1_OR_NEWER
        _manager = FindAnyObjectByType<ExamQuestionManager>();
#else
        _manager = FindObjectOfType<ExamQuestionManager>();
#endif
    }

    /// <summary>
    /// Xóa toàn bộ dữ liệu cache trong RAM,
    /// dùng khi thi xong / đóng cửa sổ để tránh tích tụ memory.
    /// </summary>
    private void ClearCache()
    {
        _correctTextMap.Clear();
        _cachedQuestions = null;
        _answersLoaded   = false;
        _fetchStarted    = false;

        if (_req != null)
        {
            _req.Dispose();
            _req = null;
        }

        // ép GC chạy (không bắt buộc, nhưng bạn muốn chủ động)
        GC.Collect();
    }

    /// <summary>
    /// Tự động gọi /lms/result-exam/{courseId}?mode=show_correct_answer
    /// một lần, sau đó cache vào _correctTextMap + _cachedQuestions.
    /// </summary>
    private void EnsureAnswersLoaded()
    {
        if (_answersLoaded) return;

        // chưa start fetch -> start luôn
        if (!_fetchStarted)
        {
            if (_ui == null)
            {
                _status = "Không tìm thấy ExamUIController.";
                return;
            }

            if (!_ui.TryGetIds(out var examId, out var courseId))
            {
                _status = "ExamUIController.TryGetIds() không có examId/courseId.";
                return;
            }

            var baseUrl = _ui.GetBaseUrl();
            if (string.IsNullOrEmpty(baseUrl))
            {
                _status = "BaseUrl rỗng. Kiểm tra LmsStore / ExamUIController.GetBaseUrl().";
                return;
            }
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            // Nếu backend bạn dùng endpoint khác thì chỉnh ở đây
            string url = $"{baseUrl}lms/result-exam/{courseId}?mode=show_correct_answer";
            var token  = _ui.GetAccessToken();

            _req = UnityWebRequest.Get(url);
            _req.timeout = Mathf.CeilToInt(Mathf.Max(1f, _ui.requestTimeout));
            if (!string.IsNullOrEmpty(token))
                _req.SetRequestHeader("Authorization", "Bearer " + token);

            _req.SendWebRequest();
            _fetchStarted = true;
            _status = $"Đang gọi API:\n{url}";
            return;
        }

        // Đã start fetch nhưng chưa xong
        if (_req != null && !_req.isDone)
        {
            _status = "Đang tải đáp án từ API...";
            return;
        }

        // Request đã xong mà chưa parse
        if (_req != null && _req.isDone && !_answersLoaded)
        {
#if UNITY_2020_2_OR_NEWER
            bool hasErr = _req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = _req.isNetworkError || _req.isHttpError;
#endif
            if (hasErr)
            {
                _status = $"Lỗi gọi API result-exam: { _req.responseCode } { _req.error }";
                Debug.LogError($"[ExamCheatTool] Error GET result-exam: {_req.responseCode} {_req.error}\n{_req.downloadHandler?.text}");
                _answersLoaded = false;
                return;
            }

            string json = _req.downloadHandler?.text ?? "";
            ParseAnswerJsonFromApi(json);
        }
    }

    private List<QuestionNode> TryExtractQuestions(string json)
    {
        // schema gốc: data.resultExam.exam.questions
        try
        {
            var root = JsonUtility.FromJson<RootNode>(json);
            var qs   = root?.data?.resultExam?.exam?.questions;
            if (qs != null && qs.Count > 0) return qs;
        }
        catch { /* bỏ qua, thử schema khác */ }

        // schema: data.exam.questions
        try
        {
            var rootAlt1 = JsonUtility.FromJson<AltRoot1>(json);
            var qs2      = rootAlt1?.data?.exam?.questions;
            if (qs2 != null && qs2.Count > 0) return qs2;
        }
        catch { }

        // schema: data.questions
        try
        {
            var rootAlt2 = JsonUtility.FromJson<AltRoot2>(json);
            var qs3      = rootAlt2?.data?.questions;
            if (qs3 != null && qs3.Count > 0) return qs3;
        }
        catch { }

        return null;
    }

    /// <summary>
    /// Parse JSON server trả về, cache toàn bộ questions + map correctAnswer.
    /// </summary>
    private void ParseAnswerJsonFromApi(string json)
    {
        _correctTextMap.Clear();
        _cachedQuestions = null;
        _answersLoaded   = false;

        if (string.IsNullOrEmpty(json))
        {
            _status = "JSON result-exam rỗng.";
            return;
        }

        // Debug: in full JSON để check khi fail
        Debug.Log($"[ExamCheatTool] Raw result-exam JSON:\n{json}");

        List<QuestionNode> qs = TryExtractQuestions(json);
        if (qs == null)
        {
            _status =
                "JSON không có danh sách câu hỏi (questions).\n" +
                "Có thể user này chưa có result hoặc backend đổi schema.\n" +
                "Xem Console để xem JSON chi tiết.";
            return;
        }

        _cachedQuestions = qs; // lưu toàn bộ vào RAM để xài lại

        try
        {
            foreach (var q in qs)
            {
                if (q == null || string.IsNullOrEmpty(q._id)) continue;

                var set  = new HashSet<string>();
                var list = q.correctAnswer ?? new List<string>();

                foreach (var ans in list)
                {
                    string cleaned = ExamFormat.CleanOptionText(ans) ?? "";
                    cleaned = cleaned.Trim();
                    if (string.IsNullOrEmpty(cleaned)) continue;

                    string norm = ExamResultReviewPanel.NormalizeForCompare(cleaned);
                    set.Add(norm);
                }

                _correctTextMap[q._id] = set;
            }

            _answersLoaded = _correctTextMap.Count > 0;
            _status = _answersLoaded
                ? $"Đã tải {_correctTextMap.Count} câu có đáp án đúng từ API (cache trong RAM)."
                : "Không thấy correctAnswer trong JSON (questions có nhưng không có trường correctAnswer).";
        }
        catch (Exception e)
        {
            _status = "Parse JSON result-exam thất bại (xem Console).";
            Debug.LogError($"[ExamCheatTool] ParseAnswerJsonFromApi FAILED: {e.Message}");
        }
    }

    private void DrawCurrentQuestion()
    {
        var paper = _ui.Paper;
        int total = paper.questions.Count;
        int idx   = Mathf.Clamp(_ui.currentIndex, 0, total - 1);
        var q     = paper.questions[idx];

        EditorGUILayout.LabelField($"-> Câu hiện tại [{idx + 1}/{total}]", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Question ID:", q.id);
        EditorGUILayout.LabelField("Type:", q.type.ToString());
        EditorGUILayout.Space();

        var titleStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize  = 13,
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField(q.title, titleStyle);
        EditorGUILayout.Space();

        if (q.options == null || q.options.Count == 0)
        {
            EditorGUILayout.HelpBox("Câu tự luận – không có đáp án lựa chọn.", MessageType.Info);
            return;
        }

        // Lấy set đáp án đúng cho câu này (nếu API đã trả)
        _correctTextMap.TryGetValue(q.id, out var correctSet);
        correctSet ??= new HashSet<string>();

        EditorGUILayout.LabelField("Đáp án:", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        for (int i = 0; i < q.options.Count; i++)
        {
            string shown = ExamFormat.CleanOptionText(q.options[i]) ?? "";
            string norm  = ExamResultReviewPanel.NormalizeForCompare(shown);

            bool isCorrect = correctSet.Contains(norm);

            var style = new GUIStyle(EditorStyles.wordWrappedLabel);
            if (isCorrect)
            {
                style.normal.textColor = Color.green;
                style.fontStyle        = FontStyle.Bold;
            }

            EditorGUILayout.LabelField($"{(isCorrect ? "[OK]" : "[ ]")} {shown}", style);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Đáp án được tải 1 lần rồi cache trong RAM từ API /lms/result-exam/... (mode=show_correct_answer).\n" +
            "Nhấn \"Clear Cache\" để xóa khỏi RAM sau khi thi xong.",
            MessageType.None);
    }
}
#endif
