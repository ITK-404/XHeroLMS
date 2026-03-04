using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
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

    [Header("Status UI")]
    [SerializeField] private Image img_status;              // icon trạng thái

    [Header("Text UI")]
    [SerializeField] private TextMeshProUGUI txt_name;
    [SerializeField] private TextMeshProUGUI txt_rating;
    [SerializeField] private TextMeshProUGUI txt_priceDiscount;
    [SerializeField] private TextMeshProUGUI txt_priceOrigin;

    [SerializeField] private CourseDetailLoader courseDetailLoader;

    // ===== Status mapping =====
    public enum StatusKey
    {
        None = 0,

        // Selling state
        Selling,
        NotSelling,

        // Modes
        Zoom,
        Offline,
        Online,

        // Special price states        Free,
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

    private void Awake()
    {
        BuildStatusMap();

        if (panelBtn != null) panelBtn.onClick.AddListener(OnLoadImgFx);
        if (directBtn != null) directBtn.onClick.AddListener(OnDirectClick);
    }

    private void OnDestroy()
    {
        if (panelBtn != null) panelBtn.onClick.RemoveListener(OnLoadImgFx);
        if (directBtn != null) directBtn.onClick.RemoveListener(OnDirectClick);

        if (_loadImgCo != null) StopCoroutine(_loadImgCo);
    }

    private void BuildStatusMap()
    {
        _statusMap.Clear();
        if (statusConfigs == null) return;

        for (int i = 0; i < statusConfigs.Count; i++)
        {
            var s = statusConfigs[i];
            if (s == null) continue;
            _statusMap[s.key] = s; // key trùng -> lấy cái sau cùng
        }
    }

    /// <summary>Bind data course vào prefab UI</summary>
    public void Bind(CourseModels.CourseLite course)
    {
        if (course == null) return;

        _courseId = course._id;
        _imageUrl = course.image;

        if (txt_name != null) txt_name.text = course.title ?? "";

        if (txt_rating != null)
        {
            float stars = course.stars;
            int count = course.evaluate;

            string starsText = stars > 0 ? stars.ToString("0.0") : "0.0";

            // evaluate = 0 -> chỉ hiện sao
            if (count <= 0)
            {
                txt_rating.text = starsText;
            }
            else
            {
                txt_rating.text = $"{starsText} ({FormatCountCompact(count)})";
            }
        }

        // ===== Status: resolve -> apply object (icon/text) =====
        var status = ResolveStatus(course);
        ApplyStatus(status);

        // ===== Price =====
        var price = course.coursePrice;

        long cur = price != null ? price.currentPrice : 0;
        long org = price != null ? price.originalPrice : 0;

        // Giá mới: to + màu
        if (txt_priceDiscount != null)
        {
            string curText = (price != null && price.isFree) ? "Miễn phí" : FormatVndCompact(cur);
            txt_priceDiscount.text = $"<size=100%><color=#E95F18>{curText}</color></size>";
        }

        // Giá cũ: luôn gạch ngang (kể cả = cur)
        if (txt_priceOrigin != null)
        {
            string orgText = (price != null && price.isFree) ? "" : FormatVndCompact(org);

            if (string.IsNullOrEmpty(orgText)) orgText = "—";

            txt_priceOrigin.text = $"<size=80%><s>{orgText}</s></size>";
            txt_priceOrigin.gameObject.SetActive(true);
        }

        // ===== Image =====
        if (img_course != null)
        {
            img_course.sprite = null;
            if (!string.IsNullOrEmpty(_imageUrl))
            {
                if (_loadImgCo != null) StopCoroutine(_loadImgCo);
                _loadImgCo = StartCoroutine(LoadImageTo(img_course, _imageUrl));
            }
        }
    }

    // ================= STATUS LOGIC =================

    private StatusData ResolveStatus(CourseModels.CourseLite course)
    {
        var modeKey = ModeToStatusKey(course.learningMode);
        if (modeKey != StatusKey.None) return GetStatus(modeKey, fallbackText: course.learningMode);

        // không match thì return null để ẩn icon
        return null;
    }

    private StatusKey ModeToStatusKey(string mode)
    {
        if (string.IsNullOrEmpty(mode)) return StatusKey.None;

        mode = mode.Trim().ToLowerInvariant();

        // match mềm để backend đổi format vẫn ăn
        if (mode.Contains("zoom")) return StatusKey.Zoom;

        // offline hay bị ghi onsite / on-site
        if (mode.Contains("offline") || mode.Contains("onsite") || mode.Contains("on-site"))
            return StatusKey.Offline;

        if (mode.Contains("online")) return StatusKey.Online;

        return StatusKey.None;
    }

    private StatusData GetStatus(StatusKey key, string fallbackText)
    {
        if (_statusMap.TryGetValue(key, out var s) && s != null)
        {
            // nếu config không set text thì fallback
            if (string.IsNullOrEmpty(s.text)) s.text = fallbackText;
            return s;
        }

        // nếu không config trong inspector -> tạo tạm
        return new StatusData { key = key, text = fallbackText, icon = null };
    }

    private void ApplyStatus(StatusData s)
    {
        if (img_status == null) return;

        if (s == null || s.icon == null)
        {
            img_status.gameObject.SetActive(false);
            return;
        }

        img_status.gameObject.SetActive(true);
        img_status.enabled = true;
        img_status.sprite = s.icon;
    }

    // ================= UI EVENTS =================

    private void OnDirectClick()
    {
        Debug.Log($"[PTS] Direct click courseId = {_courseId}");

        if (courseDetailLoader == null)
        {
            Debug.LogError("CourseDetailLoader not assigned");
            return;
        }

        // đăng ký chờ store
        StartCoroutine(WaitCourseThenShow(_courseId));

        // bắt đầu load
        courseDetailLoader.Load(_courseId);
    }

    private IEnumerator WaitCourseThenShow(string courseId)
    {
        float timeout = 8f; // chống treo
        float t = 0f;

        // chờ store load xong đúng courseId
        while (t < timeout)
        {
            // nếu lỗi
            if (!string.IsNullOrEmpty(CourseDetailStaticStore.LastError))
                yield break;

            // nếu đã có data đúng id
            if (CourseDetailStaticStore.HasData &&
                CourseDetailStaticStore.CurrentCourseId == courseId &&
                !CourseDetailStaticStore.IsLoading)
            {
                PTS_CourseDetailView.Instance.ShowBriefView(courseId);
                yield break;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        // timeout: tuỳ bạn, có thể vẫn mở view để user thấy loading/error
        PTS_CourseDetailView.Instance.ShowBriefView(courseId);
    }

    private void OnLoadImgFx()
    {
        if (bgImg == null) return;

        bgImg.DOKill();
        bgImg.DOFade(1, 0.5f).OnComplete(() =>
        {
            bgImg.DOFade(0, 0.3f).SetDelay(0.2f);
        });
    }

    // ================= IMAGE LOADER =================

    private static System.Collections.IEnumerator LoadImageTo(Image target, string url)
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
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null || target == null) yield break;

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            target.sprite = sprite;

            target.preserveAspect = false;     // <- tắt để khỏi letterbox
            // target.SetNativeSize();         // <- bỏ, kẻo ảnh nhảy size

            // Update AspectRatioFitter theo ảnh
            var fitter = target.GetComponent<AspectRatioFitter>();
            if (fitter != null)
            {
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = (float)tex.width / tex.height;
            }
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