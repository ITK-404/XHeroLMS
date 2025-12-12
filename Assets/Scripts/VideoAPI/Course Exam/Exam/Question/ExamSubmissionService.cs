using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExamSubmissionService
{
    // refs
    private readonly ExamUIController _examUI;
    private readonly ExamResultUI _resultUI;
    private readonly ExamResultReviewPanel _reviewPanel;
    private readonly Func<bool> _getWithCorrectAnswerGetter;
    private readonly Dictionary<string, HashSet<int>> _selectedMap;
    private readonly Dictionary<string, string> _essayMap;
    private readonly CertificatesExamUI _certificatesExamUI;

    // dùng cho MATCHING
    private readonly ExamQuestionManager _questionManager;

    // Regex parse JSON
    private static readonly Regex ReInt = new Regex("\"(?<k>[^\"]+)\"\\s*:\\s*(-?\\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ReBool = new Regex("\"(?<k>(is_passed|passed|isPassed|pass))\"\\s*:\\s*(true|false)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ExamSubmissionService(
        ExamUIController examUI,
        ExamResultUI resultUI,
        ExamResultReviewPanel reviewPanel,
        Func<bool> getWithCorrectAnswerGetter,
        Dictionary<string, HashSet<int>> selectedMap,
        Dictionary<string, string> essayMap,
        CertificatesExamUI certificatesExamUI,
        ExamQuestionManager questionManager
    )
    {
        _examUI = examUI;
        _resultUI = resultUI;
        _reviewPanel = reviewPanel;
        _getWithCorrectAnswerGetter = getWithCorrectAnswerGetter;
        _selectedMap = selectedMap;
        _essayMap = essayMap;
        _certificatesExamUI = certificatesExamUI;
        _questionManager = questionManager;
    }

    [Serializable] public class ResultItem { public string questionId; public List<string> result; }
    [Serializable] public class SubmitBody { public string examId; public List<ResultItem> results = new(); public int timeSpent; }

    // DTO theo schema server
    [Serializable] private class QuestionNode { public string _id; public List<string> answers; public List<string> correctAnswer; public string title; public string type; }
    [Serializable] private class ExamNode { public List<QuestionNode> questions; }
    [Serializable] private class ResultExamNode { public ExamNode exam; }
    [Serializable] private class DataNode { public ResultExamNode resultExam; }
    [Serializable] private class RootNode { public bool status; public DataNode data; }

    private struct ExamResultSummary
    {
        public int correct, wrong, skipped, total;
        public bool passed;
    }

    // ===================== Public API =====================
    public IEnumerator SubmitExamCoroutine(bool timeUp)
    {
        Debug.Log($"[ExamSubmission] SubmitExamCoroutine start (timeUp={timeUp})");

        if (_examUI == null || !_examUI.TryGetIds(out var examId, out var courseId))
        {
            Debug.LogError("[ExamSubmission] Submit failed: thiếu examId/courseId hoặc _examUI = null.");
            yield break;
        }

        Debug.Log($"[ExamSubmission] examId={examId}, courseId={courseId}");

        // PUT submit
        var submitUrl = BuildSubmitUrl(courseId);
        if (string.IsNullOrEmpty(submitUrl))
        {
            Debug.LogError("[ExamSubmission] Submit failed: không build được URL submit.");
            yield break;
        }

        var body = BuildSubmitBody(examId);
        var json = JsonUtility.ToJson(body);
        var token = _examUI.GetAccessToken();

        Debug.Log($"[ExamSubmission] SUBMIT URL: {submitUrl}");
        Debug.Log("[ExamSubmission] SUBMIT PAYLOAD:\n" + json);

        using (var req = new UnityWebRequest(submitUrl, "PUT"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = Mathf.CeilToInt(Mathf.Max(1f, _examUI.requestTimeout));
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(token))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                Debug.Log("[ExamSubmission] Authorization header attached.");
            }
            else
            {
                Debug.LogWarning("[ExamSubmission] Token rỗng, vẫn gửi request nhưng có thể bị 401.");
            }

            Debug.Log("[ExamSubmission] >>> Sending PUT submit request...");
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif

            string submitResp = req.downloadHandler?.text;
            if (hasErr)
            {
                Debug.LogError($"[ExamSubmission] Submit ERROR: code={req.responseCode}, error={req.error}\nBody:\n{submitResp}");
                yield break;
            }
            else
            {
                Debug.Log($"[ExamSubmission] Submit SUCCESS: code={req.responseCode}\nBody:\n{submitResp}");
            }
        }

        // GET result (optionally với đáp án đúng)
        bool withCorrect = _getWithCorrectAnswerGetter?.Invoke() ?? false;
        var getUrl = BuildGetResultUrl(courseId, withCorrect);

        if (!string.IsNullOrEmpty(getUrl))
        {
            Debug.Log($"[ExamSubmission] Will GET result (withCorrect={withCorrect}) URL: {getUrl}");

            using (var getReq = UnityWebRequest.Get(getUrl))
            {
                getReq.timeout = Mathf.CeilToInt(Mathf.Max(1f, _examUI.requestTimeout));
                if (!string.IsNullOrEmpty(token))
                    getReq.SetRequestHeader("Authorization", "Bearer " + token);

                Debug.Log("[ExamSubmission] >>> Sending GET result request...");
                yield return getReq.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool hasErr2 = getReq.result != UnityWebRequest.Result.Success;
#else
                bool hasErr2 = getReq.isNetworkError || getReq.isHttpError;
#endif

                var resultJson = getReq.downloadHandler?.text ?? "";

                if (hasErr2)
                {
                    Debug.LogError($"[ExamSubmission] Get Result ERROR: code={getReq.responseCode}, error={getReq.error}\nBody:\n{resultJson}");
                }
                else
                {
                    Debug.Log($"[ExamSubmission] Get Result SUCCESS: code={getReq.responseCode}\nBody:\n{resultJson}");
                    ShowResultFromJson(resultJson);
                    TryOpenReview(resultJson);
                }
            }
        }
        else
        {
            Debug.Log("[ExamSubmission] Không có GET result URL (có thể withCorrect=false hoặc build URL fail). Chỉ mở review local.");
            TryOpenReview(null);
        }
    }

    // ===================== Build request body =====================
    private SubmitBody BuildSubmitBody(string examId)
    {
        var body = new SubmitBody
        {
            examId = examId,
            timeSpent = Mathf.Max(0, _examUI?._elapsedSeconds ?? 0)
        };

        var qs = _examUI?.Paper?.questions;
        if (qs == null)
        {
            Debug.LogWarning("[ExamSubmission] BuildSubmitBody: Paper null, không có câu hỏi nào.");
            return body;
        }

        Debug.Log($"[ExamSubmission] BuildSubmitBody: total questions = {qs.Count}, timeSpent = {body.timeSpent}s");

        // snapshot MATCHING (nếu có)
        Dictionary<string, Dictionary<int, int>> matchingSnapshot =
            _questionManager != null
                ? _questionManager.GetMatchingUserPairsSnapshot()
                : new Dictionary<string, Dictionary<int, int>>();

        foreach (var q in qs)
        {
            var item = new ResultItem { questionId = q.id, result = new List<string>() };

            switch (q.type)
            {
                // =================== SINGLE / MULTI ===================
                case ExamQuestionType.SINGLE_CHOICE:
                case ExamQuestionType.MULTIPLE_CHOICE:
                    if (_selectedMap.TryGetValue(q.id, out var picked))
                    {
                        foreach (var idx in picked)
                        {
                            if (q.options != null && idx >= 0 && idx < q.options.Count)
                            {
                                var txt = "<p>" + q.options[idx] + "</p>";
                                item.result.Add(txt ?? "");
                            }
                        }
                    }

                    Debug.Log($"[ExamSubmission] [SUBMIT_SINGLE/MULTI] qid={q.id}, type={q.type}, resultCount={item.result.Count}");
                    break;

                // =================== MATCHING ===================
                case ExamQuestionType.MATCHING:
                    {
                        matchingSnapshot ??= new Dictionary<string, Dictionary<int, int>>();
                        matchingSnapshot.TryGetValue(q.id, out var pairs);

                        var optLog = new StringBuilder();
                        optLog.AppendLine($"[ExamSubmission] [SubmitMatching] QID={q.id}");

                        if (q.options != null)
                        {
                            optLog.AppendLine("  options (raw):");
                            for (int i = 0; i < q.options.Count; i++)
                                optLog.AppendLine($"    options[{i}]: {q.options[i]}");
                        }
                        else
                        {
                            optLog.AppendLine("  options: NULL");
                        }

                        if (pairs != null && pairs.Count > 0)
                        {
                            GetMatchingSides(q, out var leftTexts, out var rightTexts);

                            optLog.AppendLine("  LEFT side split:");
                            for (int i = 0; i < leftTexts.Count; i++)
                                optLog.AppendLine($"    L[{i}]: {leftTexts[i]}");

                            optLog.AppendLine("  RIGHT side split:");
                            for (int i = 0; i < rightTexts.Count; i++)
                                optLog.AppendLine($"    R[{i}]: {rightTexts[i]}");

                            optLog.AppendLine($"  pairs.Count = {pairs.Count}");
                            optLog.AppendLine("  pairs (rightIndex -> leftIndex):");

                            item.result.Clear();

                            foreach (var kv in new SortedDictionary<int, int>(pairs))
                            {
                                int rightIndex = kv.Key;   // cột phải
                                int leftIndex = kv.Value;  // cột trái
                                optLog.AppendLine($"    {rightIndex} -> {leftIndex}");

                                if (leftIndex < 0 || leftIndex >= leftTexts.Count) continue;
                                if (rightIndex < 0 || rightIndex >= rightTexts.Count) continue;

                                string leftText = leftTexts[leftIndex];
                                string rightText = rightTexts[rightIndex];

                                string leftHtml = $"<p>{leftText}</p>";
                                string rightHtml = $"<p>{rightText}</p>";

                                // Format gửi sang server:
                                string pairStr = $"{leftHtml}-{rightHtml}";

                                item.result.Add(pairStr);
                            }

                            optLog.AppendLine("  item.result (submit):");
                            for (int i = 0; i < item.result.Count; i++)
                                optLog.AppendLine($"    [{i}]: {item.result[i]}");
                        }
                        else
                        {
                            optLog.AppendLine("  pairs: NULL hoặc Count == 0 (coi như chưa nối cặp)");
                        }

                        Debug.Log(optLog.ToString());
                        Debug.Log($"[ExamSubmission] [SUBMIT_MATCHING] qid={q.id}, resultCount={item.result.Count}");
                    }
                    break;

                // =================== ESSAY ===================
                case ExamQuestionType.ESSAY:
                    if (_essayMap.TryGetValue(q.id, out var essay) &&
                        !string.IsNullOrWhiteSpace(essay))
                    {
                        item.result.Add(essay);
                    }
                    Debug.Log($"[ExamSubmission] [SUBMIT_ESSAY] qid={q.id}, resultCount={item.result.Count}");
                    break;
            }

            body.results.Add(item);
        }

        // DEBUG tổng thể
        Debug.Log($"[ExamSubmission] BuildSubmitBody DONE. results.Count = {body.results.Count}");
        foreach (var item in body.results)
        {
            Debug.Log($"[ExamSubmission] [SUBMIT_RESULT_ITEM] qid={item.questionId}, count={item.result.Count}");
            for (int i = 0; i < item.result.Count; i++)
                Debug.Log($"  [{i}] {item.result[i]}");
        }

        return body;
    }

    // ===================== Result =====================
    private void ShowResultFromJson(string json)
    {
        if (_resultUI == null)
        {
            Debug.LogWarning("[ExamSubmission] ShowResultFromJson: _resultUI = null, bỏ qua hiển thị kết quả.");
            return;
        }

        Debug.Log("[ExamSubmission] ShowResultFromJson RAW JSON:\n" + json);

        // --- LOG TRẠNG THÁI SERVER TRẢ VỀ (PASS / FAIL / ĐIỂM) ---
        bool hasPassKey        = HasPassFlagInJson(json);
        bool rawPassedFromJson = TryBool(json, "is_passed", "passed", "isPassed", "pass");
        int  rawCorrectFromJson = TryInt(json, "score", "correct", "correctCount", "totalCorrect");
        int  rawTotalFromJson   = TryInt(json, "total", "totalQuestion", "questionCount", "totalQuestions");

        Debug.Log($"[ExamSubmission] SERVER JSON FLAGS -> hasPassKey={hasPassKey}, passedField={rawPassedFromJson}, scoreField={rawCorrectFromJson}, totalField={rawTotalFromJson}");

        // --- Tính summary (có thể fallback local) ---
        var sum = ParseExamResultSummary(json);

        bool needFallbackCount = (sum.correct < 0 || sum.wrong < 0);
        var paper = _examUI?.Paper;
        if (needFallbackCount && paper?.questions != null)
        {
            Debug.Log("[ExamSubmission] Server không trả đủ score/wrong -> tính fallback local...");
            var correctIndexMap = ParseCorrectAnswerIndicesFromJson(json, paper);
            var correctTextMap = ParseCorrectAnswerTextsFromJson(json);

            (int corr, int wr, int skip) = ComputeSummaryFromDetails(
                paper, _selectedMap, correctIndexMap, correctTextMap);

            sum.correct = corr;
            sum.wrong = wr;
            sum.skipped = skip;
            sum.total = paper.Count;
        }

        if (sum.total <= 0) sum.total = _examUI?.Paper?.Count ?? 0;
        if (sum.skipped < 0) sum.skipped = Mathf.Max(0, sum.total - CountAnsweredLocal());
        if (sum.correct < 0) sum.correct = 0;
        if (sum.wrong < 0) sum.wrong = Mathf.Max(0, sum.total - sum.correct - sum.skipped);

        if (!HasPassFlagInJson(json))
        {
            int req = Mathf.CeilToInt(
                sum.total * Mathf.Clamp(_examUI?.passPointPercent ?? 0, 0, 100) / 100f
            );
            sum.passed = (sum.correct >= req);
            Debug.Log($"[ExamSubmission] No 'is_passed' flag trong JSON. Local pass check: correct={sum.correct}/{sum.total}, req={req}, passed={sum.passed}");
        }
        else
        {
            Debug.Log($"[ExamSubmission] Pass flag found trong JSON. parsed passed={sum.passed}");
        }

        Debug.Log($"[ExamSubmission] FINAL SUMMARY -> correct={sum.correct}, wrong={sum.wrong}, skipped={sum.skipped}, total={sum.total}, passed={sum.passed}");

        _resultUI.textCorrectAnswer.text = $"<color=#49D17D>{sum.correct}</color> câu";
        _resultUI.textInCorrectAnswer.text = $"<color=#FF6B6B>{sum.wrong}</color> câu";
        _resultUI.skipAnswer.text = $"<color=#F4A42B>{sum.skipped}</color> câu";
        _resultUI.SetTotalAnswerPass(sum.correct, sum.total);

        if (sum.passed)
        {
            Debug.Log("[ExamSubmission] User PASSED -> ShowSuccess + mở CertificatesExamUI (nếu có).");
            _resultUI.ShowSuccess();

            if (_certificatesExamUI != null)
                _certificatesExamUI.Show();
        }
        else
        {
            Debug.Log("[ExamSubmission] User FAILED -> ShowUnSuccess + ẩn CertificatesExamUI (nếu có).");
            _resultUI.ShowUnSuccess();

            if (_certificatesExamUI != null)
                _certificatesExamUI.Hide();
        }

        _resultUI.Show();
    }

    private void TryOpenReview(string json)
    {
        if (_reviewPanel == null || _examUI?.Paper == null)
        {
            Debug.LogWarning("[ExamSubmission] TryOpenReview: _reviewPanel null hoặc Paper null, bỏ qua review.");
            return;
        }

        var paper = _examUI.Paper;

        Dictionary<string, List<int>> correctIndexMap = null;
        Dictionary<string, List<string>> correctTextMap = null;

        if (!string.IsNullOrEmpty(json))
        {
            correctIndexMap = ParseCorrectAnswerIndicesFromJson(json, paper);
            correctTextMap = ParseCorrectAnswerTextsFromJson(json);
            Debug.Log("[ExamSubmission] TryOpenReview: Dùng JSON server để parse correctIndexMap + correctTextMap.");
        }
        else
        {
            Debug.Log("[ExamSubmission] TryOpenReview: json null/empty -> chỉ review dựa trên local state.");
        }

        // MATCHING local
        Dictionary<string, Dictionary<int, int>> matchingPairs =
            _questionManager != null
                ? _questionManager.GetMatchingUserPairsSnapshot()
                : new Dictionary<string, Dictionary<int, int>>();

        _reviewPanel.ShowReview(
            paper,
            CloneUserPicked(),
            correctIndexMap,
            0,
            correctTextMap,
            _essayMap,
            matchingPairs
        );
    }

    // ===================== Helpers: URL =====================
    private string BuildSubmitUrl(string courseId)
    {
        if (_examUI == null) return null;
        var baseUrl = _examUI.GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return null;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        var url = $"{baseUrl}lms/result-exam/{courseId}";
        return url;
    }

    private string BuildGetResultUrl(string courseId, bool withCorrect)
    {
        var url = BuildSubmitUrl(courseId);
        if (string.IsNullOrEmpty(url)) return null;

        var full = withCorrect ? $"{url}?mode=show_correct_answer" : url;
        return full;
    }

    // ===================== Parse summary =====================
    private ExamResultSummary ParseExamResultSummary(string json)
    {
        var s = new ExamResultSummary
        {
            correct = TryInt(json, "score", "correct", "correctCount", "totalCorrect"),
            wrong = TryInt(json, "wrong", "incorrect", "incorrectCount", "totalWrong"),
            skipped = TryInt(json, "skip", "skipped", "not_answer", "unanswered", "skipCount"),
            total = TryInt(json, "total", "totalQuestion", "questionCount", "totalQuestions"),
            passed = TryBool(json, "is_passed", "passed", "isPassed", "pass")
        };
        return s;
    }

    private bool HasPassFlagInJson(string json) => ReBool.IsMatch(json ?? "");

    private int TryInt(string json, params string[] keys)
    {
        if (string.IsNullOrEmpty(json)) return -1;
        foreach (Match m in ReInt.Matches(json))
        {
            string k = m.Groups["k"].Value;
            foreach (var want in keys)
            {
                if (!k.Equals(want, StringComparison.OrdinalIgnoreCase)) continue;

                if (int.TryParse(m.Groups[m.Groups.Count - 1].Value, out int v)) return v;
                if (int.TryParse(m.Value.Substring(m.Value.LastIndexOf(':') + 1), out v)) return v;
            }
        }
        return -1;
    }

    private bool TryBool(string json, params string[] keys)
    {
        if (string.IsNullOrEmpty(json)) return false;
        foreach (Match m in ReBool.Matches(json))
        {
            string k = m.Groups[1].Value;
            foreach (var want in keys)
                if (k.Equals(want, StringComparison.OrdinalIgnoreCase))
                    return m.Value.Contains("true", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    // ===================== Parse correct answers =====================
    private Dictionary<string, List<string>> ParseCorrectAnswerTextsFromJson(string json)
    {
        var map = new Dictionary<string, List<string>>();
        if (string.IsNullOrEmpty(json)) return map;

        try
        {
            var root = JsonUtility.FromJson<RootNode>(json);
            var qs = root?.data?.resultExam?.exam?.questions;
            if (qs == null) return map;

            foreach (var q in qs)
                map[q._id] = q.correctAnswer != null ? new List<string>(q.correctAnswer) : new List<string>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ExamSubmission] ParseCorrectAnswerTextsFromJson failed: {e.Message}");
        }
        return map;
    }

    private Dictionary<string, List<int>> ParseCorrectAnswerIndicesFromJson(string json, ExamPaper paper)
    {
        var map = new Dictionary<string, List<int>>();
        if (string.IsNullOrEmpty(json) || paper?.questions == null) return map;

        try
        {
            var root = JsonUtility.FromJson<RootNode>(json);
            var serverQs = root?.data?.resultExam?.exam?.questions;
            if (serverQs == null) return map;

            foreach (var sq in serverQs)
            {
                var clientQ = paper.questions.Find(x => x.id == sq._id);
                if (clientQ?.options == null || sq.correctAnswer == null) continue;

                var correctNorms = new HashSet<string>();
                foreach (var s in sq.correctAnswer)
                {
                    var cleaned = (ExamFormat.CleanOptionText(s) ?? "").Trim();
                    if (!string.IsNullOrEmpty(cleaned))
                        correctNorms.Add(ExamResultReviewPanel.NormalizeForCompare(cleaned));
                }

                var idxs = new List<int>();
                for (int i = 0; i < clientQ.options.Count; i++)
                {
                    string optClean = ExamFormat.CleanOptionText(clientQ.options[i]) ?? "";
                    string optNorm = ExamResultReviewPanel.NormalizeForCompare(optClean);
                    if (correctNorms.Contains(optNorm)) idxs.Add(i);
                }

                map[sq._id] = idxs;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ExamSubmission] ParseCorrectAnswerIndicesFromJson failed: {e.Message}");
        }

        return map;
    }

    // ===================== Summary compute =====================
    private (int correct, int wrong, int skipped) ComputeSummaryFromDetails(
        ExamPaper paper,
        Dictionary<string, HashSet<int>> userPicked,
        Dictionary<string, List<int>> correctIndexMap,
        Dictionary<string, List<string>> correctTextMap)
    {
        int c = 0, w = 0, s = 0;

        // Chuẩn hóa correctTextMap thành set để so sánh
        var correctTextSets = new Dictionary<string, HashSet<string>>();
        if (correctTextMap != null)
        {
            foreach (var kv in correctTextMap)
            {
                var set = new HashSet<string>();
                var list = kv.Value ?? new List<string>();
                foreach (var t in list)
                {
                    var cleaned = (ExamFormat.CleanOptionText(t) ?? "");
                    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
                    if (!string.IsNullOrEmpty(cleaned))
                        set.Add(ExamResultReviewPanel.NormalizeForCompare(cleaned));
                }
                correctTextSets[kv.Key] = set;
            }
        }

        // snapshot MATCHING một lần để dùng trong vòng lặp
        Dictionary<string, Dictionary<int, int>> matchingSnapshot =
            _questionManager != null
                ? _questionManager.GetMatchingUserPairsSnapshot()
                : new Dictionary<string, Dictionary<int, int>>();

        foreach (var q in paper.questions)
        {
            // ===== ESSAY & "essay-like" =====
            userPicked.TryGetValue(q.id, out var uSet);

            bool hasEssayAnswer =
                _essayMap != null &&
                _essayMap.TryGetValue(q.id, out var essayTxt) &&
                !string.IsNullOrWhiteSpace(essayTxt);

            bool isEssayLike = (q.type == ExamQuestionType.ESSAY) || hasEssayAnswer;

            if (isEssayLike)
            {
                if (hasEssayAnswer) c++;
                else s++;
                continue;
            }

            // ===== MATCHING =====
            if (q.type == ExamQuestionType.MATCHING)
            {
                matchingSnapshot ??= new Dictionary<string, Dictionary<int, int>>();
                matchingSnapshot.TryGetValue(q.id, out var userPairs);

                if (userPairs == null || userPairs.Count == 0)
                {
                    s++;
                    continue;
                }

                // Xây tập user answer text theo cùng format (dùng "-" ở đây để normalize giống server nếu cần)
                GetMatchingSides(q, out var leftSide, out var rightSide);

                var userSet = new HashSet<string>(); // normalized pair
                foreach (var kv in userPairs)
                {
                    int rightIndex = kv.Key;
                    int leftIndex = kv.Value;

                    if (leftIndex < 0 || leftIndex >= leftSide.Count) continue;
                    if (rightIndex < 0 || rightIndex >= rightSide.Count) continue;

                    string leftText = leftSide[leftIndex];
                    string rightText = rightSide[rightIndex];

                    string pairStr = $"<p>{leftText}</p>-<p>{rightText}</p>";
                    string cleaned = (ExamFormat.CleanOptionText(pairStr) ?? "");
                    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
                    if (!string.IsNullOrEmpty(cleaned))
                        userSet.Add(ExamResultReviewPanel.NormalizeForCompare(cleaned));
                }

                if (!correctTextSets.TryGetValue(q.id, out var corrSet) || corrSet == null || corrSet.Count == 0)
                {
                    // không có đáp án đúng từ server, tạm coi như user có trả lời => correct
                    if (userSet.Count > 0) c++;
                    else s++;
                }
                else
                {
                    bool equal = userSet.SetEquals(corrSet);
                    if (userSet.Count == 0) s++;
                    else if (equal) c++;
                    else w++;
                }

                continue;
            }

            // ===== Các loại trắc nghiệm bình thường =====
            if (uSet == null || uSet.Count == 0)
            {
                s++;
                continue;
            }

            var corrIdx = new HashSet<int>();
            if (correctIndexMap != null && correctIndexMap.TryGetValue(q.id, out var listIdx))
                foreach (var x in listIdx) corrIdx.Add(x);

            correctTextSets.TryGetValue(q.id, out var corrTxt);

            bool isCorrect = IsExactlyCorrectLocal(q, uSet, corrIdx, corrTxt);
            if (isCorrect) c++;
            else w++;
        }

        return (c, w, s);
    }

    private bool IsExactlyCorrectLocal(
        ExamQuestion q,
        HashSet<int> userSet,
        HashSet<int> correctIndexSet0Based,
        HashSet<string> correctTextSet)
    {
        if (q?.options == null) return false;
        userSet ??= new HashSet<int>();

        var combinedCorrect = new HashSet<int>();
        if (correctIndexSet0Based != null)
            foreach (var idx in correctIndexSet0Based)
                combinedCorrect.Add(idx);

        if (correctTextSet != null)
        {
            for (int i = 0; i < q.options.Count; i++)
            {
                string optClean = ExamFormat.CleanOptionText(q.options[i]) ?? "";
                string optNorm = ExamResultReviewPanel.NormalizeForCompare(optClean);
                if (correctTextSet.Contains(optNorm)) combinedCorrect.Add(i);
            }
        }

        if (userSet.Count == 0 || combinedCorrect.Count == 0) return false;
        if (userSet.Count != combinedCorrect.Count) return false;

        foreach (var v in userSet)
            if (!combinedCorrect.Contains(v)) return false;

        return true;
    }

    private int CountAnsweredLocal()
    {
        int count = 0;
        var qs = _examUI?.Paper?.questions;
        if (qs == null) return 0;

        // snapshot matching 1 lần
        Dictionary<string, Dictionary<int, int>> matchingSnapshot =
            _questionManager != null
                ? _questionManager.GetMatchingUserPairsSnapshot()
                : new Dictionary<string, Dictionary<int, int>>();

        foreach (var q in qs)
        {
            if (IsAnsweredLocal(q.id, q.type, matchingSnapshot))
                count++;
        }

        return count;
    }

    private bool IsAnsweredLocal(string qid, ExamQuestionType type,
        Dictionary<string, Dictionary<int, int>> matchingSnapshot)
    {
        // Nếu có bài tự luận => coi như đã trả lời
        if (_essayMap != null &&
            _essayMap.TryGetValue(qid, out var txt) &&
            !string.IsNullOrWhiteSpace(txt))
            return true;

        return type switch
        {
            ExamQuestionType.SINGLE_CHOICE
                or ExamQuestionType.MULTIPLE_CHOICE
                => _selectedMap.TryGetValue(qid, out var set) && set != null && set.Count > 0,

            ExamQuestionType.MATCHING
                => matchingSnapshot != null &&
                   matchingSnapshot.TryGetValue(qid, out var pairs) &&
                   pairs != null && pairs.Count > 0,

            ExamQuestionType.ESSAY
                => _essayMap.TryGetValue(qid, out var txt2) && !string.IsNullOrWhiteSpace(txt2),

            _ => false
        };
    }

    private Dictionary<string, HashSet<int>> CloneUserPicked()
    {
        var copy = new Dictionary<string, HashSet<int>>();
        foreach (var kv in _selectedMap)
            copy[kv.Key] = new HashSet<int>(kv.Value);
        return copy;
    }

    // ===================== MATCHING helpers =====================
    // raw: "<p>Kim</p>-<p>Thủy</p>-<p>Mộc</p>-..."
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

    private static void GetMatchingSides(ExamQuestion q, out List<string> left, out List<string> right)
    {
        left = new List<string>();
        right = new List<string>();

        if (q.options == null || q.options.Count < 2) return;

        left = SplitMatchingSideRaw(q.options[0]);  // cột trái
        right = SplitMatchingSideRaw(q.options[1]); // cột phải
    }
}
