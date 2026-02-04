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
            "https://daotao.phongthuydainam.vn/en/thanh-toan/" +
            "?course=" + UnityWebRequest.EscapeURL(course_id)+
            "&accessToken=" + UnityWebRequest.EscapeURL(token) ;

        Application.OpenURL(url);
        BookHandler.CanSelectBook = true;
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

        yield return null; // bỏ wait 1s cho nhanh
        yield return SeoResolver.LoadPrivateAndFillData();

        LoadingUI.Hide();

        if (!SeoResolver.canEnterCourse)
        {
            Debug.LogWarning($"[BookHandler] Block enter by SeoResolver.canEnterCourse=false. seo={book_seo}");
            BookHandler.CanSelectBook = false;
            LoadingUI.ShowErrorPopup(
                "Bạn cần đăng nhập để vào khóa học này.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );
            yield break;
        }

        // Vào scene theo seo như cũ
        if (book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-ii")
        {
            AudioManager.Instance.Resume();
            // LoadingTransition.Load("dai_dao_chi_gian_2");
            StartCoroutine(CoLoadSceneSmart("dai_dao_chi_gian_2"));
        }
        else if (book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-i" ||
                 book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-(trai-nghiem)" ||
                 book_seo == "cong-dong-phong-thuy-khoa-hoc" ||
                 book_seo == "tro-chuyen-ve-phong-thuy-quan-tri-nang-luong-doanh-nghiep")
        {
            // LoadingTransition.Load(SeoResolver.DefaultScene);
            StartCoroutine(CoLoadSceneSmart(SeoResolver.DefaultScene));
            AudioManager.Instance.Resume();
        }
        else
        {
            BookHandler.CanSelectBook = false;
            // LoadingUI.ShowErrorPopup(
            //     "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
            //     "Thông báo",
            //     () => { BookHandler.CanSelectBook = true; }
            // );
        }
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

private IEnumerator CoLoadSceneSmart(string targetScene)
{
#if ADDRESSABLES
    // Nếu scene là addressable (cloud) -> dùng LoadAssetBundle
    bool isCloud = false;
    yield return CoCheckIsCloudScene(targetScene, r => isCloud = r);

    if (isCloud)
        LoadingTransition.LoadAssetBundle(targetScene);
    else
        LoadingTransition.Load(targetScene);
#else
    LoadingTransition.Load(targetScene);
    yield break;
#endif
}

#if ADDRESSABLES
// Check: sceneName có tồn tại như 1 addressable scene không?
private IEnumerator CoCheckIsCloudScene(string sceneKeyOrName, System.Action<bool> result)
    {
    // var h = Addressables.LoadResourceLocationsAsync(sceneKeyOrName, typeof(SceneInstance));
    var h = Addressables.LoadResourceLocationsAsync(sceneKeyOrName);

    yield return h;

    bool ok = (h.Status == AsyncOperationStatus.Succeeded && h.Result != null && h.Result.Count > 0);

    // Release handle (tránh leak)
    Addressables.Release(h);

    result?.Invoke(ok);
}
#endif
}
