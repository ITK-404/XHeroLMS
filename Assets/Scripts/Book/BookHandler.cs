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

        bookHandleUI.Test();
    }

    private void OnDestroy()
    {
        bookHandleUI.enterCourseBtn.onClick.RemoveListener(EnterCourse);
        bookHandleUI.enterCourseBtn.onClick.RemoveListener(BuyCourse);
    }

    private void EnterCourse()
    {

    }

    private void BuyCourse()
    {

    }
}