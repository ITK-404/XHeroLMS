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

    void Awake()
    {
        if (btnAllCourses != null)
            btnAllCourses.onClick.AddListener(() => Go("All Courses"));

        if (btnMyCourses != null)
            btnMyCourses.onClick.AddListener(() => Go("My Courses"));
    }

    void Go(string key)
    {
        // Lưu key để scene sau đọc lại
        PlayerPrefs.SetString(COURSE_KEY_PREF, key);
        PlayerPrefs.Save();

        // Chuyển scene
        LoadingTransition.Load(courseSceneName);
    }

    // --- Hàm tiện lợi: để script ở scene đích gọi đọc key ---
    public static string GetSavedKey()
    {
        return PlayerPrefs.GetString(COURSE_KEY_PREF, "All Courses");
    }
}
