using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

[System.Serializable] public class SceneSeoItem { public string sceneName; public string _id; public string seo; public string image; public string title; }
[System.Serializable] class SceneSeoList { public List<SceneSeoItem> items = new(); }

public class SceneCourseRouter : MonoBehaviour
{
    [Tooltip("Tên file JSON trong Resources (không kèm .json)")]
    public string resourceJsonName = "courses";

    [Tooltip("Nếu true, sẽ FetchPrivate khi thiếu cache")]
    public bool autoFetchPrivateIfMissing = true;

    string _seo; string _courseId;

    IEnumerator Start()
    {
        // Load map JSON
        var txt = Resources.Load<TextAsset>(resourceJsonName);
        if (!txt) { Debug.LogError("[SceneCourseRouter] Missing StreamingAssets/" + resourceJsonName + ".json"); yield break; }

        // Bọc thành object để JsonUtility parse: {"items":[...]}
        var wrapped = "{\"items\":" + txt.text + "}";
        var map = JsonUtility.FromJson<SceneSeoList>(wrapped);

        var sceneName = SceneManager.GetActiveScene().name;
        var item = map.items.Find(i => i.sceneName == sceneName);
        if (item == null) { Debug.LogWarning("[SceneCourseRouter] No SEO mapping for scene " + sceneName); yield break; }

        _seo = item.seo;

        // Tra id/ private/ video
        _ = LmsStore.Instance;
        _courseId = LmsStore.Instance.GetCourseIdBySeo(_seo);

        if (autoFetchPrivateIfMissing && !string.IsNullOrEmpty(_courseId) &&
            LmsStore.Instance.GetPrivate(_courseId) == null)
        {
            yield return LmsStore.Instance.FetchPrivateIfExpired(_courseId);
        }

        if (LmsStore.Instance.TryGetVideoLinkBySeo(_seo, out var link))
        {
            Debug.Log($"[SceneCourseRouter] {sceneName} -> seo={_seo} -> courseId={_courseId} -> video={link}");
        }
        else
        {
            Debug.Log($"[SceneCourseRouter] {sceneName} -> seo={_seo} (chưa có videoLink trong cache)");
        }
    }
}
