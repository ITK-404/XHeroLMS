using UnityEngine;

public class CourseStoreTest : MonoBehaviour
{
    [ContextMenu("[zzz]Test Count Online/Zoom Courses")]
    public void TestCountOnlineOrZoom()
    {
        Debug.Log("[zzz]===== TEST ONLINE/ZOOM COURSES =====");

        if (!CourseStaticStore.HasData)
        {
            Debug.LogWarning("[zzz]CourseStaticStore chưa có data.");
            Debug.Log("[zzz]Current Count: " + CourseStaticStore.Count);
            return;
        }

        int count = 0;
        var all = CourseStaticStore.GetAll();

        Debug.Log("[zzz]Total courses in store: " + all.Count);

        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c == null) continue;

            var mode = c.learningMode;

            Debug.Log($"[zzz]Course: {c.title} | learningMode = {mode}");

            if (string.IsNullOrEmpty(mode)) continue;

            mode = mode.Trim().ToLowerInvariant();

            if (mode == "online" ||
                mode == "zoom" ||
                mode.Contains("online") ||
                mode.Contains("zoom"))
            {
                count++;
            }
        }

        if (CourseStaticStore.GetById("66cd7727cf0a681e2153fe14") != null)
            Debug.Log("[zzz]có khóa Đại Đạo Chí Giản - Phong Thủy Cổ Học II");

        Debug.Log("[zzz]=====================================");
        Debug.Log($"[zzz]Số khóa học learningMode = online hoặc zoom: {count}");
    }
}