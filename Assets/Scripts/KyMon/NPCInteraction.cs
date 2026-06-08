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
    [SerializeField] private InputCanvas inputCanvas;
    // player
    private PlayerStandUI playerStandUI;
    
    private PointClickSystem pointClickSystem;
    private PlayerCamera playerCamera;

    private int playerCameraPriority = 1;
    
    private void Awake()
    {
        focusCamera.gameObject.SetActive(false);
        interactionUIView.OnClickWorldSpaceEvent += ViewOnOnClickWorldSpaceEvent;
        actionChoiceViewUI.OnClickReturnBtn += InteractionUIViewOnOnClickReturnBtn;
    }

    
    private void OnDestroy()
    {
        interactionUIView.OnClickWorldSpaceEvent -= ViewOnOnClickWorldSpaceEvent;
        actionChoiceViewUI.OnClickReturnBtn -= InteractionUIViewOnOnClickReturnBtn;
    }

    private void SetupWorldSpaceUI()
    {
        worldSpaceUi.SetPlayer(player.transform);
        worldSpaceUi.SetTarget(target);
        worldSpaceUi.SetCamera(playerCamera.mainCamera);
        actionChoiceViewUI.Hide();
        interactionUIView.ShowWorldSpaceIcon();
    }
    
    private void Start()
    {
        playerCamera = PlayerCamera.Instance;
        playerStandUI = FindFirstObjectByType<PlayerStandUI>(FindObjectsInactive.Include);
        
        focusCamera.Priority.Value = playerCamera.playerCinemachineCamera.Priority.Value;
        SetupWorldSpaceUI();
    }
    private bool isFocused = false;

    private IEnumerator WaitForBlending(Action onComplete)
    {
        yield return new WaitForSeconds(2f);
        onComplete?.Invoke();
    }

    private void FocusCamera()
    {
        focusCamera.gameObject.SetActive(true);
    }
    
    private void EnterFocusState()
    {
        isFocused = true;
        inputCanvas.Hide();
        focusCamera.gameObject.SetActive(true);
        playerMoveSystem.enabled = false;          // tắt di chuyển
        playerStandUI.returnBtn.gameObject.SetActive(false);
        InputBlocker.SetBlocked(true);
        StartCoroutine(WaitForBlending(() =>
        {
            interactionUIView.ShowSupportChatBox();
            actionChoiceViewUI.Show();
        }));
        
        SignalBus.Send(new ChairCheckPointVisibilityCommand(false));
    }

    private void ExitFocusState()
    {
        isFocused = false;
        
        focusCamera.gameObject.SetActive(false);
        InputBlocker.SetBlocked(false);
        playerMoveSystem.enabled = true;           // bật lại di chuyển
        interactionUIView.ShowWorldSpaceIcon();
        actionChoiceViewUI.Hide();
        playerStandUI.returnBtn.gameObject.SetActive(true);
        inputCanvas.Show();
        
        SignalBus.Send(new ChairCheckPointVisibilityCommand(true));
    }

    // ==================== TRIGGERS ====================
    
    private void ViewOnOnClickWorldSpaceEvent()
    {
        if (isFocused) return;
        
        float distance = Vector3.Distance(player.position, target.position);
        if (distance < allowTriggerDistance)
            EnterFocusState();
    }
    
    private void InteractionUIViewOnOnClickReturnBtn()
    {
        OnReturnPressed();
    }

    // Gọi cái này từ Return Button
    public void OnReturnPressed()
    {
        if (isFocused)
            ExitFocusState();
    }
}