using System;
using UnityEngine;

public class InteractionManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private PTS_CourseDetailView uiInstance;
    [SerializeField] private GameObject apiInstance;
    [SerializeField] private CourseMenuButtons courseMenuBtns;
    [SerializeField] private PTS_ParticleE[] particleSystems;
    [SerializeField] private float stopEmitDistance = 0.5f;
    private UIView bindingView;
    private UIManager UIManager;
    
    private PTS_BaseView[] catchView;
    private void Awake()
    {
        // gắn sự kiện khi thoát ra khỏi View
        catchView = GetComponentsInChildren<PTS_BaseView>();
        foreach (var view in catchView)
        {
            view.OnEnterNoneView += OnExitNoneView;
        }
    }

    private void Start()
    {
        UIManager = UIManager.Instance;
    }

    private void OnDestroy()
    {
        foreach (var view in catchView)
        {
            view.OnEnterNoneView -= OnExitNoneView;
        }
    }

    private void OnExitNoneView()
    {
        InputBlocker.SetBlocked(false);
        //apiInstance.gameObject.SetActive(false);
        
        UIManager.CourseMenuButtons.Show();
        UIManager.InputCanvas.Show();
        UIManager.PlayerPanelUI.ShowAll();
    }

    public void OnEnterCourseView(UIView bindingView)
    {
        this.bindingView = bindingView;
        bindingView.Show();
        InputBlocker.SetBlocked(true);
        apiInstance.gameObject.SetActive(true);
        
        UIManager.CourseMenuButtons.Hide();
        UIManager.InputCanvas.Hide();
        UIManager.PlayerPanelUI.HideAll();
    }
    
    private void Update()
    {
        CheckParticles();
    }

    private void CheckParticles()
    {
        if (player == null) return;
        foreach (var ps in particleSystems)
        {
            float distance = Vector3.Distance(player.transform.position, ps.transform.position);
            Debug.Log("Distance: " + distance);
            if (distance < stopEmitDistance)
            {
                ps.DeActive();
            }
            else
            {
                ps.Active();
            }
        }
    }
}