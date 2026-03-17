using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CourseSearch : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField inputKeyword;
    [SerializeField] private TMP_Dropdown dropdownMode;
    [SerializeField] private TMP_Dropdown dropdownRating;
    [SerializeField] private TMP_Dropdown dropdownSort;
    [SerializeField] private Sprite ratingStarSprite;

    [Header("Right Icon UI")]
    [SerializeField] private GameObject clearButtonRoot;
    [SerializeField] private Button clearButton;
    [SerializeField] private Image clearButtonImage;
    [SerializeField] private Sprite spriteSearch;
    [SerializeField] private Sprite spriteClear;
    [SerializeField] private bool keepFocusAfterClear = true;

    [Header("Defaults")]
    [Tooltip("Nếu true: mỗi lần mở màn (OnEnable) sẽ reset keyword + dropdown về 'Tất cả'.")]
    [SerializeField] private bool resetToDefaultOnEnable = true;

    [Header("Behavior")]
    [Tooltip("Delay in seconds to reduce searching every keystroke (mobile-friendly). 0 = no debounce.")]
    [SerializeField] private float debounceSeconds = 0.15f;

    public event Action<List<CourseModels.CourseLite>> OnResultsChanged;

    public IReadOnlyList<CourseModels.CourseLite> LastResults => _lastResults;
    private readonly List<CourseModels.CourseLite> _lastResults = new();

    private enum ModeFilter { Any = 0, Zoom = 1, Online = 2, Offline = 3 }
    private enum SortMode { Any = 0, NewestFirst = 1, OldestFirst = 2, PriceHighToLow = 3, PriceLowToHigh = 4 }

    public static CourseSearch Instance { get; private set; }

    private Coroutine _debounceCo;

    private string _lastKw;
    private int _lastMode;
    private int _lastRating;
    private int _lastSort;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        AutoBindDropdownRatingImages();
        EnsureDropdownOptions();
        WireUI();
        RefreshRightIconUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        CourseStaticStore.OnChanged += HandleStoreChanged;

        if (resetToDefaultOnEnable)
        {
            ResetToDefaults();
            ResetQueryCache();
        }

        SearchNow();
    }

    private void OnDisable()
    {
        CourseStaticStore.OnChanged -= HandleStoreChanged;
        StopDebounce();
    }

    private void HandleStoreChanged() => RequestSearch();

    public void SearchNow()
    {
        var keyword = inputKeyword != null ? inputKeyword.text : null;

        int modeV = GetDropdownValueSafe(dropdownMode);
        int ratingV = GetDropdownValueSafe(dropdownRating);
        int sortV = GetDropdownValueSafe(dropdownSort);

        if (SameQuery(keyword, modeV, ratingV, sortV))
            if (_lastResults.Count > 0) return;

        CacheQuery(keyword, modeV, ratingV, sortV);

        var mode = (ModeFilter)modeV;
        var ratingMin = GetRatingMinStars(ratingV);
        var sort = (SortMode)sortV;

        var results = RunSearch(keyword, mode, ratingMin, sort);

        _lastResults.Clear();
        _lastResults.AddRange(results);

        OnResultsChanged?.Invoke(_lastResults);
    }

    private void RequestSearch()
    {
        RefreshRightIconUI();
        StopDebounce();

        if (debounceSeconds <= 0f)
        {
            SearchNow();
            return;
        }

        _debounceCo = StartCoroutine(CoDebounce());
    }

    private IEnumerator CoDebounce()
    {
        yield return new WaitForSecondsRealtime(debounceSeconds);
        _debounceCo = null;
        SearchNow();
    }

    private void StopDebounce()
    {
        if (_debounceCo != null)
        {
            StopCoroutine(_debounceCo);
            _debounceCo = null;
        }
    }

    private bool SameQuery(string kw, int mode, int rating, int sort)
    {
        kw = kw ?? string.Empty;
        return _lastKw == kw && _lastMode == mode && _lastRating == rating && _lastSort == sort;
    }

    private void CacheQuery(string kw, int mode, int rating, int sort)
    {
        _lastKw = kw ?? string.Empty;
        _lastMode = mode;
        _lastRating = rating;
        _lastSort = sort;
    }

    private void ResetQueryCache()
    {
        _lastKw = null;
        _lastMode = -999;
        _lastRating = -999;
        _lastSort = -999;
    }

    private void ResetToDefaults()
    {
        if (inputKeyword != null)
            inputKeyword.SetTextWithoutNotify(string.Empty);

        if (dropdownMode != null)
        {
            dropdownMode.SetValueWithoutNotify(0);
            dropdownMode.RefreshShownValue();
        }

        if (dropdownRating != null)
        {
            dropdownRating.SetValueWithoutNotify(0);
            dropdownRating.RefreshShownValue();
        }

        if (dropdownSort != null)
        {
            dropdownSort.SetValueWithoutNotify(0);
            dropdownSort.RefreshShownValue();
        }

        RefreshRightIconUI();
    }

    private void WireUI()
    {
        if (inputKeyword != null)
        {
            inputKeyword.onValueChanged.AddListener(_ =>
            {
                RefreshRightIconUI();
                RequestSearch();
            });

            inputKeyword.onEndEdit.AddListener(_ => RefreshRightIconUI());
        }

        if (dropdownMode != null) dropdownMode.onValueChanged.AddListener(_ => RequestSearch());
        if (dropdownRating != null) dropdownRating.onValueChanged.AddListener(_ => RequestSearch());
        if (dropdownSort != null) dropdownSort.onValueChanged.AddListener(_ => RequestSearch());

        if (clearButton == null && clearButtonRoot != null)
            clearButton = clearButtonRoot.GetComponent<Button>();

        if (clearButtonImage == null && clearButtonRoot != null)
            clearButtonImage = clearButtonRoot.GetComponent<Image>();

        if (clearButton != null)
        {
            clearButton.onClick.RemoveListener(OnRightIconClicked);
            clearButton.onClick.AddListener(OnRightIconClicked);
        }
    }

    private void OnRightIconClicked()
    {
        bool hasText = inputKeyword != null && !string.IsNullOrEmpty(inputKeyword.text);

        if (hasText)
        {
            ClearKeyword();
            return;
        }

        if (inputKeyword != null)
        {
            inputKeyword.ActivateInputField();
            inputKeyword.MoveTextEnd(false);
        }
    }

    private void ClearKeyword()
    {
        if (inputKeyword == null) return;

        inputKeyword.SetTextWithoutNotify(string.Empty);
        RefreshRightIconUI();
        RequestSearch();

        if (keepFocusAfterClear)
        {
            inputKeyword.ActivateInputField();
            inputKeyword.MoveTextEnd(false);
        }
    }

    private void RefreshRightIconUI()
    {
        bool hasText = inputKeyword != null && !string.IsNullOrEmpty(inputKeyword.text);

        if (clearButtonRoot != null && !clearButtonRoot.activeSelf)
            clearButtonRoot.SetActive(true);

        if (clearButtonImage != null)
        {
            clearButtonImage.sprite = hasText ? spriteClear : spriteSearch;
            clearButtonImage.enabled = clearButtonImage.sprite != null;
        }
    }

    private void EnsureDropdownOptions()
    {
        if (dropdownMode != null)
        {
            dropdownMode.ClearOptions();
            dropdownMode.AddOptions(new List<string> { "Tất cả", "Zoom", "Online", "Trực tiếp" });
            dropdownMode.SetValueWithoutNotify(0);
            dropdownMode.RefreshShownValue();
        }

        if (dropdownRating != null)
        {
            dropdownRating.ClearOptions();

            var opts = new List<TMP_Dropdown.OptionData>
            {
                new TMP_Dropdown.OptionData("Tất cả", null, Color.white),
                new TMP_Dropdown.OptionData("5.0", ratingStarSprite, Color.white),
                new TMP_Dropdown.OptionData("4.0", ratingStarSprite, Color.white),
                new TMP_Dropdown.OptionData("3.0", ratingStarSprite, Color.white),
                new TMP_Dropdown.OptionData("2.0", ratingStarSprite, Color.white),
                new TMP_Dropdown.OptionData("1.0", ratingStarSprite, Color.white),
            };

            dropdownRating.options = opts;
            dropdownRating.SetValueWithoutNotify(0);
            dropdownRating.RefreshShownValue();
        }

        if (dropdownSort != null)
        {
            dropdownSort.ClearOptions();
            dropdownSort.AddOptions(new List<string>
            {
                "Tất cả","Mới nhất","Cũ nhất","Giá: Cao → Thấp","Giá: Thấp → Cao"
            });
            dropdownSort.SetValueWithoutNotify(0);
            dropdownSort.RefreshShownValue();
        }
    }

    private void AutoBindDropdownRatingImages()
    {
        if (dropdownRating == null) return;

        if (dropdownRating.captionImage == null)
        {
            var cap = dropdownRating.transform.Find("Icon");
            if (cap != null) dropdownRating.captionImage = cap.GetComponent<Image>();
        }

        if (dropdownRating.itemImage == null && dropdownRating.template != null)
        {
            var itemIcon = dropdownRating.template.Find("Viewport/Content/Item/Icon");
            if (itemIcon != null) dropdownRating.itemImage = itemIcon.GetComponent<Image>();
        }
    }

    private int GetDropdownValueSafe(TMP_Dropdown dd)
    {
        if (dd == null) return 0;
        return Mathf.Clamp(dd.value, 0, dd.options.Count - 1);
    }

    private List<CourseModels.CourseLite> RunSearch(string keyword, ModeFilter mode, float minStars, SortMode sort)
    {
        var all = CourseStaticStore.GetAll();
        var result = new List<CourseModels.CourseLite>(all.Count);

        string kwNorm = NormalizeForSearch(keyword);
        string modeStr = ModeToString(mode);

        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null) continue;

            if (!PassKeywordSmart(c, kwNorm)) continue;
            if (!PassMode(c, modeStr)) continue;
            if (!PassRating(c, minStars)) continue;

            result.Add(c);
        }

        ApplySort(result, sort);
        return result;
    }

    private bool PassKeywordSmart(CourseModels.CourseLite c, string kwNorm)
    {
        if (string.IsNullOrEmpty(kwNorm)) return true;

        if (ContainsSmart(c._id, kwNorm)) return true;
        if (ContainsSmart(c.sku, kwNorm)) return true;
        if (ContainsSmart(c.title, kwNorm)) return true;

        var seoUrl = c.seo != null ? c.seo.url : null;
        if (ContainsSmart(seoUrl, kwNorm)) return true;

        return false;
    }

    private bool ContainsSmart(string hayRaw, string kwNorm)
    {
        if (string.IsNullOrEmpty(hayRaw) || string.IsNullOrEmpty(kwNorm)) return false;

        string hayNorm = NormalizeForSearch(hayRaw);

        if (hayNorm.Contains(kwNorm)) return true;

        var kwNoSpace = kwNorm.Replace(" ", "");
        if (!string.IsNullOrEmpty(kwNoSpace) && hayNorm.Replace(" ", "").Contains(kwNoSpace))
            return true;

        return false;
    }

    private string NormalizeForSearch(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;

        s = s.Trim().ToLowerInvariant();
        s = RemoveDiacritics(s);
        s = NormalizePunctuationToSpaces(s);
        s = CollapseSpaces(s);
        s = RomanTokensToArabic(s);
        s = CollapseSpaces(s);
        return s;
    }

    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        for (int i = 0; i < normalized.Length; i++)
        {
            char c = normalized[i];
            var uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString()
                 .Normalize(NormalizationForm.FormC)
                 .Replace('đ', 'd')
                 .Replace('Đ', 'D');
    }

    private static string NormalizePunctuationToSpaces(string s)
    {
        var sb = new StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];

            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else
                sb.Append(' ');
        }
        return sb.ToString();
    }

    private static string CollapseSpaces(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        var sb = new StringBuilder(s.Length);
        bool prevSpace = false;

        for (int i = 0; i < s.Length; i++)
        {
            char ch = s[i];
            if (ch == ' ')
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                prevSpace = false;
            }
        }

        return sb.ToString().Trim();
    }

    private static string RomanTokensToArabic(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        var parts = s.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            string t = parts[i];
            if (string.IsNullOrEmpty(t)) continue;

            int val = RomanToInt(t);
            if (val > 0)
                parts[i] = val.ToString();
        }
        return string.Join(" ", parts);
    }

    private static int RomanToInt(string roman)
    {
        if (string.IsNullOrEmpty(roman)) return 0;

        for (int i = 0; i < roman.Length; i++)
        {
            char ch = roman[i];
            if (ch != 'i' && ch != 'v' && ch != 'x' && ch != 'l' && ch != 'c' && ch != 'd' && ch != 'm')
                return 0;
        }

        int total = 0;
        int prev = 0;

        for (int i = roman.Length - 1; i >= 0; i--)
        {
            int v = RomanCharValue(roman[i]);
            if (v == 0) return 0;

            if (v < prev) total -= v;
            else { total += v; prev = v; }
        }

        return total;
    }

    private static int RomanCharValue(char c)
    {
        switch (c)
        {
            case 'i': return 1;
            case 'v': return 5;
            case 'x': return 10;
            case 'l': return 50;
            case 'c': return 100;
            case 'd': return 500;
            case 'm': return 1000;
            default: return 0;
        }
    }

    private bool PassMode(CourseModels.CourseLite c, string modeLowerOrNull)
    {
        if (string.IsNullOrEmpty(modeLowerOrNull)) return true;
        var m = Normalize(c.learningMode);
        return string.Equals(m, modeLowerOrNull, StringComparison.OrdinalIgnoreCase);
    }

    private bool PassRating(CourseModels.CourseLite c, float minStars)
    {
        if (minStars <= 0f) return true;
        return c.stars >= minStars;
    }

    private void ApplySort(List<CourseModels.CourseLite> list, SortMode sort)
    {
        if (list == null || list.Count <= 1) return;

        switch (sort)
        {
            case SortMode.NewestFirst:
                list.Sort((a, b) => GetUploadedTicks(b).CompareTo(GetUploadedTicks(a)));
                break;
            case SortMode.OldestFirst:
                list.Sort((a, b) => GetUploadedTicks(a).CompareTo(GetUploadedTicks(b)));
                break;
            case SortMode.PriceHighToLow:
                list.Sort((a, b) => GetPriceValue(b).CompareTo(GetPriceValue(a)));
                break;
            case SortMode.PriceLowToHigh:
                list.Sort((a, b) => GetPriceValue(a).CompareTo(GetPriceValue(b)));
                break;
        }
    }

    private long GetUploadedTicks(CourseModels.CourseLite c)
    {
        if (c == null) return 0;
        if (TryParseToUtcTicks(c.startSellTime, out var ticks))
            return ticks;
        return 0;
    }

    private long GetPriceValue(CourseModels.CourseLite c)
    {
        var p = c != null ? c.coursePrice : null;
        if (p == null) return long.MaxValue;
        if (p.isFree) return 0;

        long v = p.currentPrice > 0 ? p.currentPrice : p.originalPrice;
        return v > 0 ? v : long.MaxValue;
    }

    private bool TryParseToUtcTicks(object raw, out long ticks)
    {
        ticks = 0;
        if (raw == null) return false;

        try
        {
            if (raw is long l) return TryUnixToTicks(l, out ticks);
            if (raw is int i) return TryUnixToTicks(i, out ticks);
            if (raw is double d) return TryUnixToTicks((long)d, out ticks);
            if (raw is float f) return TryUnixToTicks((long)f, out ticks);

            if (raw is string s)
            {
                s = s.Trim();
                if (string.IsNullOrEmpty(s)) return false;

                if (long.TryParse(s, out var num))
                    return TryUnixToTicks(num, out ticks);

                if (DateTimeOffset.TryParse(s, out var dto))
                {
                    ticks = dto.UtcTicks;
                    return true;
                }
            }
            else
            {
                var str = raw.ToString();
                if (!string.IsNullOrEmpty(str))
                {
                    str = str.Trim();
                    if (long.TryParse(str, out var num2))
                        return TryUnixToTicks(num2, out ticks);

                    if (DateTimeOffset.TryParse(str, out var dto2))
                    {
                        ticks = dto2.UtcTicks;
                        return true;
                    }
                }
            }
        }
        catch { }

        return false;
    }

    private bool TryUnixToTicks(long unix, out long ticks)
    {
        ticks = 0;
        if (unix <= 0) return false;

        try
        {
            DateTimeOffset dto = (unix >= 1_000_000_000_000L)
                ? DateTimeOffset.FromUnixTimeMilliseconds(unix)
                : DateTimeOffset.FromUnixTimeSeconds(unix);

            ticks = dto.UtcTicks;
            return true;
        }
        catch { return false; }
    }

    private float GetRatingMinStars(int ddValue)
    {
        if (ddValue <= 0) return 0f;
        return Mathf.Clamp(6 - ddValue, 1, 5);
    }

    private string ModeToString(ModeFilter mode)
    {
        switch (mode)
        {
            case ModeFilter.Zoom: return "zoom";
            case ModeFilter.Online: return "online";
            case ModeFilter.Offline: return "offline";
            default: return null;
        }
    }

    private string Normalize(string s) => string.IsNullOrEmpty(s) ? null : s.Trim().ToLowerInvariant();
}