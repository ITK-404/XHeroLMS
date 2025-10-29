using NUnit.Framework;
using UnityEngine;

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

        bookModel.OnPlayerClickBook += OnPlayerClickBook;
        SetBuyCourse(true);
    }

    private void OnDestroy()
    {
        bookHandleUI.enterCourseBtn.onClick.RemoveListener(EnterCourse);
        bookHandleUI.enterCourseBtn.onClick.RemoveListener(BuyCourse);
        
        bookModel.OnPlayerClickBook -= OnPlayerClickBook;
    }
    
    private void OnPlayerClickBook()
    {
        BuyReviewCourseManager.Instance.ShowBookPreviewUI(this);
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

    public void SetBuyCourse(bool state)
    {
        bookHandleUI.enterCourseBtn.gameObject.SetActive(state);
        bookHandleUI.buyCourseBtn.gameObject.SetActive(!state);
    }

    private bool AreUserBuyCourse()
    {
        return true;
    }
}
