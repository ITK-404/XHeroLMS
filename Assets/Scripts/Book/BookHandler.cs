using System;
using System.Collections;
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
        if (bookHandleUI)
        {
            bookHandleUI.enterCourseBtn.onClick.AddListener(EnterCourse); 
            bookHandleUI.enterCourseBtn.onClick.AddListener(BuyCourse);
        }

        bookModel.OnPlayerClickBook += OnPlayerClickBook;
        
        SetBuyCourse(true);
    }

    private void OnDestroy()
    {
        if (bookHandleUI)
        {
            bookHandleUI.enterCourseBtn.onClick.RemoveListener(EnterCourse);
            bookHandleUI.enterCourseBtn.onClick.RemoveListener(BuyCourse);
        }
        
        
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
        StartCoroutine(TryEnterCourse());
    }

    private IEnumerator TryEnterCourse()
    {
        LoadingUI.Show(
                timeoutSeconds: 15f,
                timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
                timeoutHeader:  "Lỗi Mạng"
            );
        SeoResolver.seoCourse = book_seo;
        yield return new WaitForSecondsRealtime(1);
        yield return SeoResolver.LoadPrivateAndFillData();
        
        LoadingUI.Hide();

        if (SeoResolver.IsContainData())
        {
            LoadingTransition.Load(SeoResolver.DefaultScene);
        }
    }

    private void BuyCourse()
    {

    }

    public void SetBuyCourse(bool state)
    {
        if (bookHandleUI)
        {
            bookHandleUI.enterCourseBtn.gameObject.SetActive(!state);
            bookHandleUI.buyCourseBtn.gameObject.SetActive(state);
        }
    }

    private bool AreUserBuyCourse()
    {
        return true;
    }

    public void RefreshBookCover()
    {
        if (!string.IsNullOrWhiteSpace(book_sku))
        {
            var tex = BookCoverLoader.Instance.LoadCover(book_sku);
            if(tex != null)
            {
                Debug.Log("Đã tìm thấy book model");
                bookModel.SetBaseMap(tex);
            }
            else
            {
                Debug.Log($"Không tìm thấy book cover {book_sku} {book_name}");
            }
        }
        else
        {
            Debug.Log($"Book SKU {book_sku} bị rỗng");
        }
    }
}
