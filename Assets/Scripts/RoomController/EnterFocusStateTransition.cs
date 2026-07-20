using UnityEngine;

public class EnterFocusStateTransition : FocusMode
{
    [SerializeField] private PlayerPanelUI playerPanelUI;
    [SerializeField] private InputCanvas inputCanvas;
    [SerializeField] private CourseExitWayHandler courseExitWayHandler;
    [SerializeField] private PlayerStandUI playerStandUI;
    private void Awake()
    {
        ResolveRuntimeReferences();
    }

    public override void Enter()
    {
        base.Enter();
        ResolveRuntimeReferences();
        SetPlayerPanelState(false);
        inputCanvas?.Hide();
        courseExitWayHandler?.Hide();
        if (playerStandUI != null && playerStandUI.returnBtn != null)
            playerStandUI.returnBtn.gameObject.SetActive(false);
    }

    public override void Exit()
    {
        base.Exit();
        ResolveRuntimeReferences();
        SetPlayerPanelState(true);
        inputCanvas?.Show();
        courseExitWayHandler?.Show();
        if (playerStandUI != null && playerStandUI.returnBtn != null)
            playerStandUI.returnBtn.gameObject.SetActive(true);
    }

    private void SetPlayerPanelState(bool isEnable)
    {
        if (playerPanelUI == null)
            return;

        playerPanelUI.ShowUnLoginContainer(isEnable);
        playerPanelUI.ShowExternalButton(isEnable);
    }

    public void HideStandButtons() => playerStandUI?.HideButtons();
    public void ShowStandButtons() => playerStandUI?.ShowSitdownButton();

    private void ResolveRuntimeReferences()
    {
        if (playerPanelUI == null)
            playerPanelUI = PlayerPanelUI.Instance != null
                ? PlayerPanelUI.Instance
                : FindFirstObjectByType<PlayerPanelUI>(FindObjectsInactive.Include);

        if (inputCanvas == null)
            inputCanvas = FindFirstObjectByType<InputCanvas>(FindObjectsInactive.Include);

        if (courseExitWayHandler == null)
            courseExitWayHandler = FindFirstObjectByType<CourseExitWayHandler>(FindObjectsInactive.Include);

        if (playerStandUI == null)
            playerStandUI = FindFirstObjectByType<PlayerStandUI>(FindObjectsInactive.Include);
    }
}
