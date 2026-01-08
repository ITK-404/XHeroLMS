using System;
using System.Collections;
using UnityEngine;

public class BookHandler : MonoBehaviour
{
    public string book_sku;
    public string book_seo;
    public string book_name;

    public BookViewUI bookHandleUI;
    public BookModel bookModel;

    public static bool CanSelectBook = true;

    // để CourseListPageAllUI quyết định có cho EnterCourse hay không
    public Action<BookHandler> OnRequestEnterCourse;

    private void Awake()
    {
        bookModel = GetComponentInChildren<BookModel>();
        bookHandleUI = GetComponentInChildren<BookViewUI>();

        if (bookHandleUI)
        {
            // Nút "Vào học"
            if (bookHandleUI.enterCourseBtn != null)
                bookHandleUI.enterCourseBtn.onClick.AddListener(EnterCourse);

            // Nút "Ghi danh/Mua"
            if (bookHandleUI.buyCourseBtn != null)
                bookHandleUI.buyCourseBtn.onClick.AddListener(BuyCourse);
        }

        if (bookModel != null)
            bookModel.OnPlayerClickBook += OnPlayerClickBook;

        SetBuyCourse(true);
    }

    private void OnDestroy()
    {
        if (bookHandleUI)
        {
            if (bookHandleUI.enterCourseBtn != null)
                bookHandleUI.enterCourseBtn.onClick.RemoveListener(EnterCourse);

            if (bookHandleUI.buyCourseBtn != null)
                bookHandleUI.buyCourseBtn.onClick.RemoveListener(BuyCourse);
        }

        if (bookModel != null)
            bookModel.OnPlayerClickBook -= OnPlayerClickBook;
    }

    private void OnPlayerClickBook()
    {
        if (CanSelectBook == false) return;
        BuyReviewCourseManager.Instance.ShowBookPreviewUI(this);
    }

    public void UpdateData() { }

    /// <summary>
    /// Gate: ưu tiên để CourseListPageAllUI xử lý rule needLogin/isFree/free-grant
    /// </summary>
    public void EnterCourse()
    {
        if (OnRequestEnterCourse != null)
        {
            OnRequestEnterCourse.Invoke(this);
            return;
        }

        // fallback
        StartCoroutine(TryEnterCourse());
    }

    private bool IsLoggedIn()
    {
        return !string.IsNullOrWhiteSpace(TokenStore.AccessToken);
    }

    private void ShowNeedLoginPopup()
    {
        Debug.LogWarning($"[BookHandler] Need login. seo={book_seo}");
        BookHandler.CanSelectBook = false;
        LoadingUI.ShowErrorPopup(
            "Bạn cần đăng nhập để vào khóa học này.",
            "Thông báo",
            () => { BookHandler.CanSelectBook = true; }
        );
    }

    private void ShowNotSupportedPopup()
    {
        Debug.LogWarning($"[BookHandler] Not supported/unknown error. seo={book_seo}");
        BookHandler.CanSelectBook = false;
        LoadingUI.ShowErrorPopup(
            "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
            "Thông báo",
            () => { BookHandler.CanSelectBook = true; }
        );
    }

    public IEnumerator TryEnterCourse()
    {
        LoadingUI.Show(
            timeoutSeconds: 60f,
            timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
            timeoutHeader: "Lỗi Mạng"
        );

        SeoResolver.seoCourse = book_seo;

        yield return null; // bỏ wait 1s cho nhanh

        bool loggedIn = IsLoggedIn();

        yield return SeoResolver.LoadPrivateAndFillData();

        LoadingUI.Hide();

        if (!SeoResolver.canEnterCourse)
        {
            ShowNeedLoginPopup();
            yield break;
        }

        // Vào scene theo seo như cũ
        bool entered = false;

        try
        {
            if (book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-ii")
            {
                AudioManager.Instance.Resume();
                LoadingTransition.Load("dai_dao_chi_gian_2");
                entered = true;
            }
            else if (book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-i" ||
                     book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-(trai-nghiem)" ||
                     book_seo == "cong-dong-phong-thuy-khoa-hoc" ||
                     book_seo == "tro-chuyen-ve-phong-thuy-quan-tri-nang-luong-doanh-nghiep")
            {
                LoadingTransition.Load(SeoResolver.DefaultScene);
                AudioManager.Instance.Resume();
                entered = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[BookHandler] Exception when loading scene. seo={book_seo}\n{e}");
            entered = false;
        }

        if (!entered)
        {
            if (!loggedIn) ShowNeedLoginPopup();
            else ShowNotSupportedPopup();
            yield break;
        }
    }

    private void BuyCourse()
    {
        // logic ghi danh/mua (nếu có)
        // Hiện tại bạn để trống -> ok
    }

    public void SetBuyCourse(bool state)
    {
        if (bookHandleUI)
        {
            if (bookHandleUI.enterCourseBtn != null)
                bookHandleUI.enterCourseBtn.gameObject.SetActive(!state);

            if (bookHandleUI.buyCourseBtn != null)
                bookHandleUI.buyCourseBtn.gameObject.SetActive(state);
        }
    }

    public void RefreshBookCover()
    {
        if (!string.IsNullOrWhiteSpace(book_sku))
        {
            var tex = BookCoverLoader.Instance.LoadCover(book_sku);
            if (tex != null) bookModel.SetBaseMap(tex);
        }
        gameObject.name = $"Book_:{book_name}_Sku:{book_sku}";
    }
}
