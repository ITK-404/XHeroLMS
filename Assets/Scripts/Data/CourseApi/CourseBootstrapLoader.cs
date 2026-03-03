using UnityEngine;
using System.Collections;

public class CourseBootstrapLoader : MonoBehaviour
{
    public CourseApiClient api;

    private IEnumerator Start()
    {
        if (CourseStaticStore.HasData) yield break; // đã có thì thôi

        yield return api.FetchAllCoursesLite(
            onDone: resp =>
            {
                CourseStaticStore.SetCoursesLite(resp);
                Debug.Log("Loaded courses: " + CourseStaticStore.Count);
            },
            onError: err =>
            {
                Debug.LogError("FetchAllCourses error:\n" + err);
            }
        );
    }
}