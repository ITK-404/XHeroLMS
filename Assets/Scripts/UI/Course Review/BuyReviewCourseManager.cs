using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

    public AutomaticTextPreview automaticTextPreview;

    private bool needFetchData;
    private Coroutine previewCoroutine;
    private bool lastLoggedIn;


    private void Awake()
    {
        Instance = this;
        ShowBuyCourseUI();

        if (courseReviewUI != null && courseReviewUI.returnBtn != null)
            courseReviewUI.returnBtn.onClick.AddListener(ShowBuyCourseUI);

        if (replayButton != null)
            replayButton.onClick.AddListener(OnReplayClicked);

        if (enterCourseBtn != null)
            enterCourseBtn.onClick.AddListener(EnterCourse);

        LoadKey();
        lastLoggedIn = IsLoggedIn();


        if (playVideoHandleUI != null && playVideoHandleUI.autoSkipToggle != null)
        {
            playVideoHandleUI.autoSkipToggle.onValueChanged.AddListener(OnAutoSkipChanged);
            playVideoHandleUI.autoSkipToggle.isOn = autoSkipVideo;
        }

        if (playVideoHandleUI != null && playVideoHandleUI.skipButton != null)
            playVideoHandleUI.skipButton.onClick.AddListener(Skip);

        if (playVideoOpenBook != null && automaticTextPreview != null)
            playVideoOpenBook.automaticTextPreview = automaticTextPreview;
    }

    private void OnDestroy()
    {
        if (courseReviewUI != null && courseReviewUI.returnBtn != null)
            courseReviewUI.returnBtn.onClick.RemoveListener(ShowBuyCourseUI);

        if (replayButton != null)
            replayButton.onClick.RemoveListener(OnReplayClicked);

        if (enterCourseBtn != null)
            enterCourseBtn.onClick.RemoveListener(EnterCourse);

        if (playVideoHandleUI != null && playVideoHandleUI.autoSkipToggle != null)
            playVideoHandleUI.autoSkipToggle.onValueChanged.RemoveListener(OnAutoSkipChanged);

        if (playVideoHandleUI != null && playVideoHandleUI.skipButton != null)
            playVideoHandleUI.skipButton.onClick.RemoveListener(Skip);

        SaveKey();
    }

    private void EnterCourse()
    {
        if (!currentBookSelect) return;

        // đi qua gate của BookHandler (nếu CourseListPageAllUI đã gắn OnRequestEnterCourse)
        currentBookSelect.EnterCourse();
    }

    public void ShowBookPreviewUI(BookHandler bookHandler)
    {
        if (bookHandler == null) return;
        if (playVideoOpenBook != null && playVideoOpenBook.IsPlayingVideo()) return;

bool nowLoggedIn = IsLoggedIn();

// refetch nếu đổi book HOẶC vừa thay đổi trạng thái login (guest -> logged)
needFetchData = (currentBookSelect != bookHandler) || (nowLoggedIn != lastLoggedIn);

currentBookSelect = bookHandler;
lastLoggedIn = nowLoggedIn;


        StopAllPreviewRuntime();

        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }

        previewCoroutine = StartCoroutine(ShowPreviewCoroutine());
    }

    private IEnumerator ShowPreviewCoroutine()
    {
        if (playVideoHandleUI != null) playVideoHandleUI.Hide();

        SeoResolver.seoCourse = currentBookSelect.book_seo;

        if (automaticTextPreview != null)
            automaticTextPreview.seoCourse = SeoResolver.seoCourse;

        ShowBuyCourseUI();

        LoadingUI.Show(
            timeoutSeconds: 60f,
            timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
            timeoutHeader: "Lỗi Mạng"
        );

        if (needFetchData)
        {
            yield return SeoResolver.LoadPrivateAndFillData();
            yield return new WaitForSecondsRealtime(0.1f);
        }

        LoadingUI.Hide();

        // CHANGED: Preview không còn bắt buộc private
        // Nếu canEnterCourse=false => nghĩa là needLogin=true và user đang guest
if (!SeoResolver.canEnterCourse)
{
    if (!IsLoggedIn())
    {
        BookHandler.CanSelectBook = false;
        LoadingUI.ShowErrorPopup(
            "Bạn cần đăng nhập để xem/ vào khóa học này.",
            "Thông báo",
            () => { BookHandler.CanSelectBook = true; }
        );
        previewCoroutine = null;
        yield break;
    }

    // Logged-in mà vẫn canEnterCourse=false => state lỗi hoặc token invalid
    BookHandler.CanSelectBook = false;
    LoadingUI.ShowErrorPopup(
       "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
        "Thông báo",
        () => { BookHandler.CanSelectBook = true; }
    );
    previewCoroutine = null;
    yield break;
}


        // Nếu có private thì render đầy đủ, còn không thì render tối thiểu
        if (courseReviewUI != null)
        {
            if (SeoResolver.LmsCoursePrivate != null)
                courseReviewUI.RefreshCourseUI(SeoResolver.LmsCoursePrivate);
            else
                courseReviewUI.RefreshCourseUI(null); // nếu hàm không support null thì bỏ dòng này
        }

        string apiText = GetApiFullTextFromCourse();

        if (!autoSkipVideo)
        {
            if (playVideoHandleUI != null) playVideoHandleUI.Show();
            if (playVideoOpenBook != null)
                yield return playVideoOpenBook.PlayCoroutine(apiText);

            ResetAfterPreviewFinished();
        }
        else
        {
            StartShowAndPlayCourseAudio(apiText);
        }

        if (courseReviewUI != null) courseReviewUI.Show();
        if (tabItemManagerUI != null) tabItemManagerUI.Hide();
        if (playVideoHandleUI != null) playVideoHandleUI.Hide();

        previewCoroutine = null;
    }

    private string GetApiFullTextFromCourse()
    {
        string text = null;

        if (SeoResolver.LmsCoursePrivate != null)
        {
            text = SeoResolver.LmsCoursePrivate.description;

            if (!string.IsNullOrWhiteSpace(text))
            {
                text = ExamFormat.CleanHtmlToPlainText(text);
                text = text.Replace("\u00A0", " ");
                text = Regex.Replace(text, @"[ \t]+", " ").Trim();
                text = Regex.Replace(text, @"\n{3,}", "\n\n").Trim();
            }

            if (string.IsNullOrWhiteSpace(text))
                text = SeoResolver.LmsCoursePrivate.title;
        }

        // fallback khi không có private (guest free)
        if (string.IsNullOrWhiteSpace(text) && currentBookSelect != null)
            text = currentBookSelect.book_name;

        return text ?? "";
    }

    private void ResetAfterPreviewFinished()
    {
        if (automaticTextPreview != null)
            automaticTextPreview.ResetRuntimeState(stopAudio: true);
    }

    private void StartShowAndPlayCourseAudio(string fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText)) return;
        if (automaticTextPreview != null)
            automaticTextPreview.PlayTextAndSpeak(fullText);
    }

    private void StopAllPreviewRuntime()
    {
        if (automaticTextPreview != null)
            automaticTextPreview.ResetRuntimeState(stopAudio: true);

        if (playVideoOpenBook != null)
            playVideoOpenBook.Stop();
    }

    public void Skip()
    {
        if (previewCoroutine != null)
        {
            StopCoroutine(previewCoroutine);
            previewCoroutine = null;
        }

        StopAllPreviewRuntime();

        if (courseReviewUI != null) courseReviewUI.Show();
        if (tabItemManagerUI != null) tabItemManagerUI.Hide();
        if (playVideoHandleUI != null) playVideoHandleUI.Hide();
    }

/*************  ✨ Windsurf Command ⭐  *************/
/// <summary>
/// Hide CourseReviewUI and show TabItemManagerUI, stopping any preview runtime
/// </summary>
/*******  4ee58a2a-7f4d-4cca-9b5d-71ecf4ef614a  *******/
    public void ShowBuyCourseUI()
    {
        StopAllPreviewRuntime();

        if (courseReviewUI != null) courseReviewUI.Hide();
        if (tabItemManagerUI != null) tabItemManagerUI.Show();
    }

    private void OnReplayClicked()
    {
        if (playVideoHandleUI != null && playVideoHandleUI.autoSkipToggle != null)
            playVideoHandleUI.autoSkipToggle.isOn = false;

        ShowBookPreviewUI(currentBookSelect);
    }

    private void OnAutoSkipChanged(bool value) => autoSkipVideo = value;

    private void SaveKey() => PlayerPrefs.SetInt(AUTO_SKIP_SAVE_KEY, autoSkipVideo ? 1 : 0);

    private void LoadKey()
    {
        if (PlayerPrefs.HasKey(AUTO_SKIP_SAVE_KEY))
            autoSkipVideo = PlayerPrefs.GetInt(AUTO_SKIP_SAVE_KEY) == 1;
    }

private bool IsLoggedIn()
{
    return !string.IsNullOrWhiteSpace(TokenStore.AccessToken);
}

}
