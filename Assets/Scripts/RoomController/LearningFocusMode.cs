using UnityEngine;

public class LearningFocusMode : FocusMode
{
    [SerializeField] private PlayerStandUI playerStandUI;
    [SerializeField] private VideoPlayerControllerPro videoPlayerControllerPro;
    private void Awake()
    {
        ResolveRuntimeReferences();
    }

    public override void Enter()
    {
        base.Enter();
        ResolveRuntimeReferences();
        playerStandUI?.ShowLearningUI();
        videoPlayerControllerPro?.EnterFullscreenUI();
    }

    public override void Exit()
    {
        base.Exit();
        ResolveRuntimeReferences();
        playerStandUI?.HideLearningUI();
        videoPlayerControllerPro?.ExitFullscreenUI();
    }

    private void ResolveRuntimeReferences()
    {
        if (playerStandUI == null)
            playerStandUI = FindFirstObjectByType<PlayerStandUI>(FindObjectsInactive.Include);

        if (videoPlayerControllerPro == null)
            videoPlayerControllerPro = FindFirstObjectByType<VideoPlayerControllerPro>(FindObjectsInactive.Include);
    }
}
