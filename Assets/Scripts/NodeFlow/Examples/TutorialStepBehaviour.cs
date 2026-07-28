using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class TutorialStepBehaviour : MonoBehaviour
{
    [SerializeField] protected float delay = 2f;
    public event Action OnEnterStateEvent;
    public event Action OnExitStateEvent;

    public virtual void Enter()
    {
        OnEnterStateEvent?.Invoke();
    }

    public virtual void Tick()
    {
    }

    public abstract bool IsCompleted();

    public virtual void Exit()
    {
        OnExitStateEvent?.Invoke();
    }

    public TutorialStepNode CreateTutorialNode()
    {
        return new TutorialStepNode(this, delay);
    }
}