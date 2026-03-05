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
        if (buildOnEnable) Build();
        CourseDetailStaticStore.OnChanged += HandleStoreChanged;
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

        while (!CourseDetailStaticStore.HasData && t < waitStoreSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!CourseDetailStaticStore.HasData)
        {
            if (CourseDetailStaticStore.IsLoading)
                Debug.LogWarning("[PTS_ListCourse] CourseDetailStaticStore vẫn đang loading...");
            else
                Debug.LogWarning("[PTS_ListCourse] CourseDetailStaticStore chưa có data. LastError=" + CourseDetailStaticStore.LastError);

            yield break;
        }

        if (filterByCourseId)
        {
            if (string.IsNullOrEmpty(expectedCourseId))
            {
                Debug.LogWarning("[PTS_ListCourse] filterByCourseId=true nhưng expectedCourseId rỗng.");
                yield break;
            }

            if (!string.Equals(CourseDetailStaticStore.CurrentCourseId, expectedCourseId, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[PTS_ListCourse] Store đang giữ courseId={CourseDetailStaticStore.CurrentCourseId} khác expected={expectedCourseId}. Skip build.");
                yield break;
            }
        }

        var course = CourseDetailStaticStore.CurrentCourse;
        if (course == null)
        {
            Debug.LogWarning("[PTS_ListCourse] CurrentCourse null dù HasData=true (bất thường).");
            yield break;
        }

        BuildFromCourse(course);
    }

    private void BuildFromCourse(LmsCoursePrivate course)
    {
        List<LmsChapter> chapters = course.chapters;

        if (chapters == null || chapters.Count == 0)
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

            // ✅ Mặc định CHƯA CLICK => Normal
            ui.ChangeState(ChapterUI.ChapterState.Normal);

            if (ui.titleName != null)
                ui.titleName.text = ch.chapterTitle ?? "";

            if (ui.lessonContainer == null)
            {
                Debug.LogWarning("[PTS_ListCourse] ChapterUI missing lessonContainer reference.");
                continue;
            }

            // ✅ Không tự SetActive(true/false) nữa.
            // ChapterUI.UpdateUI sẽ bật/tắt lessonContainer theo state.

            Transform lessonContainerTf = ui.lessonContainer.transform;

            // build sẵn nội dung con (dù đang ẩn)
            ClearChildren(lessonContainerTf);

            if (lessonItemPrefab == null) continue;

            var lessonsObj = GetMemberValue(ch, "lessons");
            if (lessonsObj == null) continue;

            if (lessonsObj is IList list)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    var lesson = list[j];
                    if (lesson == null) continue;

                    var itemGo = Instantiate(lessonItemPrefab, lessonContainerTf);

                    string lessonTitle =
                        GetStringMember(lesson, "title") ??
                        GetStringMember(lesson, "lessonTitle") ??
                        GetStringMember(lesson, "name") ??
                        "";

                    SetAnyText(itemGo.transform, lessonTitle);
                }
            }
            else
            {
                Debug.LogWarning("[PTS_ListCourse] lessons không phải IList. Type=" + lessonsObj.GetType().Name);
            }

            // ✅ đảm bảo state Normal được áp lại lần nữa sau khi build con (tránh prefab/manager làm đổi)
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