public class PTS_CourseOpeningView : PTS_BaseView
{
    protected override void Awake()
    {
        base.Awake();
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
}