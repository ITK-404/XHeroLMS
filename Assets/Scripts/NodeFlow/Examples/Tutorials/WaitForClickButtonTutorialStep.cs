using UnityEngine;
using UnityEngine.UI;

public class WaitForClickButtonTutorialStep : TutorialStepBehaviour
{
    [SerializeField] private Button targetButton;
    private bool isClick = false;

    public override void Enter()
    {
        base.Enter();
        targetButton.onClick.AddListener(OnClicked);
    }

    public override void Exit()
    {
        base.Exit();
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