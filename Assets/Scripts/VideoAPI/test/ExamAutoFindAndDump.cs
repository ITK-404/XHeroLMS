using System;
using System.IO;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ExamAutoFindAndDump : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://apis-dev.xheroapp.com";

    [Header("Auth")]
    public bool useTokenFromStore = true;
    [TextArea(1,3)] public string tokenOverride = "";

    [Header("Output")]
    public bool prettyPrint = true;
    public bool openFolderAfterSave = true;

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
        // Token
        string token = GetToken();
        if (string.IsNullOrEmpty(token))
        {
            yield break;
        }
        string bearer = "Bearer " + token;
        string api = baseUrl.TrimEnd('/');

        // Warmup LmsStore -> tải MyCourses + Private + Market (nếu cần)
        yield return LmsStore.Instance.WarmupAll(0, 300, "", "", "", "");

        // Quét MyCourses để tìm course có finalExam (LmsStore đã normalize)
        string pickedCourseId = null;
        string pickedExamId = null;
        string pickedTitle = null;

        var myCourses = LmsStore.Instance.GetMyCourses();
        if (myCourses != null)
        {
            foreach (var uc in myCourses)
            {
                var c = uc?.course;
                if (c == null || string.IsNullOrEmpty(c._id)) continue;

                // finalExam đã được LmsStore chuẩn hoá từ settings.finalExam
                string fe = LmsStore.Instance.GetFinalExamId(c._id);
                if (!string.IsNullOrEmpty(fe))
                {
                    pickedCourseId = c._id;
                    pickedExamId = fe;
                    pickedTitle = string.IsNullOrEmpty(c.title) ? c._id : c.title;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(pickedCourseId) || string.IsNullOrEmpty(pickedExamId))
        {
            Debug.LogError("[ExamAuto] Không tìm thấy khóa học nào trong MyCourses có finalExam. " +
                           "Hãy kiểm tra lại dữ liệu BE (settings.finalExam) hoặc enroll vào một khóa có exam.");
            yield break;
        }

        Debug.Log($"[ExamAuto] Picked course: {pickedTitle} ({pickedCourseId}), examId={pickedExamId}");

        // Gọi đúng API: /lms/exam/{examId}/course/{courseId}
        string url = $"{api}/lms/exam/{pickedExamId}/course/{pickedCourseId}";
        string body = null; long code = 0;
        yield return HttpGet(url, bearer, (b, c) => { body = b; code = c; });

        if (code >= 400 || string.IsNullOrEmpty(body))
        {
            Debug.LogError($"[ExamAuto] HTTP {code}\n{body}");
            yield break;
        }

        // Dump full JSON
        var dir = Application.persistentDataPath;
        var fullPath = Path.Combine(dir, $"exam_{pickedCourseId}_{pickedExamId}.json");
        var output = prettyPrint ? TryPretty(body) : body;
        try { File.WriteAllText(fullPath, output, Encoding.UTF8); } catch (Exception ex) { Debug.LogWarning(ex.Message); }
        Debug.Log($"[ExamAuto] Saved full JSON -> {fullPath}");

        // Tách questions (nếu có)
        var q = ExtractArray(body, "questions");
        if (!string.IsNullOrEmpty(q))
        {
            var qPath = Path.Combine(dir, $"exam_{pickedCourseId}_{pickedExamId}_questions.json");
            var qJson = prettyPrint ? TryPretty("{\"questions\":" + q + "}") : "{\"questions\":" + q + "}";
            try { File.WriteAllText(qPath, qJson, Encoding.UTF8); } catch (Exception ex) { Debug.LogWarning(ex.Message); }
            Debug.Log($"[ExamAuto] Saved questions -> {qPath}");
        }
        else
        {
            Debug.Log("[ExamAuto] Response không có field 'questions'. Xem file full JSON.");
        }

        if (openFolderAfterSave)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + fullPath.Replace('/', '\\') + "\"");
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            System.Diagnostics.Process.Start("open", "-R \"" + fullPath + "\"");
#else
            Debug.Log("[ExamAuto] Output folder: " + dir);
#endif
        }
    }
    
    string GetToken()
    {
        if (useTokenFromStore && TokenStore.IsAuthenticated && !string.IsNullOrEmpty(TokenStore.AccessToken))
            return TokenStore.AccessToken;
        return tokenOverride?.Trim();
    }

    IEnumerator HttpGet(string url, string bearer, Action<string,long> done)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            // Một số API chấp nhận cả 'authorization' lẫn 'Authorization'
            req.SetRequestHeader("authorization", TokenStore.AccessToken ?? "");
            req.SetRequestHeader("Authorization", bearer);
            req.SetRequestHeader("Accept", "application/json");

            Debug.Log("[HTTP GET] " + url);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            var body = req.downloadHandler.text ?? "";
            var code = req.responseCode;
            if (error && string.IsNullOrEmpty(body)) body = req.error ?? "(network/protocol error)";
            done?.Invoke(body, code);
        }
    }

    // Cắt mảng JSON theo tên field (đủ dùng để debug)
    string ExtractArray(string raw, string field)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        var key = $"\"{field}\"";
        int i = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        int s = raw.IndexOf('[', i); if (s < 0) return null;
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
        return null;
    }

    string TryPretty(string raw)
    {
        try
        {
            var sb = new StringBuilder();
            int indent = 0; bool str = false; char prev = '\0';
            foreach (var ch in raw)
            {
                if (ch == '"' && prev != '\\') str = !str;
                if (!str)
                {
                    if (ch == '{' || ch == '[') { sb.Append(ch).Append('\n'); indent++; sb.Append(new string(' ', indent * 2)); prev = ch; continue; }
                    if (ch == '}' || ch == ']') { sb.Append('\n'); indent = Mathf.Max(0, indent - 1); sb.Append(new string(' ', indent * 2)).Append(ch); prev = ch; continue; }
                    if (ch == ',') { sb.Append(ch).Append('\n').Append(new string(' ', indent * 2)); prev = ch; continue; }
                    if (ch == ':') { sb.Append(": "); prev = ch; continue; }
                }
                sb.Append(ch); prev = ch;
            }
            return sb.ToString();
        }
        catch { return raw; }
    }
}
