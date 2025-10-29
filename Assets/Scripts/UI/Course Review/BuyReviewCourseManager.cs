using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BuyReviewCourseManager : MonoBehaviour
{
    public static BuyReviewCourseManager Instance;
    [SerializeField] private CourseReviewUI courseReviewUI;
    [SerializeField] private TabItemManagerUI tabItemManagerUI;

    [SerializeField] private SceneLessonUI sceneLessonUI;
    [SerializeField] private PlayVideoOpenBook playVideoOpenBook;
    [SerializeField] private PlayVideoHandleUI playVideoHandleUI;
    [SerializeField] private Button replayButton;
    
    private BookHandler currentBookSelect;

    private bool autoSkip = false;

    private void Awake()
    {
        Instance = this;
        ShowBuyCourseUI();

        sceneLessonUI.OnLoadCourseDone += courseReviewUI.RefreshCourseUI;
        courseReviewUI.returnBtn.onClick.AddListener(ShowBuyCourseUI);
        replayButton.onClick.AddListener(() =>
        {
            playVideoHandleUI.autoSkipToggle.SetIsOnWithoutNotify(false);
            ShowBookPreviewUI(currentBookSelect);
        });
        
        playVideoHandleUI.autoSkipToggle.onValueChanged.AddListener((value) =>
        {
            autoSkip = value;
        });
    }

    private void OnDestroy()
    {
        sceneLessonUI.OnLoadCourseDone -= courseReviewUI.RefreshCourseUI;
        courseReviewUI.returnBtn.onClick.RemoveListener(ShowBuyCourseUI);
        
        replayButton.onClick.RemoveListener(() =>
        {
            playVideoHandleUI.autoSkipToggle.SetIsOnWithoutNotify(false);
            ShowBookPreviewUI(currentBookSelect);
        });
    }

    public void ShowBookPreviewUI(BookHandler bookHandler)
    {
        if (bookHandler == null)
        {
            Debug.Log("Book handler is null");
            return;
        }
        
        Debug.Log("Bắt đầu hiển thị UI sách preview");
        currentBookSelect = bookHandler;
        if (autoSkip)
        {
            courseReviewUI.Show();
            tabItemManagerUI.Hide();
            playVideoHandleUI.Hide();
        }
        else
        {
            tabItemManagerUI.Hide();
            courseReviewUI.Hide();
            StopCoroutine(ShowPreviewCoroutine());
            StartCoroutine(ShowPreviewCoroutine());
        }
        
    }

    private IEnumerator ShowPreviewCoroutine()
    {
        // cần handling lỗi
        // create logic for turn off and of loading data
        playVideoHandleUI.Show();
        sceneLessonUI.overrideSeo = currentBookSelect.book_seo;
        Debug.Log("Bắt đầu play video");
        
        Debug.Log("Load book by seo: " + currentBookSelect.book_seo);
        
        StartCoroutine(sceneLessonUI.LoadCourseDataCoroutine());
        yield return playVideoOpenBook.PlayCoroutine();
        Debug.Log("Đã Play xong video");
       
        courseReviewUI.Show();
        playVideoHandleUI.Hide();
    }

    public void Skip()
    {
        // must wait for 
        StopCoroutine(ShowPreviewCoroutine());
        
        playVideoOpenBook.Stop();
        
        
        StartCoroutine(WaitForLoading());
    }

    private IEnumerator WaitForLoading()
    {
        while (sceneLessonUI.IsLoading)
        {
            yield return null;
        }
        courseReviewUI.Show();
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