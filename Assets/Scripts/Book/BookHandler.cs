using NUnit.Framework;
using UnityEngine;

public class BookHandler : MonoBehaviour
{

    public string book_sku;
    public string book_seo;
    public string book_name;

    public BookViewUI bookHandleUI;
    public BookModel bookHandle;

    private void Awake()
    {
        bookHandleUI.enterCourseBtn.onClick.AddListener(EnterCourse); 
        bookHandleUI.enterCourseBtn.onClick.AddListener(BuyCourse);

    }

    private void OnDestroy()
    {
        bookHandleUI.enterCourseBtn.onClick.RemoveListener(EnterCourse);
        bookHandleUI.enterCourseBtn.onClick.RemoveListener(BuyCourse);
    }
    
    public void UpdateData()
    {

    }

    private void EnterCourse()
    {

    }

    private void BuyCourse()
    {

    }
}
