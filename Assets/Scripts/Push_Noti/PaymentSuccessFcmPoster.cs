using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PaymentSuccessFcmPoster : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private string fromPlatform = "lms3d";
    [SerializeField] private bool autoCheckOnStart = true;
    [SerializeField] private float pollInterval = 1f;

    private string _postUrl;
    private bool _isPosting;
    private bool _postedThisSession;

    private const string PostedOrderIdKey = "FCM_POSTED_PAYMENT_ORDER_ID";

    private void Awake()
    {
        _postUrl = LmsStore.Instance.baseUrl.TrimEnd('/') + "/users/authenticate";
    }

    private void Start()
    {
        if (autoCheckOnStart)
            StartCoroutine(CheckPaymentFinishedRoutine());
    }

    private IEnumerator CheckPaymentFinishedRoutine()
    {
        while (true)
        {
            bool finished = WebViewTest.IsPaymentFinished || WebViewTest.GetSavedPaymentFinished();

            if (finished && !_isPosting && !_postedThisSession)
            {
                string orderId = GetCurrentOrderIdSafe();

                if (!string.IsNullOrEmpty(orderId))
                {
                    string lastPostedOrderId = PlayerPrefs.GetString(PostedOrderIdKey, "");
                    if (lastPostedOrderId == orderId)
                    {
                        Debug.Log("[PaymentSuccessFcmPoster] This orderId was already posted. Skip. orderId=" + orderId);
                        _postedThisSession = true;
                    }
                    else
                    {
                        yield return StartCoroutine(PostFcmAfterPaymentSuccess(orderId));
                    }
                }
                else
                {
                    yield return StartCoroutine(PostFcmAfterPaymentSuccess(""));
                }
            }

            yield return new WaitForSecondsRealtime(pollInterval);
        }
    }

    private IEnumerator PostFcmAfterPaymentSuccess(string orderId)
    {
        _isPosting = true;

        string username = TokenStore.Username;
        string password = TokenStore.Password;
        string deviceToken = FCMManager.GetSavedFcmToken();

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) ||
            string.IsNullOrWhiteSpace(deviceToken))
        {
            Debug.LogWarning(
                "[PaymentSuccessFcmPoster] Missing data. " +
                "username=" + username +
                " | hasPassword=" + (!string.IsNullOrWhiteSpace(password)) +
                " | hasDeviceToken=" + (!string.IsNullOrWhiteSpace(deviceToken))
            );

            _isPosting = false;
            yield break;
        }

        string safeUsername = EscapeJson(username);
        string safePassword = EscapeJson(password);
        string safeDeviceToken = EscapeJson(deviceToken);
        string safeFromPlatform = EscapeJson(fromPlatform);

        string json =
            "{" +
            $"\"username\":\"{safeUsername}\"," +
            $"\"password\":\"{safePassword}\"," +
            $"\"deviceToken\":\"{safeDeviceToken}\"," +
            $"\"fromPlatform\":\"{safeFromPlatform}\"" +
            "}";

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        Debug.Log("[PaymentSuccessFcmPoster] Posting FCM token after payment success...");
        Debug.Log("[PaymentSuccessFcmPoster] URL: " + _postUrl);
        Debug.Log("[PaymentSuccessFcmPoster] orderId: " + orderId);
        Debug.Log("[PaymentSuccessFcmPoster] Request Body: " + json);

        using (UnityWebRequest request = new UnityWebRequest(_postUrl, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool hasError = request.result == UnityWebRequest.Result.ConnectionError ||
                            request.result == UnityWebRequest.Result.ProtocolError ||
                            request.result == UnityWebRequest.Result.DataProcessingError;
#else
            bool hasError = request.isNetworkError || request.isHttpError;
#endif

            Debug.Log("[PaymentSuccessFcmPoster] Response Code: " + request.responseCode);
            Debug.Log("[PaymentSuccessFcmPoster] Response Body: " + request.downloadHandler.text);

            if (hasError)
            {
                Debug.LogError("[PaymentSuccessFcmPoster] Post failed: " + request.error);
            }
            else
            {
                Debug.Log("[PaymentSuccessFcmPoster] Post success after payment.");

                _postedThisSession = true;

                if (!string.IsNullOrEmpty(orderId))
                {
                    PlayerPrefs.SetString(PostedOrderIdKey, orderId);
                    PlayerPrefs.Save();
                }
            }
        }

        _isPosting = false;
    }

    private string GetCurrentOrderIdSafe()
    {
        string runtimeOrderId = WebViewTest.CurrentOrderId;
        if (!string.IsNullOrWhiteSpace(runtimeOrderId))
            return runtimeOrderId;

        return WebViewTest.GetSavedOrderId();
    }

    private string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}