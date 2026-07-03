using UnityEngine;
using System.Collections;

public class CourseBootstrapLoader : MonoBehaviour
{
    private const float ErrorRetryCooldownSeconds = 5f;

    [SerializeField] private CourseApiClient api;
    private static CourseBootstrapLoader s_runtimeLoader;
    private static float s_lastErrorRetryRealtime = -999f;
    private Coroutine _loadRoutine;

    private void Awake()
    {
        if (api == null)
            api = GetComponent<CourseApiClient>();
    }

    private void Start()
    {
        EnsureLoadStarted();
    }

    public static void EnsureLoaded()
    {
        if (CourseStaticStore.HasData)
            return;

        if (CourseStaticStore.IsLoading && !CourseStaticStore.IsLoadingStale())
            return;

        bool canRecoverWithRuntimeLoader =
            !string.IsNullOrEmpty(CourseStaticStore.LastError) &&
            CourseStaticStore.LastError.Contains("CourseApiClient is not assigned");

        if (!string.IsNullOrEmpty(CourseStaticStore.LastError) && !canRecoverWithRuntimeLoader)
        {
            if (Time.realtimeSinceStartup - s_lastErrorRetryRealtime < ErrorRetryCooldownSeconds)
                return;

            s_lastErrorRetryRealtime = Time.realtimeSinceStartup;
            Debug.LogWarning("[CourseBootstrapLoader] Retry course list after error: " + CourseStaticStore.LastError);
        }

        var loader = FindFirstObjectByType<CourseBootstrapLoader>();
        if (loader != null && loader.api == null)
            loader.api = loader.GetComponent<CourseApiClient>();

        if (loader == null || loader.api == null)
            loader = GetOrCreateRuntimeLoader();

        loader.EnsureLoadStarted();
    }

    private static CourseBootstrapLoader GetOrCreateRuntimeLoader()
    {
        if (s_runtimeLoader != null)
            return s_runtimeLoader;

        var go = new GameObject("[CourseBootstrapLoader Runtime]");
        DontDestroyOnLoad(go);

        s_runtimeLoader = go.AddComponent<CourseBootstrapLoader>();
        s_runtimeLoader.api = go.AddComponent<CourseApiClient>();
        return s_runtimeLoader;
    }

    private void EnsureLoadStarted()
    {
        if (_loadRoutine != null)
            return;

        _loadRoutine = StartCoroutine(LoadRoutine());
    }

    private IEnumerator LoadRoutine()
    {
        if (CourseStaticStore.HasData)
        {
            _loadRoutine = null;
            yield break;
        }

        if (!CourseStaticStore.TryBeginLoad())
        {
            while (CourseStaticStore.IsLoading)
                yield return null;

            _loadRoutine = null;
            yield break;
        }

        Debug.Log("[CourseBootstrapLoader] Start loading course list.");

        if (api == null)
            api = GetComponent<CourseApiClient>();

        if (api == null)
        {
            CourseStaticStore.SetLoadError("CourseApiClient is not assigned.");
            Debug.LogError("[CourseBootstrapLoader] CourseApiClient is not assigned.");
            _loadRoutine = null;
            yield break;
        }

        bool failed = false;
        bool committedFirstPage = false;

        yield return api.FetchAllCourseListItems(
            onDone: items =>
            {
                if (failed)
                    return;

                CourseStaticStore.SetItems(items);
                Debug.Log("[CourseBootstrapLoader] Loaded courses: " + CourseStaticStore.Count);
            },
            onError: err =>
            {
                failed = true;
                CourseStaticStore.SetLoadError(err);
                Debug.LogError("[CourseBootstrapLoader] FetchAllCourseListItems error:\n" + err);
            },
            onFirstPage: items =>
            {
                if (failed || committedFirstPage || items == null || items.Count == 0)
                    return;

                committedFirstPage = true;
                CourseStaticStore.SetItems(items);
                PTS_SimpleCourseUI.PrewarmImages(items, 16);
                Debug.Log("[CourseBootstrapLoader] Loaded first course page: " + CourseStaticStore.Count);
            }
        );

        if (!failed && !CourseStaticStore.HasData)
            CourseStaticStore.SetLoadError("[CourseBootstrapLoader] No course data returned.");

        _loadRoutine = null;
    }
}
