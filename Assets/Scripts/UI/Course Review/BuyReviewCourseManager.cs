using System.Collections;
using UnityEngine;

public class BuyReviewCourseManager : MonoBehaviour
{
    public static BuyReviewCourseManager Instance;
    [SerializeField] private  CourseReviewUI courseReviewUI;
    [SerializeField] private TabItemManagerUI tabItemManagerUI;

    [SerializeField] private  SceneLessonUI sceneLessonUI;


    private BookHandler currentBookSelect;
    
    private void Awake()
    {
        Instance = this;
        ShowBuyCourseUI();

        sceneLessonUI.OnLoadCourseDone += courseReviewUI.RefreshCourseUI;
        courseReviewUI.returnBtn.onClick.AddListener(ShowBuyCourseUI);
    }

    private void OnDestroy()
    {
        sceneLessonUI.OnLoadCourseDone -= courseReviewUI.RefreshCourseUI;
        courseReviewUI.returnBtn.onClick.RemoveListener(ShowBuyCourseUI);
    }

    public void ShowBookPreviewUI(BookHandler bookHandler)
    {
        Debug.Log("Bắt đầu hiển thị UI sách preview");
        ShowCourseReviewUI();
        currentBookSelect = bookHandler;
        StartCoroutine(ShowPreviewCoroutine());
    }

    private IEnumerator ShowPreviewCoroutine()
    {
        // cần handling lỗi
        // create logic for turn off and of loading data
        sceneLessonUI.overrideSeo = currentBookSelect.book_seo;
        yield return sceneLessonUI.LoadCourseDataCoroutine();
    }

    public void ShowBuyCourseUI()
    {
        courseReviewUI.Hide();
        tabItemManagerUI.Show();
    }

    public void ShowCourseReviewUI()
    {
        courseReviewUI.Show();
        tabItemManagerUI.Hide();
    }
}