using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CourseNotifyUI : MonoBehaviour, IPointerDownHandler
{
    private static int s_cachedCourseCount = -1;
    private static bool s_pendingPaymentSuccessCheck = false;
    private static bool s_isNotifyShowing = false;
    private static int s_pendingNotifyDiff = 0;

    [Header("Refs")]
    [SerializeField] private Image circleVfx;
    [SerializeField] private TMP_Text newCourseText;
    [SerializeField] private ShakeNotification shakeNotification;
    [SerializeField] private GameObject shakeNotificationRoot;
    [SerializeField] private Button markAsSeenButton;

    [Header("API")]
    [SerializeField] private int limitPerPage = 100;
    [SerializeField] private bool useTokenFromStore = true;
    [SerializeField] private string overrideAccessToken = "";

    [Header("Payment Check")]
    [SerializeField] private float delayAfterPaymentSuccess = 0.5f;

    [Header("Fade")]
    [SerializeField] private float fadeStart = 0f;
    [SerializeField] private float fadeEnd = 1f;
    [SerializeField] private float fadeDuration = 0.5f;

    private Tween fadeTween;
    private string baseUrl;
    private int cachedCourseCount = -1;
    private bool isChecking;
    private Coroutine paymentCheckCoroutine;
    private static bool s_userMarkedSeen = false;

    public static void RequestPaymentSuccessCheck()
    {
        Debug.Log("[CourseNotifyUI] Static RequestPaymentSuccessCheck");

        CourseNotifyUI ui = FindFirstObjectByType<CourseNotifyUI>(FindObjectsInactive.Include);

        if (ui != null)
        {
            ui.NotifyPaymentSuccess();
        }
        else
        {
            s_pendingPaymentSuccessCheck = true;
            Debug.LogWarning("[CourseNotifyUI] No instance now -> pending payment success check saved in RAM");
        }
    }

    private void Awake()
    {
        Debug.Log("[CourseNotifyUI] Awake");

        baseUrl = LmsStore.Instance.baseUrl;

        if (shakeNotification == null)
            shakeNotification = GetComponentInChildren<ShakeNotification>(true);

        if (shakeNotificationRoot == null && shakeNotification != null)
            shakeNotificationRoot = shakeNotification.gameObject;

        if (markAsSeenButton != null)
        {
            markAsSeenButton.onClick.RemoveListener(MarkAsSeen);
            markAsSeenButton.onClick.AddListener(MarkAsSeen);
        }

        HideNotifyVisualOnly();
    }

    private void Start()
    {
        Debug.Log("[CourseNotifyUI] Start -> load initial course count in RAM");
        StartCoroutine(LoadInitialCourseCount());
    }

    private void OnDestroy()
    {
        if (markAsSeenButton != null)
            markAsSeenButton.onClick.RemoveListener(MarkAsSeen);

        StopVfx();
    }

    private IEnumerator LoadInitialCourseCount()
    {
        if (s_cachedCourseCount >= 0)
        {
            cachedCourseCount = s_cachedCourseCount;
            Debug.Log($"[CourseNotifyUI] Use static cached course count from RAM = {cachedCourseCount}");

            if (!s_userMarkedSeen && s_isNotifyShowing && s_pendingNotifyDiff > 0)
            {
                ShowNewCourseNotify(s_pendingNotifyDiff);
            }
            else
            {
                HideNotifyVisualOnly();
            }

            if (s_pendingPaymentSuccessCheck)
            {
                s_pendingPaymentSuccessCheck = false;
                NotifyPaymentSuccess();
            }

            yield break;
        }

        int count = 0;
        bool success = false;

        yield return LoadMyCourseCount(result =>
        {
            count = result;
            success = true;
        });

        if (!success)
        {
            Debug.LogWarning("[CourseNotifyUI] Initial load failed. cachedCourseCount still -1.");
            yield break;
        }

        cachedCourseCount = count;
        s_cachedCourseCount = count;

        Debug.Log($"[CourseNotifyUI] Initial course count cached in RAM = {cachedCourseCount}");

        if (s_pendingPaymentSuccessCheck)
        {
            s_pendingPaymentSuccessCheck = false;
            NotifyPaymentSuccess();
        }
    }

    public void NotifyPaymentSuccess()
    {
        Debug.Log("[CourseNotifyUI] Payment success received -> wait then recheck course count");

        s_userMarkedSeen = false;

        if (paymentCheckCoroutine != null)
            StopCoroutine(paymentCheckCoroutine);

        paymentCheckCoroutine = StartCoroutine(CheckAfterPaymentSuccessRoutine());
    }

    private IEnumerator CheckAfterPaymentSuccessRoutine()
    {
        yield return new WaitForSeconds(delayAfterPaymentSuccess);

        yield return CheckCourseCountAfterPayment();

        paymentCheckCoroutine = null;
    }

    private IEnumerator CheckCourseCountAfterPayment()
    {
        if (isChecking)
        {
            Debug.Log("[CourseNotifyUI] Already checking, skip.");
            yield break;
        }

        isChecking = true;

        int latestCount = 0;
        bool success = false;

        Debug.Log("[CourseNotifyUI] Recheck course count after payment...");

        yield return LoadMyCourseCount(result =>
        {
            latestCount = result;
            success = true;
        });

        isChecking = false;

        if (!success)
        {
            Debug.LogWarning("[CourseNotifyUI] Recheck failed.");
            yield break;
        }

        Debug.Log($"[CourseNotifyUI] Compare count | cached={cachedCourseCount} | static={s_cachedCourseCount} | latest={latestCount}");

        if (cachedCourseCount < 0)
        {
            if (s_cachedCourseCount >= 0)
            {
                cachedCourseCount = s_cachedCourseCount;
            }
            else
            {
                cachedCourseCount = latestCount;
                s_cachedCourseCount = latestCount;
                Debug.Log($"[CourseNotifyUI] No baseline -> set baseline = {latestCount}");
                yield break;
            }
        }

        int diff = latestCount - cachedCourseCount;

        if (diff > 0)
        {
            cachedCourseCount = latestCount;
            s_cachedCourseCount = latestCount;

            if (s_userMarkedSeen)
            {
                Debug.Log("[CourseNotifyUI] New course diff exists but user already marked seen -> skip show.");
                yield break;
            }

            s_isNotifyShowing = true;
            s_pendingNotifyDiff = diff;

            Debug.Log($"[CourseNotifyUI] New course detected after payment: +{diff}");
            ShowNewCourseNotify(diff);
        }
        else
        {
            cachedCourseCount = latestCount;
            s_cachedCourseCount = latestCount;

            Debug.Log("[CourseNotifyUI] No new course after payment.");
        }
    }

    private IEnumerator LoadMyCourseCount(Action<int> onDone)
    {
        string token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[CourseNotifyUI] No token -> cannot check my courses.");
            yield break;
        }

        int total = 0;
        int nextSkip = 0;

        while (true)
        {
            string url = $"{baseUrl}/users/lms/courses?skip={nextSkip}&limit={limitPerPage}";
            string body;

            Debug.Log($"[CourseNotifyUI] GET {url}");

            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Authorization", "Bearer " + token);
                req.SetRequestHeader("Accept", "application/json");

                string xData = LmsSecurityHeader.BuildXDataHeader();
                req.SetRequestHeader("x-data", xData);

                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                             req.result == UnityWebRequest.Result.ProtocolError;
#else
                bool error = req.isNetworkError || req.isHttpError;
#endif

                body = req.downloadHandler.text;

                Debug.Log($"[CourseNotifyUI] RESPONSE code={req.responseCode}, error={req.error}, body={body}");

                if (error)
                {
                    Debug.LogError($"[CourseNotifyUI] GET my courses failed: {req.responseCode}\n{body}");
                    yield break;
                }
            }

            string arr = ExtractNamedArray(body, "list");
            if (string.IsNullOrEmpty(arr))
                arr = ExtractNamedArray(body, "items");

            if (string.IsNullOrEmpty(arr))
            {
                Debug.LogWarning("[CourseNotifyUI] Cannot find list/items array in response.");
                break;
            }

            List<string> items = SplitTopLevelObjects(arr);
            total += items.Count;

            Debug.Log($"[CourseNotifyUI] Parsed page count={items.Count}, total={total}");

            if (items.Count < limitPerPage)
                break;

            nextSkip += limitPerPage;
        }

        onDone?.Invoke(total);
    }

    private void ShowNewCourseNotify(int diff)
    {
        if (circleVfx != null)
            circleVfx.gameObject.SetActive(true);

        if (newCourseText != null)
        {
            newCourseText.text = "+" + diff;
            newCourseText.gameObject.SetActive(true);
        }

        if (shakeNotificationRoot != null)
            shakeNotificationRoot.SetActive(true);

        StartVfx();

        if (shakeNotification != null)
            shakeNotification.StartShake();

        Debug.Log($"[CourseNotifyUI] SHOW notify +{diff}");
    }

    public void MarkAsSeen()
    {
        MarkAsSeenPriority();
    }
    private static bool s_isMarkingSeen = false;

    private void MarkAsSeenPriority()
    {
        if (s_isMarkingSeen) return;
        s_isMarkingSeen = true;

        Debug.Log("[CourseNotifyUI] MarkAsSeenPriority FIRST -> reset notify before other button actions");

        s_userMarkedSeen = true;
        ResetGlobalNotifyState();

        CourseNotifyUI[] all = FindObjectsByType<CourseNotifyUI>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var ui in all)
        {
            if (ui == null) continue;

            ui.StopRunningCheck();
            ui.HideNotifyVisualOnly();
        }

        Debug.Log("[CourseNotifyUI] MarkAsSeenPriority DONE");

        StartCoroutine(UnlockMarkingSeenNextFrame());
    }

    private IEnumerator UnlockMarkingSeenNextFrame()
    {
        yield return null;
        s_isMarkingSeen = false;
    }

    private static void ResetGlobalNotifyState()
    {
        Debug.Log("[CourseNotifyUI] ResetGlobalNotifyState");
        s_pendingPaymentSuccessCheck = false;
        s_isNotifyShowing = false;
        s_pendingNotifyDiff = 0;
    }

private void StopRunningCheck()
{
    if (paymentCheckCoroutine != null)
    {
        StopCoroutine(paymentCheckCoroutine);
        paymentCheckCoroutine = null;
    }

    isChecking = false;
}

private void HideNotifyVisualOnly()
{
    StopVfx();

    if (newCourseText != null)
    {
        newCourseText.text = "";
        newCourseText.gameObject.SetActive(false);
    }

    if (circleVfx != null)
    {
        SetAlpha(fadeStart);
        circleVfx.gameObject.SetActive(false);
    }

    if (shakeNotification != null)
        shakeNotification.StopShake();

    if (shakeNotificationRoot != null && shakeNotificationRoot != gameObject)
        shakeNotificationRoot.SetActive(false);
}

    private void StartVfx()
    {
        fadeTween?.Kill();

        if (circleVfx == null) return;

        SetAlpha(fadeStart);

        fadeTween = circleVfx
            .DOFade(fadeEnd, fadeDuration)
            .From(fadeStart)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopVfx()
    {
        fadeTween?.Kill();
        fadeTween = null;
        SetAlpha(fadeStart);
    }

    private void SetAlpha(float value)
    {
        if (circleVfx == null) return;

        var c = circleVfx.color;
        c.a = value;
        circleVfx.color = c;
    }

    private string GetToken()
    {
        if (!string.IsNullOrWhiteSpace(overrideAccessToken))
            return NormalizeBearer(overrideAccessToken);

        if (useTokenFromStore && !string.IsNullOrWhiteSpace(TokenStore.AccessToken))
            return NormalizeBearer(TokenStore.AccessToken);

        return null;
    }

    private string NormalizeBearer(string raw)
    {
        var t = raw?.Trim() ?? "";

        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();

        return t;
    }

    private string ExtractNamedArray(string raw, string name)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        int key = raw.IndexOf($"\"{name}\"", StringComparison.OrdinalIgnoreCase);
        if (key < 0) return null;

        int bracket = raw.IndexOf('[', key);
        if (bracket < 0) return null;

        int end = FindMatchingBracket(raw, bracket, '[', ']');
        if (end <= bracket) return null;

        return raw.Substring(bracket, end - bracket + 1);
    }

    private int FindMatchingBracket(string s, int openIdx, char openCh, char closeCh)
    {
        int depth = 0;

        for (int i = openIdx; i < s.Length; i++)
        {
            char c = s[i];

            if (c == '"')
            {
                i = SkipString(s, i);
                continue;
            }

            if (c == openCh) depth++;
            else if (c == closeCh)
            {
                depth--;
                if (depth == 0) return i;
            }
        }

        return -1;
    }

    private int SkipString(string s, int startQuoteIdx)
    {
        int i = startQuoteIdx + 1;
        bool escaped = false;

        for (; i < s.Length; i++)
        {
            char c = s[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
                break;
        }

        return i;
    }

    private List<string> SplitTopLevelObjects(string arrJson)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(arrJson)) return list;

        int start = arrJson.IndexOf('[');
        int end = arrJson.LastIndexOf(']');
        if (start < 0 || end <= start) return list;

        int i = start + 1;

        while (i < end)
        {
            while (i < end && char.IsWhiteSpace(arrJson[i])) i++;
            if (i < end && arrJson[i] == ',') { i++; continue; }
            while (i < end && char.IsWhiteSpace(arrJson[i])) i++;

            if (i >= end) break;

            if (arrJson[i] == '{')
            {
                int objEnd = FindMatchingBracket(arrJson, i, '{', '}');

                if (objEnd > i)
                {
                    list.Add(arrJson.Substring(i, objEnd - i + 1));
                    i = objEnd + 1;
                }
                else break;
            }
            else
            {
                i++;
            }
        }

        return list;
    }
    public void OnPointerDown(PointerEventData eventData)
{
    if (markAsSeenButton == null) return;

    if (eventData.pointerPress == markAsSeenButton.gameObject ||
        eventData.pointerEnter == markAsSeenButton.gameObject)
    {
        MarkAsSeenPriority();
    }
}
}