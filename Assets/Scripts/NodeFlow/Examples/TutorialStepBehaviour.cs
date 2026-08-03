using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class TutorialStepBehaviour : MonoBehaviour
{
    public event Action OnEnterStateEvent;
    public event Action OnExitStateEvent;

    public virtual void Enter(CutsceneContext context = null)
    {
        OnEnterStateEvent?.Invoke();
    }

    public virtual void Tick(CutsceneContext context = null)
    {
    }

    public abstract bool IsCompleted();

    public virtual void Exit(CutsceneContext context = null)
    {
        OnExitStateEvent?.Invoke();
    }

    public TutorialStepNode CreateTutorialNode()
    {
        return new TutorialStepNode(this);
    }
}