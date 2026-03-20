using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PTS_SimpleCourseUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button panelBtn;
    [SerializeField] private Button directBtn;

    [Header("FX")]
    [SerializeField] private Image bgImg;

    [Header("Course UI Fields")]
    [SerializeField] private Image img_course;
    [SerializeField] private Sprite placeholderSprite;
    [SerializeField] private bool usePlaceholderWhileLoading = true;
    [SerializeField] private bool preserveAspectAfterLoad = false;

    [Header("Status UI")]
    [SerializeField] private Image img_status;

    [Header("Text UI")]
    [SerializeField] private TextMeshProUGUI txt_name;
    [SerializeField] private TextMeshProUGUI txt_rating;
    [SerializeField] private TextMeshProUGUI txt_priceDiscount;
    [SerializeField] private TextMeshProUGUI txt_priceOrigin;

    [Header("Loaders")]
    [SerializeField] private CourseDetailLoader courseDetailLoader;
    [SerializeField] private CourseReviewLoader courseReviewLoader;

    [Header("Debug")]
    [SerializeField] private bool debugBindPrice = false;

    public UnityEvent ChangeViewClicked;

    public enum StatusKey
    {
        None = 0,
        Selling,
        NotSelling,
        Zoom,
        Offline,
        Online,
        Free,
        Quotation,
        Contract
    }

    [Serializable]
    public class StatusData
    {
        public StatusKey key;
        public string text;
        public Sprite icon;
        public bool hideText;
        public bool hideIcon;
    }

    [Header("Status Config (set in Inspector)")]
    [SerializeField] private List<StatusData> statusConfigs = new();

    private readonly Dictionary<StatusKey, StatusData> _statusMap = new();

    private string _courseId;
    private string _imageUrl;
    private Coroutine _loadImgCo;
    private Coroutine _waitDataCo;

    private static bool CourseLoading = false;

    // =========================
    // IMAGE CACHE
    // =========================
    private static readonly Dictionary<string, Sprite> s_spriteCache = new();
    private static readonly Dictionary<string, Texture2D> s_textureCache = new();

    // =========================
    // DEBUG IMAGE TIMING
    // =========================
    public static bool DebugImageTiming = true;
    public static int DebugTrackFirstNImages = 10;
    public static float DebugImageMeasureStartTime = -1f;
    public static int DebugImageMeasureVersion = 0;

    private static int s_reportedVersion = -1;
    private static int s_reportedCount = 0;

    // token chống ảnh cũ ghi đè ảnh mới khi item bị reuse
    private int _bindImageToken = 0;
    private bool _reportedImageReadyForThisBind = false;

    private void Awake()
    {
        BuildStatusMap();

        if (panelBtn != null)
            panelBtn.onClick.AddListener(OnLoadImgFx);

        if (directBtn != null)
            directBtn.onClick.AddListener(OnLoadImgFx);
    }

    private void OnDisable()
    {
        if (_loadImgCo != null)
        {
            StopCoroutine(_loadImgCo);
            _loadImgCo = null;
        }

        if (_waitDataCo != null)
        {
            StopCoroutine(_waitDataCo);
            _waitDataCo = null;
        }
    }

    private void OnDestroy()
    {
        if (panelBtn != null)
            panelBtn.onClick.RemoveListener(OnLoadImgFx);

        if (directBtn != null)
            directBtn.onClick.RemoveListener(OnLoadImgFx);

        if (_loadImgCo != null)
            StopCoroutine(_loadImgCo);

        if (_waitDataCo != null)
            StopCoroutine(_waitDataCo);
    }

    private void BuildStatusMap()
    {
        _statusMap.Clear();

        if (statusConfigs == null)
            return;

        for (int i = 0; i < statusConfigs.Count; i++)
        {
            var s = statusConfigs[i];
            if (s == null) continue;

            _statusMap[s.key] = s;
        }
    }

    public void Bind(CourseListItemData course)
    {
        if (course == null)
            return;

        _courseId = course.id;
        _imageUrl = course.image;
        _bindImageToken++;
        _reportedImageReadyForThisBind = false;

        if (debugBindPrice)
        {
            Debug.Log(
                $"[PTS][Bind] title={course.title} | id={course.id} | " +
                $"cur={course.currentPrice} | org={course.originalPrice} | " +
                $"free={course.isFree} | quotation={course.isQuotation} | contract={course.isContract}"
            );
        }

        if (txt_name != null)
            txt_name.text = course.title ?? "";

        if (txt_rating != null)
        {
            float stars = course.stars;
            int count = course.evaluate;
            string starsText = stars > 0 ? stars.ToString("0.0") : "0.0";

            txt_rating.text = count <= 0
                ? starsText
                : $"{starsText} ({FormatCountCompact(count)})";
        }

        ApplyStatus(ResolveStatus(course));
        ApplyPrice(course);

        BindThumbnail(_imageUrl, _bindImageToken);
    }

    private void ApplyPrice(CourseListItemData course)
    {
        if (course == null)
            return;

        long cur = course.currentPrice;
        long org = course.originalPrice;

        if (txt_priceDiscount != null)
        {
            string curText;

            if (course.isFree)
                curText = "Miễn phí";
            else if (cur > 0)
                curText = FormatVndCompact(cur);
            else if (course.isQuotation)
                curText = "Liên hệ báo giá";
            else if (course.isContract)
                curText = "Theo hợp đồng";
            else
                curText = "—";

            txt_priceDiscount.text = $"<size=100%><color=#E95F18>{curText}</color></size>";
        }

        if (txt_priceOrigin != null)
        {
            bool canShowOriginalPrice =
                !course.isFree &&
                cur > 0 &&
                org > 0 &&
                org > cur;

            string orgText = canShowOriginalPrice ? FormatVndCompact(org) : "";

            txt_priceOrigin.text = string.IsNullOrEmpty(orgText)
                ? ""
                : $"<size=80%><s>{orgText}</s></size>";

            txt_priceOrigin.gameObject.SetActive(!string.IsNullOrEmpty(orgText));
        }
    }


    private void BindThumbnail(string url, int token)
    {
        if (img_course == null)
            return;

        if (_loadImgCo != null)
        {
            StopCoroutine(_loadImgCo);
            _loadImgCo = null;
        }

        if (usePlaceholderWhileLoading)
            img_course.sprite = placeholderSprite;
        else
            img_course.sprite = null;

        img_course.preserveAspect = preserveAspectAfterLoad;

        if (string.IsNullOrWhiteSpace(url))
            return;

        if (s_spriteCache.TryGetValue(url, out var cachedSprite) && cachedSprite != null)
        {
            ApplyLoadedSprite(cachedSprite, token, url, true);
            return;
        }

        _loadImgCo = StartCoroutine(LoadImageTo(img_course, url, token));
    }

    private void ApplyLoadedSprite(Sprite sprite, int token, string url, bool fromCache)
    {
        if (token != _bindImageToken)
            return;

        if (img_course == null || sprite == null)
            return;

        img_course.sprite = sprite;
        img_course.preserveAspect = preserveAspectAfterLoad;

        var tex = sprite.texture;
        var fitter = img_course.GetComponent<AspectRatioFitter>();
        if (fitter != null && tex != null && tex.height > 0)
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)tex.width / tex.height;
        }

        ReportImageReady(url, fromCache);
    }

    private void ReportImageReady(string url, bool fromCache)
    {
        if (_reportedImageReadyForThisBind)
            return;

        _reportedImageReadyForThisBind = true;

        if (!DebugImageTiming || DebugImageMeasureStartTime < 0f)
            return;

        if (s_reportedVersion != DebugImageMeasureVersion)
        {
            s_reportedVersion = DebugImageMeasureVersion;
            s_reportedCount = 0;
        }

        s_reportedCount++;

        float elapsed = Time.realtimeSinceStartup - DebugImageMeasureStartTime;

        if (s_reportedCount <= DebugTrackFirstNImages)
        {
            Debug.Log($"[PTS][ImageReady] #{s_reportedCount} time={elapsed:F3}s cache={fromCache} url={url}");
        }

        if (s_reportedCount == DebugTrackFirstNImages)
        {
            Debug.Log($"[PTS][First{DebugTrackFirstNImages}ImagesReady] time={elapsed:F3}s");
        }
    }

    private StatusData ResolveStatus(CourseListItemData course)
    {
        if (course == null)
            return null;

        var modeKey = ModeToStatusKey(course.learningMode);
        if (modeKey != StatusKey.None)
            return GetStatus(modeKey, course.learningMode);

        if (course.isFree)
            return GetStatus(StatusKey.Free, "Miễn phí");

        if (course.isQuotation)
            return GetStatus(StatusKey.Quotation, "Báo giá");

        if (course.isContract)
            return GetStatus(StatusKey.Contract, "Hợp đồng");

        return null;
    }

    private StatusKey ModeToStatusKey(string mode)
    {
        if (string.IsNullOrEmpty(mode))
            return StatusKey.None;

        mode = mode.Trim().ToLowerInvariant();

        if (mode.Contains("zoom")) return StatusKey.Zoom;
        if (mode.Contains("offline") || mode.Contains("onsite") || mode.Contains("on-site")) return StatusKey.Offline;
        if (mode.Contains("online")) return StatusKey.Online;

        return StatusKey.None;
    }

    private StatusData GetStatus(StatusKey key, string fallbackText)
    {
        if (_statusMap.TryGetValue(key, out var s) && s != null)
        {
            if (string.IsNullOrEmpty(s.text))
                s.text = fallbackText;

            return s;
        }

        return new StatusData
        {
            key = key,
            text = fallbackText,
            icon = null
        };
    }

    private void ApplyStatus(StatusData s)
    {
        if (img_status == null)
            return;

        if (s == null || s.icon == null)
        {
            img_status.gameObject.SetActive(false);
            return;
        }

        img_status.gameObject.SetActive(true);
        img_status.enabled = true;
        img_status.sprite = s.icon;
    }

    private void OnDirectClick()
    {
        Debug.Log($"[PTS] Direct click courseId = {_courseId}");

        if (string.IsNullOrEmpty(_courseId))
        {
            Debug.LogWarning("[PTS] courseId is null/empty");
            CourseLoading = false;
            return;
        }

        if (courseDetailLoader == null)
        {
            Debug.LogError("[PTS] CourseDetailLoader not assigned");
            CourseLoading = false;
            return;
        }

        if (courseReviewLoader == null)
        {
            Debug.LogError("[PTS] CourseReviewLoader not assigned");
            CourseLoading = false;
            return;
        }

        if (_waitDataCo != null)
            StopCoroutine(_waitDataCo);

        courseDetailLoader.Load(_courseId);
        courseReviewLoader.LoadReviews(_courseId);

        _waitDataCo = StartCoroutine(WaitAllDataThenShow(_courseId));
    }

    private IEnumerator WaitAllDataThenShow(string courseId)
    {
        float timeout = 10f;
        float t = 0f;

        while (t < timeout)
        {
            bool detailDone = IsCourseDetailLoaded(courseId);
            bool reviewDone = IsCourseReviewLoaded(courseId);

            bool detailError = !string.IsNullOrEmpty(CourseDetailStaticStore.LastError);
            bool reviewError = !string.IsNullOrEmpty(CourseReviewStaticStore.LastError);

            if (detailError)
            {
                Debug.LogWarning("[PTS] Course detail load error: " + CourseDetailStaticStore.LastError);
                CourseLoading = false;
                yield break;
            }

            if (reviewError)
            {
                Debug.LogWarning("[PTS] Course review load error: " + CourseReviewStaticStore.LastError);

                if (detailDone)
                {
                    CourseLoading = false;
                    ChangeViewClicked?.Invoke();
                    yield break;
                }
            }

            if (detailDone && reviewDone)
            {
                Debug.Log("[PTS] Detail + Review loaded successfully");
                CourseLoading = false;
                ChangeViewClicked?.Invoke();
                yield break;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[PTS] WaitAllDataThenShow timeout");

        if (IsCourseDetailLoaded(courseId))
        {
            CourseLoading = false;
            ChangeViewClicked?.Invoke();
            yield break;
        }

        CourseLoading = false;
    }

    private bool IsCourseDetailLoaded(string courseId)
    {
        return CourseDetailStaticStore.HasData
               && !CourseDetailStaticStore.IsLoading
               && CourseDetailStaticStore.CurrentCourseId == courseId
               && CourseDetailStaticStore.CurrentDetail != null;
    }

    private bool IsCourseReviewLoaded(string courseId)
    {
        return CourseReviewStaticStore.CurrentCourseId == courseId
               && !CourseReviewStaticStore.IsLoading
               && string.IsNullOrEmpty(CourseReviewStaticStore.LastError);
    }

    private void OnLoadImgFx()
    {
        if (bgImg == null)
        {
            OnDirectClick();
            return;
        }

        if (CourseLoading)
            return;

        CourseLoading = true;
        bgImg.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.Append(bgImg.DOFade(1f, 0.2f));
        seq.AppendCallback(OnDirectClick);
        seq.AppendInterval(0.2f);
        seq.Append(bgImg.DOFade(0f, 0.15f));
    }

    private IEnumerator LoadImageTo(Image target, string url, int token)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"[PTS] Load image failed: {url} | {req.error}");
                _loadImgCo = null;
                yield break;
            }

            if (token != _bindImageToken)
            {
                _loadImgCo = null;
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null || target == null)
            {
                _loadImgCo = null;
                yield break;
            }

            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            if (!s_textureCache.ContainsKey(url))
                s_textureCache[url] = tex;

            Sprite sprite;
            if (!s_spriteCache.TryGetValue(url, out sprite) || sprite == null)
            {
                sprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );

                s_spriteCache[url] = sprite;
            }

            ApplyLoadedSprite(sprite, token, url, false);
            _loadImgCo = null;
        }
    }

    private static string FormatVndCompact(long v)
    {
        if (v <= 0) return "—";
        return v.ToString("N0").Replace(",", ".") + "đ";
    }

    private static string FormatCountCompact(int n)
    {
        if (n <= 0) return "0";
        if (n < 1000) return n.ToString();
        if (n < 1_000_000) return (n / 1000f).ToString("0.#") + "k";
        return (n / 1_000_000f).ToString("0.#") + "M";
    }
}