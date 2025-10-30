using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SeoResolver
{
    public const string DefaultScene = "dai_dao_chi_gian_1";
    public static string seoCourse;
    private static TextAsset textAsset;
    private static LmsCoursePrivate _lmsCoursePrivate;

    public static LmsCoursePrivate LmsCoursePrivate
    {
        get => _lmsCoursePrivate;
    }

    public static void SetSeoCourse(string scene)
    {
        seoCourse = GetSeoCourseByScene(scene);
    }
    
    // Returns the seo string for the given scene name by loading Resources/{resourceJsonName}.json
    // Returns null and logs an error if not found or missing mapping.
    public static string GetSeoCourseByScene(string scene, string resourceJsonName = "courses")
    {
        if (string.IsNullOrEmpty(scene))
        {
            Debug.LogError("[SeoResolver] scene is null or empty");
            return null;
        }

        // Load the TextAsset from Resources
        var txt = Resources.Load<TextAsset>(resourceJsonName);
        if (!txt)
        {
            Debug.LogError($"[SeoResolver] Missing Resources/{resourceJsonName}.json");
            return null;
        }

        // Parse JSON (wrap into {\"items\": ...} because the resource is an array)
        var wrapped = "{\"items\":" + txt.text + "}";
        SceneSeoList map = null;
        try
        {
            map = JsonUtility.FromJson<SceneSeoList>(wrapped);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SeoResolver] Failed parsing {resourceJsonName}: {ex}");
            return null;
        }

        string sceneName = scene;
        string s1 = sceneName, s2 = $"<{sceneName}>", s3 = sceneName.Trim('<', '>'), s4 = s3.Trim();
        var candidates = new HashSet<string>(new[] { s1, s2, s3, s4 }, StringComparer.OrdinalIgnoreCase);

        SceneSeoItem item = null;
        if (map?.items != null)
        {
            foreach (var it in map.items)
            {
                if (it == null || string.IsNullOrEmpty(it.sceneName)) continue;
                var raw = it.sceneName;
                var norm = raw.Trim().Trim('<', '>');
                if (candidates.Contains(raw) || candidates.Contains(norm) || candidates.Contains($"<{norm}>") )
                {
                    item = it;
                    break;
                }
            }
        }

        if (item == null)
        {
            Debug.LogError($"[SeoResolver] No SEO mapping for scene '{sceneName}'");
            return null;
        }

        return item.seo;
    }

    public static IEnumerator LoadPrivateAndFillData()
    {
        var _seo = seoCourse;
        _lmsCoursePrivate = null;
        if (string.IsNullOrEmpty(_seo))
        {
            yield break;
        }
        // Resolve courseId theo seo.url
        var courseId = LmsStore.Instance.GetCourseIdBySeo(_seo);
        if (string.IsNullOrEmpty(courseId))
        {
            if (!TokenStore.IsAuthenticated)
            {
                Debug.LogError("[SceneLessonUI] Not authenticated -> không thể fetch.");
                yield break;
            }

            yield return LmsStore.Instance.FetchMarketIfExpired(0, 500, "", "", "", "");
            yield return LmsStore.Instance.FetchMyCoursesIfExpired();
            courseId = LmsStore.Instance.GetCourseIdBySeo(_seo);
            if (string.IsNullOrEmpty(courseId))
            {
                Debug.LogError($"[SceneLessonUI] Không resolve được courseId cho seo='{_seo}'");
                yield break;
            }
        }

        // Private
        if (LmsStore.Instance.GetPrivate(courseId) == null)
            yield return LmsStore.Instance.FetchPrivateIfExpired(courseId);

        var p = LmsStore.Instance.GetPrivate(courseId);
        if (p == null)
        {
            Debug.LogError($"[SceneLessonUI] Private null cho courseId='{courseId} ");
        }

        _lmsCoursePrivate = p;
    }

    public static bool IsContainData()
    {
        return _lmsCoursePrivate != null;
    }
}