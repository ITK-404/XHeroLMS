using System.Collections.Generic;
using UnityEngine;

public class PTS_DifferencesCourse : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private RelatedCoursesView relatedCoursePrefab;

    [Header("Options")]
    [SerializeField] private bool clearOldItems = true;
    [SerializeField] private bool autoRefreshOnEnable = true;

    [Header("Display Format")]
    [SerializeField] private bool useCompactNumber = true;

    private readonly string learnersUnit = "học viên";
    private readonly string viewsUnit = "lượt xem";

    private readonly List<RelatedCoursesView> spawnedItems = new();

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += Refresh;

        if (autoRefreshOnEnable)
            Refresh();
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (!CourseDetailStaticStore.HasData)
        {
            ClearAllItems();
            return;
        }

        var course = CourseDetailStaticStore.CurrentDetail;
        if (course == null || course.upsell == null || course.upsell.Count == 0)
        {
            ClearAllItems();
            return;
        }

        if (contentParent == null || relatedCoursePrefab == null)
        {
            Debug.LogWarning("[PTS_DifferencesCourse] Missing contentParent or relatedCoursePrefab.");
            ClearAllItems();
            return;
        }

        if (clearOldItems)
            ClearAllItems();

        List<CourseModels.CourseRelated> sortedList = new List<CourseModels.CourseRelated>(course.upsell);

        sortedList.Sort(CompareDefault);
        PromoteSpecialCoursesOneStep(sortedList);

        for (int i = 0; i < sortedList.Count; i++)
        {
            var item = sortedList[i];
            if (item == null) continue;

            string learnersText = BuildCountText(item.learners, learnersUnit);
            string viewsText = BuildCountText(item.stars, viewsUnit);

            RelatedCoursesView view = Instantiate(relatedCoursePrefab, contentParent);
            view.gameObject.SetActive(true);

            view.Setup(
                item._id,
                item.title,
                learnersText,
                viewsText,
                item.image
            );

            spawnedItems.Add(view);
        }
    }

    private int CompareDefault(CourseModels.CourseRelated a, CourseModels.CourseRelated b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        int compareViews = b.stars.CompareTo(a.stars);
        if (compareViews != 0) return compareViews;

        int compareLearners = b.learners.CompareTo(a.learners);
        if (compareLearners != 0) return compareLearners;

        return 0;
    }

    private void PromoteSpecialCoursesOneStep(List<CourseModels.CourseRelated> list)
    {
        if (list == null || list.Count < 2) return;

        for (int i = 1; i < list.Count; i++)
        {
            var current = list[i];
            if (current == null) continue;

            bool isSpecial = current.learners > current.stars;
            if (!isSpecial) continue;

            var prev = list[i - 1];
            list[i - 1] = current;
            list[i] = prev;

            i++;
        }
    }

    private void ClearAllItems()
    {
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }

        spawnedItems.Clear();
    }

    private string BuildCountText(float value, string unit)
    {
        return $"{FormatCompactNumber(value)} {unit}";
    }

    private string FormatCompactNumber(float value)
    {
        if (value < 0f)
            value = 0f;

        if (!useCompactNumber)
            return value % 1f == 0f ? ((int)value).ToString("N0") : value.ToString("0.#");

        if (value < 1000f)
            return value % 1f == 0f ? ((int)value).ToString() : value.ToString("0.#");

        if (value < 1000000f)
        {
            float k = value / 1000f;

            if (Mathf.Approximately(k % 1f, 0f))
                return $"{k:0}k";

            return $"{k:0.#}k";
        }

        float m = value / 1000000f;

        if (Mathf.Approximately(m % 1f, 0f))
            return $"{m:0}M";

        return $"{m:0.#}M";
    }
}