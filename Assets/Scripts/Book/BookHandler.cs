using NUnit.Framework;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class BookHandler : MonoBehaviour
{

    public string book_sku;
    public string book_seo;
    public string book_name;

    public BookViewUI bookHandleUI;
    public BookModel bookModel;

    private void Awake()
    {
        bookModel = GetComponentInChildren<BookModel>();
        bookHandleUI = GetComponentInChildren<BookViewUI>();

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
