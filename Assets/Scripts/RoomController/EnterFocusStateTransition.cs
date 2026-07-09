using UnityEngine;

public class EnterFocusStateTransition : FocusMode
{
    [SerializeField] private PlayerPanelUI playerPanelUI;
    [SerializeField] private InputCanvas inputCanvas;
    [SerializeField] private CourseExitWayHandler courseExitWayHandler;
    [SerializeField] private PlayerStandUI playerStandUI;
    private void Awake()
    {
        playerPanelUI = PlayerPanelUI.Instance;
    }

    public override void Enter()
    {
        base.Enter();
        SetPlayerPanelState(false);
        inputCanvas.Hide();
        courseExitWayHandler.Hide();
        playerStandUI.returnBtn.gameObject.SetActive(false);
    }

    public override void Exit()
    {
        base.Exit();
        SetPlayerPanelState(true);
        inputCanvas.Show();
        courseExitWayHandler.Show();
        playerStandUI.returnBtn.gameObject.SetActive(true);
    }

    private void SetPlayerPanelState(bool isEnable)
    {
        playerPanelUI.ShowUnLoginContainer(isEnable);
        playerPanelUI.ShowExternalButton(isEnable);
    }
}