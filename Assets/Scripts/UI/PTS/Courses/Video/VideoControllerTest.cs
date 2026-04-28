using System;
using UnityEngine;
using UnityEngine.UI;

public class VideoControllerTest : MonoBehaviour
{
    [SerializeField] private VideoPlayerCore core;
    [SerializeField] private CourseIntroVideoView viewA; // màn hình đầy đủ
    [SerializeField] private CourseIntroVideoView viewB;

    [SerializeField]
    private string testUrl = "https://www.youtube.com/watch?v=dk6ZFR_Ebkg&list=RDdk6ZFR_Ebkg&start_radio=1";

    private LocalProxyAutoBoot _proxy;

    private void Start()
    {
        _proxy = FindFirstObjectByType<LocalProxyAutoBoot>();

        viewA.SetCore(core);
        viewB.SetCore(core);

        CourseDetailStaticStore.OnChanged += CourseDetailStaticStoreOnOnChanged;

        // Nếu store đã có data trước khi script này subscribe
        CourseDetailStaticStoreOnOnChanged();
    }
    
    
    private void OnDisable()
    {
        core.Stop();
    }

    private void OnDestroy()
    {
        CourseDetailStaticStore.OnChanged -= CourseDetailStaticStoreOnOnChanged;
    }

    private void CourseDetailStaticStoreOnOnChanged()
    {
        if (!CourseDetailStaticStore.HasData ||
            CourseDetailStaticStore.IsLoading ||
            string.IsNullOrEmpty(CourseDetailStaticStore.CurrentCourseId) ||
            CourseDetailStaticStore.CurrentDetail == null)
        {
            Debug.Log("[VideoIntro] Store chưa sẵn sàng, bỏ qua OnChanged.");
            return;
        }
    }

    public void ShowVideoIntro()
    {
        string videoUrl = CourseDetailStaticStore.VideoIntro;

        if (string.IsNullOrEmpty(videoUrl))
        {
            Debug.Log("[VideoIntro] Không có video intro.");
            return;
        }
        string playableUrl = _proxy != null ? _proxy.GetPlayableUrl(videoUrl) : videoUrl;
        core.LoadAndPlay(playableUrl, CourseDetailStaticStore.GetFirstBanner());
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
}