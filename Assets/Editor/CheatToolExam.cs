#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Cửa sổ cheat: tự gọi API /lms/result-exam/... để lấy đáp án,
/// sau đó hiển thị câu hiện tại + highlight đáp án đúng.
/// Không cần TextAsset, không cần script khác.
/// </summary>
public class CheatToolExam : EditorWindow
{
    // Auto find trong scene
    private ExamQuestionManager _manager;
    private ExamUIController _ui;

    // Cache đáp án: questionId -> normalized correct texts
    private readonly Dictionary<string, HashSet<string>> _correctTextMap =
        new Dictionary<string, HashSet<string>>();

    private bool _answersLoaded;
    private bool _fetchStarted;
    private UnityWebRequest _req;
    private string _status = "Chưa tải đáp án từ API.";

    // DTO theo schema /lms/result-exam
    [Serializable] private class QuestionNode { public string _id; public List<string> correctAnswer; }
    [Serializable] private class ExamNode { public List<QuestionNode> questions; }
    [Serializable] private class ResultExamNode { public ExamNode exam; }
    [Serializable] private class DataNode { public ResultExamNode resultExam; }
    [Serializable] private class RootNode { public bool status; public DataNode data; }

    [MenuItem("Window/Exam/Auto Cheat Tool (API)")]
    private static void Open()
    {
        var win = GetWindow<CheatToolExam>("Exam Cheat");
        win.Show();
    }

    private void OnEnable()
    {
        // Auto repaint liên tục khi đang Play
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
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

        // Status dòng trên
        EditorGUILayout.HelpBox(_status, _answersLoaded ? MessageType.None : MessageType.Info);
        EditorGUILayout.Space();

        DrawCurrentQuestion();
    }

    // Tự tìm manager trong scene lần đầu
    private void AutoFindManager()
    {
        if (_manager != null) return;
        _manager = FindAnyObjectByType<ExamQuestionManager>();
    }

    /// <summary>
    /// Tự động gọi /lms/result-exam/{courseId}?mode=show_correct_answer
    /// một lần, sau đó cache vào _correctTextMap.
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

            string url = $"{baseUrl}lms/result-exam/{courseId}?mode=show_correct_answer";

            var token = _ui.GetAccessToken();

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

    /// <summary>
    /// Parse JSON server trả về và lấp _correctTextMap.
    /// </summary>
    private void ParseAnswerJsonFromApi(string json)
    {
        _correctTextMap.Clear();
        _answersLoaded = false;

        if (string.IsNullOrEmpty(json))
        {
            _status = "JSON result-exam rỗng.";
            return;
        }

        try
        {
            var root = JsonUtility.FromJson<RootNode>(json);
            var qs   = root?.data?.resultExam?.exam?.questions;
            if (qs == null)
            {
                _status = "JSON không có data.resultExam.exam.questions.";
                return;
            }

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
                ? $"Đã tải {_correctTextMap.Count} câu có đáp án đúng từ API."
                : "Không tìm thấy câu nào có correctAnswer trong JSON.";
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
        HashSet<string> correctSet = null;
        _correctTextMap.TryGetValue(q.id, out correctSet);
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
            "Đáp án được lấy trực tiếp từ API /lms/result-exam/... (mode=show_correct_answer)\n" +
            "Đề random / cắt câu vẫn ok vì map theo questionId + text normalize.",
            MessageType.None);
    }
}
#endif
