using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[DefaultExecutionOrder(-1000)]
public class LmsStore : MonoBehaviour
{
    #region Singleton
    static LmsStore _instance;
    public static LmsStore Instance
    {
        get
        {
            if (_instance) return _instance;
            var go = new GameObject(nameof(LmsStore));
            _instance = go.AddComponent<LmsStore>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }
    #endregion

    [Header("API")]
    [NonSerialized] public string baseUrl;

    [Header("Caching")]
    public bool autoLoadOnAwake = true;
    public bool autoSaveAfterFetch = true;
    public bool prettyPrintJson = true;
    [Tooltip("TTL cho /lms/courses (giây)")]
    public int ttlMarketSeconds = 300;
    [Tooltip("TTL cho /users/lms/courses (giây)")]
    public int ttlMyCoursesSeconds = 120;
    [Tooltip("TTL cho /lms/courses/{id}/private (giây)")]
    public int ttlPrivateSeconds = 300;

    [Header("QR Login (step1 cache)")]
    [Tooltip("Code từ /auth-for-lms/request step=1")]
    public string lastLoginQrCode;

    [Tooltip("Timestamp từ /auth-for-lms/request step=1")]
    public string lastLoginQrTimestamp;

    public void ClearQrLoginCache()
    {
        lastLoginQrCode = null;
        lastLoginQrTimestamp = null;
    }

    [Serializable]
    public class StoreData
    {
        public string tokenUserId;
        public List<LmsCourse> marketCourses = new();
        public List<LmsCourseUser> userCourses = new();
        public List<LmsCoursePrivate> coursePrivates = new();

        // timestamps
        public long marketFetchedAtUnix;
        public long myCoursesFetchedAtUnix;
        public Dictionary<string, long> privateFetchedAt = new(); // courseId -> unix time
    }

    public StoreData Data { get; private set; } = new();

    // Index tra cứu nhanh (không serialize)
    Dictionary<string, LmsCourse> _idxMarketById = new();
    Dictionary<string, LmsCoursePrivate> _idxPrivateById = new();
    HashSet<string> _myCourseIds = new();

    string StorePath => Path.Combine(Application.persistentDataPath, $"lms_store_{TokenStore.UserID ?? "anonymous"}.json");
    static long NowUnix => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    void Awake()
    {
        if (_instance && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        baseUrl = SecurityConfig.GetBaseUrl();

        if (autoLoadOnAwake) LoadFromDisk();
        RebuildIndexes();
    }

    #region Public API (high level)

    /// <summary>Gọi cả 3 API tuần tự và lưu cache.</summary>
    public IEnumerator WarmupAll(int marketSkip = 0, int marketLimit = 50, string keyword = "", string category = "", string sortBy = "", string order = "")
    {
        if (!EnsureToken()) yield break;

        yield return FetchMarketIfExpired(marketSkip, marketLimit, keyword, category, sortBy, order);
        yield return FetchMyCoursesIfExpired();

        foreach (var uc in Data.userCourses)
        {
            var courseId = uc?.course?._id;
            if (string.IsNullOrEmpty(courseId)) continue;
            yield return FetchPrivateIfExpired(courseId);
            yield return null;
        }

        if (autoSaveAfterFetch) SaveToDisk();
        RebuildIndexes();
        Debug.Log("[LMS] WarmupAll done.");
    }

    public bool TryGetVideoLink(string courseId, out string videoLink)
    {
        videoLink = null;
        if (string.IsNullOrEmpty(courseId)) return false;
        if (_idxPrivateById.TryGetValue(courseId, out var p) && !string.IsNullOrEmpty(p.videoLink))
        {
            videoLink = p.videoLink;
            return true;
        }
        return false;
    }

    public LmsCoursePrivate GetPrivate(string courseId)
    {
        _idxPrivateById.TryGetValue(courseId, out var p);
        return p;
    }

    public LmsCourse GetMarketCourse(string courseId)
    {
        _idxMarketById.TryGetValue(courseId, out var c);
        return c;
    }

    public IReadOnlyCollection<string> GetMyCourseIds() => _myCourseIds;
    public IReadOnlyList<LmsCourseUser> GetMyCourses() => Data.userCourses;

    /// <summary>Trả về examId (có thể = "" nếu không có), ưu tiên Private -> Market -> MyCourses.</summary>
    public string GetFinalExamId(string courseId)
    {
        if (string.IsNullOrEmpty(courseId)) return "";

        if (_idxPrivateById.TryGetValue(courseId, out var p) && p != null && !string.IsNullOrEmpty(p.finalExam))
            return p.finalExam;

        if (_idxMarketById.TryGetValue(courseId, out var m) && m != null && !string.IsNullOrEmpty(m.finalExam))
            return m.finalExam;

        if (Data.userCourses != null)
        {
            foreach (var uc in Data.userCourses)
            {
                if (uc?.course?._id == courseId && !string.IsNullOrEmpty(uc.course.finalExam))
                    return uc.course.finalExam;
            }
        }
        return "";
    }

    /// <summary>Tra examId theo SEO (có thể = "" nếu không có).</summary>
    public string GetFinalExamIdBySeo(string seoUrl)
    {
        var id = GetCourseIdBySeo(seoUrl);
        return string.IsNullOrEmpty(id) ? "" : GetFinalExamId(id);
    }

    #endregion

    #region Fetchers with TTL

    public IEnumerator FetchMarketIfExpired(int skip, int limit, string keyword, string category, string sortBy, string order)
    {
        if (!IsExpired(Data.marketFetchedAtUnix, ttlMarketSeconds))
        {
            Debug.Log("[LMS] Market cache valid, skip fetch.");
            yield break;
        }

        var url = BuildMarketUrl(skip, limit, keyword, category, sortBy, order);

        // Bật withXData = true cho chắc, BE có thể check timestamp cho mọi endpoint
        yield return GET(url, body =>
        {
            var root = JsonUtility.FromJson<ListWrapper<LmsCourse>>(WrapAsListRoot(body, "data"));
            Data.marketCourses = root.items ?? new List<LmsCourse>();

            if (Data.marketCourses != null)
                foreach (var c in Data.marketCourses)
                    NormalizeFinalExam(c);

            Data.marketFetchedAtUnix = NowUnix;
            Debug.Log($"[LMS] Market fetched: {Data.marketCourses.Count} items.");
            RebuildIndexes();
            if (autoSaveAfterFetch) SaveToDisk();
        }, withXData: true);
    }

    public IEnumerator FetchMyCoursesIfExpired()
    {
        if (!IsExpired(Data.myCoursesFetchedAtUnix, ttlMyCoursesSeconds))
        {
            Debug.Log("[LMS] MyCourses cache valid, skip fetch.");
            yield break;
        }

        var url = $"{baseUrl}/users/lms/courses?skip=0&limit=200";

        // Bật withXData = true luôn
        yield return GET(url, body =>
        {
            var root = JsonUtility.FromJson<ListWrapper<LmsCourseUser>>(WrapAsListRoot(body, "data"));
            Data.userCourses = root.items ?? new List<LmsCourseUser>();

            if (Data.userCourses != null)
                foreach (var uc in Data.userCourses)
                    NormalizeFinalExam(uc?.course);

            Data.myCoursesFetchedAtUnix = NowUnix;
            Debug.Log($"[LMS] MyCourses fetched: {Data.userCourses.Count} items.");
            RebuildIndexes();
            if (autoSaveAfterFetch) SaveToDisk();
        }, withXData: true);
    }

    public IEnumerator FetchPrivateIfExpired(string courseId)
    {
        if (string.IsNullOrEmpty(courseId)) yield break;

        long fetchedAt = 0;
        if (Data.privateFetchedAt != null) Data.privateFetchedAt.TryGetValue(courseId, out fetchedAt);
        if (!IsExpired(fetchedAt, ttlPrivateSeconds) && _idxPrivateById.ContainsKey(courseId))
            yield break;

        var url = $"{baseUrl}/lms/courses/{courseId}/private";

        // ======= Ở ĐÂY TRUYỀN withXData = true =======
        yield return GET(url, body =>
        {
            LmsCoursePrivate p = null;

            try
            {
                var root = JsonUtility.FromJson<PrivateRoot>(body);
                if (root != null)
                {
                    if (root.data != null && !string.IsNullOrEmpty(root.data._id)) p = root.data;
                    else if (root.course != null && !string.IsNullOrEmpty(root.course._id)) p = root.course;
                }
            }
            catch { }

            if (p == null)
            {
                try
                {
                    var direct = JsonUtility.FromJson<LmsCoursePrivate>(body);
                    if (direct != null && !string.IsNullOrEmpty(direct._id)) p = direct;
                }
                catch { }
            }

            if (p == null)
            {
                Debug.LogError($"[LMS] Parse private FAILED for courseId='{courseId}'. Body snippet:\n{body.Substring(0, Mathf.Min(body.Length, 400))}");
                return;
            }

            NormalizeFinalExam(p);

            int idx = Data.coursePrivates.FindIndex(x => x._id == p._id);
            if (idx >= 0) Data.coursePrivates[idx] = p;
            else Data.coursePrivates.Add(p);

            if (Data.privateFetchedAt == null) Data.privateFetchedAt = new Dictionary<string, long>();
            Data.privateFetchedAt[courseId] = NowUnix;

            RebuildIndexes();
            if (autoSaveAfterFetch) SaveToDisk();
            Debug.Log($"[LMS] Private fetched OK: {courseId} ({p._id})");
        }, withXData: true); 
    }

    // hỗ trợ nhiều dạng root
    [Serializable]
    public class PrivateRoot
    {
        public bool status;
        public LmsCoursePrivate data;   // nhiều API dùng "data"
        public LmsCoursePrivate course; // có nơi dùng "course"
    }

    public string GetCourseIdBySeo(string seoUrl)
    {
        if (string.IsNullOrEmpty(seoUrl)) return null;

        // ưu tiên private (nếu đã tải) rồi đến market
        foreach (var p in Data.coursePrivates)
            if (p != null && p.seo != null && p.seo.url == seoUrl)
                return p._id;

        foreach (var c in Data.marketCourses)
            if (c != null && c.seo != null && c.seo.url == seoUrl)
                return c._id;

        // userCourses.course.seo.url
        foreach (var uc in Data.userCourses)
            if (uc?.course?.seo != null && uc.course.seo.url == seoUrl)
                return uc.course._id;

        return null;
    }

    public LmsCoursePrivate GetPrivateBySeo(string seo)
    {
        var id = GetCourseIdBySeo(seo);
        return string.IsNullOrEmpty(id) ? null : GetPrivate(id);
    }

    public bool TryGetVideoLinkBySeo(string seo, out string link)
    {
        link = null;
        var p = GetPrivateBySeo(seo);
        if (p != null && !string.IsNullOrEmpty(p.videoLink)) { link = p.videoLink; return true; }
        return false;
    }

    /// <summary>
    /// Resolve courseId theo SEO bằng cách gọi Market rồi lọc exact seo ở client.
    /// Nếu backend hỗ trợ query ?seo=... thì thay URL trong phần GET cho tối ưu.
    /// </summary>
    public IEnumerator ResolveCourseIdBySeoOnline(string seo, Action<string> onDone)
    {
        if (onDone == null) onDone = _ => { };
        onDone(null);

        if (string.IsNullOrEmpty(seo)) yield break;
        if (!EnsureToken()) yield break;

        // lấy list đủ lớn rồi lọc client
        var url = BuildMarketUrl(0, 500, "", "", "", "");

        yield return GET(url, body =>
        {
            var root = JsonUtility.FromJson<ListWrapper<LmsCourse>>(WrapAsListRoot(body, "data"));
            var list = root.items ?? new List<LmsCourse>();
            string foundId = null;

            foreach (var c in list)
            {
                if (c?.seo != null && c.seo.url == seo) { foundId = c._id; break; }
            }

            onDone(foundId);
        }, withXData: true);
    }

    /// <summary>
    /// Lấy private theo SEO (tự resolve ID nếu cần), trả về cache object.
    /// </summary>
    public IEnumerator FetchPrivateBySeo(string seo, Action<LmsCoursePrivate> onDone)
    {
        if (onDone == null) onDone = _ => { };
        onDone(null);

        string id = GetCourseIdBySeo(seo);
        if (string.IsNullOrEmpty(id))
        {
            yield return ResolveCourseIdBySeoOnline(seo, rid => id = rid);
            if (string.IsNullOrEmpty(id)) yield break;
        }

        yield return FetchPrivateIfExpired(id);
        onDone(GetPrivate(id));
    }

    #endregion

    #region HTTP

    bool EnsureToken()
    {
        if (!TokenStore.IsAuthenticated || string.IsNullOrEmpty(TokenStore.AccessToken))
        {
            Debug.LogError("[LMS] Chưa có token trong TokenStore.");
            return false;
        }
        return true;
    }
    public void RefreshBaseUrl(bool force = false)
{
    if (force)
        baseUrl = SecurityConfig.ForceRefreshAndGet();
    else
        baseUrl = SecurityConfig.GetBaseUrl();
}

// (khuyên) khi switch env: clear cache data luôn cho sạch
public void ClearAllCacheData()
{
    Data = new StoreData();
    _idxMarketById.Clear();
    _idxPrivateById.Clear();
    _myCourseIds.Clear();
}

    IEnumerator GET(string url, Action<string> onSuccess, bool withXData = false)
    {
        RefreshBaseUrl(force: false);
        // if (!EnsureToken()) yield break;

        using (var req = UnityWebRequest.Get(url))
        {
            var token = TokenStore.AccessToken;

            Debug.Log($"[LMS] BaseUrl = {baseUrl}");
            Debug.Log($"[LMS] Token (first 40 chars) = {token?.Substring(0, Mathf.Min(40, token.Length))}");
            Debug.Log($"[LMS] Token length = {token?.Length}");

            // JWT
            if (!string.IsNullOrWhiteSpace(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);

            req.SetRequestHeader("Accept", "application/json");

            // === THÊM HEADER x-data KHI CẦN ===
            if (withXData)
            {
                string cipherB64 = LmsSecurityHeader.BuildXDataHeader();
                req.SetRequestHeader("x-data", cipherB64);
                Debug.Log($"[LMS] x-data header (Base64 cipher) length={cipherB64?.Length} value={cipherB64}");
            }

            Debug.Log("[HTTP GET] " + url);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            var body = req.downloadHandler.text;

            if (error)
            {
                Debug.LogError($"[HTTP] {req.responseCode} {req.error}\nBody: {body}");
            }
            else
            {
                onSuccess?.Invoke(body);
            }
        }
    }

    string BuildMarketUrl(int skip, int limit, string keyword, string category, string sortBy, string order)
    {
        var sb = new StringBuilder($"{baseUrl}/lms/courses?skip={skip}&limit={limit}");
        if (!string.IsNullOrEmpty(keyword))  sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));
        if (!string.IsNullOrEmpty(sortBy))   sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));
        if (!string.IsNullOrEmpty(order))    sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));
        if (!string.IsNullOrEmpty(category)) sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));
        return sb.ToString();
    }

    #endregion

    #region Save/Load + Index

    public void SaveToDisk()
    {
        try
        {
            Data.tokenUserId = TokenStore.UserID ?? "";
            var json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(StorePath, json, Encoding.UTF8);
            Debug.Log("[LMS] Saved store -> " + StorePath);
        }
        catch (Exception e) { Debug.LogWarning("[LMS] Save failed: " + e.Message); }
    }

    public void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(StorePath)) { Data = new StoreData(); return; }
            var json = File.ReadAllText(StorePath, Encoding.UTF8);
            Data = JsonUtility.FromJson<StoreData>(json) ?? new StoreData();
            Debug.Log("[LMS] Loaded store : " + StorePath);
        }
        catch (Exception e) { Debug.LogWarning("[LMS] Load failed: " + e.Message); Data = new StoreData(); }

        // normalize lại dữ liệu cũ để finalExam luôn non-null
        if (Data.marketCourses != null)
            foreach (var c in Data.marketCourses) NormalizeFinalExam(c);
        if (Data.userCourses != null)
            foreach (var uc in Data.userCourses) NormalizeFinalExam(uc?.course);
        if (Data.coursePrivates != null)
            foreach (var p in Data.coursePrivates) NormalizeFinalExam(p);

        RebuildIndexes();
    }

    void RebuildIndexes()
    {
        _idxMarketById.Clear();
        _idxPrivateById.Clear();
        _myCourseIds.Clear();

        if (Data.marketCourses != null)
            foreach (var c in Data.marketCourses)
                if (c != null && !string.IsNullOrEmpty(c._id))
                    _idxMarketById[c._id] = c;

        if (Data.coursePrivates != null)
            foreach (var p in Data.coursePrivates)
                if (p != null && !string.IsNullOrEmpty(p._id))
                    _idxPrivateById[p._id] = p;

        if (Data.userCourses != null)
            foreach (var uc in Data.userCourses)
            {
                var id = uc?.course?._id;
                if (!string.IsNullOrEmpty(id)) _myCourseIds.Add(id);
            }
    }

    static bool IsExpired(long fetchedAtUnix, int ttlSeconds)
    {
        if (fetchedAtUnix <= 0) return true;
        return (NowUnix - fetchedAtUnix) > ttlSeconds;
    }

    // JsonUtility
    [Serializable]
    class ListWrapper<T> { public List<T> items; }

    static string WrapAsListRoot(string rawJson, string arrayField)
    {
        try
        {
            int i = rawJson.IndexOf($"\"{arrayField}\"");
            if (i < 0) return "{\"items\":[]}";
            int startArr = rawJson.IndexOf('[', i);
            int endArr = rawJson.LastIndexOf(']');
            if (startArr < 0 || endArr < startArr) return "{\"items\":[]}";
            var arr = rawJson.Substring(startArr, endArr - startArr + 1);
            return "{\"items\":" + arr + "}";
        }
        catch { return "{\"items\":[]}"; }
    }

    // ==== Normalize (finalExam luôn non-null) ====
    void NormalizeFinalExam(LmsCourse c)
    {
        if (c == null) return;
        if (c.settings != null && !string.IsNullOrEmpty(c.settings.finalExam))
            c.finalExam = c.settings.finalExam;
        if (c.finalExam == null) c.finalExam = ""; // ensure non-null
    }

    void NormalizeFinalExam(LmsCoursePrivate p)
    {
        if (p == null) return;
        if (p.settings != null && !string.IsNullOrEmpty(p.settings.finalExam))
            p.finalExam = p.settings.finalExam;
        if (p.finalExam == null) p.finalExam = ""; // ensure non-null
    }

    #endregion
}

#region Data Models

[Serializable]
public class LmsSettings
{
    public string finalExam;   // BE có thể trả "finalExam": "<examId>"
    public bool needLogin;
}

[Serializable]
public class LmsCourse
{
    public string _id;
    public string title;
    public string category;
    public string thumbnail;
    public float price;
    public bool isFree;
    public bool isJoined;

    public SeoInfo seo;
    public string image;

    //
    public LmsSettings settings; // giữ nguyên cấu trúc gốc nếu có
    public string finalExam;     // tiện tra cứu nhanh; luôn != null ("" nếu không có)
}

[Serializable]
public class LmsCourseUser
{
    public string _id;
    public LmsCourse course;
    public bool completed;
    public string progress;
    public string joinedAt;
}

[Serializable]
public class LmsCoursePrivate
{
    public string _id;
    public string title;
    public string description;

    public List<string> banner;        // JSON: "banner": ["url1","url2"...]
    public string introduction;        // JSON: "introduction": "<p>...</p>"
    public string videoLink;
    public List<LmsChapter> chapters;
    public SeoInfo seo;
    public string image;

    public List<LmsProduct> products;

    //
    public LmsSettings settings; // phòng khi BE trả trong private
    public string finalExam;     // luôn != null ("" nếu không có)
    // ===== ADD THESE (from private JSON) =====
    public LmsInstructor instructor;

    public int totalDuration;     // JSON: totalDuration (seconds)
    public float stars;           // JSON: stars
    public int evaluate;          // JSON: evaluate (count đánh giá)

    public long startSellTime;    // JSON: startSellTime (unix seconds). Nếu BE đôi lúc null -> đổi sang string/object.

    public List<LmsRelatedCourse> upsell;
    public LmsCoursePrice coursePrice;
}
// Chương trong private
[Serializable]
public class LmsChapter
{
    public string _id;
    public string type;            // "learn" ...
    public string chapterTitle;    // tiêu đề chương
    public List<LmsPrivateLesson> lessons;
}
[Serializable]
public class LmsInstructor
{
    public string _id;
    public string fullName;
    public string title;
    public string description;
    public int courses;
    public int learners;
}
[Serializable]
public class LmsProduct
{
    public int _id;
    public string productName;
    public string image;
    public string externalUrl;
}
[Serializable]
public class LmsPrivateLesson
{
    public string _id;
    public string title;       // JSON dùng "title"
    public string type;        // "video" | "text" ...
    public string videoLink;   // có thể có
    public string videoLink2;  // có thể có
    public string duration;
    public int progressTime = -1;
    public CompletionCondition completionCondition;
}
[Serializable]
public class LmsRelatedCourse
{
    public string _id;
    public string title;
    public string image;
    public int learners;
    public int stars;
    public bool isSelling;
    public bool isJoined;
    public string promotionText;
    public SeoInfo seo;
    public LmsCoursePrice coursePrice;
}
[Serializable]
public class LmsCoursePrice
{
    public bool isFree;
    public float originalPrice;
    public float currentPrice;
    public bool isQuotation;
    public bool isContract;
}
[Serializable]
public class CompletionCondition
{
    public string condition;
    public string percent;
}

[Serializable]
public class SeoInfo
{
    public string url;
    public string title;
    public List<string> keywords;
    public string description;
}

#endregion
