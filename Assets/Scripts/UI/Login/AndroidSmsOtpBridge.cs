using UnityEngine;

public sealed class AndroidSmsOtpBridge : MonoBehaviour
{
    public const string BridgeGameObjectName = "XHeroSmsOtpBridge";

    private static AndroidSmsOtpBridge _instance;
    private static OtpVerificationController _target;
    private static string _pendingOtp;
    private static string _pendingError;

    public static AndroidSmsOtpBridge Ensure(OtpVerificationController target)
    {
        if (target != null)
            _target = target;

        if (_instance == null)
        {
            var existing = GameObject.Find(BridgeGameObjectName);
            _instance = existing != null
                ? existing.GetComponent<AndroidSmsOtpBridge>()
                : null;

            if (_instance == null)
            {
                var go = existing != null ? existing : new GameObject(BridgeGameObjectName);
                _instance = go.AddComponent<AndroidSmsOtpBridge>();
            }

            DontDestroyOnLoad(_instance.gameObject);
        }

        _instance.FlushPendingCallbacks();
        return _instance;
    }

    public void OnAndroidSmsOtpReceived(string code)
    {
        Debug.Log("[OTP SMS] Bridge received OTP callback from Android.");

        if (_target != null)
        {
            _target.OnAndroidSmsOtpReceived(code);
            return;
        }

        _pendingOtp = code;
    }

    public void OnAndroidSmsOtpError(string error)
    {
        Debug.Log("[OTP SMS] Bridge received Android callback: " + error);

        if (_target != null)
        {
            _target.OnAndroidSmsOtpError(error);
            return;
        }

        _pendingError = error;
    }

    private void FlushPendingCallbacks()
    {
        if (_target == null)
            return;

        if (!string.IsNullOrEmpty(_pendingOtp))
        {
            var otp = _pendingOtp;
            _pendingOtp = null;
            _target.OnAndroidSmsOtpReceived(otp);
        }

        if (!string.IsNullOrEmpty(_pendingError))
        {
            var error = _pendingError;
            _pendingError = null;
            _target.OnAndroidSmsOtpError(error);
        }
    }
}
