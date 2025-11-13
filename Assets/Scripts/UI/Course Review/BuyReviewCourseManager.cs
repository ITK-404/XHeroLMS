using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BuyReviewCourseManager : MonoBehaviour
{
    public static BuyReviewCourseManager Instance;
    [SerializeField] private CourseReviewUI courseReviewUI;
    [SerializeField] private TabItemManagerUI tabItemManagerUI;

    [SerializeField] private PlayVideoOpenBook playVideoOpenBook;
    [SerializeField] private PlayVideoHandleUI playVideoHandleUI;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button enterCourseBtn;
    private BookHandler currentBookSelect;

    private bool autoSkipVideo = false;
    private const string AUTO_SKIP_SAVE_KEY = "autoSkipVideo";
    private void Awake()
    {
        Instance = this;
        ShowBuyCourseUI();

        courseReviewUI.returnBtn.onClick.AddListener(ShowBuyCourseUI);
        replayButton.onClick.AddListener(() =>
        {
            playVideoHandleUI.autoSkipToggle.isOn = false;
            ShowBookPreviewUI(currentBookSelect);
        });
        enterCourseBtn.onClick.AddListener(EnterCourse);

        LoadKey();
        playVideoHandleUI.autoSkipToggle.onValueChanged.AddListener((value) => { autoSkipVideo = value; });
        playVideoHandleUI.autoSkipToggle.isOn = autoSkipVideo;
    }

    private void SaveKey()
    {
        PlayerPrefs.SetInt(AUTO_SKIP_SAVE_KEY,autoSkipVideo ? 1 : 0);
    }

    private void LoadKey()
    {
        if (PlayerPrefs.HasKey(AUTO_SKIP_SAVE_KEY))
        {
            autoSkipVideo = PlayerPrefs.GetInt(AUTO_SKIP_SAVE_KEY) == 1;
        }
    }


    private void OnDestroy()
    {
        courseReviewUI.returnBtn.onClick.RemoveListener(ShowBuyCourseUI);
        enterCourseBtn.onClick.RemoveListener(EnterCourse);

        replayButton.onClick.RemoveListener(() =>
        {
            playVideoHandleUI.autoSkipToggle.isOn = false;
            ShowBookPreviewUI(currentBookSelect);
        });
        
        SaveKey();
    }

    private void EnterCourse()
    {
        Debug.Log("Enter course "+SeoResolver.IsContainData());
        if (SeoResolver.IsContainData())
        {
            LoadingTransition.Load(SeoResolver.DefaultScene);
        }
    }

    public void ShowBookPreviewUI(BookHandler bookHandler)
    {
        if (bookHandler == null)
        {
            Debug.Log("Book handler is null");
            return;
        }

        Debug.Log("Bắt đầu hiển thị UI sách preview");
        needFetchData = currentBookSelect != bookHandler;
        currentBookSelect = bookHandler;
        if (currentBookSelect == null)
        {
            Debug.LogError("Sách bị null không thể load");
            return;
        }
        StopCoroutine(ShowPreviewCoroutine());
        StartCoroutine(ShowPreviewCoroutine());
    }

    private bool needFetchData;

    private IEnumerator ShowPreviewCoroutine()
    {
        // cần handling lỗi
        // create logic for turn off and of loading data
        playVideoHandleUI.Hide();
        SeoResolver.seoCourse = currentBookSelect.book_seo;
        Debug.Log("Load book by seo: " + currentBookSelect.book_seo);
        ShowBuyCourseUI();
        LoadingUI.Show();
        // hiển thị loading UI
        if (needFetchData)
        {
            yield return SeoResolver.LoadPrivateAndFillData();
            yield return new WaitForSecondsRealtime(1);
        }
        

        LoadingUI.Hide();

        // không có data không hiển thị nữa
        if (!SeoResolver.IsContainData())
        {
            Debug.Log("Không có data");
            yield break;
        }

        Debug.Log("Có data hiển thị đi");
        courseReviewUI.RefreshCourseUI(SeoResolver.LmsCoursePrivate);

        if (!autoSkipVideo)
        {
            Debug.Log("Skip Video");
            playVideoHandleUI.Show();

            yield return playVideoOpenBook.PlayCoroutine();
        }


        Debug.Log("Đã Play xong video");

        // Hiển thị UI preview ban đầu
        courseReviewUI.Show();
        tabItemManagerUI.Hide();
        playVideoHandleUI.Hide();
    }

    public void Skip()
    {
        // must wait for 
        StopCoroutine(ShowPreviewCoroutine());

        playVideoOpenBook.Stop();
    }


    public void ShowBuyCourseUI()
    {
        courseReviewUI.Hide();
        tabItemManagerUI.Show();
    }
}