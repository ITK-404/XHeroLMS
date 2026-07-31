using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class WaitForUnityEventCallback : TutorialStepBehaviour
{
    [SerializeField] private UnityEvent OnEnterStep;
    [SerializeField] private UnityEvent OnExitStep;

    private bool isCompleted = false;
    
    public override void Enter(CutsceneContext context = null)
    {
        base.Enter(context);
        OnEnterStep?.Invoke();

        StartCoroutine(WaitForDelay());
    }

    public override void Exit(CutsceneContext context = null)
    {
        base.Exit(context);
        OnExitStep?.Invoke();
    }

    private IEnumerator WaitForDelay()
    {
        isCompleted = false;
        yield return new WaitForSecondsRealtime(0.5f);
        isCompleted = true;
    }

    public override bool IsCompleted()
    {
        return isCompleted;
    }
}