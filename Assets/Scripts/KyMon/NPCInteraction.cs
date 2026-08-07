using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera focusCamera;
    [Header("Setting")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform target;
    [SerializeField] private float allowTriggerDistance = 5f;
    [Header("UI")]
    [SerializeField] private NPCInteractionUIView interactionUIView;
    [SerializeField] private ActionChoiceViewUI actionChoiceViewUI;
    [SerializeField] private KyMon_WorldSpaceUI worldSpaceUi;
    [SerializeField] private EnterFocusStateTransition focusStateTransition;
    private PlayerCamera playerCamera;

    private int playerCameraPriority = 1;
    
    private void Awake()
    {
        focusCamera.gameObject.SetActive(false);
        interactionUIView.OnClickWorldSpaceEvent += OnClickWorldSpaceButtonHandle;
        actionChoiceViewUI.OnClickReturnBtn += OnReturnClickedHandle;
    }

    
    private void OnDestroy()
    {
        interactionUIView.OnClickWorldSpaceEvent -= OnClickWorldSpaceButtonHandle;
        actionChoiceViewUI.OnClickReturnBtn -= OnReturnClickedHandle;
    }

    private void Start()
    {
        interactionUIView.Show();
        interactionUIView.ShowWorldSpaceIcon();
        actionChoiceViewUI.Hide();
        
        playerCamera = PlayerCamera.Instance;
        
        focusCamera.Priority.Value = playerCamera.playerCinemachineCamera.Priority.Value;
        
        worldSpaceUi.SetPlayer(player.transform);
        worldSpaceUi.SetTarget(target);
        worldSpaceUi.SetCamera(playerCamera.mainCamera);
        
        actionChoiceViewUI.Hide();
        interactionUIView.ShowWorldSpaceIcon();
        
        canActiveWorldSpaceUI = true;
        previousRuntimeWorldSpaceEnable = !canActiveWorldSpaceUI;
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
        HandleFocusState(true);
        StartCoroutine(WaitForBlending(() =>
        {
            interactionUIView.ShowSupportChatBox();
            actionChoiceViewUI.Show();
        }));
        // playerChairManager.ShowAllCheckPoints(false);
    }

    private void ExitFocusState()
    {
        isFocused = false;
        focusCamera.gameObject.SetActive(false);
        
        // InputBlocker.SetBlocked(false);
        interactionUIView.Show();
        interactionUIView.ShowWorldSpaceIcon();
        actionChoiceViewUI.Hide();

        HandleFocusState(false);
    }

    private void HandleFocusState(bool isEnter)
    {
        if (isEnter)
        {
            focusStateTransition.Enter();
            focusStateTransition.HideStandButtons();
        }
        else
        {
            focusStateTransition.Exit();
            focusStateTransition.ShowStandButtons();
        }
    }

    // ==================== TRIGGERS ====================
    
    private void OnClickWorldSpaceButtonHandle()
    {
        if (isFocused) return;
        
        float distance = Vector3.Distance(player.position, target.position);
        if (distance < allowTriggerDistance)
            EnterFocusState();
    }
    
    private void OnReturnClickedHandle() => OnReturnPressed();

    // Gọi cái này từ Return Button
    public void OnReturnPressed()
    {
        if (isFocused)
            ExitFocusState();
    }

    private bool previousRuntimeWorldSpaceEnable = true;
    private void LateUpdate()
    {
        WorldSpaceUIRuntimeStateCheck();
    }

    private void WorldSpaceUIRuntimeStateCheck()
    {
        // top priority
        var isFacingIcon = worldSpaceUi.GetIsPlayerFacingIcon();
        if (isFacingIcon == false)
        {
            previousRuntimeWorldSpaceEnable = false;
            worldSpaceUi.SetActive(false);
            return;
        }
        if (previousRuntimeWorldSpaceEnable != canActiveWorldSpaceUI )
        {
            worldSpaceUi.SetActive(canActiveWorldSpaceUI);
            previousRuntimeWorldSpaceEnable = canActiveWorldSpaceUI;
        }
    }

    private bool canActiveWorldSpaceUI;

    public void SetActiveState(bool state)
    {
        canActiveWorldSpaceUI = state;
    }
}