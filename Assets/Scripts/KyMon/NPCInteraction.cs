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
    [SerializeField] private CourseExitWayHandler courseExitWayHandler;

    [SerializeField] private PlayerChairManager playerChairManager;
    // player
    [SerializeField] private PlayerStandUI playerStandUI;
    
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

    private void Start()
    {
        interactionUIView.Show();
        interactionUIView.ShowWorldSpaceIcon();
        actionChoiceViewUI.Hide();
        
        playerCamera = PlayerCamera.Instance;
        playerStandUI = FindFirstObjectByType<PlayerStandUI>(FindObjectsInactive.Include);
        
        focusCamera.Priority.Value = playerCamera.playerCinemachineCamera.Priority.Value;
        
        worldSpaceUi.SetPlayer(player.transform);
        worldSpaceUi.SetTarget(target);
        worldSpaceUi.SetCamera(playerCamera.mainCamera);
        
        actionChoiceViewUI.Hide();
        interactionUIView.ShowWorldSpaceIcon();
    }
    private bool isFocused = false;

    private IEnumerator WaitForBlending(Action onComplete)
    {
        yield return new WaitForSeconds(2f);
        onComplete?.Invoke();
    }

    
    private void EnterFocusState()
    {
        isFocused = true;
        focusCamera.gameObject.SetActive(true);
        InputBlocker.SetBlocked(true);
        // playerMoveSystem.enabled = false;          // tắt di chuyển
        // playerStandUI.returnBtn.gameObject.SetActive(false);
        // courseExitWayHandler.Hide();

        playerChairManager.OnSitdownUI_Immediate();
        playerStandUI.HideButtons();
        
        StartCoroutine(WaitForBlending(() =>
        {
            interactionUIView.ShowSupportChatBox();
            actionChoiceViewUI.Show();
        }));
        playerChairManager.ShowAllCheckPoints(false);
    }

    private void ExitFocusState()
    {
        isFocused = false;
        focusCamera.gameObject.SetActive(false);
        
        InputBlocker.SetBlocked(false);
        interactionUIView.Show();
        interactionUIView.ShowWorldSpaceIcon();
        actionChoiceViewUI.Hide();
     
        playerChairManager.OnStandupUI_Deferred();
        playerChairManager.ShowAllCheckPoints(true);
        playerStandUI.ShowSitdownButton();
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