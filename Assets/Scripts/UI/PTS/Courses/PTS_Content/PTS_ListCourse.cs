using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

public class PTS_ListCourse : MonoBehaviour
{
    [Header("Course Source (CourseDetailStaticStore)")]
    [Tooltip("Nếu bật, chỉ build khi CurrentCourseId == expectedCourseId")]
    [SerializeField] private bool filterByCourseId = false;

    [SerializeField] private string expectedCourseId = "";

    [Tooltip("Đợi store có data (giây) nếu OnEnable chạy sớm.")]
    [SerializeField] private float waitStoreSeconds = 5f;

    [Header("Chapter Prefab & Parent")]
    [SerializeField] private ChapterUI chapterPrefab;
    [SerializeField] private Transform contentParent;
    [Header("Lesson Item Prefab")]
    [SerializeField] private GameObject lessonItemPrefab;

    [Header("Options")]
    [SerializeField] private bool clearChildrenBeforeBuild = true;

    [SerializeField] private bool buildOnEnable = true;

    private Coroutine _running;

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += HandleStoreChanged;
        if (buildOnEnable) Build();
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= HandleStoreChanged;
        if (_running != null) StopCoroutine(_running);
        _running = null;
    }

    private void HandleStoreChanged() => Build();

    [ContextMenu("Build")]
    public void Build()
    {
        if (chapterPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[PTS_ListCourse] Missing chapterPrefab/contentParent");
            return;
        }

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(CoWaitStoreThenBuild());
    }

    private IEnumerator CoWaitStoreThenBuild()
    {
        float t = 0f;

        while (string.IsNullOrEmpty(CourseDetailStaticStore.CurrentCourseId) && t < waitStoreSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        string courseId = CourseDetailStaticStore.CurrentCourseId;

        if (string.IsNullOrEmpty(courseId))
        {
            Debug.LogWarning("[PTS_ListCourse] Chưa có CurrentCourseId.");
            yield break;
        }

        if (filterByCourseId)
        {
            if (string.IsNullOrEmpty(expectedCourseId))
            {
                Debug.LogWarning("[PTS_ListCourse] filterByCourseId=true nhưng expectedCourseId rỗng.");
                yield break;
            }

            if (!string.Equals(courseId, expectedCourseId, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[PTS_ListCourse] Store đang giữ courseId={courseId} khác expected={expectedCourseId}. Skip build.");
                yield break;
            }
        }

        yield return LmsStore.Instance.FetchPrivateIfExpired(courseId);

        var course = LmsStore.Instance.GetPrivate(courseId);
        if (course == null)
        {
            Debug.LogWarning($"[PTS_ListCourse] Private course null for courseId={courseId}");
            yield break;
        }

        BuildFromCourse(course);
    }

    private void BuildFromCourse(LmsCoursePrivate course)
    {
        List<LmsChapter> chapters = course.chapters;
        bool isEmpty = chapters == null || chapters.Count == 0;
        
        if (isEmpty)
        {
            Debug.LogWarning("[PTS_ListCourse] Course không có chapters hoặc chapters rỗng.");
            if (clearChildrenBeforeBuild) ClearChildren(contentParent);
            return;
        }
        

        if (clearChildrenBeforeBuild) ClearChildren(contentParent);

        for (int i = 0; i < chapters.Count; i++)
        {
            var ch = chapters[i];
            if (ch == null) continue;

            var ui = Instantiate(chapterPrefab, contentParent);

            ui.chapterID = ch._id ?? "";
            ui.ChangeState(ChapterUI.ChapterState.Normal);

            if (ui.titleName != null)
                ui.titleName.text = ch.chapterTitle ?? "";

            if (ui.lessonContainer == null)
            {
                Debug.LogWarning("[PTS_ListCourse] ChapterUI missing lessonContainer reference.");
                continue;
            }

            Transform lessonContainerTf = ui.lessonContainer.transform;
            ClearChildren(lessonContainerTf);

            if (lessonItemPrefab == null) continue;

            var lessonsObj = ch.lessons;
            if (lessonsObj == null) continue;

            for (int j = 0; j < lessonsObj.Count; j++)
            {
                var lesson = lessonsObj[j];
                if (lesson == null) continue;

                var itemGo = Instantiate(lessonItemPrefab, lessonContainerTf);

                string lessonTitle =
                    lesson.title ?? "";

                SetAnyText(itemGo.transform, lessonTitle);
            }

            ui.ChangeState(ChapterUI.ChapterState.Normal);
        }
    }

    // ===== Reflection helpers =====

    private static object GetMemberValue(object obj, string memberName)
    {
        if (obj == null) return null;
        var t = obj.GetType();

        var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (f != null) { try { return f.GetValue(obj); } catch { } }

        var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
        if (p != null && p.CanRead) { try { return p.GetValue(obj, null); } catch { } }

        return null;
    }

    private static string GetStringMember(object obj, string memberName)
    {
        var v = GetMemberValue(obj, memberName);
        return v != null ? v.ToString() : null;
    }

    // ===== UI helpers =====

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private static void SetAnyText(Transform root, string value)
    {
        if (root == null) return;

        var tmp = root.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) { tmp.text = value ?? ""; return; }

        var txt = root.GetComponentInChildren<UnityEngine.UI.Text>(true);
        if (txt != null) { txt.text = value ?? ""; return; }
    }
}