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

    public string course_id;

    public BookViewUI bookHandleUI;
    public BookModel bookModel;

    public static bool CanSelectBook = true;

    public bool HasBoundCourseState { get; private set; }
    public bool BoundCourseIsJoined { get; private set; }
    public bool BoundCourseGuestAllowed { get; private set; }

    public Action<BookHandler> OnRequestEnterCourse;

    private const string SceneRoom1 = "dai_dao_chi_gian_1";
    private const string SceneRoom2 = "dai_dao_chi_gian_2";

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
        if (!IsLoggedIn())
        {
            ShowLoginRequiredPopup();
            return;
        }

        string token = TokenStore.AccessToken;
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
            SecurityConfig.UrlWeb + "/en/thanh-toan/" +
            "?course=" + UnityWebRequest.EscapeURL(course_id) +
            "&accessToken=" + UnityWebRequest.EscapeURL(token);

        WebViewTest.SetCourseContext(course_id, book_seo, book_name);
        WebViewTest.LoadWebView(url, book_name);
    }

    private void OnPlayerClickBook()
    {
        if (GameplayLock.IsLocked(GameplayLockTarget.BookInteract))
        {
            return;
        }
        
        if (CanSelectBook == false) return;

        if (!IsLoggedIn())
        {
            if (CanGuestAccessCourse())
                BuyReviewCourseManager.Instance.ShowBookPreviewUI(this);
            else
                ShowLoginRequiredPopup();

            return;
        }

        BuyReviewCourseManager.Instance.ShowBookPreviewUI(this);
    }

    public void EnterCourse()
    {
        if (GameplayLock.IsLocked(GameplayLockTarget.BookInteract))
        {
            return;
        }
        
        if (!CanEnterCourseFromCurrentAuth())
            return;

        if (OnRequestEnterCourse != null)
        {
            OnRequestEnterCourse.Invoke(this);
            return;
        }

        BuyReviewCourseManager.Instance.StartCoroutine(TryEnterCourse());
    }

    private bool CanEnterCourseFromCurrentAuth()
    {
        if (IsLoggedIn())
            return true;

        if (CanGuestAccessCourse())
            return true;

        ShowLoginRequiredPopup();
        return false;
    }

    public void SetCourseState(bool isJoined, bool guestAllowed)
    {
        HasBoundCourseState = true;
        BoundCourseIsJoined = isJoined;
        BoundCourseGuestAllowed = guestAllowed;
    }

    public void ShowLoginRequiredPopup()
    {
        BookHandler.CanSelectBook = false;

        LoadingUI.ShowErrorPopup(
            "Bạn cần đăng nhập để tham gia khóa học này.",
            "Thông báo",
            () => { BookHandler.CanSelectBook = true; }
        );
    }

    public bool RequiresLoginForCurrentCourse()
    {
        return !IsLoggedIn() && !CanGuestAccessCourse();
    }

    private static bool IsLoggedIn()
    {
        if (!TokenStore.IsAuthenticated)
            TokenStore.TryRestoreFromDisk();

        return TokenStore.IsAuthenticated && !string.IsNullOrWhiteSpace(TokenStore.AccessToken);
    }

    private bool CanGuestAccessCourse()
    {
        if (HasBoundCourseState)
            return BoundCourseGuestAllowed;

        if (bookHandleUI != null && bookHandleUI.HasCourseState)
        {
            SetCourseState(bookHandleUI.IsJoined, bookHandleUI.IsFree);
            return bookHandleUI.IsFree;
        }

        string resolvedCourseId = course_id;

        if (string.IsNullOrWhiteSpace(resolvedCourseId) && !string.IsNullOrWhiteSpace(book_seo))
            resolvedCourseId = LmsStore.Instance.GetCourseIdBySeo(book_seo);

        if (!string.IsNullOrWhiteSpace(resolvedCourseId))
        {
            var market = LmsStore.Instance.GetMarketCourse(resolvedCourseId);
            if (market != null)
            {
                bool guestAllowed = LmsStore.AllowsGuestCourse(market);
                SetCourseState(market.isJoined, guestAllowed);
                return guestAllowed;
            }
        }

        return false;
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
                "Bạn cần đăng nhập để tham gia khóa học này.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );

            yield break;
        }

        string targetScene = ResolveTargetScene();

        if (string.IsNullOrEmpty(targetScene))
        {
            BookHandler.CanSelectBook = false;

            LoadingUI.ShowErrorPopup(
                "Không xác định được phòng học phù hợp.\nVui lòng thử lại sau.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );

            yield break;
        }

        AudioManager.Instance.Resume();
        LoadingTransition.Load_Scene(targetScene);
    }

    private string ResolveTargetScene()
    {
        string courseId = SeoResolver.lastResolvedCourseId;

        var market = !string.IsNullOrEmpty(courseId)
            ? LmsStore.Instance.GetMarketCourse(courseId)
            : null;

        bool isFree = market != null && LmsStore.IsCourseFree(market);
        bool isJoined = market != null && market.isJoined;
        bool isCoHoc1 = IsCoHoc1Seo(book_seo);

        Debug.Log(
            $"[BookHandler] ResolveTargetScene | seo={book_seo}, courseId={courseId}, " +
            $"marketNull={market == null}, isFree={isFree}, isJoined={isJoined}, isCoHoc1={isCoHoc1}"
        );

        // Khóa free hoặc Cổ học 1 vào phòng 1
        if (isFree || isCoHoc1)
            return SceneRoom1;
        
        if (SeoResolver.TryGetSceneNameBySeoID(book_seo, out var customScene))
        {
            return customScene;
        }
        // Khóa đã sở hữu vào phòng 2
        if (isJoined)
            return SceneRoom2;

        // TẠM THỜI: khóa chưa định vị được thì cho vào phòng 1
        Debug.LogWarning(
            $"[BookHandler] Cannot resolve target scene. Fallback to Room1. seo={book_seo}, courseId={courseId}"
        );

        return SceneRoom1;
    }

    private bool IsCoHoc1Seo(string seo)
    {
        return seo == "dai-dao-chi-gian-phong-thuy-co-hoc-i" ||
               seo == "dai-dao-chi-gian-phong-thuy-co-hoc-(trai-nghiem)";
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

            if (tex != null)
                bookModel.SetBaseMap(tex);
        }

        gameObject.name = $"Book_:{book_name}_Sku:{book_sku}";
    }
}
