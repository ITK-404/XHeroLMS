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
    [SerializeField] private bool bindToCourseDetailStaticStore = true;

    [Header("Banner")]
    [SerializeField] private GameObject bannerCheck;
    [SerializeField] private Transform bannerContainer;
    [SerializeField] private Image bannerImagePrefab;
    [SerializeField] private bool clearOldBanners = true;

    [Header("Introduction")]
    [SerializeField] private TextMeshProUGUI introductionText;

    [Header("Options")]
    [SerializeField] private bool downloadImages = true;
    [SerializeField] private Sprite placeholderSprite;
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoFallbackContainerToThisTransform = false;

    private readonly List<GameObject> spawnedBannerItems = new();
    private readonly List<Coroutine> runningImageCoroutines = new();

    // ============ Regex ============

    private static readonly Regex RxBr = new Regex(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RxOpenPDiv = new Regex(
        @"<(p|div)[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex RxClosePDiv = new Regex(
        @"</(p|div)\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex RxOpenUl = new Regex(@"<ul[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxCloseUl = new Regex(@"</ul\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RxOpenOl = new Regex(@"<ol[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxCloseOl = new Regex(@"</ol\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RxLi = new Regex(
        @"<li[^>]*>(.*?)</li>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
    );

    // rgb(...)
    private static readonly Regex RxColorSpanRgb = new Regex(
        @"<span[^>]*style\s*=\s*[""'][^""']*color\s*:\s*rgb\s*\(\s*(\d{1,3})\s*,\s*(\d{1,3})\s*,\s*(\d{1,3})\s*\)\s*;?[^""']*[""'][^>]*>(.*?)</span>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
    );

    // #RRGGBB / #RGB
    private static readonly Regex RxColorSpanHex = new Regex(
        @"<span[^>]*style\s*=\s*[""'][^""']*color\s*:\s*(#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}))\s*;?[^""']*[""'][^>]*>(.*?)</span>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
    );

    // named colors: red, white...
    private static readonly Regex RxColorSpanNamed = new Regex(
        @"<span[^>]*style\s*=\s*[""'][^""']*color\s*:\s*([a-zA-Z]+)\s*;?[^""']*[""'][^>]*>(.*?)</span>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled
    );

    private static readonly Regex RxStrongOpen = new Regex(@"<(strong|b)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxStrongClose = new Regex(@"</(strong|b)\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RxItalicOpen = new Regex(@"<(em|i)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxItalicClose = new Regex(@"</(em|i)\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RxUnderlineOpen = new Regex(@"<u[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxUnderlineClose = new Regex(@"</u\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RxNbsp = new Regex(@"&nbsp;", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxMultiNewline = new Regex(@"\n{3,}", RegexOptions.Compiled);
    private static readonly Regex RxTrimSpacesAroundNewline = new Regex(@"[ \t]*\n[ \t]*", RegexOptions.Compiled);

    // strip all remaining tags
    private static readonly Regex RxAnyTag = new Regex(@"</?[^>]+>", RegexOptions.Singleline | RegexOptions.Compiled);

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

        var course = CourseDetailStaticStore.CurrentDetail;
        if (course == null)
        {
            if (enableDebugLogs)
                Debug.LogWarning("[PTS_Introduce] Store HasData=true nhưng CurrentCourse=null (bất thường).");
            return;
        }

        // Ghép description + introduction chung vào 1 text
        string combinedHtml = BuildCombinedHtml(course.description, course.introduction);
        Render(course.banner, combinedHtml);
    }

    private static string BuildCombinedHtml(string description, string introduction)
    {
        bool hasDescription = !string.IsNullOrWhiteSpace(description);
        bool hasIntroduction = !string.IsNullOrWhiteSpace(introduction);

        if (!hasDescription && !hasIntroduction)
            return string.Empty;

        if (hasDescription && hasIntroduction)
            return $"{description}<br><br>{introduction}";

        if (hasDescription)
            return description;

        return introduction;
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

        if (bannerCheck != null)
            bannerCheck.SetActive(hasBanner);

        if (!hasBanner)
        {
            StopAllRunningImageCoroutines();
            if (clearOldBanners) ClearBanners();

            if (enableDebugLogs)
                Debug.Log("[PTS_Introduce] No banner -> bannerCheck hidden.");
            return;
        }

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
                target.preserveAspect = false;
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
            introductionText.text = "Thông tin đang được chúng tôi cập nhật";
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
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        string converted = html;

        // line break / block
        converted = RxBr.Replace(converted, "\n");
        converted = RxOpenPDiv.Replace(converted, "");
        converted = RxClosePDiv.Replace(converted, "\n\n");

        converted = RxOpenUl.Replace(converted, "\n");
        converted = RxCloseUl.Replace(converted, "\n");
        converted = RxOpenOl.Replace(converted, "\n");
        converted = RxCloseOl.Replace(converted, "\n");

        converted = RxLi.Replace(converted, m =>
        {
            string inner = m.Groups[1].Value;
            return $"• {inner}\n";
        });

        // basic rich text
        converted = RxStrongOpen.Replace(converted, "<b>");
        converted = RxStrongClose.Replace(converted, "</b>");

        converted = RxItalicOpen.Replace(converted, "<i>");
        converted = RxItalicClose.Replace(converted, "</i>");

        converted = RxUnderlineOpen.Replace(converted, "<u>");
        converted = RxUnderlineClose.Replace(converted, "</u>");

        // color: rgb(...)
        converted = RxColorSpanRgb.Replace(converted, m =>
        {
            int r = ClampByte(m.Groups[1].Value);
            int g = ClampByte(m.Groups[2].Value);
            int b = ClampByte(m.Groups[3].Value);
            string inner = m.Groups[4].Value;
            return $"<color=#{r:X2}{g:X2}{b:X2}>{inner}</color>";
        });

        // color: #hex
        converted = RxColorSpanHex.Replace(converted, m =>
        {
            string hex = NormalizeHexColor(m.Groups[1].Value);
            string inner = m.Groups[2].Value;
            return $"<color={hex}>{inner}</color>";
        });

        // color: named
        converted = RxColorSpanNamed.Replace(converted, m =>
        {
            string colorName = m.Groups[1].Value.ToLowerInvariant();
            string inner = m.Groups[2].Value;

            string mapped = colorName switch
            {
                "red" => "#FF0000",
                "green" => "#00FF00",
                "blue" => "#0000FF",
                "yellow" => "#FFFF00",
                "white" => "#FFFFFF",
                "black" => "#000000",
                "grey" => "#808080",
                "gray" => "#808080",
                "orange" => "#FFA500",
                "purple" => "#800080",
                _ => "#FFFFFF"
            };

            return $"<color={mapped}>{inner}</color>";
        });

        converted = RxNbsp.Replace(converted, " ");

        // decode html entities trước
        converted = System.Net.WebUtility.HtmlDecode(converted);

        // strip all remaining unsupported tags
        converted = StripUnknownHtmlKeepTmpTags(converted);

        converted = converted.Replace("\r", "");
        converted = RxTrimSpacesAroundNewline.Replace(converted, "\n");
        converted = RxMultiNewline.Replace(converted, "\n\n");

        return converted.Trim();
    }

    private static string StripUnknownHtmlKeepTmpTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        const string TMP_COLOR_OPEN = "__TMP_COLOR_OPEN__";
        const string TMP_COLOR_CLOSE = "__TMP_COLOR_CLOSE__";
        const string TMP_B_OPEN = "__TMP_B_OPEN__";
        const string TMP_B_CLOSE = "__TMP_B_CLOSE__";
        const string TMP_I_OPEN = "__TMP_I_OPEN__";
        const string TMP_I_CLOSE = "__TMP_I_CLOSE__";
        const string TMP_U_OPEN = "__TMP_U_OPEN__";
        const string TMP_U_CLOSE = "__TMP_U_CLOSE__";

        input = input.Replace("<color=", TMP_COLOR_OPEN);
        input = input.Replace("</color>", TMP_COLOR_CLOSE);
        input = input.Replace("<b>", TMP_B_OPEN);
        input = input.Replace("</b>", TMP_B_CLOSE);
        input = input.Replace("<i>", TMP_I_OPEN);
        input = input.Replace("</i>", TMP_I_CLOSE);
        input = input.Replace("<u>", TMP_U_OPEN);
        input = input.Replace("</u>", TMP_U_CLOSE);

        input = RxAnyTag.Replace(input, "");

        input = input.Replace(TMP_COLOR_OPEN, "<color=");
        input = input.Replace(TMP_COLOR_CLOSE, "</color>");
        input = input.Replace(TMP_B_OPEN, "<b>");
        input = input.Replace(TMP_B_CLOSE, "</b>");
        input = input.Replace(TMP_I_OPEN, "<i>");
        input = input.Replace(TMP_I_CLOSE, "</i>");
        input = input.Replace(TMP_U_OPEN, "<u>");
        input = input.Replace(TMP_U_CLOSE, "</u>");

        return input;
    }

    private static int ClampByte(string s)
    {
        if (!int.TryParse(s, out int v)) v = 255;
        if (v < 0) v = 0;
        if (v > 255) v = 255;
        return v;
    }

    private static string NormalizeHexColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return "#FFFFFF";

        hex = hex.Trim();

        if (!hex.StartsWith("#"))
            hex = "#" + hex;

        if (hex.Length == 4) // #RGB -> #RRGGBB
        {
            char r = hex[1];
            char g = hex[2];
            char b = hex[3];
            return $"#{r}{r}{g}{g}{b}{b}";
        }

        if (hex.Length == 7)
            return hex.ToUpperInvariant();

        return "#FFFFFF";
    }

    #endregion
}