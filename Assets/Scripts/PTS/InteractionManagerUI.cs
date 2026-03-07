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

    private UIManager UIManager;
    
    private void Awake()
    {
        uiInstance.OnEnterNoneView += OnExitNoneView;
    }

    private void Start()
    {
        UIManager = UIManager.Instance;
    }

    private void OnDestroy()
    {
        uiInstance.OnEnterNoneView -= OnExitNoneView;
    }

    private void OnExitNoneView()
    {
        InputBlocker.SetBlocked(false);
        uiInstance.Hide();
        //apiInstance.gameObject.SetActive(false);
        
        UIManager.CourseMenuButtons.Show();
        UIManager.InputCanvas.Show();
        UIManager.PlayerPanelUI.ShowAll();
    }

    public void OnEnterCourseView()
    {
        InputBlocker.SetBlocked(true);
        uiInstance.ShowIntroView();
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