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

    private Coroutine ttsCoroutine;

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

    private void OnReplayClicked()
    {
        if (playVideoHandleUI != null && playVideoHandleUI.autoSkipToggle != null)
            playVideoHandleUI.autoSkipToggle.isOn = false;

        ShowBookPreviewUI(currentBookSelect);
    }

    private void OnAutoSkipChanged(bool value)
    {
        autoSkipVideo = value;
    }

    private void SaveKey()
    {
        PlayerPrefs.SetInt(AUTO_SKIP_SAVE_KEY, autoSkipVideo ? 1 : 0);
    }

    private void LoadKey()
    {
        if (PlayerPrefs.HasKey(AUTO_SKIP_SAVE_KEY))
            autoSkipVideo = PlayerPrefs.GetInt(AUTO_SKIP_SAVE_KEY) == 1;
    }

    private void EnterCourse()
    {
        if (currentBookSelect == null) return;

        if (currentBookSelect.book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-ii")
            LoadingTransition.Load("dai_dao_chi_gian_2");
        else if (currentBookSelect.book_seo == "dai-dao-chi-gian-phong-thuy-co-hoc-i")
            LoadingTransition.Load(SeoResolver.DefaultScene);
        else
        {
            LoadingUI.ShowErrorPopup(
                "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );
        }
    }

    public void ShowBookPreviewUI(BookHandler bookHandler)
    {
        if (bookHandler == null)
        {
            Debug.Log("Book handler is null");
            return;
        }

        if (playVideoOpenBook != null && playVideoOpenBook.IsPlayingVideo())
            return;

        needFetchData = currentBookSelect != bookHandler;
        currentBookSelect = bookHandler;

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

        ShowBuyCourseUI();

        LoadingUI.Show(
            timeoutSeconds: 60f,
            timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
            timeoutHeader: "Lỗi Mạng"
        );

        if (needFetchData)
        {
            yield return SeoResolver.LoadPrivateAndFillData();
            yield return new WaitForSecondsRealtime(0.2f);
        }

        LoadingUI.Hide();

        if (!SeoResolver.IsContainData())
        {
            BookHandler.CanSelectBook = false;
            LoadingUI.ShowErrorPopup(
                "Phiên bản hiện tại chưa hỗ trợ.\nVui lòng thử lại sau hoặc chọn khóa học khác.",
                "Thông báo",
                () => { BookHandler.CanSelectBook = true; }
            );
            previewCoroutine = null;
            yield break;
        }

        if (courseReviewUI != null)
            courseReviewUI.RefreshCourseUI(SeoResolver.LmsCoursePrivate);

        string apiText = GetApiFullTextFromCourse();

        if (!autoSkipVideo)
        {
            if (playVideoHandleUI != null) playVideoHandleUI.Show();

            if (playVideoOpenBook != null)
                yield return playVideoOpenBook.PlayCoroutine(apiText);

            // chạy xong video -> reset lại (để lần sau chạy ngon)
            ResetAfterPreviewFinished();
        }
        else
        {
            StartShowAndSpeak(apiText);
        }

        if (courseReviewUI != null) courseReviewUI.Show();
        if (tabItemManagerUI != null) tabItemManagerUI.Hide();
        if (playVideoHandleUI != null) playVideoHandleUI.Hide();

        previewCoroutine = null;
    }

    private void ResetAfterPreviewFinished()
    {
        // stop chunk reader nếu có
        if (ttsCoroutine != null)
        {
            StopCoroutine(ttsCoroutine);
            ttsCoroutine = null;
        }

        // reset text preview state
        if (automaticTextPreview != null)
            automaticTextPreview.ResetRuntimeState(stopTTS: true);

        // đảm bảo TTS cũng tắt
        if (TTSManager.I != null)
            TTSManager.I.Stop();
    }

    private void StartShowAndSpeak(string fullText)
    {
        if (string.IsNullOrWhiteSpace(fullText)) return;

        if (automaticTextPreview != null)
            automaticTextPreview.PlayTextAndSpeak(fullText);

        if (ttsCoroutine != null) StopCoroutine(ttsCoroutine);
        ttsCoroutine = StartCoroutine(SpeakLongTextCoroutine(fullText));
    }

    private void StopAllPreviewRuntime()
    {
        if (ttsCoroutine != null)
        {
            StopCoroutine(ttsCoroutine);
            ttsCoroutine = null;
        }

        if (automaticTextPreview != null)
            automaticTextPreview.ResetRuntimeState(stopTTS: true);

        if (TTSManager.I != null)
            TTSManager.I.Stop();

        if (playVideoOpenBook != null)
            playVideoOpenBook.Stop();
    }

    private string GetApiFullTextFromCourse()
    {
        string text = null;

        if (SeoResolver.IsContainData() && SeoResolver.LmsCoursePrivate != null)
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

        if (string.IsNullOrWhiteSpace(text) && currentBookSelect != null)
            text = currentBookSelect.book_name;

        if (string.IsNullOrWhiteSpace(text))
            return "";

        Debug.Log($"[API_TEXT_FULL] len={text.Length}");
        return text;
    }

    private IEnumerator SpeakLongTextCoroutine(string fullText)
    {
        if (TTSManager.I == null) yield break;
        if (string.IsNullOrWhiteSpace(fullText)) yield break;

        TTSManager.I.SetRatePitch(0.95f, 1.05f);

        foreach (var chunk in SplitToChunks(fullText, maxChars: 220))
        {
            if (string.IsNullOrWhiteSpace(chunk))
                continue;

            TTSManager.I.Speak(chunk);

            float estimated = Mathf.Clamp(chunk.Length / 14f, 1.2f, 12f);
            yield return new WaitForSecondsRealtime(estimated);
        }

        ttsCoroutine = null;
    }

    private static IEnumerable<string> SplitToChunks(string text, int maxChars)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        var parts = Regex.Split(text, @"(?<=[\.\!\?])\s+");
        var sb = new System.Text.StringBuilder();

        foreach (var p in parts)
        {
            var s = (p ?? "").Trim();
            if (s.Length == 0) continue;

            if (sb.Length + s.Length + 1 <= maxChars)
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(s);
            }
            else
            {
                if (sb.Length > 0)
                {
                    yield return sb.ToString();
                    sb.Clear();
                }

                if (s.Length <= maxChars)
                {
                    sb.Append(s);
                }
                else
                {
                    int idx = 0;
                    while (idx < s.Length)
                    {
                        int len = Mathf.Min(maxChars, s.Length - idx);
                        yield return s.Substring(idx, len);
                        idx += len;
                    }
                }
            }
        }

        if (sb.Length > 0)
            yield return sb.ToString();
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

    public void ShowBuyCourseUI()
    {
        StopAllPreviewRuntime();

        if (courseReviewUI != null) courseReviewUI.Hide();
        if (tabItemManagerUI != null) tabItemManagerUI.Show();
    }
}
