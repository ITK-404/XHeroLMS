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

    // giữ đúng kiểu bạn đang dùng
    public string book_sku;
    public string seo_url;

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
            if (buyCourseBtn) buyCourseBtn.gameObject.SetActive(true);
            if (findWayBtn) findWayBtn.gameObject.SetActive(false);
        }
        else
        {
            if (buyCourseBtn) buyCourseBtn.gameObject.SetActive(false);
            if (findWayBtn) findWayBtn.gameObject.SetActive(true);
        }
    }

    public Button FindWayBtn => findWayBtn;
    public Button BuyCourseBtn => buyCourseBtn;
    public BookModel BookModel => bookModel;
}
