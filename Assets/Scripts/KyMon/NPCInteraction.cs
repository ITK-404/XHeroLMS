using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera focusCamera;
    [SerializeField] private PointClickSystem playerMoveSystem;
  
    [Header("Setting")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private float allowTriggerDistance = 5f;
    [Header("UI")]
    [SerializeField] private NPCInteractionUIView interactionUIView;
    [SerializeField] private ActionChoiceViewUI actionChoiceViewUI;
    [SerializeField] private KyMon_WorldSpaceUI worldSpaceUi;
    // player
    private PointClickSystem pointClickSystem;
    private PlayerCamera playerCamera;

    private int playerCameraPriority = 1;
    
    private void Awake()
    {
        focusCamera.gameObject.SetActive(false);
        interactionUIView.OnClickWorldSpaceEvent += ViewOnOnClickWorldSpaceEvent;
    }

    private void OnDestroy()
    {
        interactionUIView.OnClickWorldSpaceEvent -= ViewOnOnClickWorldSpaceEvent;
    }

    private void SetupWorldSpaceUI()
    {
        worldSpaceUi.SetPlayer(pointClickSystem.transform);
        worldSpaceUi.SetTarget(target);
        actionChoiceViewUI.Hide();
        interactionUIView.ShowWorldSpaceIcon();
    }
    
    private void Start()
    {
        playerCamera = PlayerCamera.Instance;
        pointClickSystem = FindFirstObjectByType<PointClickSystem>();
        focusCamera.Priority.Value = playerCamera.playerCinemachineCamera.Priority.Value;
        SetupWorldSpaceUI();
    }

    private void ViewOnOnClickWorldSpaceEvent()
    {
        // alway have, because this room is alway learn
        // if (PlayerChairManager.Instance.playerState == PlayerChairManager.PlayerState.Sitdown)
        // {
        //     Debug.Log($"Return");
        //     return;
        // }
        // if (pointClickSystem.IsBlendingCamera())
        // {
        //     Debug.Log($"Return");
        //     return;
        // }
        //
        float distance = Vector3.Distance(player.transform.position, target.transform.position);
        Debug.Log($"Checking distance: " + distance);
        //
        if (distance < allowTriggerDistance)
        {
            FocusCamera();
            StartCoroutine(WaitForBlending(() =>
            {
                interactionUIView.ShowSupportChatBox();
                actionChoiceViewUI.Show();
            }));
        }
        Debug.Log("NPC INteract, try interact with ui");
    }

    private IEnumerator WaitForBlending(Action onComplete)
    {
        yield return new WaitForSeconds(2f);
        onComplete?.Invoke();
    }

    private void FocusCamera()
    {
        focusCamera.gameObject.SetActive(true);
    }
}