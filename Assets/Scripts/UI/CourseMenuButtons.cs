using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CourseMenuButtons : MonoBehaviour
{
    [Header("UI")]
    public Button btnAllCourses;
    public Button btnMyCourses;

    [Header("Destination")]
    [Tooltip("Tên scene đích")]
    public string courseSceneName = "Course Scene";

    // Khóa lưu tạm key giữa các scene
    private const string COURSE_KEY_PREF = "CourseListKey";

    // Hằng số để dùng thống nhất
    public const string KEY_ALL = "All Courses";
    public const string KEY_MY  = "My Courses";

    void Awake()
    {
        if (btnAllCourses != null)
            btnAllCourses.onClick.AddListener(() => Go(KEY_ALL));

        if (btnMyCourses != null)
            btnMyCourses.onClick.AddListener(() => Go(KEY_MY));
    }

    void Go(string key)
    {
        PlayerPrefs.SetString(COURSE_KEY_PREF, key);
        PlayerPrefs.Save();

        // Nếu có transition loader của bạn:
        LoadingTransition.Load(courseSceneName);
        // hoặc: SceneManager.LoadScene(courseSceneName);
    }

    // --- Hàm tiện lợi: để script ở scene đích gọi đọc key ---
    public static string GetSavedKey()
    {
        return PlayerPrefs.GetString(COURSE_KEY_PREF, KEY_ALL);
    }
}
