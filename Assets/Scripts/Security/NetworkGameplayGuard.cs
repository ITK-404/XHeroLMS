using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class NetworkGameplayGuard : MonoBehaviour
{
    private static NetworkGameplayGuard instance;

    // Để trống thì tự lấy scene đang đặt code lần đầu làm scene gameplay đầu tiên.")]
    private string firstGameplaySceneName = "";

    // Các scene trung gian/loading không được lưu làm scene hiện tại.")]
    [SerializeField] private string[] ignoredSceneNames =
    {
        "LoadingScene",
        "Loading Scene"
    };

    //Tự bỏ qua mọi scene có chữ Loading trong tên.
    private bool autoIgnoreLoadingSceneName = true;

    //Nếu sau 5 phút bị đưa về scene đầu, khi có mạng lại thì tự quay về scene gameplay trước đó.
    private bool autoResumeLastGameplaySceneWhenOnline = true;

    //Nếu project đang load scene bằng LoadingTransition.Load_Scene thì bật cái này.
    private bool useLoadingTransitionIfAvailable = true;

    //Tham số bool thứ 2 nếu LoadingTransition.Load_Scene(string, bool) tồn tại.
    private bool loadingTransitionSecondBool = true;

    [Header("Network Check")]
    [SerializeField] private string internetCheckUrl = "https://clients3.google.com/generate_204";

    [SerializeField] private float checkIntervalSeconds = 2f;
    [SerializeField] private int requestTimeoutSeconds = 4;
    [SerializeField] private float startupGraceSeconds = 8f;
    [SerializeField] private float offlineConfirmSeconds = 4f;
    [SerializeField] private int offlineConfirmChecks = 2;

    [Header("Offline Flow")]
    [SerializeField] private float retryCheckDelaySeconds = 5f;
    [SerializeField] private float weakToFatalSeconds = 300f; // 5 phút
    [SerializeField] private bool recoverBootFlowWhenOnline = true;
    [SerializeField] private float bootFlowRecoveryCooldownSeconds = 2f;

    [Header("Popup Text")]
    [SerializeField] private string weakHeader = "Mạng không ổn định";
    [SerializeField] private string weakMessage = "Đường truyền yếu";
    [SerializeField] private string weakButtonText = "Tải lại";

    [SerializeField] private string fatalHeader = "Mất kết nối";
    [SerializeField] private string fatalMessage = "Vui lòng kiểm tra lại đường truyền";
    [SerializeField] private string fatalButtonText = "OK";

    private string currentRealSceneName;
    private string lastGameplaySceneName;
    private string resumeSceneAfterFallback;

    private bool isOffline;
    private bool fatalPopupShowing;
    private bool networkPopupShowing;
    private bool ownsLoadingUI;

    private bool isWaitingOnlineToResumeAfterFallback;

    private float appStartedAtRealtime;
    private float offlineCandidateStartedAtRealtime = -1f;
    private float offlineStartedAtRealtime;
    private int consecutiveOfflineChecks;
    private float lastBootFlowRecoveryAtRealtime = -999f;

    private Coroutine watchRoutine;
    private Coroutine retryRoutine;
    private Coroutine returnRoutine;
    private Coroutine resumeRoutine;

    private bool ownsInputBlockerLock;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        appStartedAtRealtime = Time.realtimeSinceStartup;

        string activeSceneName = SceneManager.GetActiveScene().name;
        currentRealSceneName = activeSceneName;

        if (!IsIgnoredScene(activeSceneName))
        {
            lastGameplaySceneName = activeSceneName;

            if (string.IsNullOrWhiteSpace(firstGameplaySceneName))
                firstGameplaySceneName = activeSceneName;
        }
        else
        {
            Debug.LogWarning("[NetworkGameplayGuard] Script đang được tạo trong scene loading: " + activeSceneName +
                             ". Nên đặt script ở scene gameplay đầu tiên hoặc set First Gameplay Scene Name thủ công.");
        }

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (watchRoutine == null)
            watchRoutine = StartCoroutine(NetworkWatchRoutine());
    }

private void OnDestroy()
{
    if (instance == this)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        UnlockGameplayInputByNetwork();

        instance = null;
    }
}

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            return;

        // Khi app quay lại foreground, check sớm hơn.
        if (watchRoutine == null)
            watchRoutine = StartCoroutine(NetworkWatchRoutine());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentRealSceneName = scene.name;

        // Quan trọng:
        // Không bao giờ lưu LoadingScene làm scene gameplay hiện tại.
        if (IsIgnoredScene(scene.name))
            return;

        lastGameplaySceneName = scene.name;

        if (string.IsNullOrWhiteSpace(firstGameplaySceneName))
            firstGameplaySceneName = scene.name;
    }

    private IEnumerator NetworkWatchRoutine()
    {
        while (true)
        {
            bool hasInternet = false;
            yield return CheckInternetRoutine(result => hasInternet = result);

            if (hasInternet)
            {
                ResetOfflineCandidate();
                HandleOnline();
            }
            else
            {
                TrackOfflineCandidate();

                if (ShouldConfirmOfflineNow())
                    HandleOffline();
            }

            yield return new WaitForSecondsRealtime(checkIntervalSeconds);
        }
    }

    private IEnumerator CheckInternetRoutine(Action<bool> callback)
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            callback?.Invoke(false);
            yield break;
        }

        string url = BuildNoCacheUrl(internetCheckUrl);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = requestTimeoutSeconds;
            request.SetRequestHeader("Cache-Control", "no-cache");

            yield return request.SendWebRequest();

            callback?.Invoke(IsRequestSuccess(request));
        }
    }

    private bool IsRequestSuccess(UnityWebRequest request)
    {
#if UNITY_2020_2_OR_NEWER
        if (request.result != UnityWebRequest.Result.Success)
            return false;
#else
        if (request.isNetworkError || request.isHttpError)
            return false;
#endif

        return request.responseCode >= 200 && request.responseCode < 400;
    }

    private string BuildNoCacheUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "https://clients3.google.com/generate_204";

        string separator = url.Contains("?") ? "&" : "?";
        return $"{url}{separator}_t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

private void HandleOnline()
{
    bool shouldRecoverBootFlow = ShouldRecoverBootFlowWhenOnline();

    if (!isOffline && !networkPopupShowing && !ownsLoadingUI && !shouldRecoverBootFlow)
        return;

    isOffline = false;
    fatalPopupShowing = false;
    offlineStartedAtRealtime = 0f;

    DestroyCurrentNetworkPopupIfAny();
    UnlockGameplayInputByNetwork();

    if (ownsLoadingUI)
    {
        LoadingUI.Hide();
        ownsLoadingUI = false;
    }

    if (shouldRecoverBootFlow)
        TryRecoverBootFlowWhenOnline();

    if (isWaitingOnlineToResumeAfterFallback &&
        autoResumeLastGameplaySceneWhenOnline &&
        !string.IsNullOrWhiteSpace(resumeSceneAfterFallback) &&
        !IsIgnoredScene(resumeSceneAfterFallback) &&
        resumeSceneAfterFallback != currentRealSceneName)
    {
        if (resumeRoutine == null)
            resumeRoutine = StartCoroutine(ResumeSceneAfterNetworkBackRoutine(resumeSceneAfterFallback));
    }
    else
    {
        isWaitingOnlineToResumeAfterFallback = false;
        resumeSceneAfterFallback = "";
    }
}

    private void TrackOfflineCandidate()
    {
        consecutiveOfflineChecks++;

        if (offlineCandidateStartedAtRealtime < 0f)
            offlineCandidateStartedAtRealtime = Time.realtimeSinceStartup;
    }

    private void ResetOfflineCandidate()
    {
        consecutiveOfflineChecks = 0;
        offlineCandidateStartedAtRealtime = -1f;
    }

    private bool ShouldConfirmOfflineNow()
    {
        if (isOffline)
            return true;

        float now = Time.realtimeSinceStartup;

        if (now - appStartedAtRealtime < Mathf.Max(0f, startupGraceSeconds))
            return false;

        if (consecutiveOfflineChecks < Mathf.Max(1, offlineConfirmChecks))
            return false;

        if (offlineCandidateStartedAtRealtime < 0f)
            return false;

        return now - offlineCandidateStartedAtRealtime >= Mathf.Max(0f, offlineConfirmSeconds);
    }

    private bool ShouldRecoverBootFlowWhenOnline()
    {
        if (!recoverBootFlowWhenOnline)
            return false;

        if (Time.realtimeSinceStartup - lastBootFlowRecoveryAtRealtime < Mathf.Max(0.1f, bootFlowRecoveryCooldownSeconds))
            return false;

        BootFlow bootFlow = BootFlow.Instance;
        return bootFlow != null && bootFlow.NeedsNetworkRecovery;
    }

    private void TryRecoverBootFlowWhenOnline()
    {
        BootFlow bootFlow = BootFlow.Instance;
        if (bootFlow == null || !bootFlow.NeedsNetworkRecovery)
            return;

        lastBootFlowRecoveryAtRealtime = Time.realtimeSinceStartup;
        bootFlow.RetryAfterNetworkRestored();
    }

    private void HandleOffline()
    {
        if (retryRoutine != null || returnRoutine != null || resumeRoutine != null)
            return;

        if (!isOffline)
        {
            isOffline = true;
            fatalPopupShowing = false;
            offlineStartedAtRealtime = Time.realtimeSinceStartup;

            ShowWeakNetworkPopup();
            return;
        }

        float offlineDuration = Time.realtimeSinceStartup - offlineStartedAtRealtime;

        if (!fatalPopupShowing && offlineDuration >= weakToFatalSeconds)
        {
            ShowFatalNetworkPopup();
        }
        else if (!networkPopupShowing && !fatalPopupShowing)
        {
            ShowWeakNetworkPopup();
        }
    }

private void ShowWeakNetworkPopup()
{
    LockGameplayInputByNetwork();

    DestroyCurrentNetworkPopupIfAny();

    ownsLoadingUI = true;
    LoadingUI.Show();

    networkPopupShowing = true;
    fatalPopupShowing = false;

    LoadingUI.ShowErrorPopup(
        weakMessage,
        weakHeader,
        OnRetryButtonClicked
    );

    StartCoroutine(PatchPopupButtonTextNextFrames(weakButtonText));
}

private void ShowFatalNetworkPopup()
{
    LockGameplayInputByNetwork();

    DestroyCurrentNetworkPopupIfAny();

    ownsLoadingUI = true;
    LoadingUI.Show();

    networkPopupShowing = true;
    fatalPopupShowing = true;

    LoadingUI.ShowErrorPopup(
        fatalMessage,
        fatalHeader,
        OnFatalOkButtonClicked
    );

    StartCoroutine(PatchPopupButtonTextNextFrames(fatalButtonText));
}

    private void OnRetryButtonClicked()
    {
        networkPopupShowing = false;

        if (retryRoutine != null)
            return;

        retryRoutine = StartCoroutine(RetryCheckNetworkOnlyRoutine());
    }

    private IEnumerator RetryCheckNetworkOnlyRoutine()
    {
        // Chờ LoadingUI.ShowErrorPopup chạy Hide + Destroy popup xong.
        yield return null;

        ownsLoadingUI = true;
        LoadingUI.Show();

        // Đây chỉ là delay kiểu F5/check lại, KHÔNG reload scene.
        yield return new WaitForSecondsRealtime(retryCheckDelaySeconds);

        bool hasInternet = false;
        yield return CheckInternetRoutine(result => hasInternet = result);

        retryRoutine = null;

        if (hasInternet)
        {
            HandleOnline();
            yield break;
        }

        if (!isOffline)
        {
            isOffline = true;
            offlineStartedAtRealtime = Time.realtimeSinceStartup;
        }

        float offlineDuration = Time.realtimeSinceStartup - offlineStartedAtRealtime;

        if (offlineDuration >= weakToFatalSeconds)
            ShowFatalNetworkPopup();
        else
            ShowWeakNetworkPopup();
    }

    private void OnFatalOkButtonClicked()
    {
        networkPopupShowing = false;

        if (returnRoutine != null)
            return;

        returnRoutine = StartCoroutine(ReturnFirstGameplaySceneRoutine());
    }

    private IEnumerator ReturnFirstGameplaySceneRoutine()
    {
        // Chờ popup tự đóng xong.
        yield return null;

        ownsLoadingUI = true;
        LoadingUI.Show();

        // Lưu lại scene gameplay trước khi fallback.
        resumeSceneAfterFallback = GetSafeResumeSceneName();
        isWaitingOnlineToResumeAfterFallback = !string.IsNullOrWhiteSpace(resumeSceneAfterFallback);

        // Reset vòng offline để khi về scene đầu vẫn hiện "Đường truyền yếu" + "Tải lại",
        // không bị nhảy ngay lại popup 5 phút.
        isOffline = false;
        fatalPopupShowing = false;
        offlineStartedAtRealtime = 0f;

        string targetScene = GetFirstGameplaySceneName();

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("[NetworkGameplayGuard] Không có First Gameplay Scene Name. Hãy set trong Inspector.");
            returnRoutine = null;
            yield break;
        }

        LoadSceneSafe(targetScene);

        returnRoutine = null;
    }

    private IEnumerator ResumeSceneAfterNetworkBackRoutine(string sceneName)
    {
        isWaitingOnlineToResumeAfterFallback = false;
        resumeSceneAfterFallback = "";

        yield return null;

        ownsLoadingUI = true;
        LoadingUI.Show();

        LoadSceneSafe(sceneName);

        resumeRoutine = null;
    }

    private string GetFirstGameplaySceneName()
    {
        if (!string.IsNullOrWhiteSpace(firstGameplaySceneName) && !IsIgnoredScene(firstGameplaySceneName))
            return firstGameplaySceneName;

        if (!string.IsNullOrWhiteSpace(lastGameplaySceneName) && !IsIgnoredScene(lastGameplaySceneName))
            return lastGameplaySceneName;

        return "";
    }

    private string GetSafeResumeSceneName()
    {
        if (!string.IsNullOrWhiteSpace(lastGameplaySceneName) && !IsIgnoredScene(lastGameplaySceneName))
            return lastGameplaySceneName;

        if (!string.IsNullOrWhiteSpace(currentRealSceneName) && !IsIgnoredScene(currentRealSceneName))
            return currentRealSceneName;

        return "";
    }

    private bool IsIgnoredScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return true;

        if (ignoredSceneNames != null)
        {
            for (int i = 0; i < ignoredSceneNames.Length; i++)
            {
                string ignored = ignoredSceneNames[i];

                if (string.IsNullOrWhiteSpace(ignored))
                    continue;

                if (string.Equals(sceneName.Trim(), ignored.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        if (autoIgnoreLoadingSceneName &&
            sceneName.IndexOf("Loading", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[NetworkGameplayGuard] Scene name is empty.");
            return;
        }

        if (IsIgnoredScene(sceneName))
        {
            Debug.LogWarning("[NetworkGameplayGuard] Không load scene bị ignore: " + sceneName);
            return;
        }

        if (TryLoadByLoadingTransition(sceneName))
            return;

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    private bool TryLoadByLoadingTransition(string sceneName)
    {
        if (!useLoadingTransitionIfAvailable)
            return false;

        Type type = FindTypeInLoadedAssemblies("LoadingTransition");
        if (type == null)
            return false;

        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

        foreach (MethodInfo method in methods)
        {
            if (method.Name != "Load_Scene")
                continue;

            ParameterInfo[] p = method.GetParameters();

            try
            {
                if (p.Length == 2 &&
                    p[0].ParameterType == typeof(string) &&
                    p[1].ParameterType == typeof(bool))
                {
                    method.Invoke(null, new object[] { sceneName, loadingTransitionSecondBool });
                    return true;
                }

                if (p.Length == 1 &&
                    p[0].ParameterType == typeof(string))
                {
                    method.Invoke(null, new object[] { sceneName });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NetworkGameplayGuard] LoadingTransition.Load_Scene failed. Fallback SceneManager. " + ex.Message);
                return false;
            }
        }

        return false;
    }

    private Type FindTypeInLoadedAssemblies(string typeName)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (Assembly assembly in assemblies)
        {
            Type type = assembly.GetType(typeName);
            if (type != null)
                return type;
        }

        return null;
    }

private void LockGameplayInputByNetwork()
{
    // Chỉ lock 1 lần để tránh blockCount tăng liên tục khi popup bị show lại.
    if (ownsInputBlockerLock)
        return;

    InputBlocker.SetBlocked(true);
    ownsInputBlockerLock = true;

    Debug.Log("[NetworkGameplayGuard] Gameplay input locked by network popup.");
}

private void UnlockGameplayInputByNetwork()
{
    // Chỉ unlock nếu chính NetworkGameplayGuard là bên đã lock.
    // Tránh mở nhầm input của hệ thống khác.
    if (!ownsInputBlockerLock)
        return;

    InputBlocker.SetBlocked(false);
    ownsInputBlockerLock = false;

    Debug.Log("[NetworkGameplayGuard] Gameplay input unlocked after network restored.");
}

    private void DestroyCurrentNetworkPopupIfAny()
    {
        GameObject errorCanvas = GameObject.Find("~LoadingErrorCanvas");
        if (errorCanvas != null)
            Destroy(errorCanvas);

        GameObject updateCanvas = GameObject.Find("~LoadingUpdateCanvas");
        if (updateCanvas != null)
            Destroy(updateCanvas);

        networkPopupShowing = false;
    }

    private IEnumerator PatchPopupButtonTextNextFrames(string buttonText)
    {
        yield return null;
        PatchPopupButtonText(buttonText);

        yield return null;
        PatchPopupButtonText(buttonText);
    }

    private void PatchPopupButtonText(string buttonText)
    {
        if (string.IsNullOrWhiteSpace(buttonText))
            return;

        GameObject canvas = GameObject.Find("~LoadingErrorCanvas");
        if (canvas == null)
            return;

        Button button = canvas.GetComponentInChildren<Button>(true);
        if (button == null)
            return;

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = buttonText;
            return;
        }

        Text unityText = button.GetComponentInChildren<Text>(true);
        if (unityText != null)
            unityText.text = buttonText;
    }
}
