using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PaymentWebViewFlowManager : MonoBehaviour
{
    [Header("Popup Prefab (Canvas Prefab)")]
    [SerializeField] private PaymentWebViewUI popupPrefab;

    private string returnButtonName = "Hover Button Yellow Variant";
    private string actionButtonName = "Hover Button Red Variant";

    [Header("Optional Loading Text Name")]
    [SerializeField] private string loadingTextName = "StateTmp";

    [Header("Polling")]
    [SerializeField] private float pollInterval = 2f;
    [SerializeField] private bool autoCheckWhenFlagRaised = true;

    private PaymentWebViewUI currentPopup;
    private Button currentReturnBtn;
    private Button currentActionBtn;
    private TextMeshProUGUI currentStateTmp;

    private Coroutine pollingCoroutine;
    private bool isChecking;
    private bool isFinalState;
    private bool isPaymentSuccess;

    private string currentCheckingOrderId = "";
    private string currentCourseId = "";
    private string currentCourseSeo = "";
    private string currentCourseName = "";

    private string BaseUrl => SecurityConfig.GetBaseUrl();

    private void Update()
    {
        if (!autoCheckWhenFlagRaised)
            return;

        if (!WebViewTest.IsPaymentFinished)
            return;

        if (string.IsNullOrEmpty(WebViewTest.CurrentOrderId))
            return;

        if (currentPopup != null)
            return;

        Debug.Log("[PaymentWebViewFlowManager] StartPaymentCheckFlow");
        StartPaymentCheckFlow(
            WebViewTest.CurrentOrderId,
            TokenStore.AccessToken,
            WebViewTest.CurrentCourseId,
            WebViewTest.CurrentCourseSeo,
            WebViewTest.CurrentCourseName
        );
    }

private string currentPaymentUrl = "";

public void StartPaymentCheckFlow(string orderId, string accessToken, string courseId, string courseSeo, string courseName = "", string paymentUrl = "")
{
    if (string.IsNullOrWhiteSpace(orderId))
    {
        Debug.LogWarning("[PaymentWebViewFlowManager] orderId is empty.");
        return;
    }

    if (string.IsNullOrWhiteSpace(accessToken))
    {
        Debug.LogWarning("[PaymentWebViewFlowManager] accessToken is empty.");
        return;
    }

    currentCheckingOrderId = orderId;
    currentCourseId = courseId;
    currentCourseSeo = courseSeo;
    currentCourseName = courseName;
    currentPaymentUrl = paymentUrl;

    isFinalState = false;
    isPaymentSuccess = false;

    CreatePopupIfNeeded();
    SetLoadingUI();

    if (pollingCoroutine != null)
        StopCoroutine(pollingCoroutine);

    pollingCoroutine = StartCoroutine(PollOrderRoutine(orderId, accessToken));
}

    private void CreatePopupIfNeeded()
    {
        if (currentPopup != null)
            return;

        currentPopup = Instantiate(popupPrefab);
        CachePopupReferences(currentPopup.gameObject);
        BindPopupButtons();
    }

    private void CachePopupReferences(GameObject popupObject)
    {
        currentReturnBtn = null;
        currentActionBtn = null;
        currentStateTmp = null;

        Button[] buttons = popupObject.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn == null) continue;

            if (btn.name.Equals(returnButtonName, StringComparison.OrdinalIgnoreCase))
                currentReturnBtn = btn;
            else if (btn.name.Equals(actionButtonName, StringComparison.OrdinalIgnoreCase))
                currentActionBtn = btn;
        }

        TextMeshProUGUI[] texts = popupObject.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            if (txt == null) continue;

            if (txt.name.Equals(loadingTextName, StringComparison.OrdinalIgnoreCase))
            {
                currentStateTmp = txt;
                break;
            }
        }
    }

    private void BindPopupButtons()
    {
        if (currentReturnBtn != null)
        {
            currentReturnBtn.onClick.RemoveListener(OnReturnClicked);
            currentReturnBtn.onClick.AddListener(OnReturnClicked);
        }

        if (currentActionBtn != null)
        {
            currentActionBtn.onClick.RemoveListener(OnActionClicked);
            currentActionBtn.onClick.AddListener(OnActionClicked);
        }
    }

    private IEnumerator PollOrderRoutine(string orderId, string accessToken)
    {
        while (true)
        {
            yield return CheckOrderOnce(orderId, accessToken);

            if (currentPopup == null)
                yield break;

            if (isFinalState)
                yield break;

            yield return new WaitForSeconds(pollInterval);
        }
    }

    private IEnumerator CheckOrderOnce(string orderId, string accessToken)
    {
        if (isChecking)
            yield break;

        isChecking = true;

        string url = $"{BaseUrl}/orders/{orderId}";
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

            isChecking = false;

            if (currentPopup == null)
                yield break;

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[PaymentWebViewFlowManager] CheckOrder error: " + req.error);
                ShowFail("Không thể kiểm tra thanh toán");
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("[PaymentWebViewFlowManager] Order detail: " + json);

            OrderDetailResponse response = null;
            try
            {
                response = JsonUtility.FromJson<OrderDetailResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[PaymentWebViewFlowManager] Parse JSON failed: " + e.Message);
                ShowFail("Dữ liệu thanh toán không hợp lệ");
                yield break;
            }

            if (response == null || !response.status || response.data == null)
            {
                ShowFail("Không lấy được trạng thái đơn hàng");
                yield break;
            }

            string orderStatus = SafeLower(response.data.status);
            string priceText = FormatPrice(response.data.totalPrice);

            if (orderStatus == "finished")
            {
                isFinalState = true;
                isPaymentSuccess = true;

                currentPopup.ShowPayment(true, priceText);
                SetStateText("Thanh toán thành công");

                Debug.Log("[PaymentWebViewFlowManager] Payment SUCCESS -> request CourseNotifyUI check");
                CourseNotifyUI.RequestPaymentSuccessCheck();

                yield break;
            }

            if (orderStatus == "cancel")
            {
                isFinalState = true;
                isPaymentSuccess = false;

                currentPopup.ShowPayment(false, priceText);
                SetStateText("Thanh toán thất bại / đã hủy");
                yield break;
            }

            isFinalState = false;
            isPaymentSuccess = false;

            currentPopup.ShowPayment(false, priceText);
            SetStateText("Trạng thái hiện tại: " + orderStatus);
        }
    }

    private void SetLoadingUI()
    {
        if (currentPopup != null)
            currentPopup.Show();

        SetStateText("Đang kiểm tra thanh toán...");
    }

    private void ShowFail(string message)
    {
        isFinalState = true;
        isPaymentSuccess = false;

        if (currentPopup != null)
            currentPopup.ShowPayment(false, "--");

        SetStateText(message);
    }

    private void SetStateText(string message)
    {
        if (currentStateTmp != null)
            currentStateTmp.text = message;
    }

    private void OnReturnClicked()
    {
        ClosePopupAndReset();
    }

private void OnActionClicked()
{
    if (isPaymentSuccess)
    {
        EnterOwnedCourse();
        return;
    }

    ReopenPaymentPage();
}
    private void EnterOwnedCourse()
    {
        if (string.IsNullOrWhiteSpace(currentCourseId))
        {
            Debug.LogWarning("[PaymentWebViewFlowManager] currentCourseId is empty.");
            return;
        }

        SeoResolver.lastResolvedCourseId = currentCourseId;
        SeoResolver.seoCourse = currentCourseSeo;

        Debug.Log($"[PaymentWebViewFlowManager] EnterOwnedCourse -> courseId={currentCourseId}, seo={currentCourseSeo}, name={currentCourseName}");

        ClosePopupAndReset();

        AudioManager.Instance.Resume();
        LoadingTransition.Load_Scene("dai_dao_chi_gian_2");
    }

    public void ManualRecheck()
    {
        if (string.IsNullOrWhiteSpace(currentCheckingOrderId))
            currentCheckingOrderId = WebViewTest.CurrentOrderId;

        if (string.IsNullOrWhiteSpace(currentCheckingOrderId))
        {
            Debug.LogWarning("[PaymentWebViewFlowManager] No orderId to recheck.");
            return;
        }

        if (string.IsNullOrWhiteSpace(TokenStore.AccessToken))
        {
            Debug.LogWarning("[PaymentWebViewFlowManager] No token to recheck.");
            return;
        }

        isFinalState = false;
        isPaymentSuccess = false;

        if (pollingCoroutine != null)
            StopCoroutine(pollingCoroutine);

        CreatePopupIfNeeded();
        SetLoadingUI();
        pollingCoroutine = StartCoroutine(PollOrderRoutine(currentCheckingOrderId, TokenStore.AccessToken));
    }

    public void ClosePopupAndReset()
    {
        if (pollingCoroutine != null)
        {
            StopCoroutine(pollingCoroutine);
            pollingCoroutine = null;
        }

        isChecking = false;
        isFinalState = false;
        isPaymentSuccess = false;

        currentCheckingOrderId = "";
        currentCourseId = "";
        currentCourseSeo = "";
        currentCourseName = "";

        if (currentReturnBtn != null)
            currentReturnBtn.onClick.RemoveListener(OnReturnClicked);

        if (currentActionBtn != null)
            currentActionBtn.onClick.RemoveListener(OnActionClicked);

        currentReturnBtn = null;
        currentActionBtn = null;
        currentStateTmp = null;

        WebViewTest.ClearPaymentState();

        if (currentPopup != null)
        {
            Destroy(currentPopup.gameObject);
            currentPopup = null;
        }
    }

    private static string SafeLower(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();
    }

    private static string FormatPrice(double value)
    {
        return string.Format("{0:N0}đ", value).Replace(",", ".");
    }

    [Serializable]
    public class OrderDetailResponse
    {
        public bool status;
        public OrderData data;
    }

    [Serializable]
    public class OrderData
    {
        public string _id;
        public string status;
        public double totalPrice;
    }
    private void ReopenPaymentPage()
    {
        if (string.IsNullOrWhiteSpace(currentPaymentUrl))
        {
            Debug.LogWarning("[PaymentWebViewFlowManager] currentPaymentUrl is empty. Fallback to recheck.");
            ManualRecheck();
            return;
        }

        Debug.Log("[PaymentWebViewFlowManager] Reopen payment page: " + currentPaymentUrl);

        WebViewTest.SetCourseContext(currentCourseId, currentCourseSeo, currentCourseName);
        WebViewTest.LoadWebView(currentPaymentUrl, currentCourseName);

        ClosePopupAndReset();
    }
}