using System.Collections;
using UnityEngine;

public class CourseLearnHandle : MonoBehaviour
{
    public SceneLessonUI sceneLessonUI;
    private LmsCoursePrivate coursePrivate;
    public CourseProgressAPI courseProgressAPI;
    public CourseListView courseListView;
    private void Awake()
    {
        sceneLessonUI.OnLoadCourseDone += OnGetData;
    }

    private void OnDestroy()
    {
        sceneLessonUI.OnLoadCourseDone -= OnGetData;
    }

    private void OnGetData(LmsCoursePrivate coursePrivate)
    {
        this.coursePrivate = coursePrivate;
        StartCoroutine(WaitingForProgress());
        // then waiting for fetch progress Data;
    }

    private IEnumerator WaitingForProgress()
    {
        yield return courseProgressAPI.GetProgressCourseCoroutine();

        foreach (var chapter in coursePrivate.chapters)
        {
            foreach (var lesson in chapter.lessons)
            {
                lesson.progressTime = courseProgressAPI.GetLessonProgress(lesson._id);
            }
        }
        
        
        courseListView.BuildListUI(coursePrivate);
    }

  
}