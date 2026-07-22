using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class TutorialStepBehaviour : MonoBehaviour
{
    [SerializeField] protected float delay = 2f;
    public virtual void Enter()
    {
    }

    public virtual void Tick()
    {
    }

    public abstract bool IsCompleted();

    public virtual void Exit()
    {
    }

    public TutorialStepNode CreateTutorialNode()
    {
        return new TutorialStepNode(this, delay);
    }
}