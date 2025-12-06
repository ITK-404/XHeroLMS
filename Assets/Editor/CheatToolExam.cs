#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class CheatToolExam : EditorWindow
{
    // Auto find trong scene
    private ExamQuestionManager _manager;
    private ExamUIController    _ui;

    // Cache đáp án: questionId -> normalized correct texts
    private readonly Dictionary<string, HashSet<string>> _correctTextMap =
        new Dictionary<string, HashSet<string>>();

    // Cache raw questions (nếu muốn debug JSON)
    private List<QuestionNode> _cachedQuestions;

    // Nhớ exam/course mà cache đang dùng
    private string _cachedExamId;
    private string _cachedCourseId;

    private bool            _answersLoaded;
    private bool            _fetchStarted;
    private UnityWebRequest _req;
    private string          _status = "Chưa tải đáp án từ API.";

    // =========== DTO theo các schema khác nhau của /lms/result-exam ===========

    [Serializable]
    private class QuestionNode {
        public string       _id;
        public List<string> correctAnswer;
    }

    // schema 1: data.resultExam.exam.questions
    [Serializable] private class ExamNode       { public List<QuestionNode> questions; }
    [Serializable] private class ResultExamNode { public ExamNode exam; }
    [Serializable] private class DataNode       { public ResultExamNode resultExam; }
    [Serializable] private class RootNode       { public bool status; public DataNode data; }

    // schema 2: data.exam.questions
    [Serializable] private class AltData1       { public ExamNode exam; }
    [Serializable] private class AltRoot1       { public bool status; public AltData1 data; }

    // schema 3: data.questions
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
        ClearCache();   // đóng cửa sổ là tự giải phóng RAM
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

        // Tự động gọi /lms/result-exam/{courseId}?mode=show_correct_answer nếu chưa có cache
        EnsureAnswersLoaded();

        // Status
        EditorGUILayout.HelpBox(_status, _answersLoaded ? MessageType.None : MessageType.Info);
        EditorGUILayout.Space();

        // Nút clear cache (giải phóng RAM)
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

    // Tự find manager
    private void AutoFindManager()
    {
        if (_manager != null) return;
#if UNITY_2023_1_OR_NEWER
        _manager = FindAnyObjectByType<ExamQuestionManager>();
#else
        _manager = UnityEngine.Object.FindObjectOfType<ExamQuestionManager>();
#endif
    }

    /// <summary>
    /// Xóa toàn bộ dữ liệu cache trong RAM.
    /// Gọi khi thi xong / bấm nút clear hoặc đóng cửa sổ.
    /// </summary>
    private void ClearCache()
    {
        _correctTextMap.Clear();
        _cachedQuestions = null;
        _answersLoaded   = false;
        _fetchStarted    = false;
        _cachedExamId    = null;
        _cachedCourseId  = null;

        if (_req != null)
        {
            _req.Dispose();
            _req = null;
        }

        GC.Collect();
    }

    /// <summary>
    /// Tự động gọi /lms/result-exam/{courseId}?mode=show_correct_answer một lần
    /// rồi cache toàn bộ câu hỏi + correctAnswer vào RAM.
    /// Nếu examId/courseId đổi so với cache hiện tại -> reset cache & fetch lại.
    /// </summary>
    private void EnsureAnswersLoaded()
    {
        // Lấy id hiện tại từ UI
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

        // Nếu đang cache cho exam/course cũ -> reset
        if (_cachedCourseId != null && (_cachedCourseId != courseId || _cachedExamId != examId))
        {
            ClearCache();
            _status = "Phát hiện đổi bộ đề (exam/course mới). Đang tải lại đáp án từ API...";
        }

        // Nếu đã có cache cho chính exam/course này -> thôi
        if (_answersLoaded && _cachedCourseId == courseId && _cachedExamId == examId)
            return;

        // chưa start fetch -> start luôn
        if (!_fetchStarted)
        {
            var baseUrl = _ui.GetBaseUrl();
            if (string.IsNullOrEmpty(baseUrl))
            {
                _status = "BaseUrl rỗng. Kiểm tra LmsStore / ExamUIController.GetBaseUrl().";
                return;
            }
            if (!baseUrl.EndsWith("/")) baseUrl += "/";

            // ĐÚNG Ý BẠN: dùng /lms/result-exam/{courseId}
            string url   = $"{baseUrl}lms/result-exam/{courseId}?mode=show_correct_answer";
            string token = _ui.GetAccessToken();

            _req = UnityWebRequest.Get(url);
            _req.timeout = Mathf.CeilToInt(Mathf.Max(1f, _ui.requestTimeout));
            if (!string.IsNullOrEmpty(token))
                _req.SetRequestHeader("Authorization", "Bearer " + token);

            _req.SendWebRequest();
            _fetchStarted   = true;
            _cachedCourseId = courseId;
            _cachedExamId   = examId;
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
                _status = $"Lỗi gọi API result-exam: {_req.responseCode} {_req.error}";
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
        // data.resultExam.exam.questions
        try
        {
            var root = JsonUtility.FromJson<RootNode>(json);
            var qs   = root?.data?.resultExam?.exam?.questions;
            if (qs != null && qs.Count > 0) return qs;
        }
        catch { }

        // data.exam.questions
        try
        {
            var rootAlt1 = JsonUtility.FromJson<AltRoot1>(json);
            var qs2      = rootAlt1?.data?.exam?.questions;
            if (qs2 != null && qs2.Count > 0) return qs2;
        }
        catch { }

        // data.questions
        try
        {
            var rootAlt2 = JsonUtility.FromJson<AltRoot2>(json);
            var qs3      = rootAlt2?.data?.questions;
            if (qs3 != null && qs3.Count > 0) return qs3;
        }
        catch { }

        return null;
    }

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

        Debug.Log($"[ExamCheatTool] Raw result-exam JSON:\n{json}");

        var qs = TryExtractQuestions(json);
        if (qs == null)
        {
            _status =
                "JSON không có danh sách câu hỏi (questions).\n" +
                "Có thể user chưa có result hoặc backend đổi schema.\n" +
                "Xem Console để xem JSON chi tiết.";
            return;
        }

        _cachedQuestions = qs;   // cache full đề

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
                ? $"Đã tải {_correctTextMap.Count} câu có đáp án đúng từ /result-exam (cache trong RAM)."
                : "Questions có nhưng không có trường correctAnswer.";
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
        int idx = Mathf.Clamp(_ui.currentIndex, 0, total - 1);
        var q = paper.questions[idx];

        EditorGUILayout.LabelField($"-> Câu hiện tại [{idx + 1}/{total}]", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Question ID:", q.id);
        EditorGUILayout.LabelField("Type:", q.type.ToString());
        EditorGUILayout.Space();

        var titleStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField(q.title, titleStyle);
        EditorGUILayout.Space();

        // ======== MATCHING: hiển thị cặp ghép ========
        if (q.type == ExamQuestionType.MATCHING)
        {
            // Cột trái/phải từ q.options[0], q.options[1]
            List<string> leftCol = new List<string>();
            List<string> rightCol = new List<string>();

            if (q.options != null && q.options.Count >= 2)
            {
                string leftRaw = ExamFormat.CleanOptionText(q.options[0]) ?? "";
                string rightRaw = ExamFormat.CleanOptionText(q.options[1]) ?? "";

                leftCol = SplitMatchingSideRaw(leftRaw);
                rightCol = SplitMatchingSideRaw(rightRaw);
            }

            EditorGUILayout.LabelField("Cột TRÁI:", EditorStyles.boldLabel);
            for (int i = 0; i < leftCol.Count; i++)
                EditorGUILayout.LabelField($"  L[{i}]: {leftCol[i]}");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Cột PHẢI:", EditorStyles.boldLabel);
            for (int i = 0; i < rightCol.Count; i++)
                EditorGUILayout.LabelField($"  R[{i}]: {rightCol[i]}");

            EditorGUILayout.Space();

            // Lấy đúng QuestionNode từ JSON cache để đọc correctAnswer
            QuestionNode qNode = null;
            if (_cachedQuestions != null)
                qNode = _cachedQuestions.Find(n => n != null && n._id == q.id);

            if (qNode != null && qNode.correctAnswer != null && qNode.correctAnswer.Count > 0)
            {
                EditorGUILayout.LabelField("CÁC CẶP ĐÚNG (theo API):", EditorStyles.boldLabel);

                var pairStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    fontStyle = FontStyle.Bold
                };
                pairStyle.normal.textColor = Color.green;

                foreach (var raw in qNode.correctAnswer)
                {
                    // ví dụ raw: "<p>Kim</p>-<p>Canh, Tân</p>"
                    string cleaned = ExamFormat.CleanOptionText(raw) ?? "";
                    var parts = cleaned.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length == 2)
                    {
                        string left = parts[0].Trim();
                        string right = parts[1].Trim();
                        EditorGUILayout.LabelField($"{left}  ↔  {right}", pairStyle);
                    }
                    else
                    {
                        // fallback nếu format khác
                        EditorGUILayout.LabelField(cleaned, pairStyle);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "API /result-exam không trả correctAnswer cho câu MATCHING này " +
                    "hoặc cache chưa load được.",
                    MessageType.Info);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Câu MATCHING: nối mỗi phần tử cột TRÁI với đúng phần tử cột PHẢI như list \"CÁC CẶP ĐÚNG\" ở trên.",
                MessageType.None);

            return; // đã render xong MATCHING, khỏi vẽ kiểu SINGLE/MULTI nữa
        }

        // ======== CÁC LOẠI CÂU HỎI KHÁC ========

        if (q.options == null || q.options.Count == 0)
        {
            EditorGUILayout.HelpBox("Câu tự luận – không có đáp án lựa chọn.", MessageType.Info);
            return;
        }

        // Lấy set đáp án đúng (TEXT) cho các loại thường
        _correctTextMap.TryGetValue(q.id, out var correctSet);
        correctSet ??= new HashSet<string>();

        EditorGUILayout.LabelField("Đáp án:", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        for (int i = 0; i < q.options.Count; i++)
        {
            string shown = ExamFormat.CleanOptionText(q.options[i]) ?? "";
            string norm = ExamResultReviewPanel.NormalizeForCompare(shown);

            bool isCorrect = correctSet.Contains(norm);

            var style = new GUIStyle(EditorStyles.wordWrappedLabel);
            if (isCorrect)
            {
                style.normal.textColor = Color.green;
                style.fontStyle = FontStyle.Bold;
            }

            EditorGUILayout.LabelField($"{(isCorrect ? "[OK]" : "[ ]")} {shown}", style);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Đáp án được tải 1 lần rồi cache trong RAM từ API:\n" +
            "/lms/result-exam/{courseId}?mode=show_correct_answer\n" +
            "Nếu đổi bài thi (exam/course khác) tool sẽ tự clear cache & tải lại.\n" +
            "Nhấn \"Clear Cache\" hoặc đóng cửa sổ để giải phóng RAM thủ công.",
            MessageType.None);
    }

    private static List<string> SplitMatchingSideRaw(string raw)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(raw)) return list;

        var parts = raw.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var trimmed = p.Trim();
            if (!string.IsNullOrEmpty(trimmed))
                list.Add(trimmed);
        }
        return list;
    }
}
#endif
