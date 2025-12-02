using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.SceneManagement;

public class IntroManager : MonoBehaviour
{
    [Header("Intro Config")]
    public VideoPlayer videoPlayer;
    public string nextSceneName = "New Scene";

    private bool videoIsPlaying = false;
    private AsyncOperation preloadSceneOp;
    private float videoStartTimeout = 5f; // nếu sau 5s video chưa chạy => skip

    private void Start()
    {
        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer is not assigned!");
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        // 🔹 Preload scene ngay từ đầu (nhưng chưa active)
        preloadSceneOp = SceneManager.LoadSceneAsync(nextSceneName);
        preloadSceneOp.allowSceneActivation = false;

        StartCoroutine(LoadAndPlayVideo());
        StartCoroutine(VideoStartTimeoutCheck());
    }

    IEnumerator LoadAndPlayVideo()
    {
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, "myintro.mp4");
        string videoUrl = "file://" + videoPath;

        Debug.Log("Video URL: " + videoUrl);

        if (System.IO.File.Exists(videoPath) || Application.platform == RuntimePlatform.Android)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = videoUrl;

            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Prepare();
        }
        else
        {
            Debug.LogError("Video file NOT FOUND! Skipping...");
            ActivateNextScene();
        }

        yield return null;
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("Video Prepared!");
        videoPlayer.Play();
        videoIsPlaying = true;
    }

    IEnumerator VideoStartTimeoutCheck()
    {
        float timer = 0f;
        while (timer < videoStartTimeout && !videoIsPlaying)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (!videoIsPlaying)
        {
            Debug.LogWarning("Video didn't start! Skip to next scene.");
            ActivateNextScene();
        }
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("Video finished!");
        ActivateNextScene();
    }

    private void ActivateNextScene()
    {
        if (preloadSceneOp != null)
        {
            Debug.Log("Activating preloaded scene...");
            preloadSceneOp.allowSceneActivation = true;
        }
        else
        {
            Debug.LogWarning("Scene wasn't preloaded! Loading normally...");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
