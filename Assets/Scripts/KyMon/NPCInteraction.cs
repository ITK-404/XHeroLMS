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
    }
    
    private void Start()
    {
        playerCamera = PlayerCamera.Instance;
        pointClickSystem = FindFirstObjectByType<PointClickSystem>();
        playerCameraPriority = playerCamera.playerCinemachineCamera.Priority.Value + 1;
        SetupWorldSpaceUI();
    }

    private void ViewOnOnClickWorldSpaceEvent()
    {
        // alway have, because this room is alway learn
        if (PlayerChairManager.Instance.playerState == PlayerChairManager.PlayerState.Sitdown) return;
        if (pointClickSystem.IsBlendingCamera())
        {
            return;
        }
        
        float distance = Vector3.Distance(player.transform.position, target.transform.position);

        if (distance < allowTriggerDistance)
        {
            // interactionUIView.ShowSupportChatBox();
            // actionChoiceViewUI.Show();
            FocusCamera();
            Debug.Log("Cho phep hien thi UI");
        }
    }

    private void FocusCamera()
    {
        focusCamera.gameObject.SetActive(true);
    }
}