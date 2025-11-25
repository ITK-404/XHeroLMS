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
        if (_currentRef != null)
        {
            Debug.Log("[FirebaseLoginQrPerCode] Remove old listener");
            _currentRef.ValueChanged -= OnQrNodeChanged;
            _currentRef = null;
        }

        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                Debug.Log("[FirebaseLoginQrPerCode] CheckAndFixDependenciesAsync result = " + task.Result);

                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError("[FirebaseLoginQrPerCode] Firebase dependency error: " + task.Result);
                    return;
                }

                var db = FirebaseDatabase.DefaultInstance;

                // login-qr/<code>
                _currentRef = db.RootReference.Child("login-qr").Child(code);

                _currentRef.ValueChanged += OnQrNodeChanged;
                Debug.Log("[FirebaseLoginQrPerCode] Listening at /login-qr/" + code);
            });
    }

    private void OnDestroy()
    {
        if (_currentRef != null)
            _currentRef.ValueChanged -= OnQrNodeChanged;
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

        // Log raw object giống realtime
        string rawJson = snap.GetRawJsonValue();
        Debug.Log($"[FirebaseLoginQrPerCode] Raw snapshot at /login-qr/{snap.Key} = {rawJson}");

        string browserName     = snap.Child("browserName").Value?.ToString();
        string browserFullName = snap.Child("browserfullName").Value?.ToString();
        string ip              = snap.Child("ip").Value?.ToString();
        string expireAt        = snap.Child("expireAt").Value?.ToString();
        string code            = snap.Child("code").Value?.ToString();
        string token           = snap.Child("token").Value?.ToString();       // tuỳ backend
        string accessToken     = snap.Child("accessToken").Value?.ToString(); // tuỳ backend

        // ==== LOG THEO DẠNG CÂY ====
        var sb = new StringBuilder();
        sb.AppendLine("[FirebaseLoginQrPerCode]");
        sb.AppendLine("login-qr");
        sb.AppendLine($"  {snap.Key}");
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

        // Khi node thay đổi ⇒ lấy token nếu có, không thì fallback về code (test)
        if (!_notifiedSuccess)
        {
            string finalToken = !string.IsNullOrEmpty(accessToken) ? accessToken : token;

            if (string.IsNullOrEmpty(finalToken))
            {
                finalToken = code;   // Fallback: dùng code
                Debug.LogWarning("[FirebaseLoginQrPerCode] Không thấy token/accessToken, fallback dùng code.");
            }

            if (!string.IsNullOrEmpty(finalToken))
            {
                _notifiedSuccess = true;

                Debug.Log("[FirebaseLoginQrPerCode] Notify listeners with token = " + finalToken);
                OnAccessTokenReceived?.Invoke(finalToken);

                // Sau khi đã login thành công thì không cần nghe nữa
                StopListenInternal();
            }
            else
            {
                Debug.LogWarning("[FirebaseLoginQrPerCode] Node thay đổi nhưng không có code/token/accessToken hợp lệ.");
            }
        }
    }
}
