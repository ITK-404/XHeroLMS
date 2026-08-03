using UnityEngine;

public class WaitForPlayerStateChange : TutorialStepBehaviour
{
    [SerializeField] private PlayerChairManager.PlayerState targetState;

    public override bool IsCompleted()
    {
        return PlayerChairManager.Instance.playerState == targetState;
    }
}

