using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class ExamAutoFindAndDump : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://apis-dev.xheroapp.com";

    [Header("Options")]
    public bool showCorrectAnswer = false;

    [Tooltip("Ghi token vào JSON debug (bật theo yêu cầu test).")]
    public bool includeTokenInDump = true;

    [Tooltip("Tự copy 'Bearer <token>' vào clipboard sau khi lấy token.")]
    public bool copyBearerToClipboard = true;

    [Header("Auth")]
    public bool useTokenFromStore = true;
    [TextArea(1,4)] public string tokenOverride = "";

    [Header("Run")]
    public bool autoRunOnStart = true;

    void Start()
    {
        if (autoRunOnStart) StartCoroutine(Run());
    }

    [ContextMenu("Run Auto Exam Fetch")]
    public void RunNow() => StartCoroutine(Run());

    IEnumerator Run()
    {
        // --- TOKEN ---
        string token = tokenOverride;
        if (useTokenFromStore)
        {
            try
            {
                var p = typeof(TokenStore).GetProperty("AccessToken");
                if (p != null) token = (string)p.GetValue(null, null);
            } catch {}
        }
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("[ExamAuto] Missing token. Login or paste tokenOverride.");
            yield break;
        }

        string api = baseUrl.TrimEnd('/');
        string bearer = "Bearer " + token;

        // Log chuỗi để dùng ngay trên Swagger / Postman
        Debug.Log("[ExamAuto] === Authorization header for Swagger / Postman ===");
        Debug.Log("Header name: Authorization");
        Debug.Log("Header value: " + bearer); // <- dán vào ô 'authorization' của Swagger
        Debug.Log("[ExamAuto] ================================================");

        // Copy clipboard (tuỳ chọn)
        if (copyBearerToClipboard)
        {
            GUIUtility.systemCopyBuffer = bearer;
            Debug.Log("[ExamAuto] Copied to clipboard: " + bearer);
        }

        // --- 1) GET my-courses ---
        string myUrl = $"{api}/users/lms/courses?skip=0&limit=500";
        string myBody = null; long myCode = 0;
        yield return HttpGet(myUrl, bearer, (body, code) => { myBody = body; myCode = code; });
        if (myCode >= 400 || string.IsNullOrEmpty(myBody))
        {
            Debug.LogError($"[ExamAuto] Cannot load my-courses. HTTP {myCode}\n{myBody}");
            yield break;
        }

        // Trích danh sách courseId
        var courseIds = ExtractCourseIds(myBody);
        if (courseIds.Count == 0)
        {
            Debug.LogError("[ExamAuto] No course found in my-courses.");
            yield break;
        }

        // Dump JSON: token + courseIds (+ raw my-courses)
        var dumpJson = BuildDumpJson(
            includeTokenInDump ? token : null,
            includeTokenInDump ? bearer : null,
            courseIds,
            api
        );
        Debug.Log("[ExamAuto] DEBUG DUMP JSON:\n" + dumpJson);
        var dumpPath = System.IO.Path.Combine(Application.persistentDataPath, "exam_debug_info.json");
        System.IO.File.WriteAllText(dumpPath, dumpJson, Encoding.UTF8);
        Debug.Log("[ExamAuto] Saved dump: " + dumpPath);

        var rawPath = System.IO.Path.Combine(Application.persistentDataPath, "my_courses_raw.json");
        System.IO.File.WriteAllText(rawPath, myBody, Encoding.UTF8);
        Debug.Log("[ExamAuto] Saved my-courses raw: " + rawPath);

        // --- 2) Quét từng course để cố lấy đề ---
        // 2a) Ưu tiên course có finalExam._id trong /lms/courses/{id}
        string pickedCourseId = null;
        string pickedExamId = null;

        foreach (var cid in courseIds)
        {
            string detailUrl = $"{api}/lms/courses/{cid}";
            string detailBody = null; long detailCode = 0;
            yield return HttpGet(detailUrl, bearer, (body, code) => { detailBody = body; detailCode = code; });

            if (detailCode < 400 && !string.IsNullOrEmpty(detailBody))
            {
                var m = Regex.Match(detailBody, "\"finalExam\"\\s*:\\s*\\{[^}]*?\"_id\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Singleline);
                if (m.Success)
                {
                    pickedCourseId = cid;
                    pickedExamId = m.Groups[1].Value;
                    Debug.Log($"[ExamAuto] Found finalExam: course={pickedCourseId}, examId={pickedExamId}");
                    break;
                }
            }
        }

        // 2b) Nếu có examId → mở phiên thi (không bắt buộc thành công)
        if (!string.IsNullOrEmpty(pickedCourseId) && !string.IsNullOrEmpty(pickedExamId))
        {
            string openUrl = $"{api}/lms/exam/{pickedExamId}/course/{pickedCourseId}";
            string openBody = null; long openCode = 0;
            yield return HttpGet(openUrl, bearer, (body, code) => { openBody = body; openCode = code; });
            if (openCode >= 400) Debug.LogWarning($"[ExamAuto] Open session HTTP {openCode}\n{openBody}");
        }

        // 2c) Lấy đề: nếu đã pick được course có exam → thử result-exam trước với course đó
        // Nếu không thành công, fallback: thử gọi thẳng /lms/result-exam cho TỪNG course trong list.
        if (!string.IsNullOrEmpty(pickedCourseId))
        {
            bool ok = false;
            yield return TryFetchQuestions(api, bearer, pickedCourseId, showCorrectAnswer, success => ok = success);
            if (ok) yield break; // đã lấy được đề và đã lưu file
        }

        // 2d) Fallback mạnh tay: thử lần lượt tất cả course bằng /lms/result-exam/{courseId}
        foreach (var cid in courseIds)
        {
            bool ok = false;
            yield return TryFetchQuestions(api, bearer, cid, showCorrectAnswer, success => ok = success);
            if (ok) yield break;
        }

        Debug.LogError("[ExamAuto] No exam questions fetched. Likely no course is configured for exams or backend blocks start.");
    }

    // ----- Try fetch questions for a courseId -----
    IEnumerator TryFetchQuestions(string api, string bearer, string courseId, bool showCorrect, Action<bool> done)
    {
        string resultUrl = $"{api}/lms/result-exam/{courseId}";
        if (showCorrect) resultUrl += "?mode=show_correct_answer";

        string qBody = null; long qCode = 0;
        yield return HttpGet(resultUrl, bearer, (body, code) => { qBody = body; qCode = code; });

        Debug.Log($"[ExamAuto] result-exam ({courseId}) HTTP {qCode}");
        if (qCode >= 400 || string.IsNullOrEmpty(qBody))
        {
            done?.Invoke(false);
            yield break;
        }

        // Đã có JSON (tùy BE, có thể là questions). Lưu lại cho bạn kiểm tra.
        Debug.Log("[ExamAuto] QUESTIONS(?) JSON:\n" + qBody);
        var path = System.IO.Path.Combine(Application.persistentDataPath, $"exam_{courseId}.json");
        try { System.IO.File.WriteAllText(path, qBody, Encoding.UTF8); Debug.Log("[ExamAuto] Saved: " + path); }
        catch (Exception ex) { Debug.LogWarning("[ExamAuto] Save failed: " + ex.Message); }

        done?.Invoke(true);
    }

    // ----- HTTP helper -----
    IEnumerator HttpGet(string url, string bearerHeader, Action<string,long> onDone)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            // CHỈ cần Authorization: Bearer <token>
            req.SetRequestHeader("Authorization", bearerHeader);
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                         req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            string body = req.downloadHandler.text ?? "";
            long code = req.responseCode;
            if (error && string.IsNullOrEmpty(body)) body = req.error ?? "(network/protocol error)";
            onDone?.Invoke(body, code);
        }
    }

    // ----- Extract courseIds from /users/lms/courses -----
    List<string> ExtractCourseIds(string json)
    {
        var ids = new List<string>();
        // tìm "course": { "_id": "<id>" }
        var itemRegex = new Regex("\"course\"\\s*:\\s*\\{(.*?)\\}", RegexOptions.Singleline);
        foreach (Match m in itemRegex.Matches(json))
        {
            string obj = "{" + m.Groups[1].Value + "}";
            var idMatch = Regex.Match(obj, "\"_id\"\\s*:\\s*\"([^\"]+)\"");
            if (idMatch.Success)
            {
                string id = idMatch.Groups[1].Value;
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
        }
        return ids;
    }

    // ----- Build simple JSON dump (token + bearer + courseIds + curl) -----
    string BuildDumpJson(string accessToken, string bearerHeader, List<string> courseIds, string api)
    {
        // lệnh curl mẫu (thay <courseId> để test)
        string curlExample = $"curl -X GET \"{api}/lms/result-exam/<courseId>\" -H \"Authorization: {Escape(bearerHeader ?? "Bearer <ACCESS_TOKEN>")}\" -H \"Accept: application/json\"";

        var sb = new StringBuilder();
        sb.Append("{");

        if (!string.IsNullOrEmpty(accessToken))
        {
            sb.Append("\"accessToken\":\"").Append(Escape(accessToken)).Append("\",");
        }
        if (!string.IsNullOrEmpty(bearerHeader))
        {
            sb.Append("\"authorizationHeader\":\"").Append(Escape(bearerHeader)).Append("\",");
        }

        sb.Append("\"courseIds\":[");
        for (int i = 0; i < courseIds.Count; i++)
        {
            if (i > 0) sb.Append(",");
            sb.Append("\"").Append(Escape(courseIds[i])).Append("\"");
        }
        sb.Append("],");

        sb.Append("\"curlExample\":\"").Append(Escape(curlExample)).Append("\"");

        sb.Append("}");
        return sb.ToString();
    }

    string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
