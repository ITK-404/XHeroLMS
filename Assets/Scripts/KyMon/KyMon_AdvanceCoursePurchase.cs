using System;
using System.Collections;
using System.Globalization;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class KyMon_AdvanceCoursePurchase : MonoBehaviour
{
    // TODO: Khong co API đăng ký thông tin khoá học
    [SerializeField] private KyMon_AdvanceCoursePurchaseUI view;
    [SerializeField] private string courseIDAdvance;
    [SerializeField] private Transform confirmUITransform;
    private CourseListPageAllUI.CourseData courseData;

    private void Awake()
    {
        view.ButtonHandle.OnBuyClickedEvent += HandleBuyButtonClicked;
        view.ButtonHandle.OnRegisterClickedEvent += HandleRegisterClicked;
    }

    private void OnDestroy()
    {
        view.ButtonHandle.OnBuyClickedEvent -= HandleBuyButtonClicked;
        view.ButtonHandle.OnRegisterClickedEvent -= HandleRegisterClicked;
    }

    private void Start()
    {
        GetCourseData(courseIDAdvance).Forget();
    }

    private void HandleRegisterClicked()
    {
        StartCoroutine(FakeLoading());
    }

    private IEnumerator FakeLoading()
    {
        Debug.Log($"Fake loading for 2 second");
        LoadingUI.Show(0f);
        yield return new WaitForSecondsRealtime(1f);
        confirmUITransform.gameObject.SetActive(true);
        LoadingUI.Hide();
    }

    private async UniTask<CourseListPageAllUI.CourseData> GetCourseData(string seoID)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(3));
        var baseUrl = LmsStore.Instance.baseUrl;
        var url = $"{baseUrl}/lms/courses/{seoID}";
        Debug.Log($"LogRawJson {url}");
        using var request = UnityWebRequest.Get(url);

        request.SetRequestHeader("accept", "application/json");

        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(request.error);
            return null;
        }

        var json = request.downloadHandler.text;
        courseData = CourseListPageAllUI.ParseCourse(json);
        Debug.Log($"LogRawJson: {json}");
        UpdateData();
        return courseData;
    }

    private void UpdateData()
    {
        if (courseData == null) return;
        if (courseData.currentPrice != null)
        {
            string currentPrice = FormatCurrency(courseData.currentPrice.Value);
            view.ButtonHandle.SetBuyText($"GHI DANH H.PHÍ " + currentPrice);
            return;
        }

        view.ButtonHandle.SetBuyText("Null");
    }

    public static string FormatCurrency(float value)
    {
        return value.ToString("N0", new CultureInfo("vi-VN")) + "đ";
    }

    private void HandleBuyButtonClicked()
    {
        if (courseData == null) return;
        WebViewTest.LoadWebView(courseData.seoUrl, courseData.title);
    }
}