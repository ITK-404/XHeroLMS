using System;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PTS_WebView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button buyNowButton;
    [SerializeField] private TMP_Text priceText;

    [Header("Text Config")]
    [SerializeField] private string buyTextPrefix = "MUA NGAY GIÁ ";
    [SerializeField] private string ownedText = "ĐÃ SỞ HỮU";
    [SerializeField] private string loadingText = "Đang tải...";
    [SerializeField] private string emptyText = "Chưa có dữ liệu";

    private void Awake()
    {
        if (buyNowButton != null)
        {
            buyNowButton.onClick.RemoveAllListeners();
            buyNowButton.onClick.AddListener(OnClickBuyNow);
        }
    }

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= RefreshUI;
    }

    private void OnDestroy()
    {
        if (buyNowButton != null)
            buyNowButton.onClick.RemoveListener(OnClickBuyNow);
    }

    public void RefreshUI()
    {
        if (priceText == null)
            return;

        if (CourseDetailStaticStore.IsLoading)
        {
            priceText.text = loadingText;
            SetButtonInteractable(false);
            return;
        }

        var course = CourseDetailStaticStore.CurrentDetail;
        var courseId = CourseDetailStaticStore.CurrentCourseId;

        if (!CourseDetailStaticStore.HasData || course == null || string.IsNullOrWhiteSpace(courseId))
        {
            priceText.text = emptyText;
            SetButtonInteractable(false);
            return;
        }

        bool isOwned = IsCourseOwned(courseId);

        if (isOwned)
        {
            priceText.text = ownedText;
            SetButtonInteractable(false);
            return;
        }

        string price = GetCurrentPriceText(course);
        priceText.text = buyTextPrefix + price;
        SetButtonInteractable(true);
    }

    private void OnClickBuyNow()
    {
        var course = CourseDetailStaticStore.CurrentDetail;
        string courseId = CourseDetailStaticStore.CurrentCourseId;

        if (course == null || string.IsNullOrWhiteSpace(courseId))
        {
            Debug.LogWarning("[PTS_WebView] Missing course detail or courseId.");
            return;
        }

        if (IsCourseOwned(courseId))
        {
            Debug.Log("[PTS_WebView] Course already owned.");
            return;
        }

        string token = TokenStore.AccessToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            LoadingUI.ShowErrorPopup(
                "Bạn cần đăng nhập để tiếp tục thanh toán.",
                "Thông báo"
            );
            return;
        }

        token = token.Trim();
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring("Bearer ".Length).Trim();

        string url =
            SecurityConfig.UrlWeb+"/en/thanh-toan/" +
            "?course=" + UnityWebRequest.EscapeURL(courseId) +
            "&accessToken=" + UnityWebRequest.EscapeURL(token);

        Debug.Log("[PTS_WebView] Open payment webview: " + url);
        WebViewTest.LoadWebView(url,course.title);
    }

    private bool IsCourseOwned(string courseId)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            return false;

        var myCourseIds = LmsStore.Instance.GetMyCourseIds();
        if (myCourseIds == null)
            return false;

        foreach (var id in myCourseIds)
        {
            if (string.Equals(id, courseId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private void SetButtonInteractable(bool state)
    {
        if (buyNowButton != null)
            buyNowButton.interactable = state;
    }

    // private string GetCurrentPriceText(LmsCoursePrivate course)
    private string GetCurrentPriceText(CourseModels.CourseDetail course)
    {
        if (course == null || course.coursePrice == null)
            return "0đ";

        long price = 0;

        if (course.coursePrice.currentPrice > 0)
            price = Convert.ToInt64(course.coursePrice.currentPrice);
        else if (course.coursePrice.originalPrice > 0)
            price = Convert.ToInt64(course.coursePrice.originalPrice);

        return FormatVnd(price);
    }

    private string FormatVnd(long amount)
    {
        return string.Format("{0:N0}đ", amount);
    }
}