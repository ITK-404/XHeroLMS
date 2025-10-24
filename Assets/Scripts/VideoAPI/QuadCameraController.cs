using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class QuadCinemachineController : MonoBehaviour
{
    private enum ViewState { Player, Def, Ex }

    [Header("Scene Refs")]
    public GameObject quad;
    public Camera mainRenderCamera;
    public Camera targetCamera;
    public CinemachineCamera playerVCam;
    public Button btnDef;
    public Button btnEx;
    public Button btnFull;

    [Header("UI (tuỳ chọn)")]
    public Canvas[] worldSpaceCanvases;

    [Header("Priority")]
    public int playerPriority = 10;
    public int targetPriority = 20;             // > playerPriority để thắng

    private Vector3 originalQuadPos;
    private CinemachineCamera targetVCam;       // VCam bám theo targetCamera.transform
    private int originalPlayerPriority;
    private bool targetWasEnabled;
    private float targetOriginalDepth;
    private ViewState state = ViewState.Player;
    private CinemachineBrain brain;

    VideoPlayerControllerPro videoPlayerController;
    LearnUI learnUI;

    void Awake()
    {
        learnUI = FindAnyObjectByType<LearnUI>();
        videoPlayerController = FindAnyObjectByType<VideoPlayerControllerPro>();

        if (!mainRenderCamera) mainRenderCamera = Camera.main;
        if (quad) originalQuadPos = quad.transform.position;
    }

    void Start()
    {
        EnsureBrainOnMain();

        // targetCamera chỉ làm anchor -> tắt render
        if (targetCamera)
        {
            targetWasEnabled     = targetCamera.enabled;
            targetOriginalDepth  = targetCamera.depth;
            targetCamera.enabled = false;
        }

        if (playerVCam)
        {
            originalPlayerPriority = playerVCam.Priority;
            playerVCam.Priority    = playerPriority;
        }

        CreateOrUpdateTargetVCam();

        // World/ScreenSpace-Camera canvases raycast qua Main Camera
        if (worldSpaceCanvases != null && mainRenderCamera != null)
        {
            foreach (var cvs in worldSpaceCanvases)
            {
                if (!cvs) continue;
                if (cvs.renderMode == RenderMode.WorldSpace || cvs.renderMode == RenderMode.ScreenSpaceCamera)
                    cvs.worldCamera = mainRenderCamera;
            }
        }

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        if (btnDef)  btnDef.onClick.AddListener(OnDefClicked);
        if (btnEx)   btnEx.onClick.AddListener(OnExClicked);
        if (btnFull) btnFull.onClick.AddListener(OnFullClicked);

        state = ViewState.Player;
    }

    void EnsureBrainOnMain()
    {
        if (!mainRenderCamera) return;
        brain = mainRenderCamera.GetComponent<CinemachineBrain>();
        if (!brain) brain = mainRenderCamera.gameObject.AddComponent<CinemachineBrain>();
    }

    void CreateOrUpdateTargetVCam()
    {
        if (!targetCamera) return;

        if (!targetVCam)
        {
            var go = new GameObject("~VCam_TargetAnchor");
            targetVCam = go.AddComponent<CinemachineCamera>();
            targetVCam.Priority = playerPriority - 1; // mặc định dưới player
        }

        // copy pose/lens từ targetCamera
        var t = targetCamera.transform;
        targetVCam.transform.SetPositionAndRotation(t.position, t.rotation);

        var lens = targetVCam.Lens;
        lens.FieldOfView = targetCamera.fieldOfView;
        if (targetCamera.orthographic)
            lens.OrthographicSize = targetCamera.orthographicSize;
        targetVCam.Lens = lens;
    }

    // ===== Buttons =====
    public void OnDefClicked()
    {
        switch (state)
        {
            case ViewState.Player:
                GoTargetDef();
                break;

            case ViewState.Def:
                SetQuadZ(-1.2f);
                state = ViewState.Ex;
                break;

            case ViewState.Ex:
                SetQuadZ(originalQuadPos.z);
                state = ViewState.Def;
                break;
        }
        learnUI?.Show();
        UpdateLearnUI();
        videoPlayerController.DefEx();
        // videoPlayerController.ExitFullscreenUI();
        ClearUISelection();
    }

    public void OnExClicked()
    {
        switch (state)
        {
            case ViewState.Player:
                GoTargetEx();
                break;

            case ViewState.Def:
                SetQuadZ(-1.2f);
                state = ViewState.Ex;
                break;

            case ViewState.Ex:
                SetQuadZ(-1.2f);
                break;
        }
        learnUI?.Hide();
        UpdateLearnUI();
        videoPlayerController.DefEx();
        // videoPlayerController.ExitFullscreenUI();
        ClearUISelection();
    }

    void OnFullClicked()
    {
        GoPlayer();
        ClearUISelection();
    }

    // ===== Actions =====
    void GoTargetDef()
    {
        CreateOrUpdateTargetVCam();
        SetQuadZ(originalQuadPos.z);
        MakeTargetLive();
        state = ViewState.Def;
    }

    void GoTargetEx()
    {
        CreateOrUpdateTargetVCam();
        SetQuadZ(-1.2f);
        MakeTargetLive();
        state = ViewState.Ex;
    }

    void GoPlayer()
    {
        if (playerVCam)  playerVCam.Priority  = Mathf.Max(playerPriority, targetPriority + 1);
        if (targetVCam)  targetVCam.Priority  = playerPriority - 1;
        state = ViewState.Player;
    }

    void MakeTargetLive()
    {
        if (!playerVCam || !targetVCam) return;
        playerVCam.Priority = playerPriority;
        targetVCam.Priority = targetPriority;   // Brain blend nhẹ (hoặc instant tuỳ Default Blend)
    }

    void SetQuadZ(float z)
    {
        if (!quad) return;
        var p = quad.transform.position;
        p.z = z;
        quad.transform.position = p;
    }

    void ClearUISelection()
    {
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(null);
    }
void UpdateLearnUI()
{
    if (!learnUI) return;
    if (state == ViewState.Ex) learnUI.Hide();
    else                       learnUI.Show();  // Player & Def đều hiện
}

}
