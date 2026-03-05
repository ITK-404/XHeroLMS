using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PTS_Introduce : MonoBehaviour
{
    [Header("Auto Bind Store (Optional)")]
    // Nếu bật, script sẽ tự lắng nghe CourseDetailStaticStore.OnChanged và render khi có data.")]
    [SerializeField] private bool bindToCourseDetailStaticStore = true;

    [Header("Banner")]
    // Object cha của khu banner (vd: ScrollView/Panel). Không có banner sẽ SetActive(false) để không chiếm layout.")]
    [SerializeField] private GameObject bannerCheck;

    // Container/Content để spawn banner item vào (ScrollView/Content).")]
    [SerializeField] private Transform bannerContainer;

    // Prefab UI có component Image (mỗi banner = 1 prefab).")]
    [SerializeField] private Image bannerImagePrefab;

    [SerializeField] private bool clearOldBanners = true;

    [Header("Introduction")]
    [SerializeField] private TextMeshProUGUI introductionText;

    [Header("Options")]
    [SerializeField] private bool downloadImages = true;
    [SerializeField] private Sprite placeholderSprite;

    // Bật để log debug ra Console.")]
    [SerializeField] private bool enableDebugLogs = true;

    // Nếu quên gán bannerContainer, script sẽ tự dùng transform này làm container.")]
    [SerializeField] private bool autoFallbackContainerToThisTransform = false;

    private readonly List<GameObject> spawnedBannerItems = new();
    private readonly List<Coroutine> runningImageCoroutines = new();

    // ============ Regex ============

    private static readonly Regex RxParagraph = new Regex(
        @"<p[^>]*>(.*?)</p>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly Regex RxColorSpanRgb = new Regex(
        @"<span[^>]*style\s*=\s*[""'][^""']*color\s*:\s*rgb\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)\s*;?[^""']*[""'][^>]*>(.*?)</span>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly Regex RxBr = new Regex(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxNbsp = new Regex(@"&nbsp;", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxAnyTag = new Regex(@"</?[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);

    private const string TMP_COLOR_OPEN = "__TMP_COLOR_OPEN__";
    private const string TMP_COLOR_CLOSE = "__TMP_COLOR_CLOSE__";

    private void OnEnable()
    {
        if (autoFallbackContainerToThisTransform && bannerContainer == null)
            bannerContainer = transform;

        if (bindToCourseDetailStaticStore)
        {
            CourseDetailStaticStore.OnChanged += HandleStoreChanged;
            HandleStoreChanged();
        }
    }

    private void OnDisable()
    {
        if (bindToCourseDetailStaticStore)
            CourseDetailStaticStore.OnChanged -= HandleStoreChanged;

        StopAllRunningImageCoroutines();
    }

    private void HandleStoreChanged()
    {
        if (!CourseDetailStaticStore.HasData)
        {
            if (enableDebugLogs)
                Debug.Log($"[PTS_Introduce] Store changed but HasData=false | loading={CourseDetailStaticStore.IsLoading} | err={CourseDetailStaticStore.LastError}");
            return;
        }

        var course = CourseDetailStaticStore.CurrentCourse;
        if (course == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[PTS_Introduce] Store HasData=true nhưng CurrentCourse=null (bất thường).");
            return;
        }

        Render(course.banner, course.introduction);
    }

    public void Render(IReadOnlyList<string> bannerUrls, string introductionHtml)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[PTS_Introduce] Render called | bannerCount={(bannerUrls?.Count ?? 0)} | introLen={(introductionHtml?.Length ?? 0)}");
            Debug.Log($"[PTS_Introduce] refs | bannerCheck={(bannerCheck != null)} | container={(bannerContainer != null)} | prefab={(bannerImagePrefab != null)} | introText={(introductionText != null)}");
        }

        RenderBanner(bannerUrls);
        RenderIntroduction(introductionHtml);
    }

    #region Banner

    private void RenderBanner(IReadOnlyList<string> bannerUrls)
    {
        // Đếm URL hợp lệ (không rỗng)
        int validCount = 0;
        if (bannerUrls != null)
        {
            for (int i = 0; i < bannerUrls.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(bannerUrls[i]))
                    validCount++;
            }
        }

        bool hasBanner = validCount > 0;

        // Ẩn/hiện bannerCheck để không chiếm layout
        if (bannerCheck != null)
            bannerCheck.SetActive(hasBanner);

        if (!hasBanner)
        {
            // Không có banner -> dọn sạch cái cũ để khỏi còn “chiếm chỗ” trong content
            StopAllRunningImageCoroutines();
            if (clearOldBanners) ClearBanners();

            if (enableDebugLogs)
                Debug.Log("[PTS_Introduce] No banner -> bannerCheck hidden.");
            return;
        }

        // Có banner mà thiếu refs thì vẫn log và return
        if (bannerContainer == null || bannerImagePrefab == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[PTS_Introduce] Has banner nhưng thiếu bannerContainer hoặc bannerImagePrefab.");
            return;
        }

        StopAllRunningImageCoroutines();
        if (clearOldBanners) ClearBanners();

        for (int i = 0; i < bannerUrls.Count; i++)
        {
            string url = bannerUrls[i];
            if (string.IsNullOrWhiteSpace(url)) continue;

            Image img = Instantiate(bannerImagePrefab, bannerContainer);
            img.gameObject.SetActive(true);
            spawnedBannerItems.Add(img.gameObject);

            if (placeholderSprite != null)
                img.sprite = placeholderSprite;

            if (downloadImages)
            {
                var c = StartCoroutine(LoadImageTo(img, url));
                runningImageCoroutines.Add(c);
            }
        }
    }

    private void ClearBanners()
    {
        for (int i = 0; i < spawnedBannerItems.Count; i++)
        {
            if (spawnedBannerItems[i] != null)
                Destroy(spawnedBannerItems[i]);
        }
        spawnedBannerItems.Clear();
    }

    private void StopAllRunningImageCoroutines()
    {
        for (int i = 0; i < runningImageCoroutines.Count; i++)
        {
            if (runningImageCoroutines[i] != null)
                StopCoroutine(runningImageCoroutines[i]);
        }
        runningImageCoroutines.Clear();
    }

    private IEnumerator LoadImageTo(Image target, string url)
    {
        if (target == null) yield break;

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            req.timeout = 20;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                if (enableDebugLogs)
                    Debug.LogError($"[PTS_Introduce] Load image failed: {url}\n{req.error}");
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
            {
                if (enableDebugLogs)
                    Debug.LogError($"[PTS_Introduce] Texture null: {url}");
                yield break;
            }

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            if (target != null)
            {
                target.sprite = sprite;
                target.preserveAspect = true;
            }
        }
    }

    #endregion

    #region Introduction (HTML -> TMP rich text)

    private void RenderIntroduction(string html)
    {
        if (introductionText == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[PTS_Introduce] Missing introductionText -> không thể render introduction.");
            return;
        }

        introductionText.richText = true;

        if (string.IsNullOrWhiteSpace(html))
        {
            introductionText.text = "";
            if (enableDebugLogs)
                Debug.Log("[PTS_Introduce] Introduction empty.");
            return;
        }

        string converted = ConvertHtmlToTmpRichText(html);
        introductionText.text = converted;

        if (enableDebugLogs)
            Debug.Log($"[PTS_Introduce] Introduction rendered | len={converted.Length}");
    }

    public static string ConvertHtmlToTmpRichText(string html)
    {
        html = RxBr.Replace(html, "\n");
        html = RxNbsp.Replace(html, " ");

        var matches = RxParagraph.Matches(html);
        if (matches.Count == 0)
            return ConvertInlineHtmlToTmp(html).Trim();

        StringBuilder sb = new StringBuilder(1024);

        for (int i = 0; i < matches.Count; i++)
        {
            string inner = matches[i].Groups[1].Value;
            inner = ConvertInlineHtmlToTmp(inner).Trim();

            if (string.IsNullOrEmpty(inner)) continue;

            if (sb.Length > 0) sb.Append("\n\n");
            sb.Append(inner);
        }

        return sb.ToString().Trim();
    }

    private static string ConvertInlineHtmlToTmp(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        string converted = RxColorSpanRgb.Replace(input, m =>
        {
            int r = ClampByte(m.Groups[1].Value);
            int g = ClampByte(m.Groups[2].Value);
            int b = ClampByte(m.Groups[3].Value);

            string text = m.Groups[4].Value;
            text = RxAnyTag.Replace(text, "");
            text = System.Net.WebUtility.HtmlDecode(text);

            return $"<color=#{r:X2}{g:X2}{b:X2}>{text}</color>";
        });

        // Protect TMP tags
        converted = converted.Replace("<color=", TMP_COLOR_OPEN);
        converted = converted.Replace("</color>", TMP_COLOR_CLOSE);

        // Strip remaining HTML
        converted = RxAnyTag.Replace(converted, "");

        // Restore TMP tags
        converted = converted.Replace(TMP_COLOR_OPEN, "<color=");
        converted = converted.Replace(TMP_COLOR_CLOSE, "</color>");

        converted = System.Net.WebUtility.HtmlDecode(converted);
        converted = converted.Replace("\r", "");
        return converted;
    }

    private static int ClampByte(string s)
    {
        if (!int.TryParse(s, out int v)) v = 255;
        if (v < 0) v = 0;
        if (v > 255) v = 255;
        return v;
    }

    #endregion
}