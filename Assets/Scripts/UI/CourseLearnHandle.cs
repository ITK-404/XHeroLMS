using System.Collections;
using UnityEngine;

public class CourseLearnHandle : MonoBehaviour
{
    public SceneLessonUI sceneLessonUI;
    private LmsCoursePrivate coursePrivate;
    public CourseProgressAPI courseProgressAPI;
    public CourseListView courseListView;
    public FinalExamHandler finalExamHandler;

    private Coroutine buildRoutine;
    private bool hasBuiltCourseList;
    private string activeCourseId;

    private void Awake()
    {
        if (sceneLessonUI != null)
            sceneLessonUI.OnLoadCourseDone += OnGetData;
        else
            Debug.LogError("[CourseLearnHandle] Missing SceneLessonUI reference.");

        if (courseListView != null && finalExamHandler != null)
            courseListView.OnClickFinalExamEvt += finalExamHandler.OnClickFinalExam;
    }

    private IEnumerator Start()
    {
        yield return null;

        if (hasBuiltCourseList)
            yield break;

        LmsCoursePrivate loadedCourse = sceneLessonUI != null
            ? sceneLessonUI.CurrentCourse
            : SeoResolver.LmsCoursePrivate;

        if (loadedCourse != null)
        {
            OnGetData(loadedCourse);
            yield break;
        }

        if (sceneLessonUI != null)
        {
            yield return sceneLessonUI.EnsureCourseLoadedRoutine();
        }
        else
        {
            Debug.LogError("[CourseLearnHandle] Cannot load course because SceneLessonUI is null.");
        }
    }

    private void OnDestroy()
    {
        if (sceneLessonUI != null)
            sceneLessonUI.OnLoadCourseDone -= OnGetData;

        if (courseListView != null && finalExamHandler != null)
            courseListView.OnClickFinalExamEvt -= finalExamHandler.OnClickFinalExam;
    }

    private void OnGetData(LmsCoursePrivate coursePrivate)
    {
        if (coursePrivate == null)
        {
            Debug.LogWarning("[CourseLearnHandle] Course data is null. Keep current lesson list visible if it already exists.");
            return;
        }

        string newCourseId = coursePrivate._id;
        if (hasBuiltCourseList && activeCourseId == newCourseId)
            return;

        this.coursePrivate = coursePrivate;
        activeCourseId = newCourseId;

        if (courseProgressAPI != null)
            courseProgressAPI.courseID = coursePrivate._id;

        if (finalExamHandler != null)
            finalExamHandler.SetCourseID(coursePrivate._id);

        if (buildRoutine != null)
            StopCoroutine(buildRoutine);

        buildRoutine = StartCoroutine(BuildCourseListThenRefreshProgress());
    }

    private IEnumerator BuildCourseListThenRefreshProgress()
    {
        if (coursePrivate == null)
            yield break;

        if (courseListView == null)
        {
            Debug.LogError("[CourseLearnHandle] Missing CourseListView reference.");
            yield break;
        }

        courseListView.BuildListUI(coursePrivate);
        hasBuiltCourseList = true;

        if (courseProgressAPI == null || string.IsNullOrEmpty(coursePrivate._id))
            yield break;

        yield return courseProgressAPI.GetProgressCourseCoroutine();

        if (!courseProgressAPI.HasProgressData)
        {
            Debug.LogWarning("[CourseLearnHandle] Progress data not ready. Lesson list is already visible.");
            yield break;
        }

        ApplyProgressToCourse(coursePrivate);
        courseListView.BuildListUI(coursePrivate);
    }

    private void ApplyProgressToCourse(LmsCoursePrivate coursePrivate)
    {
        if (coursePrivate?.chapters == null)
            return;

        foreach (var chapter in coursePrivate.chapters)
        {
            if (chapter?.lessons == null)
                continue;

            foreach (var lesson in chapter.lessons)
            {
                if (lesson == null || string.IsNullOrEmpty(lesson._id))
                    continue;

                lesson.progressTime = courseProgressAPI.GetLessonProgress(lesson._id);
            }
        }
    }
}
