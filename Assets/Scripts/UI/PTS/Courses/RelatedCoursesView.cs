using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class RelatedCoursesView : MonoBehaviour
{
    [Header("UI")] [SerializeField] private Button clickCourseBtn;
    [SerializeField] private Image courseImage;
    [SerializeField] private Sprite defaultImage;
    [SerializeField] private TextMeshProUGUI courseTitle;
    [SerializeField] private TextMeshProUGUI viewCount;
    [SerializeField] private TextMeshProUGUI customerCount;

    [Header("Data")] [SerializeField] private string courseID;

    [Header("Options")] [SerializeField] private bool downloadImage = true;

    private string imageUrl;
    private bool loadImageDone = false;
    private Coroutine loadingImgCoroutine;

    private void Awake()
    {
        if (clickCourseBtn)
            clickCourseBtn.onClick.AddListener(OnShowCourse);
    }

    private void OnEnable()
    {
        HandleLoadingNewImage();
    }

    private void HandleLoadingNewImage()
    {
        if (loadImageDone) return;
        
        if (!string.IsNullOrEmpty(imageUrl))
        {
            if (loadingImgCoroutine != null)
            {
                StopCoroutine(loadingImgCoroutine);
            }

            // loadingImgCoroutine = StartCoroutine(LoadRoutine(imageUrl,courseImage));
        }
        else
        {
            courseImage.sprite = defaultImage;
        }
    }

    
    private void OnDisable()
    {
        ResetState();
    }

    private void ResetState()
    {
        if (loadingImgCoroutine != null)
        {
            StopCoroutine(loadingImgCoroutine);
        }
    }

    private void OnDestroy()
    {
        if (clickCourseBtn)
            clickCourseBtn.onClick.RemoveListener(OnShowCourse);
        
        if (courseImage.sprite != null && courseImage.sprite != defaultImage)
        {
            Object.Destroy(courseImage.sprite.texture);
            Object.Destroy(courseImage.sprite);
        }
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
        
        loadImageDone = false;
    }


    public void Clear()
    {
        courseID = "";
        imageUrl = "";

        if (courseTitle != null)
            courseTitle.text = "";

        if (customerCount != null)
            customerCount.text = "";

        if (viewCount != null)
            viewCount.text = "";
    }

    private IEnumerator LoadRoutine(string url, Image target)
    {
        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
        {
            req.timeout = 5;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"Load image failed: {req.error}");
                yield break;
            }

            Texture2D temp = DownloadHandlerTexture.GetContent(req);
            temp.name = courseID;
            var tex = temp.Resize(256);
            tex.name = courseID;
            Destroy(temp);
            if (tex == null)
                yield break;

            // cleanup sprite cũ
            if (target.sprite != null && target.sprite != defaultImage)
            {
                Object.Destroy(target.sprite.texture);
                Object.Destroy(target.sprite);
            }

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );
            target.sprite = sprite;

            loadImageDone = true;
        }
    }

    private void OnShowCourse()
    {
        Debug.Log($"[RelatedCoursesView] Click course id = {courseID}");
        PTS_ViewManager.Instance.Current.GetComponent<PTS_CourseDetailView>().GoBackward();

        APICourseLoaderService.Instance.Load(courseID, () => { Debug.Log("[Related Course] Load thanh cong"); },
            () => { Debug.Log("[Related Course] Load that bai"); });
    }
}