using UnityEngine;
using UnityEngine.UI;

public class WaitForClickButtonTutorialStep : TutorialStepBehaviour
{
    [SerializeField] private Button targetButton;
    private bool isClick = false;

    public override void Enter(CutsceneContext context = null)
    {
        base.Enter(context);
        targetButton.onClick.AddListener(OnClicked);
    }

    public override void Exit(CutsceneContext context = null)
    {
        base.Exit(context);
        targetButton.onClick.RemoveListener(OnClicked);
    }

    private void OnClicked()
    {
        isClick = true;
    }

    public override bool IsCompleted()
    {
        return isClick;
    }
}