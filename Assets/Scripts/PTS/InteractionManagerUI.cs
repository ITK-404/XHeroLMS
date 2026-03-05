using UnityEngine;

public class InteractionManagerUI : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject uiInstance;
    [SerializeField] private GameObject apiInstance;
    [SerializeField] private CourseMenuButtons courseMenuBtns;
    [SerializeField] private PTS_ParticleE[] particleSystems;
    [SerializeField] private float stopEmitDistance = 0.5f;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            InputBlocker.SetBlocked(true);
            uiInstance.gameObject.SetActive(true);
            apiInstance.gameObject.SetActive(true);
            courseMenuBtns.Hide();
            PlayerPanelUI.Instance.HideAll();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            InputBlocker.SetBlocked(false);
            uiInstance.gameObject.SetActive(false);
            apiInstance.gameObject.SetActive(false);
            courseMenuBtns.Show();
            PlayerPanelUI.Instance.ShowAll();
        }
        
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