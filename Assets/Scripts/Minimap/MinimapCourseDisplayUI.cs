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
    
    public string book_sku;
    public string seo_url;
    
    public void SetPriceText(string priceText)
    {
        priceTmp.text = priceText;
    }

    public void SetDisplayCourseName(string displayName)
    {
        displayTmp.text = displayName;
    }
    
}