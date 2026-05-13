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
    private bool _notifiedSuccess;
    private FirebaseApp _firebaseApp;

    private const string FIREBASE_APP_NAME = "XHeroLmsApp";

    [Header("DEBUG")]
    // Nếu bật, luôn lắng nghe node /login-qr/{debugFixedCode} bất kể code truyền vào
    public bool useDebugFixedCode = false;

    // Code debug để test, chỉ dùng khi useDebugFixedCode = true
    public string debugFixedCode;

    // Nếu bật, sẽ dump parent node /login-qr để kiểm tra backend có ghi đúng schema không (chỉ log, không ảnh hưởng flow)
    public bool debugDumpParentLoginQr = false;

    [Header("Firebase Key Handling")]
    // Nếu code có ký tự cấm trong Firebase key, script sẽ tự encode Base64Url để tạo key hợp lệ. Backend PHẢI ghi theo key đã encode thì mới match.
    public bool autoEncodeInvalidKey = false;

    // Nếu bật, sẽ chỉ trim + bỏ whitespace. Không thay đổi ký tự khác.
    public bool trimAndRemoveWhitespace = true;

    [Header("API step=2 config")]
    // Path cho API step=2 (trùng với step=1: /auth-for-lms/request)
    public string pathStep2 = "/auth-for-lms/request";
    public string platform = "pc";
    public float requestTimeout = 10f;

    private Coroutine _step2Co;

    public event Action<string> OnAccessTokenReceived;

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

    public void StartListen(string code)
    {
        string listenCode = code;

        // DEBUG override nếu cần
        if (useDebugFixedCode && !string.IsNullOrEmpty(debugFixedCode))
        {
            Debug.Log($"[FirebaseLoginQrPerCode] DEBUG: Override code '{code}' -> '{debugFixedCode}'");
            listenCode = debugFixedCode;
        }

        listenCode = NormalizeCode(listenCode);

        if (string.IsNullOrEmpty(listenCode))
        {
            Debug.LogError("[FirebaseLoginQrPerCode] code rỗng, không listen được.");
            return;
        }

        // Nếu key không hợp lệ -> tuỳ chọn encode
        if (!IsValidFirebaseKey(listenCode))
        {
            Debug.LogWarning($"[FirebaseLoginQrPerCode] listenCode INVALID for Firebase key: '{listenCode}'");

            if (autoEncodeInvalidKey)
            {
                string encoded = ToBase64Url(listenCode);
                Debug.LogWarning($"[FirebaseLoginQrPerCode] autoEncodeInvalidKey=TRUE -> encode '{listenCode}' => '{encoded}'");
                listenCode = encoded;
            }
            else
            {
                Debug.LogError("[FirebaseLoginQrPerCode] autoEncodeInvalidKey=FALSE -> KHÔNG listen để tránh sai path. " +
                               "Hãy sửa backend tạo key hợp lệ hoặc bật autoEncodeInvalidKey (và backend cũng phải ghi theo key đó).");
                return;
            }
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

                // OPTIONAL: dump parent schema để biết backend có ghi đúng node không
                if (debugDumpParentLoginQr)
                {
                    db.RootReference.Child("login-qr").GetValueAsync().ContinueWithOnMainThread(t =>
                    {
                        if (t.IsFaulted) Debug.LogError("[FirebaseLoginQrPerCode] debugDumpParentLoginQr error: " + t.Exception);
                        else if (t.IsCompleted)
                        {
                            var s = t.Result;
                            Debug.Log("[FirebaseLoginQrPerCode] debugDumpParentLoginQr /login-qr raw = " + s.GetRawJsonValue());
                        }
                    });
                }

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
                            Debug.LogWarning("[FirebaseLoginQrPerCode] GetValueAsync: node /login-qr/" + listenCode + " hiện KHÔNG tồn tại (có thể backend chưa ghi hoặc bạn listen sai key/schema).");
                        }
                        else
                        {
                            Debug.Log("[FirebaseLoginQrPerCode] GetValueAsync snapshot: " + s.GetRawJsonValue());
                        }
                    }
                });
            });
    }

    public void StopListen()
    {
        Debug.Log("[FirebaseLoginQrPerCode] StopListen() called (public)");
        StopListenInternal();

        _notifiedSuccess = false;

        if (_step2Co != null)
        {
            StopCoroutine(_step2Co);
            _step2Co = null;
        }
    }

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

        var options = new AppOptions
        {
            ApiKey = "AIzaSyDbJburhmWdkiH_ed6_oyQHlObe3QJLTOM",
            AppId = "1:934560089313:ios:a892cbefcba9a279ba63a0",
            ProjectId = "xhero-e1eee",
            DatabaseUrl = new Uri("https://xhero-e1eee-default-rtdb.firebaseio.com"),   
            // DatabaseUrl = new Uri("https://xhero-dev-default-rtdb.firebaseio.com"),
            MessageSenderId = "934560089313",
            StorageBucket = "xhero-e1eee.firebasestorage.app"
            
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

        string rawJson = snap.GetRawJsonValue();
        Debug.Log($"[FirebaseLoginQrPerCode] Raw snapshot at /login-qr/{snap.Key} = {rawJson}");

        // ---- ĐỌC isScanned ----
        bool isScanned = false;
        var isScannedVal = snap.Child("isScanned").Value;

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
        string browserSimple = snap.Child("browser").Value?.ToString();
        string browserName = snap.Child("browserName").Value?.ToString();
        string browserFullName = snap.Child("browserfullName").Value?.ToString();
        string ip = snap.Child("ip").Value?.ToString();
        string expireAt = snap.Child("expireAt").Value?.ToString();
        string code = snap.Child("code").Value?.ToString();
        string token = snap.Child("token").Value?.ToString();
        string accessToken = snap.Child("accessToken").Value?.ToString();
        string timestamp = snap.Child("timestamp").Value?.ToString();

        var browserNode = snap.Child("browser");
        string browserObjName = browserNode.Exists ? browserNode.Child("name").Value?.ToString() : null;
        string browserObjFull = browserNode.Exists ? browserNode.Child("fullName").Value?.ToString() : null;
        string browserUserAgent = browserNode.Exists ? browserNode.Child("userAgent").Value?.ToString() : null;

        // ==== LOG TREE ====
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

        if (!string.IsNullOrEmpty(browserName)) sb.AppendLine($"    browserName: \"{browserName}\"");
        if (!string.IsNullOrEmpty(browserFullName)) sb.AppendLine($"    browserfullName: \"{browserFullName}\"");

        sb.AppendLine($"    code: \"{code}\"");
        sb.AppendLine($"    timestamp(FB): {timestamp}");
        sb.AppendLine($"    expireAt: {expireAt}");
        sb.AppendLine($"    ip: \"{ip}\"");

        if (!string.IsNullOrEmpty(token)) sb.AppendLine($"    token: \"{token}\"");
        if (!string.IsNullOrEmpty(accessToken)) sb.AppendLine($"    accessToken: \"{accessToken}\"");

        Debug.Log(sb.ToString());
        // =================

        if (_notifiedSuccess)
        {
            Debug.Log("[FirebaseLoginQrPerCode] Already notified success, ignore further changes.");
            return;
        }

        // Nếu node có sẵn token -> dùng luôn
        string finalTokenInNode = null;
        if (!string.IsNullOrEmpty(accessToken)) finalTokenInNode = accessToken;
        else if (!string.IsNullOrEmpty(token)) finalTokenInNode = token;

        if (!string.IsNullOrEmpty(finalTokenInNode))
        {
            Debug.Log("[FirebaseLoginQrPerCode] Có sẵn token/accessToken trong node -> dùng luôn, không gọi step=2.");
            NotifySuccess(finalTokenInNode);
            return;
        }

        // fallback timestamp từ store
        string timestampFromStore = null;
        if (LmsStore.Instance != null)
            timestampFromStore = LmsStore.Instance.lastLoginQrTimestamp;

        if (string.IsNullOrEmpty(timestamp) && !string.IsNullOrEmpty(timestampFromStore))
        {
            Debug.Log($"[FirebaseLoginQrPerCode] Firebase không có timestamp -> dùng LmsStore.lastLoginQrTimestamp = {timestampFromStore}");
            timestamp = timestampFromStore;
        }

        if (string.IsNullOrEmpty(timestamp))
        {
            Debug.LogError("[FirebaseLoginQrPerCode] timestamp đang RỖNG (Firebase không có, LmsStore cũng không có). BE cần timestamp từ step=1 -> không gọi step=2 được.");
            return;
        }

        string codeToUse = !string.IsNullOrEmpty(code) ? code : snap.Key;

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

            long statusCode = req.responseCode;
            string body = req.downloadHandler != null ? req.downloadHandler.text : "<null>";

            if (hasError)
            {
                Debug.LogError(
                    $"[FirebaseLoginQrPerCode] step=2 request error: {req.error}, statusCode={statusCode}, body={body}"
                );
                yield break;
            }

            Debug.Log($"[FirebaseLoginQrPerCode] step=2 response (status={statusCode}): {body}");

            string finalToken = null;

            try
            {
                Step2Response resp = JsonUtility.FromJson<Step2Response>(body);
                if (resp != null)
                {
                    if (resp.data != null && !string.IsNullOrEmpty(resp.data.accessToken))
                        finalToken = resp.data.accessToken;
                    else if (!string.IsNullOrEmpty(resp.token))
                        finalToken = resp.token;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[FirebaseLoginQrPerCode] Parse step=2 JSON fail, dùng raw string: " + e);
            }

            if (string.IsNullOrEmpty(finalToken))
                finalToken = body.Trim().Trim('"');

            if (string.IsNullOrEmpty(finalToken))
            {
                Debug.LogError("[FirebaseLoginQrPerCode] step=2 không lấy được token hợp lệ.");
                yield break;
            }

            Debug.Log("[FirebaseLoginQrPerCode] step=2 OK, nhận token = " + finalToken);
            NotifySuccess(finalToken);
        }
    }

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

    private string NormalizeCode(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        string s = input;

        if (trimAndRemoveWhitespace)
        {
            s = s.Trim();
            // remove whitespace inside too (space, tab, newline)
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (!char.IsWhiteSpace(c))
                    sb.Append(c);
            }
            s = sb.ToString();
        }

        return s;
    }

    private bool IsValidFirebaseKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;

        for (int i = 0; i < key.Length; i++)
        {
            char c = key[i];
            if (c == '.' || c == '#' || c == '$' || c == '[' || c == ']' || c == '/')
                return false;
            if (char.IsControl(c))
                return false;
        }
        return true;
    }

    private string ToBase64Url(string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        string b64 = Convert.ToBase64String(bytes);
        // Base64Url: + -> -, / -> _, trim '='
        return b64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    [Serializable]
    private class Step2Response
    {
        public bool status;
        public bool step;
        public string message;
        public int statusCode;
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
