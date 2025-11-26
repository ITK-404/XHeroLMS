using System;
using System.Text;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseLoginQrPerCode : MonoBehaviour
{
    public static FirebaseLoginQrPerCode Instance { get; private set; }

    private DatabaseReference _currentRef;
    private bool _notifiedSuccess;   // tránh bắn event nhiều lần
    private FirebaseApp _firebaseApp;

    private const string FIREBASE_APP_NAME = "XHeroLmsApp"; // tên app custom để dùng lại

    // Event bắn ra accessToken khi backend ghi vào Firebase
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

    /// <summary>
    /// Bắt đầu lắng nghe node login-qr/{code}
    /// </summary>
    public void StartListen(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogError("[FirebaseLoginQrPerCode] code rỗng, không listen được.");
            return;
        }

        Debug.Log("[FirebaseLoginQrPerCode] StartListen for code = " + code);

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

                // Dùng AppOptions thủ công, KHÔNG cần google-services.json
                var app = EnsureFirebaseApp();
                if (app == null)
                {
                    Debug.LogError("[FirebaseLoginQrPerCode] Không tạo được FirebaseApp, dừng StartListen.");
                    return;
                }

                var db = FirebaseDatabase.GetInstance(app);

                // login-qr/<code>
                _currentRef = db.RootReference.Child("login-qr").Child(code);
                _currentRef.ValueChanged += OnQrNodeChanged;

                Debug.Log("[FirebaseLoginQrPerCode] Listening at /login-qr/" + code);
            });
    }

    /// <summary>
    /// Tạo (hoặc lấy lại) FirebaseApp dựa trên config web mà sếp gửi.
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

        // ====== CONFIG LẤY TỪ firebaseConfig BÊN WEB ======
        // const firebaseConfig = {
        //   apiKey: "AIzaSyBxffhvcu1CFrusqiI1nWv2axHd7vuVkNo",
        //   authDomain: "xhero-dev.firebaseapp.com",
        //   databaseURL: "https://xhero-dev-default-rtdb.firebaseio.com",
        //   projectId: "xhero-dev",
        //   storageBucket: "xhero-dev.firebasestorage.app",
        //   messagingSenderId: "175094863110",
        //   appId: "1:175094863110:web:aa802adb6857f4469efa87",
        //   measurementId: "G-5B3MMR68Q5"
        // };

        var options = new AppOptions
        {
            ApiKey        = "AIzaSyBxffhvcu1CFrusqiI1nWv2axHd7vuVkNo",
            AppId         = "1:175094863110:web:aa802adb6857f4469efa87",
            ProjectId     = "xhero-dev",
            DatabaseUrl   = new Uri("https://xhero-lms-default-rtdb.firebaseio.com"),
            MessageSenderId = "175094863110",
            StorageBucket   = "xhero-dev.firebasestorage.app"
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

        // Không Destroy FirebaseApp ở đây, vì có thể chỗ khác còn dùng
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

        // ===== RAW JSON (cả cục object) =====
        string rawJson = snap.GetRawJsonValue();
        Debug.Log($"[FirebaseLoginQrPerCode] Raw snapshot at /login-qr/{snap.Key} = {rawJson}");

        // Các field có thể có ở root
        string browserSimple   = snap.Child("browser").Value?.ToString();          // nếu backend ghi browser: "..."
        string browserName     = snap.Child("browserName").Value?.ToString();
        string browserFullName = snap.Child("browserfullName").Value?.ToString();
        string ip              = snap.Child("ip").Value?.ToString();
        string expireAt        = snap.Child("expireAt").Value?.ToString();
        string code            = snap.Child("code").Value?.ToString();
        string token           = snap.Child("token").Value?.ToString();            // tuỳ backend
        string accessToken     = snap.Child("accessToken").Value?.ToString();      // tuỳ backend

        // Nếu backend trả object con "browser": { name: "...", fullName: "...", ... }
        var browserNode = snap.Child("browser");
        string browserObjName     = browserNode.Exists ? browserNode.Child("name").Value?.ToString()      : null;
        string browserObjFullName = browserNode.Exists ? browserNode.Child("fullName").Value?.ToString()  : null;
        string browserUserAgent   = browserNode.Exists ? browserNode.Child("userAgent").Value?.ToString() : null;

        // ==== LOG THEO DẠNG CÂY ====
        var sb = new StringBuilder();
        sb.AppendLine("[FirebaseLoginQrPerCode]");
        sb.AppendLine("login-qr");
        sb.AppendLine($"  {snap.Key}");

        // browser: có thể là string hoặc object
        if (!string.IsNullOrEmpty(browserSimple))
            sb.AppendLine($"    browser: \"{browserSimple}\"");
        if (!string.IsNullOrEmpty(browserObjName) ||
            !string.IsNullOrEmpty(browserObjFullName) ||
            !string.IsNullOrEmpty(browserUserAgent))
        {
            sb.AppendLine("    browser (object):");
            sb.AppendLine($"      name: \"{browserObjName}\"");
            sb.AppendLine($"      fullName: \"{browserObjFullName}\"");
            sb.AppendLine($"      userAgent: \"{browserUserAgent}\"");
        }

        sb.AppendLine($"    browserName: \"{browserName}\"");
        sb.AppendLine($"    browserfullName: \"{browserFullName}\"");
        sb.AppendLine($"    code: \"{code}\"");
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

        // CHỈ coi là login thành công khi có token hoặc accessToken
        string finalToken = null;
        if (!string.IsNullOrEmpty(accessToken))
            finalToken = accessToken;
        else if (!string.IsNullOrEmpty(token))
            finalToken = token;

        if (string.IsNullOrEmpty(finalToken))
        {
            // Trường hợp này: backend có thể mới chỉ ghi info browser / handshake
            // → chỉ log, tiếp tục listen chờ lần update sau có token/accessToken.
            Debug.Log("[FirebaseLoginQrPerCode] Chưa có token/accessToken, mới chỉ nhận info browser/handshake. Tiếp tục listen.");
            return;
        }

        // Đến đây tức là đã có token/accessToken hợp lệ
        _notifiedSuccess = true;

        Debug.Log("[FirebaseLoginQrPerCode] Notify listeners with token = " + finalToken);
        OnAccessTokenReceived?.Invoke(finalToken);

        // Sau khi đã login thành công thì không cần nghe nữa
        StopListenInternal();
    }
}
