using System;
using System.Collections;
using System.Collections.Generic;
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

        try
        {
            map = JsonUtility.FromJson<SceneSeoList>(wrapped);
        }
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

    /// <summary>
    /// Flow mới:
    /// - Guest: nếu needLogin=false thì cho vào (không bắt buộc fetch private)
    /// - Login: nếu isFree=true thì POST /users/lms/courses/{id}/free, rồi fetch private
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

        // Lấy market course để đọc needLogin / isFree
        var market = LmsStore.Instance.GetMarketCourse(courseId);
        bool needLogin = false;
        bool isFree = false;

        if (market != null)
        {
            needLogin = GetNeedLoginSafe(market);
            isFree = market.isFree;
        }
        else
        {
            Debug.LogWarning($"[SeoResolver] Market course null cho courseId='{courseId}'. Fallback rule.");
            // fallback:
            needLogin = !TokenStore.IsAuthenticated;
        }

        // Guest gate
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
            shouldHavePrivate = false; // guest không bắt buộc có private
            Debug.Log($"[SeoResolver] Guest + needLogin=false => ALLOW (no private). seo={_seo} courseId={courseId}");
            // yield break;
        }

        // Login: allow
        canEnterCourse = true;
        shouldHavePrivate = true;

        // Login + free => grant /free trước
        if (isFree)
        {
            bool ok = false;
            yield return GrantFreeCourse(courseId, TokenStore.AccessToken, done => ok = done);

            if (!ok)
            {
                canEnterCourse = false; // không grant được => block
                shouldHavePrivate = false;
                Debug.LogError($"[SeoResolver] GrantFreeCourse FAILED => BLOCK. courseId={courseId}");
                yield break;
            }
        }

        // fetch private
        if (LmsStore.Instance.GetPrivate(courseId) == null)
            yield return LmsStore.Instance.FetchPrivateIfExpired(courseId);

        var p = LmsStore.Instance.GetPrivate(courseId);
        if (p == null)
        {
            // login mà vẫn không có private => dẫn đi mua luôn (đã login sẵn bằng token)
            canEnterCourse = false;
            shouldHavePrivate = false;

            // token
            string token = TokenStore.AccessToken;
            token = (token ?? "").Trim();
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring("Bearer ".Length).Trim();

            if (string.IsNullOrWhiteSpace(token))
            {
                Debug.LogError($"[SeoResolver] Private null + token empty. courseId='{courseId}' => BLOCK");
                yield break;
            }

            string url =
                SecurityConfig.UrlWeb + "/en/thanh-toan/" +
                "?course=" + UnityWebRequest.EscapeURL(courseId) +
                "&accessToken=" + UnityWebRequest.EscapeURL(token);

            // Debug.LogWarning($"[SeoResolver] Private null => OPEN PAYMENT. courseId='{courseId}' url={url}");

            // Application.OpenURL(url);
            Debug.LogError("Check null title here");
            // WebViewTest.LoadWebView(url, "@@@@@@@@");
            WebViewTest.LoadWebView(url, "");
            yield break;
        }

        _lmsCoursePrivate = p;
    }

    private static IEnumerator ResolveCourseIdBySeo(string seo, Action<string> onDone)
    {
        if (onDone == null) onDone = _ => { };

        // ĐỪNG dùng cache nữa để tránh lẫn prod/dev
        yield return LmsStore.Instance.FetchMarketIfExpired(0, 500, "", "", "", "");

        if (TokenStore.IsAuthenticated)
            yield return LmsStore.Instance.FetchMyCoursesIfExpired();

        var id = LmsStore.Instance.GetCourseIdBySeo(seo);
        onDone(id);
    }

    private static IEnumerator GrantFreeCourse(string courseId, string token, Action<bool> onDone)
    {
        if (onDone == null) onDone = _ => { };

        token = NormalizeBearer(token);

        if (string.IsNullOrEmpty(courseId) || string.IsNullOrWhiteSpace(token))
        {
            onDone(false);
            yield break;
        }

        string url = $"{LmsStore.Instance.baseUrl}/users/lms/courses/{courseId}/free";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("Content-Type", "application/json");

            string xData = LmsSecurityHeader.BuildXDataHeader();
            req.SetRequestHeader("x-data", xData);

            Debug.Log($"[SeoResolver/FREE] POST {url}");
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                         req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            string body = req.downloadHandler.text;
            Debug.Log($"[SeoResolver/FREE] Status={req.responseCode} Error={req.error} Body={body}");

            bool ok = !error && (req.responseCode == 200 || req.responseCode == 201 || req.responseCode == 204);
            onDone(ok);
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
        catch
        {
            return false;
        }
    }

    public static bool IsContainData()
    {
        // chỉ đúng khi đã load private (login)
        return _lmsCoursePrivate != null;
    }
}