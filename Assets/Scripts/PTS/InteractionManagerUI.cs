using System;
using UnityEngine;

public class InteractionManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private CourseMenuButtons courseMenuBtns;
    [SerializeField] private PTS_ParticleE[] particleSystems;
    [SerializeField] private float stopEmitDistance = 0.5f;
    private UIManager UIManager;
    private bool ownsGameplayLock;
    
    private void Start()
    {
        UIManager = UIManager.Instance;
    }

    public void OnExitNoneView()
    {
        PlayerPanelUI.Instance.ShowAll();
        SetGameplayLock(false);
        //apiInstance.gameObject.SetActive(false);
        
        UIManager.CourseMenuButtons.Show();
        UIManager.InputCanvas.Show();
        // UIManager.PlayerPanelUI.ShowAll();
        UIManager.PlayerPanelUI.playerInformation.gameObject.SetActive(true);
    }

    public void OnEnterCourseView()
    {
        PlayerPanelUI.Instance.HideAll();
        SetGameplayLock(true);
        
        UIManager.CourseMenuButtons.Hide();
        UIManager.InputCanvas.Hide();
        UIManager.PlayerPanelUI.playerInformation.gameObject.SetActive(false);
        
        // UIManager.PlayerPanelUI.HideAll();
    }
    
    private void Update()
    {
        CheckParticles();
    }

    private void OnDestroy()
    {
        SetGameplayLock(false);
    }

    private void CheckParticles()
    {
        if (player == null) return;
        foreach (var ps in particleSystems)
        {
            float distance = Vector3.Distance(player.transform.position, ps.transform.position);
            // Debug.Log("Distance: " + distance);
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

    private void SetGameplayLock(bool locked)
    {
        if (ownsGameplayLock == locked)
            return;

        if (locked)
        {
            GameplayLock.Lock(GameplayLockReason.UI, GameplayLockTarget.All);
        }
        else
        {
            GameplayLock.Unlock(GameplayLockReason.UI);
        }

        ownsGameplayLock = locked;
    }
}
