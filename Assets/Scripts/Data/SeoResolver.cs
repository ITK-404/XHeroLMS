using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class SeoResolver
{
    public const string DefaultScene = "dai_dao_chi_gian_1";

    public static string seoCourse;
    private static TextAsset textAsset;
    private static LmsCoursePrivate _lmsCoursePrivate;

    public static LmsCoursePrivate LmsCoursePrivate => _lmsCoursePrivate;

    // cache courseId đã resolve
    public static string lastResolvedCourseId;

    // gate result
    public static bool canEnterCourse { get; private set; } = false;

    // để UI biết có nên có private hay không
    public static bool shouldHavePrivate { get; private set; } = false;

    public static void SetSeoCourse(string scene)
    {
        seoCourse = GetSeoCourseByScene(scene);
    }

    public static string GetSeoCourseByScene(string scene, string resourceJsonName = "courses")
    {
        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogError("[SeoResolver] scene is null or empty");
            return null;
        }

        var txt = Resources.Load<TextAsset>(resourceJsonName);
        if (!txt)
        {
            Debug.LogError($"[SeoResolver] Missing Resources/{resourceJsonName}.json");
            return null;
        }

        var wrapped = "{\"items\":" + txt.text + "}";
        SceneSeoList map = null;

        try { map = JsonUtility.FromJson<SceneSeoList>(wrapped); }
        catch (Exception ex)
        {
            Debug.LogError($"[SeoResolver] Failed parsing {resourceJsonName}: {ex}");
            return null;
        }

        string sceneName = scene;
        string s1 = sceneName, s2 = $"<{sceneName}>", s3 = sceneName.Trim('<', '>'), s4 = s3.Trim();
        var candidates = new HashSet<string>(new[] { s1, s2, s3, s4 }, StringComparer.OrdinalIgnoreCase);

        SceneSeoItem item = null;
        if (map?.items != null)
        {
            foreach (var it in map.items)
            {
                if (it == null || string.IsNullOrEmpty(it.sceneName)) continue;
                var raw = it.sceneName;
                var norm = raw.Trim().Trim('<', '>');
                if (candidates.Contains(raw) || candidates.Contains(norm) || candidates.Contains($"<{norm}>"))
                {
                    item = it;
                    break;
                }
            }
        }

        if (item == null)
        {
            Debug.LogError($"[SeoResolver] No SEO mapping for scene '{sceneName}'");
            return null;
        }

        return item.seo;
    }

    /// </summary>
    public static IEnumerator LoadPrivateAndFillData()
    {
        var _seo = seoCourse;

        _lmsCoursePrivate = null;
        lastResolvedCourseId = null;

        // reset flags
        canEnterCourse = false;
        shouldHavePrivate = false;

        if (string.IsNullOrEmpty(_seo))
            yield break;

        // Resolve courseId theo seo.url
        yield return ResolveCourseIdBySeo(_seo, id => lastResolvedCourseId = id);

        if (string.IsNullOrEmpty(lastResolvedCourseId))
        {
            Debug.LogError($"[SeoResolver] Không resolve được courseId cho seo='{_seo}'");
            yield break;
        }

        string courseId = lastResolvedCourseId;

        // Lấy market course để đọc needLogin
        var market = LmsStore.Instance.GetMarketCourse(courseId);
        bool needLogin = false;

        if (market != null)
        {
            needLogin = GetNeedLoginSafe(market);
        }
        else
        {
            Debug.LogWarning($"[SeoResolver] Market course null cho courseId='{courseId}'. Fallback rule.");
            // fallback: guest -> coi như cần login (an toàn), login -> allow
            needLogin = !TokenStore.IsAuthenticated;
        }

        Debug.Log($"[SeoResolver] courseId={courseId} needLogin={needLogin} authed={TokenStore.IsAuthenticated}");

        // -------------------------
        // Guest gate
        // -------------------------
        if (!TokenStore.IsAuthenticated)
        {
            if (needLogin)
            {
                canEnterCourse = false;
                shouldHavePrivate = false;
                Debug.Log($"[SeoResolver] Guest + needLogin=true => BLOCK. seo={_seo}");
                yield break;
            }

            // allow guest
            canEnterCourse = true;
            shouldHavePrivate = false;
            Debug.Log($"[SeoResolver] Guest + needLogin=false => ALLOW (no private). seo={_seo} courseId={courseId}");
            yield break; 
        }

        // -------------------------
        // Login
        // -------------------------
        canEnterCourse = true;
        shouldHavePrivate = true;

        // Always try /free first to ensure isJoined=true for courses using this flow
        bool freeOk = true;
        bool authFail = false;

        yield return TryGrantFreeCourse(courseId, TokenStore.AccessToken,
            ok => freeOk = ok,
            isAuthFail => authFail = isAuthFail
        );

        if (!freeOk && authFail)
        {
            canEnterCourse = false;
            shouldHavePrivate = false;
            Debug.LogError($"[SeoResolver] /free auth failed => BLOCK. courseId={courseId}");
            yield break;
        }

        // fetch private
        if (LmsStore.Instance.GetPrivate(courseId) == null)
            yield return LmsStore.Instance.FetchPrivateIfExpired(courseId);

        var p = LmsStore.Instance.GetPrivate(courseId);
        if (p == null)
        {
            canEnterCourse = false;
            shouldHavePrivate = false;
            Debug.LogError($"[SeoResolver] Private null cho courseId='{courseId}' => BLOCK");
            yield break;
        }

        _lmsCoursePrivate = p;
    }

    private static IEnumerator ResolveCourseIdBySeo(string seo, Action<string> onDone)
    {
        if (onDone == null) onDone = _ => { };

        var id = LmsStore.Instance.GetCourseIdBySeo(seo);
        if (!string.IsNullOrEmpty(id))
        {
            onDone(id);
            yield break;
        }

        yield return LmsStore.Instance.FetchMarketIfExpired(0, 500, "", "", "", "");

        if (TokenStore.IsAuthenticated)
            yield return LmsStore.Instance.FetchMyCoursesIfExpired();

        id = LmsStore.Instance.GetCourseIdBySeo(seo);
        onDone(id);
    }

    private static IEnumerator TryGrantFreeCourse(string courseId, string token, Action<bool> onOk, Action<bool> onAuthFail)
    {
        if (onOk == null) onOk = _ => { };
        if (onAuthFail == null) onAuthFail = _ => { };

        token = NormalizeBearer(token);

        if (string.IsNullOrEmpty(courseId) || string.IsNullOrWhiteSpace(token))
        {
            Debug.LogError($"[SeoResolver/FREE] Missing courseId or token. courseId='{courseId}', tokenEmpty={string.IsNullOrWhiteSpace(token)}");
            onOk(false);
            onAuthFail(true);
            yield break;
        }

        string url = $"{LmsStore.Instance.baseUrl}/users/lms/courses/{courseId}/free";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            // nhiều BE cần body JSON tối thiểu
            var payload = Encoding.UTF8.GetBytes("{}");
            req.uploadHandler = new UploadHandlerRaw(payload);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("Content-Type", "application/json");

            string xData = LmsSecurityHeader.BuildXDataHeader();
            req.SetRequestHeader("x-data", xData);

            Debug.Log($"[SeoResolver/FREE] POST {url} tokenLen={token.Length}");
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                         req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            string body = req.downloadHandler != null ? req.downloadHandler.text : "";
            Debug.Log($"[SeoResolver/FREE] Status={req.responseCode} Error={req.error} Body={body}");

            // auth fail => block
            if (req.responseCode == 401 || req.responseCode == 403)
            {
                onOk(false);
                onAuthFail(true);
                yield break;
            }

            // ok codes (409: already joined)
            bool ok = !error && (req.responseCode == 200 || req.responseCode == 201 || req.responseCode == 204 || req.responseCode == 409);

            onOk(ok);
            onAuthFail(false);
        }
    }

    private static string NormalizeBearer(string raw)
    {
        var t = raw != null ? raw.Trim() : "";
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }

    private static bool GetNeedLoginSafe(LmsCourse c)
    {
        if (c == null) return false;
        try
        {
            var s = c.settings;
            if (s == null) return false;

            var f = s.GetType().GetField("needLogin");
            if (f != null && f.FieldType == typeof(bool))
                return (bool)f.GetValue(s);

            var p = s.GetType().GetProperty("needLogin");
            if (p != null && p.PropertyType == typeof(bool))
                return (bool)p.GetValue(s);

            return false;
        }
        catch { return false; }
    }

    public static bool IsContainData()
    {
        return _lmsCoursePrivate != null;
    }
}
