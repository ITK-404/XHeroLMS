using UnityEngine;

public class PTS_CourseOpeningView : PTS_BaseView
{
    public static PTS_CourseOpeningView Instance;

    
    [SerializeField] private FrameEduCourseUI frameEduCourse;
    [SerializeField] private Transform arrowNavigation;
    [SerializeField] private CourseDetailInformation courseDetailInformation;
    [SerializeField] private GameObject coursePanel;
    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        btnReturn.onClick.AddListener(OnReturn);
    }

    private void OnDestroy()
    {
        btnReturn.onClick.RemoveListener(OnReturn);
    }

    private void OnReturn()
    {
        OnEnterNoneView?.Invoke();
        Hide();   
    }

    protected override void OnBeforeShow()
    {
        base.OnBeforeShow();
        frameEduCourse.Show();
        frameEduCourse.FirstIndex();
        arrowNavigation.gameObject.SetActive(true);
        courseDetailInformation.Hide();
        coursePanel.gameObject.SetActive(false);
    }

    public void ShowCourseInformation()
    {
        // tạm thời
        coursePanel.gameObject.SetActive(true);
        frameEduCourse.Hide();
        var informationType = CourseDetailInformation.InformationType.ContainClass;
        courseDetailInformation.Show(informationType);
    }

}