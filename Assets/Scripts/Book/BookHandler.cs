using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class BookHandler : MonoBehaviour
{
    public string book_sku;
    public string book_seo;
    public string book_name;

    public string course_id; // thêm dòng này (ID khóa học)

    public BookViewUI bookHandleUI;
    public BookModel bookModel;

    public static bool CanSelectBook = true;

    public Action<BookHandler> OnRequestEnterCourse;

    private void Awake()
    {
        bookModel = GetComponentInChildren<BookModel>();
        bookHandleUI = GetComponentInChildren<BookViewUI>();

        if (bookHandleUI)
        {
            bookHandleUI.enterCourseBtn.onClick.AddListener(EnterCourse);
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
            bookHandleUI.enterCourseBtn.onClick.RemoveListener(EnterCourse);
            bookHandleUI.buyCourseBtn.onClick.RemoveListener(BuyCourse);
        }
        if (bookModel != null)
            bookModel.OnPlayerClickBook -= OnPlayerClickBook;
    }

    private void BuyCourse()
    {
        string token = TokenStore.AccessToken;

        if (string.IsNullOrWhiteSpace(token))
        {
            LoadingUI.ShowErrorPopup(
                "Bạn cần đăng nhập để xem khóa học này.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );
            return;
        }

        token = token.Trim();
        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token.Substring("Bearer ".Length).Trim();

        if (string.IsNullOrWhiteSpace(course_id))
        {
            Debug.LogWarning($"[BuyCourse] Missing course_id for book seo={book_seo}, sku={book_sku}");
            LoadingUI.ShowErrorPopup(
                "Không xác định được khóa học để thanh toán.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );
            return;
        }
        string url =
            SecurityConfig.UrlWeb+"/en/thanh-toan/" +
            "?course=" + UnityWebRequest.EscapeURL(course_id)+
            "&accessToken=" + UnityWebRequest.EscapeURL(token) ;

        var uniWebView = BuyReviewCourseManager.Instance.UniWebView;
        // IWebOpener opener = new OpenWebviewService(uniWebView);

        // var result = opener.Open(url);
        // if (result.IsCompleted)
        // {
        //     BookHandler.CanSelectBook = true;
        // }
        WebViewTest.SetCourseContext(course_id, book_seo, book_name);
        WebViewTest.LoadWebView(url,book_name);
    }

    private void OnPlayerClickBook()
    {
        if (CanSelectBook == false) return;
        BuyReviewCourseManager.Instance.ShowBookPreviewUI(this);
    }

    public void EnterCourse()
    {
        if (OnRequestEnterCourse != null)
        {
            OnRequestEnterCourse.Invoke(this);
            return;
        }

        BuyReviewCourseManager.Instance.StartCoroutine(TryEnterCourse());
    }

    public IEnumerator TryEnterCourse()
    {
        LoadingUI.Show(
            timeoutSeconds: 60f,
            timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
            timeoutHeader: "Lỗi Mạng"
        );

        SeoResolver.seoCourse = book_seo;

        yield return null;
        yield return SeoResolver.LoadPrivateAndFillData();

        LoadingUI.Hide();

        if (!SeoResolver.canEnterCourse)
        {
            Debug.LogWarning($"[BookHandler] Block enter by SeoResolver.canEnterCourse=false. seo={book_seo}");
            BookHandler.CanSelectBook = false;
            LoadingUI.ShowErrorPopup(
                "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );
            yield break;
        }

        // Giữ logic cũ: chỉ xử lý các SEO đang hỗ trợ
        bool isSupportedSeo =
            book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-ii" ||
            book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-i" ||
            book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-(trai-nghiem)" ||
            book_seo == "cong-dong-phong-thuy-khoa-hoc" ||
            book_seo == "tro-chuyen-ve-phong-thuy-quan-tri-nang-luong-doanh-nghiep";

        if (!isSupportedSeo)
        {
            BookHandler.CanSelectBook = false;
            LoadingUI.ShowErrorPopup(
                "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );
            yield break;
        }

        // Quyết định scene mới
        string targetScene = null;

        string courseId = SeoResolver.lastResolvedCourseId;
        var market = !string.IsNullOrEmpty(courseId) ? LmsStore.Instance.GetMarketCourse(courseId) : null;

        bool isFree = market != null && market.isFree;
        bool isJoined = market != null && market.isJoined;

        if (isFree)
        {
            targetScene = "dai_dao_chi_gian_1";
        }
        else if (isJoined)
        {
            targetScene = "dai_dao_chi_gian_2";
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            BookHandler.CanSelectBook = false;
            LoadingUI.ShowErrorPopup(
                "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );
            yield break;
        }

        AudioManager.Instance.Resume();
        LoadingTransition.Load_Scene(targetScene);
    }

    public void SetBuyCourse(bool state)
    {
        if (bookHandleUI)
        {
            bookHandleUI.enterCourseBtn.gameObject.SetActive(!state);
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
