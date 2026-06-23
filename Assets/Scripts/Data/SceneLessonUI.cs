using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLessonUI : MonoBehaviour
{
    [Header("Data")]
    public string overrideSeo = "";

    [Header("UI")]
    [Header("Options")]
    public bool autoFetchPrivateIfMissing = true;
    public bool autoStart = true;
    [SerializeField, Min(1)] private int loadRetryCount = 3;
    [SerializeField, Min(0.1f)] private float loadRetryDelaySeconds = 0.75f;

    public Action<LmsCoursePrivate> OnLoadCourseDone;

    private bool isLoading;
    private LmsCoursePrivate currentCourse;
    private Coroutine loadCoroutine;

    public bool IsLoading => isLoading;
    public LmsCoursePrivate CurrentCourse => currentCourse != null ? currentCourse : SeoResolver.LmsCoursePrivate;

    private void Awake()
    {
        _ = LmsStore.Instance;
    }

    private IEnumerator Start()
    {
        if (!autoStart)
            yield break;

        yield return EnsureCourseLoadedRoutine();
    }

    public void RequestLoadCourse()
    {
        if (!isActiveAndEnabled)
            return;

        if (loadCoroutine == null)
            loadCoroutine = StartCoroutine(EnsureCourseLoadedRoutine());
    }

    public IEnumerator EnsureCourseLoadedRoutine()
    {
        if (isLoading)
        {
            while (isLoading)
                yield return null;

            if (CurrentCourse != null)
                OnLoadCourseDone?.Invoke(CurrentCourse);

            yield break;
        }

        currentCourse = CurrentCourse;
        if (currentCourse != null)
        {
            OnLoadCourseDone?.Invoke(currentCourse);
            yield break;
        }

        isLoading = true;
        int attempts = Mathf.Max(1, loadRetryCount);

        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            ResolveSeoForLoad();
            Debug.Log($"[SceneLessonUI] Load course attempt {attempt}/{attempts}, seo={SeoResolver.seoCourse}");

            if (!string.IsNullOrEmpty(SeoResolver.seoCourse))
            {
                yield return SeoResolver.LoadPrivateAndFillData();
                currentCourse = SeoResolver.LmsCoursePrivate;

                if (currentCourse != null)
                {
                    isLoading = false;
                    loadCoroutine = null;
                    OnLoadCourseDone?.Invoke(currentCourse);
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning("[SceneLessonUI] Missing seo course, cannot load lesson data.");
            }

            if (attempt < attempts)
                yield return new WaitForSecondsRealtime(loadRetryDelaySeconds * attempt);
        }

        isLoading = false;
        loadCoroutine = null;
        Debug.LogError($"[SceneLessonUI] Load course failed after {attempts} attempts. seo={SeoResolver.seoCourse}");
        OnLoadCourseDone?.Invoke(null);
    }

    private void ResolveSeoForLoad()
    {
        if (!string.IsNullOrEmpty(overrideSeo))
        {
            SeoResolver.seoCourse = overrideSeo;
            return;
        }

        if (!string.IsNullOrEmpty(SeoResolver.seoCourse))
            return;

        string activeScene = SceneManager.GetActiveScene().name;
        string sceneSeo = SeoResolver.GetSeoCourseByScene(activeScene);
        if (!string.IsNullOrEmpty(sceneSeo))
            SeoResolver.seoCourse = sceneSeo;
    }
}
