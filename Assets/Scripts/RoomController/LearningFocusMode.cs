using UnityEngine;

public class LearningFocusMode : FocusMode
{
    [SerializeField] private PlayerStandUI playerStandUI;
    [SerializeField] private VideoPlayerControllerPro videoPlayerControllerPro;
    public override void Enter()
    {
        base.Enter();
        playerStandUI.ShowLearningUI();
        videoPlayerControllerPro.EnterFullscreenUI();
    }

    public override void Exit()
    {
        base.Exit();
        playerStandUI.HideLearningUI();
        videoPlayerControllerPro.ExitFullscreenUI();
    }
}