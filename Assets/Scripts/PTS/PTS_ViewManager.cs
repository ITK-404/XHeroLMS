using UnityEngine;

public class PTS_ViewManager : MonoBehaviour
{
    public static PTS_ViewManager Instance;
    private PTS_BaseView[] views;
    private InteractionManagerUI interactionManager;
    private void Awake()
    {
        Instance = this;        
        
        views = GetComponentsInChildren<PTS_BaseView>();
        interactionManager = GetComponent<InteractionManagerUI>();
        if (views == null)
        {
            Debug.LogError("Views is null");
            return;
        }
        foreach (var item in views)
        {
            item.OnViewClosed += interactionManager.OnExitNoneView;
            item.OnViewOpened += interactionManager.OnEnterCourseView;
        }
    }

    private void OnDestroy()
    {
        if (views == null)
        {
            Debug.LogError("Views is null");
            return;
        }
        foreach (var item in views)
        {
            item.OnViewClosed -= interactionManager.OnExitNoneView;
            item.OnViewOpened -= interactionManager.OnEnterCourseView;
        }
    }

    public UIView Current;
    public void TryShow(string target)
    {
        Debug.Log("PTS_View try find: "+target);

        foreach (var view in views)
        {
            if (view.TargetID == target)
            {
                Current = view;
                view.Show();
                view.ShowDefault();
            }
        }
    }
}