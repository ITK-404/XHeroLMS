using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLessonUI : MonoBehaviour
{
    [Header("Data")]
    public string overrideSeo = "";

    [Header("UI")]
    // Prefab item (Tag "Title" và/hoặc "QA")
    [Header("Options")] 
    public bool autoFetchPrivateIfMissing = true;

    public bool autoStart = true;

    // --- internal ---
    private string _seo;
    private string _courseTitle = "(no course title)";

    public Action<LmsCoursePrivate> OnLoadCourseDone;
    
    private void Awake()
    {
        _ = LmsStore.Instance; // đảm bảo singleton tồn tại
    }

    private IEnumerator Start()
    {
        if (autoStart == false)
        {
            yield break;
        }
        Debug.Log("Bắt đầu load data");
        if (!string.IsNullOrEmpty(overrideSeo))
        {
            SeoResolver.seoCourse = overrideSeo;
            yield return SeoResolver.LoadPrivateAndFillData();
        }
        OnLoadCourseDone?.Invoke(SeoResolver.LmsCoursePrivate);
    }

    private bool isLoading = false;

    public bool IsLoading
    {
        get => isLoading;
    }
}