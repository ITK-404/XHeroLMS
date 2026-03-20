using UnityEngine;
using System.Collections;

public class CourseBootstrapLoader : MonoBehaviour
{
    [SerializeField] private CourseApiClient api;

    private IEnumerator Start()
    {
        if (CourseStaticStore.HasData)
            yield break;

        if (api == null)
        {
            Debug.LogError("[CourseBootstrapLoader] CourseApiClient is not assigned.");
            yield break;
        }

        yield return api.FetchAllCourseListItems(
            onDone: items =>
            {
                CourseStaticStore.SetItems(items);
                Debug.Log("[CourseBootstrapLoader] Loaded courses: " + CourseStaticStore.Count);
            },
            onError: err =>
            {
                Debug.LogError("[CourseBootstrapLoader] FetchAllCourseListItems error:\n" + err);
            }
        );
    }
}