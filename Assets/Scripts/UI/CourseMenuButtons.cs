using UnityEngine;
using UnityEngine.UI;
using System.Collections;


#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class CourseMenuButtons : MonoBehaviour
{
    [Header("UI")] public Button btnAllCourses;
    public Button btnMyCourses;
    public Transform container;

    [Header("Destination")] [Tooltip("Tên scene đích")]
    public string courseSceneName = "Course Scene";

    // [Header("Mobile Redirect")] public string mobileAllCoursesUrl = SecurityConfig.UrlWeb;

    // Khóa lưu tạm key giữa các scene
    private const string COURSE_KEY_PREF = "CourseListKey";

    // Hằng số để dùng thống nhất
    public const string KEY_ALL = "All Courses";
    public const string KEY_MY = "My Courses";

    public Transform player;

    // Chặn spam click / gọi nhiều hành động cùng lúc
    private bool isProcessing = false;

    void Awake()
    {
        // Reset trạng thái mỗi khi object được tạo
        isProcessing = false;
        SetButtonsInteractable(true);

        if (TokenStore.IsAuthenticated)
        {
            Show();
        }
        else
        {
            LoginController.OnLoginComplete += Show;
        }

        if (btnAllCourses != null)
        {
            btnAllCourses.onClick.RemoveAllListeners();
            btnAllCourses.onClick.AddListener(() =>
            {
                // MOBILE: mở web
                // if (IsMobileBuild())
                // {
                //     Debug.Log("Open hyper link to the web");
                //     Application.OpenURL(mobileAllCoursesUrl);
                //     return;
                // }

                // PC/Editor: giữ nguyên logic cũ
                Go(KEY_ALL);
            });
        }


        if (btnMyCourses != null)
        {
            btnMyCourses.onClick.RemoveAllListeners();
            btnMyCourses.onClick.AddListener(() =>
            {
                Go(KEY_MY);
            });
        }
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= Show;
    }

    public void Show()
    {
        if (container != null)
            container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (container != null)
            container.gameObject.SetActive(false);
    }

    private void Go(string key)
    {
        // Đã có một hành động đang chạy -> bỏ qua toàn bộ click sau
        if (isProcessing)
        {
            Debug.LogWarning($"[CourseMenuButtons] Ignore click '{key}' because transition is already processing.");
            return;
        }

        // Khóa ngay lập tức
        isProcessing = true;
        SetButtonsInteractable(false);

        Debug.Log($"[CourseMenuButtons] Selected: {key}");

        // Lưu lựa chọn
        PlayerPrefs.SetString(COURSE_KEY_PREF, key);
        PlayerPrefs.Save();

        // Lưu vị trí TRƯỚC khi bắt đầu load scene
        if (player != null)
        {
            LoadingTransition.SavePosition(
                player.position,
                player.rotation
            );
        }

        // Bắt đầu chuyển scene
        LoadingTransition.Load_Scene(courseSceneName);
    }

    /// <summary>
    /// Khóa/mở toàn bộ button course.
    /// Khi một button được nhấn thì khóa cả hai.
    /// </summary>
    private void SetButtonsInteractable(bool value)
    {
        if (btnAllCourses != null)
            btnAllCourses.interactable = value;

        if (btnMyCourses != null)
            btnMyCourses.interactable = value;
    }

    // --- Hàm tiện lợi: để script ở scene đích gọi đọc key ---
    public static string GetSavedKey()
    {
        return PlayerPrefs.GetString(COURSE_KEY_PREF, KEY_ALL);
    }

    private static bool IsMobileBuild()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return false;
#endif
    }
}

public static class PlayerData
{
    public static Vector3 worldPosition;
    public static Quaternion worldRotation;
}