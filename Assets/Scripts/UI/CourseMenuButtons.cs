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

    // [Header("Mobile Redirect")] public string mobileAllCoursesUrl = "https://daotao.phongthuydainam.vn/vi";

    // Khóa lưu tạm key giữa các scene
    private const string COURSE_KEY_PREF = "CourseListKey";

    // Hằng số để dùng thống nhất
    public const string KEY_ALL = "All Courses";
    public const string KEY_MY = "My Courses";

    public Transform player;

    void Awake()
    {
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
                PlayerLocator.Save(player.gameObject);
                Go(KEY_ALL);
            });
        }


        if (btnMyCourses != null)
        {
            btnMyCourses.onClick.RemoveAllListeners();
            btnMyCourses.onClick.AddListener(() =>
            {
                PlayerLocator.Save(player.gameObject);
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
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }

    void Go(string key)
    {
        PlayerPrefs.SetString(COURSE_KEY_PREF, key);
        PlayerPrefs.Save();

        // LoadingTransition.Load(courseSceneName);
        StartCoroutine(CoLoadSceneSmart(courseSceneName));
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

public static class PlayerData
{
    public static Vector3 worldPosition;
    public static Quaternion worldRotation;
}