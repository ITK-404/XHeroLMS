using System;
using UnityEngine;
using UnityEngine.UI;

public class VideoControllerTest : MonoBehaviour
{
    [SerializeField] private VideoPlayerCore core;
    [SerializeField] private CourseIntroVideoView viewA;   // màn hình đầy đủ
    [SerializeField] private CourseIntroVideoView viewB;
    [SerializeField] private string testUrl = "https://www.youtube.com/watch?v=dk6ZFR_Ebkg&list=RDdk6ZFR_Ebkg&start_radio=1";

    private void Start()
    {
        // Truyền Core vào từng View — chỉ cần làm 1 lần
        viewA.SetCore(core);
        viewB.SetCore(core);

        // core.LoadAndPlay(testUrl, bannerUrl: testUrl);

        // Lúc đầu chỉ bật ViewA
        // viewA.gameObject.SetActive(false);
        // viewB.gameObject.SetActive(false);
        
        string newCourseId = CourseDetailStaticStore.CurrentCourseId;
        
        CourseDetailStaticStore.OnChanged += CourseDetailStaticStoreOnOnChanged;
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
        Debug.Log($"Video Intro: "+CourseDetailStaticStore.VideoIntro);
        Debug.Log($"Video Intro: "+CourseDetailStaticStore.CurrentCourseId);
        core.LoadAndPlay(CourseDetailStaticStore.VideoIntro,"");
    }

    [ContextMenu("SnapView")]
    public void SwapView()
    {
        bool aActive = viewA.gameObject.activeSelf;
        viewA.gameObject.SetActive(!aActive);  // OnDisable → Unbind tự động
        viewB.gameObject.SetActive(aActive);   // OnEnable  → Bind tự động
    }
}