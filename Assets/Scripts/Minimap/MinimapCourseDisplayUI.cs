using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
        LoadingUI.ShowErrorPopup(
            "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
            "Thông báo",
            () => { }
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

        priceTmp.color = owned ? ownerTextColor : priceTextColor;
        priceTmp.enableVertexGradient = owned;
    }

    public Button FindWayBtn => findWayBtn;
    public Button BuyCourseBtn => buyCourseBtn;
    public BookModel BookModel => bookModel;
}

