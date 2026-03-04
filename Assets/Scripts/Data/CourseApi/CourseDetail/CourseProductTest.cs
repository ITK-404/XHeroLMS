// CourseProductTest.cs
using UnityEngine;

public class CourseProductTest : MonoBehaviour
{
    [SerializeField] private CourseDetailLoader loader;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += OnStoreChanged;
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= OnStoreChanged;
    }

    // Gọi khi nhấn button
    public void TestLoadCourse(string courseId)
    {
        if (debugLog)
            Debug.Log("TEST LOAD COURSE ID: " + courseId);

        if (loader == null)
        {
            Debug.LogError("CourseDetailLoader missing");
            return;
        }

        // Nếu bạn muốn “đổi id mà vẫn chắc chắn reload” thì dùng forceReload=true
        loader.Load(courseId, forceReload: true);
    }

    private void OnStoreChanged()
    {
        if (debugLog)
        {
            Debug.Log($"[OnStoreChanged] IsLoading={CourseDetailStaticStore.IsLoading} HasData={CourseDetailStaticStore.HasData} StoreId={CourseDetailStaticStore.CurrentCourseId} Err={CourseDetailStaticStore.LastError}");
        }

        if (CourseDetailStaticStore.IsLoading)
        {
            Debug.Log("Course loading...");
            return;
        }

        if (!CourseDetailStaticStore.HasData)
        {
            Debug.LogWarning("Course detail not found: " + CourseDetailStaticStore.LastError);
            return;
        }

        var course = CourseDetailStaticStore.CurrentCourse;

        Debug.Log("Course Loaded: " + course.title);
        Debug.Log("Course Type: " + course.GetType().FullName);

        if (course.products == null || course.products.Count == 0)
        {
            Debug.Log("No products in this course");
            return;
        }

        Debug.Log("===== PRODUCTS =====");
        for (int i = 0; i < course.products.Count; i++)
        {
            var p = course.products[i];
            Debug.Log($"Product {i} | id={p._id} | name={p.productName} | image={p.image} | url={p.externalUrl}");
        }
    }
}