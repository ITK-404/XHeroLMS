using System;
using System.Text;
using System.Collections;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.Networking;

public class FirebaseLoginQrPerCode : MonoBehaviour
{
    public static FirebaseLoginQrPerCode Instance { get; private set; }

    private DatabaseReference _currentRef;
    private bool _notifiedSuccess;   // tránh bắn event nhiều lần
    private FirebaseApp _firebaseApp;

    private const string FIREBASE_APP_NAME = "XHeroLmsApp"; // tên app custom để dùng lại

    [Header("DEBUG")]
    [Tooltip("Nếu bật, luôn lắng nghe node /login-qr/{debugFixedCode} bất kể code truyền vào")]
    public bool useDebugFixedCode = false;   // để false mặc định

    [Tooltip("Code debug để test, chỉ dùng khi useDebugFixedCode = true")]
    public string debugFixedCode;            // không gán mặc định trong code

    [Header("API step=2 config")]
    [Tooltip("Path cho API step=2 (trùng với step=1: /auth-for-lms/request)")]
    public string pathStep2 = "/auth-for-lms/request";
    public string platform  = "pc";
    public float  requestTimeout = 10f;

    private Coroutine _step2Co;

    // Event bắn ra accessToken khi backend trả về
    public event Action<string> OnAccessTokenReceived;

    // ============================================================
    //  Singleton
    // ============================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ============================================================
    //  BẮT ĐẦU LISTEN
    // ============================================================
    /// <summary>
    /// Bắt đầu lắng nghe node login-qr/{code}
    /// </summary>
    public void StartListen(string code)
    {
        string listenCode = code;

        // DEBUG override nếu cần (chỉ khi bạn tick trong Inspector và nhập code)
        if (useDebugFixedCode && !string.IsNullOrEmpty(debugFixedCode))
        {
            Debug.Log($"[FirebaseLoginQrPerCode] DEBUG: Override code '{code}' -> '{debugFixedCode}'");
            listenCode = debugFixedCode;
        }

        if (string.IsNullOrEmpty(listenCode))
        {
            Debug.LogError("[FirebaseLoginQrPerCode] code rỗng, không listen được.");
            return;
        }

        Debug.Log("[FirebaseLoginQrPerCode] StartListen for code = " + listenCode);

        _notifiedSuccess = false;

        // Ngắt listener cũ (nếu có)
        StopListenInternal();

        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                Debug.Log("[FirebaseLoginQrPerCode] CheckAndFixDependenciesAsync result = " + task.Result);

                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError("[FirebaseLoginQrPerCode] Firebase dependency error: " + task.Result);
                    return;
                }

                var app = EnsureFirebaseApp();
                if (app == null)
                {
                    Debug.LogError("[FirebaseLoginQrPerCode] Không tạo được FirebaseApp, dừng StartListen.");
                    return;
                }

                var db = FirebaseDatabase.GetInstance(app);
                Debug.Log("[FirebaseLoginQrPerCode] Using DB URL = " + db.App.Options.DatabaseUrl);

                // login-qr/<code>
                _currentRef = db.RootReference.Child("login-qr").Child(listenCode);
                _currentRef.ValueChanged += OnQrNodeChanged;

                Debug.Log("[FirebaseLoginQrPerCode] Listening at /login-qr/" + listenCode);

                // Test 1 lần xem node có tồn tại chưa
                _currentRef.GetValueAsync().ContinueWithOnMainThread(t =>
                {
                    if (t.IsFaulted)
                    {
                        Debug.LogError("[FirebaseLoginQrPerCode] GetValueAsync lỗi: " + t.Exception);
                    }
                    else if (t.IsCanceled)
                    {
                        Debug.LogWarning("[FirebaseLoginQrPerCode] GetValueAsync bị cancel.");
                    }
                    else if (t.IsCompleted)
                    {
                        var s = t.Result;
                        if (!s.Exists)
                        {
                            Debug.LogWarning("[FirebaseLoginQrPerCode] GetValueAsync: node /login-qr/" + listenCode + " hiện KHÔNG tồn tại.");
                        }
                        else
                        {
                            Debug.Log("[FirebaseLoginQrPerCode] GetValueAsync snapshot: " + s.GetRawJsonValue());
                        }
                    }
                });
            });
    }

    // ============================================================
    //  FIREBASE APP
    // ============================================================
    /// <summary>
    /// Tạo (hoặc lấy lại) FirebaseApp dựa trên config web (xhero-e1eee).
    /// Không phụ thuộc google-services.json.
    /// </summary>
    private FirebaseApp EnsureFirebaseApp()
    {
        if (_firebaseApp != null)
            return _firebaseApp;

        // Nếu app đã được tạo ở chỗ khác với tên này, lấy lại
        try
        {
            _firebaseApp = FirebaseApp.GetInstance(FIREBASE_APP_NAME);
            if (_firebaseApp != null)
            {
                Debug.Log("[FirebaseLoginQrPerCode] Reuse existing FirebaseApp: " + FIREBASE_APP_NAME);
                return _firebaseApp;
            }
        }
        catch
        {
            // ignore, sẽ tạo mới
        }

        // ====== CONFIG MỚI TỪ firebaseConfig (xhero-e1eee) ======
        var options = new AppOptions
        {
            ApiKey          = "AIzaSyCw9j5LCQuupiidpQ3nEplzujl7l2LfIc8",
            AppId           = "1:934560089313:web:8edd540216d9dcd6ba63a0",
            ProjectId       = "xhero-e1eee",
            DatabaseUrl     = new Uri("https://xhero-e1eee-default-rtdb.firebaseio.com"),
            MessageSenderId = "934560089313",
            StorageBucket   = "xhero-e1eee.firebasestorage.app"
        };

        try
        {
            _firebaseApp = FirebaseApp.Create(options, FIREBASE_APP_NAME);
            Debug.Log("[FirebaseLoginQrPerCode] Created FirebaseApp with AppOptions: " + FIREBASE_APP_NAME);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[FirebaseLoginQrPerCode] FirebaseApp.Create failed (maybe already created): " + e.Message);
            try
            {
                _firebaseApp = FirebaseApp.GetInstance(FIREBASE_APP_NAME);
            }
            catch (Exception e2)
            {
                Debug.LogError("[FirebaseLoginQrPerCode] GetInstance after Create failed: " + e2);
                _firebaseApp = null;
            }
        }

        return _firebaseApp;
    }

    // ============================================================
    //  CLEANUP
    // ============================================================
    private void OnDestroy()
    {
        if (_currentRef != null)
            _currentRef.ValueChanged -= OnQrNodeChanged;

        if (_step2Co != null)
        {
            StopCoroutine(_step2Co);
            _step2Co = null;
        }
    }

    private void StopListenInternal()
    {
        if (_currentRef != null)
        {
            Debug.Log("[FirebaseLoginQrPerCode] StopListenInternal()");
            _currentRef.ValueChanged -= OnQrNodeChanged;
            _currentRef = null;
        }
    }

    // ============================================================
    //  HANDLE VALUE CHANGED
    // ============================================================
    private void OnQrNodeChanged(object sender, ValueChangedEventArgs args)
    {
        Debug.Log("[FirebaseLoginQrPerCode] OnQrNodeChanged CALLED");

        if (args.DatabaseError != null)
        {
            Debug.LogError("[FirebaseLoginQrPerCode] error: " + args.DatabaseError.Message);
            return;
        }

        var snap = args.Snapshot;
        if (!snap.Exists)
        {
            Debug.Log("[FirebaseLoginQrPerCode] snapshot not exists at key = " + snap.Key);
            return;
        }

        // ===== RAW JSON (cả cục object) =====
        string rawJson = snap.GetRawJsonValue();
        Debug.Log($"[FirebaseLoginQrPerCode] Raw snapshot at /login-qr/{snap.Key} = {rawJson}");

        // ---- ĐỌC isScanned ----
        bool isScanned = false;
        var  isScannedVal = snap.Child("isScanned").Value;

        Debug.Log($"[FirebaseLoginQrPerCode] isScanned raw = {(isScannedVal == null ? "null" : isScannedVal + " (" + isScannedVal.GetType().Name + ")")}");

        if (isScannedVal != null)
        {
            if (isScannedVal is bool b)
            {
                isScanned = b;
            }
            else
            {
                string s = isScannedVal.ToString();
                if (bool.TryParse(s, out var tmpBool))
                    isScanned = tmpBool;
                else if (s == "1")
                    isScanned = true;
            }
        }

        if (!isScanned)
        {
            Debug.Log("[FirebaseLoginQrPerCode] isScanned = false -> chỉ log, chưa xử lý login.");
            return;
        }

        // Các field có thể có ở root
        string browserSimple   = snap.Child("browser").Value?.ToString();
        string browserName     = snap.Child("browserName").Value?.ToString();
        string browserFullName = snap.Child("browserfullName").Value?.ToString();
        string ip              = snap.Child("ip").Value?.ToString();
        string expireAt        = snap.Child("expireAt").Value?.ToString();
        string code            = snap.Child("code").Value?.ToString();
        string token           = snap.Child("token").Value?.ToString();       // tuỳ backend
        string accessToken     = snap.Child("accessToken").Value?.ToString(); // tuỳ backend
        string timestamp       = snap.Child("timestamp").Value?.ToString();   // *** QUAN TRỌNG ***

        var    browserNode      = snap.Child("browser");
        string browserObjName   = browserNode.Exists ? browserNode.Child("name").Value?.ToString()      : null;
        string browserObjFull   = browserNode.Exists ? browserNode.Child("fullName").Value?.ToString()  : null;
        string browserUserAgent = browserNode.Exists ? browserNode.Child("userAgent").Value?.ToString() : null;

        // ==== LOG THEO DẠNG CÂY ====
        var sb = new StringBuilder();
        sb.AppendLine("[FirebaseLoginQrPerCode]");
        sb.AppendLine("login-qr");
        sb.AppendLine($"  {snap.Key}");
        sb.AppendLine($"    isScanned: {isScanned}");

        if (!string.IsNullOrEmpty(browserSimple))
            sb.AppendLine($"    browser: \"{browserSimple}\"");
        if (!string.IsNullOrEmpty(browserObjName) ||
            !string.IsNullOrEmpty(browserObjFull) ||
            !string.IsNullOrEmpty(browserUserAgent))
        {
            sb.AppendLine("    browser (object):");
            sb.AppendLine($"      name: \"{browserObjName}\"");
            sb.AppendLine($"      fullName: \"{browserObjFull}\"");
            sb.AppendLine($"      userAgent: \"{browserUserAgent}\"");
        }

        sb.AppendLine($"    browserName: \"{browserName}\"");
        sb.AppendLine($"    browserfullName: \"{browserFullName}\"");
        sb.AppendLine($"    code: \"{code}\"");
        sb.AppendLine($"    timestamp(FB): {timestamp}");
        sb.AppendLine($"    expireAt: {expireAt}");
        sb.AppendLine($"    ip: \"{ip}\"");

        if (!string.IsNullOrEmpty(token))
            sb.AppendLine($"    token: \"{token}\"");
        if (!string.IsNullOrEmpty(accessToken))
            sb.AppendLine($"    accessToken: \"{accessToken}\"");

        Debug.Log(sb.ToString());
        // ==========================

        // Nếu đã notify 1 lần rồi thì bỏ qua các update sau
        if (_notifiedSuccess)
        {
            Debug.Log("[FirebaseLoginQrPerCode] Already notified success, ignore further changes.");
            return;
        }

        // Nếu backend có ghi sẵn token/accessToken vào node -> dùng luôn, khỏi gọi step=2
        string finalTokenInNode = null;
        if (!string.IsNullOrEmpty(accessToken))
            finalTokenInNode = accessToken;
        else if (!string.IsNullOrEmpty(token))
            finalTokenInNode = token;

        if (!string.IsNullOrEmpty(finalTokenInNode))
        {
            Debug.Log("[FirebaseLoginQrPerCode] Có sẵn token/accessToken trong node -> dùng luôn, không gọi step=2.");
            NotifySuccess(finalTokenInNode);
            return;
        }

        // Fallback timestamp từ LmsStore (step=1)
        string timestampFromStore = null;
        if (LmsStore.Instance != null)
        {
            timestampFromStore = LmsStore.Instance.lastLoginQrTimestamp;
        }

        if (string.IsNullOrEmpty(timestamp) && !string.IsNullOrEmpty(timestampFromStore))
        {
            Debug.Log($"[FirebaseLoginQrPerCode] Firebase không có timestamp -> dùng LmsStore.lastLoginQrTimestamp = {timestampFromStore}");
            timestamp = timestampFromStore;
        }

        // Nếu vẫn rỗng thì không gọi step=2 nữa vì BE require cả code + timestamp
        if (string.IsNullOrEmpty(timestamp))
        {
            Debug.LogError("[FirebaseLoginQrPerCode] timestamp đang RỖNG (Firebase không có, LmsStore cũng không có). BE cần timestamp từ step=1 -> không gọi step=2 được.");
            return;
        }

        // Nếu chưa có token trong node, nhưng isScanned = true -> gọi API step=2 để lấy JWT
        string codeToUse = !string.IsNullOrEmpty(code) ? code : snap.Key; // fallback: dùng key nếu field code trống
        Debug.Log("[FirebaseLoginQrPerCode] isScanned = true nhưng chưa có token/accessToken -> gọi API step=2 với code = " + codeToUse + ", timestamp = " + timestamp);

        if (!string.IsNullOrEmpty(codeToUse))
        {
            if (_step2Co != null)
            {
                StopCoroutine(_step2Co);
                _step2Co = null;
            }
            _step2Co = StartCoroutine(CoCallStep2(codeToUse, timestamp));
        }
        else
        {
            Debug.LogWarning("[FirebaseLoginQrPerCode] Không tìm được code để gọi step=2.");
        }
    }

    // ============================================================
    //  CALL STEP=2 (GET + timestamp)
    // ============================================================
    /// <summary>
    /// Gọi API auth-for-lms/request?step=2&platform=pc&code=<code>&timestamp=<timestamp> để lấy JWT
    /// </summary>
    private IEnumerator CoCallStep2(string code, string timestamp)
    {
        string baseUrl = LmsStore.Instance != null ? LmsStore.Instance.baseUrl : "";
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[FirebaseLoginQrPerCode] LmsStore.Instance.baseUrl rỗng, không gọi được API step=2.");
            yield break;
        }

        if (string.IsNullOrEmpty(timestamp))
        {
            Debug.LogError("[FirebaseLoginQrPerCode] CoCallStep2 được gọi nhưng timestamp rỗng -> abort.");
            yield break;
        }

        string url = $"{baseUrl}{pathStep2}?step=2&platform={platform}&code={UnityWebRequest.EscapeURL(code)}&timestamp={UnityWebRequest.EscapeURL(timestamp)}";

        Debug.Log("[FirebaseLoginQrPerCode] Call step=2 URL = " + url);

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = (int)requestTimeout;

#if UNITY_2020_2_OR_NEWER
            yield return req.SendWebRequest();
            bool hasError = req.result != UnityWebRequest.Result.Success;
#else
            yield return req.SendWebRequest();
            bool hasError = req.isNetworkError || req.isHttpError;
#endif

            long   statusCode = req.responseCode;
            string body       = req.downloadHandler != null ? req.downloadHandler.text : "<null>";

            if (hasError)
            {
                Debug.LogError(
                    $"[FirebaseLoginQrPerCode] step=2 request error: {req.error}, " +
                    $"statusCode={statusCode}, body={body}"
                );
                yield break;
            }

            Debug.Log($"[FirebaseLoginQrPerCode] step=2 response (status={statusCode}): {body}");

            // Thử parse theo kiểu status + data.accessToken, hoặc token, hoặc đơn giản là raw JWT string
            string finalToken = null;

            try
            {
                Step2Response resp = JsonUtility.FromJson<Step2Response>(body);
                if (resp != null)
                {
                    if (resp.data != null && !string.IsNullOrEmpty(resp.data.accessToken))
                    {
                        finalToken = resp.data.accessToken;
                    }
                    else if (!string.IsNullOrEmpty(resp.token))
                    {
                        finalToken = resp.token;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FirebaseLoginQrPerCode] Parse step=2 JSON fail, dùng raw string: " + e);
            }

            // Nếu vẫn không parse ra thì giả định backend trả thẳng JWT (plain text)
            if (string.IsNullOrEmpty(finalToken))
            {
                finalToken = body.Trim().Trim('"'); // phòng khi backend trả "jwt"
            }

            if (string.IsNullOrEmpty(finalToken))
            {
                Debug.LogError("[FirebaseLoginQrPerCode] step=2 không lấy được token hợp lệ.");
                yield break;
            }

            Debug.Log("[FirebaseLoginQrPerCode] step=2 OK, nhận token = " + finalToken);
            NotifySuccess(finalToken);
        }
    }

    // ============================================================
    //  NOTIFY
    // ============================================================
    /// <summary>
    /// Gửi token ra ngoài + stop listen
    /// </summary>
    private void NotifySuccess(string token)
    {
        if (_notifiedSuccess)
        {
            Debug.Log("[FirebaseLoginQrPerCode] NotifySuccess called nhưng đã notify trước đó.");
            return;
        }

        _notifiedSuccess = true;

        Debug.Log("[FirebaseLoginQrPerCode] Notify listeners with token = " + token);
        OnAccessTokenReceived?.Invoke(token);

        StopListenInternal();

        if (_step2Co != null)
        {
            StopCoroutine(_step2Co);
            _step2Co = null;
        }
    }

    // ============================================================
    //  JSON STEP2
    // ============================================================
    [Serializable]
    private class Step2Response
    {
        public bool   status;
        public bool   step;
        public string message;
        public int    statusCode;
        public string token;
        public Step2Data data;
    }

    [Serializable]
    private class Step2Data
    {
        public string accessToken;
        public string userId;
    }
}
