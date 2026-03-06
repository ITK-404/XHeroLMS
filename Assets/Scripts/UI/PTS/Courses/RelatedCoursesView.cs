using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class RelatedCoursesView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button clickCourseBtn;
    [SerializeField] private Image courseImage;

    [SerializeField] private TextMeshProUGUI courseTitle;
    [SerializeField] private TextMeshProUGUI viewCount;
    [SerializeField] private TextMeshProUGUI customerCount;

    [Header("Data")]
    [SerializeField] private string courseID;

    [Header("Options")]
    [SerializeField] private bool downloadImage = true;

    private string imageUrl;
    private Coroutine loadImageCoroutine;
    private bool pendingLoadImage;

    private void Awake()
    {
        if (clickCourseBtn)
            clickCourseBtn.onClick.AddListener(OnShowCourse);
    }

    private void OnEnable()
    {
        if (pendingLoadImage && downloadImage && !string.IsNullOrWhiteSpace(imageUrl) && courseImage != null)
        {
            pendingLoadImage = false;
            StopLoadingImage();
            loadImageCoroutine = StartCoroutine(LoadImage(imageUrl));
        }
    }

    private void OnDestroy()
    {
        if (clickCourseBtn)
            clickCourseBtn.onClick.RemoveListener(OnShowCourse);

        StopLoadingImage();
    }

    public void Setup(string id, string title, string learnersText, string viewsText, string image)
    {
        courseID = id ?? "";
        imageUrl = image ?? "";

        if (courseTitle != null)
            courseTitle.text = title ?? "";

        if (customerCount != null)
            customerCount.text = learnersText ?? "";

        if (viewCount != null)
            viewCount.text = viewsText ?? "";

        ResetImageView();

        StopLoadingImage();
        pendingLoadImage = false;

        if (downloadImage && !string.IsNullOrWhiteSpace(imageUrl) && courseImage != null)
        {
            if (isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                loadImageCoroutine = StartCoroutine(LoadImage(imageUrl));
            }
            else
            {
                pendingLoadImage = true;
            }
        }
    }

    public void Clear()
    {
        courseID = "";
        imageUrl = "";
        pendingLoadImage = false;

        if (courseTitle != null)
            courseTitle.text = "";

        if (customerCount != null)
            customerCount.text = "";

        if (viewCount != null)
            viewCount.text = "";

        ResetImageView();
        StopLoadingImage();
    }

    private void ResetImageView()
    {
        if (courseImage == null) return;

        courseImage.sprite = null;
        courseImage.enabled = false;
        courseImage.color = Color.white;
        courseImage.preserveAspect = false;

        RectTransform rt = courseImage.rectTransform;
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private void OnShowCourse()
    {
        Debug.Log($"[RelatedCoursesView] Click course id = {courseID}");
        LoadingUI.Show(3);
    }

    private IEnumerator LoadImage(string url)
    {
        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 20;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"[RelatedCoursesView] Load image failed\nURL: {url}\nCode: {req.responseCode}\nError: {req.error}");
                loadImageCoroutine = null;
                yield break;
            }

            byte[] data = req.downloadHandler.data;
            if (data == null || data.Length == 0)
            {
                Debug.LogWarning($"[RelatedCoursesView] Image bytes empty: {url}");
                loadImageCoroutine = null;
                yield break;
            }

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool ok = tex.LoadImage(data);

            if (!ok)
            {
                Debug.LogWarning($"[RelatedCoursesView] Texture.LoadImage failed: {url}");
                Destroy(tex);
                loadImageCoroutine = null;
                yield break;
            }

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            if (courseImage != null)
            {
                courseImage.sprite = sprite;
                courseImage.enabled = true;
                courseImage.color = Color.white;
                courseImage.preserveAspect = false;

                ApplyCenterCrop(tex.width, tex.height);
            }
        }

        loadImageCoroutine = null;
    }

    private void ApplyCenterCrop(float imageWidth, float imageHeight)
    {
        if (courseImage == null) return;

        RectTransform imageRT = courseImage.rectTransform;
        RectTransform parentRT = imageRT.parent as RectTransform;

        if (imageRT == null || parentRT == null) return;

        float frameWidth = parentRT.rect.width;
        float frameHeight = parentRT.rect.height;

        if (frameWidth <= 0f || frameHeight <= 0f || imageWidth <= 0f || imageHeight <= 0f)
            return;

        float frameRatio = frameWidth / frameHeight;
        float imageRatio = imageWidth / imageHeight;

        float targetWidth;
        float targetHeight;

        if (imageRatio > frameRatio)
        {
            targetHeight = frameHeight;
            targetWidth = targetHeight * imageRatio;
        }
        else
        {
            targetWidth = frameWidth;
            targetHeight = targetWidth / imageRatio;
        }

        imageRT.anchorMin = new Vector2(0.5f, 0.5f);
        imageRT.anchorMax = new Vector2(0.5f, 0.5f);
        imageRT.pivot = new Vector2(0.5f, 0.5f);
        imageRT.anchoredPosition = Vector2.zero;
        imageRT.sizeDelta = new Vector2(targetWidth, targetHeight);
        imageRT.localScale = Vector3.one;
    }

    private void StopLoadingImage()
    {
        if (loadImageCoroutine != null)
        {
            StopCoroutine(loadImageCoroutine);
            loadImageCoroutine = null;
        }
    }
}