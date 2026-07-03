using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class VideoControllerTest : MonoBehaviour
{
    [SerializeField] private VideoPlayerCore core;
    [SerializeField] private CourseIntroVideoView viewA; // màn hình đầy đủ
    [SerializeField] private CourseIntroVideoView viewB;

    [SerializeField]
    private string testUrl = "https://www.youtube.com/watch?v=dk6ZFR_Ebkg&list=RDdk6ZFR_Ebkg&start_radio=1";

    private LocalProxyAutoBoot _proxy;
    private bool _pendingIntroRequest;

    private void Start()
    {
        _proxy = FindFirstObjectByType<LocalProxyAutoBoot>();

        viewA.SetCore(core);
        viewB.SetCore(core);

        CourseDetailStaticStore.OnChanged += CourseDetailStaticStoreOnOnChanged;
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;

        // Nếu store đã có data trước khi script này subscribe.
        CourseDetailStaticStoreOnOnChanged();
    }

    private void SceneManagerOnsceneLoaded(Scene newScene, LoadSceneMode loadSceneMode)
    {
        core.Pause();
    }

    private void OnDestroy()
    {
        CourseDetailStaticStore.OnChanged -= CourseDetailStaticStoreOnOnChanged;
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }

    private void OnDisable()
    {
        _pendingIntroRequest = false;
        core.Stop();
    }

    private void CourseDetailStaticStoreOnOnChanged()
    {
        if (!IsStoreReady())
        {
            Debug.Log("[VideoIntro] Store chưa sẵn sàng, bỏ qua OnChanged.");
            return;
        }

        if (!_pendingIntroRequest)
            return;

        _pendingIntroRequest = false;
        PlayVideoIntroFromStore();
    }

    public void ShowVideoIntro()
    {
        if (!IsStoreReady())
        {
            _pendingIntroRequest = true;
            Debug.Log("[VideoIntro] Store chưa sẵn sàng, đợi OnChanged.");
            return;
        }

        PlayVideoIntroFromStore();
    }

    public void ShowViewA()
    {
        viewA.gameObject.SetActive(true);
        viewB.gameObject.SetActive(false);
    }

    public void ShowViewB()
    {
        viewA.gameObject.SetActive(false);
        viewB.gameObject.SetActive(true);
    }

    private void PlayVideoIntroFromStore()
    {
        string videoUrl = CourseDetailStaticStore.GetVideoIntro();

        if (string.IsNullOrEmpty(videoUrl))
        {
            Debug.Log("[VideoIntro] Không có video intro.");
            return;
        }

        string playableUrl = _proxy != null ? _proxy.GetPlayableUrl(videoUrl) : videoUrl;
        core.LoadAndPlay(playableUrl, CourseDetailStaticStore.GetFirstBanner());
    }

    private static bool IsStoreReady()
    {
        return CourseDetailStaticStore.HasData &&
               !CourseDetailStaticStore.IsLoading &&
               !string.IsNullOrEmpty(CourseDetailStaticStore.CurrentCourseId) &&
               CourseDetailStaticStore.CurrentDetail != null;
    }
}
