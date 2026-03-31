using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class RelatedCoursesView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button clickCourseBtn;
    [SerializeField] private Image courseImage;
    [SerializeField] private Sprite defaultImage;
    [SerializeField] private TextMeshProUGUI courseTitle;
    [SerializeField] private TextMeshProUGUI viewCount;
    [SerializeField] private TextMeshProUGUI customerCount;

    [Header("Data")]
    [SerializeField] private string courseID;

    [Header("Options")]
    [SerializeField] private bool downloadImage = true;
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private int requestTimeout = 5;

    private string imageUrl;
    private bool loadImageDone;
    private Coroutine loadingImgCoroutine;
    private UnityWebRequest activeImageRequest;

    private int bindToken;
    private bool isDestroyed;

    // Chỉ destroy sprite/texture runtime do chính class này tạo ra
    private Sprite runtimeSprite;
    private Texture2D runtimeTexture;

    private void Awake()
    {
        if (clickCourseBtn != null)
            clickCourseBtn.onClick.AddListener(OnShowCourse);
    }

    private void OnEnable()
    {
        HandleLoadingNewImage();
    }

    private void OnDisable()
    {
        CancelImageLoad();
    }

    private void OnDestroy()
    {
        isDestroyed = true;

        if (clickCourseBtn != null)
            clickCourseBtn.onClick.RemoveListener(OnShowCourse);

        CancelImageLoad();
        ReleaseRuntimeImage();
    }

    public void Setup(string id, string title, string learnersText, string viewsText, string image)
    {
        bindToken++;

        CancelImageLoad();
        ReleaseRuntimeImage();

        courseID = id ?? "";
        imageUrl = image ?? "";
        loadImageDone = false;

        if (courseTitle != null)
            courseTitle.text = title ?? "";

        if (customerCount != null)
            customerCount.text = learnersText ?? "";

        if (viewCount != null)
            viewCount.text = viewsText ?? "";

        SetDefaultImage();

        if (isActiveAndEnabled)
            HandleLoadingNewImage();
    }

    public void Clear()
    {
        bindToken++;

        CancelImageLoad();
        ReleaseRuntimeImage();

        courseID = "";
        imageUrl = "";
        loadImageDone = false;

        if (courseTitle != null)
            courseTitle.text = "";

        if (customerCount != null)
            customerCount.text = "";

        if (viewCount != null)
            viewCount.text = "";

        SetDefaultImage();
    }

    private void HandleLoadingNewImage()
    {
        if (!downloadImage)
        {
            SetDefaultImage();
            return;
        }

        if (loadImageDone)
            return;

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            SetDefaultImage();
            return;
        }

        CancelImageLoad();
        SetDefaultImage();

        int token = bindToken;
        loadingImgCoroutine = StartCoroutine(LoadRoutine(imageUrl, token));
    }

    private IEnumerator LoadRoutine(string url, int token)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url, true))
        {
            activeImageRequest = req;
            req.timeout = requestTimeout;

            yield return req.SendWebRequest();

            if (activeImageRequest != req)
                yield break;

            activeImageRequest = null;
            loadingImgCoroutine = null;

            if (isDestroyed || !isActiveAndEnabled)
                yield break;

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"[RelatedCoursesView] Load image failed: {req.error} | url={url}");
                yield break;
            }

            if (token != bindToken)
                yield break;

            Texture2D downloaded = DownloadHandlerTexture.GetContent(req);
            if (downloaded == null)
                yield break;

            downloaded.name = string.IsNullOrEmpty(courseID) ? "RelatedCourseImage" : courseID;

            // Nếu vẫn muốn resize thì giữ logic này.
            // Nhưng đây vẫn là chỗ tốn RAM hơn so với dùng thumbnail từ server.
            Texture2D finalTex = downloaded.Resize(256);

            if (finalTex != downloaded)
                Object.Destroy(downloaded);

            if (finalTex == null)
                yield break;

            if (token != bindToken || isDestroyed || !isActiveAndEnabled)
            {
                Object.Destroy(finalTex);
                yield break;
            }

            ReleaseRuntimeImage();

            runtimeTexture = finalTex;
            runtimeTexture.name = string.IsNullOrEmpty(courseID) ? "RelatedCourseImage" : courseID;

            runtimeSprite = Sprite.Create(
                runtimeTexture,
                new Rect(0, 0, runtimeTexture.width, runtimeTexture.height),
                new Vector2(0.5f, 0.5f)
            );

            if (courseImage == null)
            {
                ReleaseRuntimeImage();
                yield break;
            }

            courseImage.sprite = runtimeSprite;
            courseImage.preserveAspect = false;
            loadImageDone = true;
        }
    }

    private void CancelImageLoad()
    {
        if (loadingImgCoroutine != null)
        {
            StopCoroutine(loadingImgCoroutine);
            loadingImgCoroutine = null;
        }

        if (activeImageRequest != null)
        {
            try
            {
                activeImageRequest.Abort();
            }
            catch
            {
            }

            activeImageRequest.Dispose();
            activeImageRequest = null;
        }
    }

    private void ReleaseRuntimeImage()
    {
        if (courseImage != null && courseImage.sprite == runtimeSprite)
            courseImage.sprite = defaultImage;

        if (runtimeSprite != null)
        {
            Object.Destroy(runtimeSprite);
            runtimeSprite = null;
        }

        if (runtimeTexture != null)
        {
            Object.Destroy(runtimeTexture);
            runtimeTexture = null;
        }
    }

    private void SetDefaultImage()
    {
        if (courseImage == null)
            return;

        courseImage.sprite = defaultImage;
        courseImage.preserveAspect = preserveAspect;
    }

    private void OnShowCourse()
    {
        Debug.Log($"[RelatedCoursesView] Click course id = {courseID}");

        var current = PTS_ViewManager.Instance.Current;
        if (current != null)
        {
            var detailView = current.GetComponent<PTS_CourseDetailView>();
            if (detailView != null)
                detailView.GoBackward();
        }

        APICourseLoaderService.Instance.Load(
            courseID,
            () => { Debug.Log("[Related Course] Load thanh cong"); },
            () => { Debug.Log("[Related Course] Load that bai"); }
        );
    }
}