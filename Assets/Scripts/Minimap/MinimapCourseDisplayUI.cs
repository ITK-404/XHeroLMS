using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class MinimapCourseDisplayUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayTmp;
    [SerializeField] private TextMeshProUGUI priceTmp;
    [SerializeField] private Button findWayBtn;
    [SerializeField] private Button buyCourseBtn;

    [SerializeField] private BookModel bookModel;
    public static Action<string> OnFindWayAction;

    [SerializeField] private Image backgroundImg;
    // giữ đúng kiểu bạn đang dùng
    public string book_sku;
    public string seo_url;

    [SerializeField] private Color priceColor;
    [SerializeField] private Color ownCourseColor;
    [SerializeField] private Color ownerTextColor;
    [SerializeField] private Color priceTextColor;
    public string course_id; // thêm vào MinimapCourseDisplayUI
    public string course_title;
    private void Awake()
    {
        findWayBtn.onClick.AddListener(ClickFindWayButton);
        buyCourseBtn.onClick.AddListener(OnShowPopup);
    }

    private void OnDestroy()
    {
        findWayBtn.onClick.RemoveListener(ClickFindWayButton);
        buyCourseBtn.onClick.RemoveListener(OnShowPopup);
        
    }

    private void OnShowPopup()
    {
        bool isLoggedIn = TokenStore.IsAuthenticated && !string.IsNullOrWhiteSpace(TokenStore.AccessToken);

        if (isLoggedIn)
        {
            string token = TokenStore.AccessToken.Trim();

            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring("Bearer ".Length).Trim();

            string url =
                "https://daotao.phongthuydainam.vn/en/thanh-toan/" +
                "?course=" + UnityWebRequest.EscapeURL(course_id) +
                "&accessToken=" + UnityWebRequest.EscapeURL(token);

            // Application.OpenURL(url);
            WebViewTest.LoadWebView(url,course_title);
            BookHandler.CanSelectBook = true;
            return;
        }

        LoadingUI.ShowErrorPopup(
            "Bạn cần đăng nhập để xem khóa học này.",
            "Thông báo",
            () => { BookHandler.CanSelectBook = true; }
        );
    }

    private void ClickFindWayButton()
    {
        Debug.Log("Nhan tim duong toi khoa hoc");
        OnFindWayAction?.Invoke(seo_url);
    }

    public void SetMeta(string sku, string seo)
    {
        book_sku = sku ?? "";
        seo_url  = seo ?? "";
    }

    public void SetPriceText(string priceText)
    {
        if (priceTmp) priceTmp.text = priceText ?? "";
    }

    public void SetDisplayCourseName(string displayName)
    {
        if (displayTmp) displayTmp.text = displayName ?? "";
    }

    public void SetOwnedUI(bool owned)
    {
        if (owned)
        {
            // giá -> "ĐÃ SỞ HỮU"
            SetPriceText("ĐÃ SỞ HỮU");
            if (buyCourseBtn) buyCourseBtn.gameObject.SetActive(false);
            if (findWayBtn) findWayBtn.gameObject.SetActive(true);
        }
        else
        {
            if (buyCourseBtn) buyCourseBtn.gameObject.SetActive(true);
            if (findWayBtn) findWayBtn.gameObject.SetActive(false);
        }

        backgroundImg.color = owned ? ownCourseColor : priceColor;

        if (priceTmp)
        {
            priceTmp.color = owned ? ownerTextColor : priceTextColor;
            priceTmp.enableVertexGradient = owned;
        }
    }

    public Button FindWayBtn => findWayBtn;
    public Button BuyCourseBtn => buyCourseBtn;
    public BookModel BookModel => bookModel;
}

