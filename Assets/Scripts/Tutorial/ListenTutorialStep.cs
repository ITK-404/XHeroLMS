using UnityEngine;
using UnityEngine.Events;

public abstract class ListenTutorialStep : MonoBehaviour
{
    [SerializeField] private TutorialStepType listenStep;
    [SerializeField] private UnityEvent onSameStep;
    [SerializeField] private UnityEvent onDifferentStep;
    private void Start()
    {
        TutorialHandler.Instance.ChangedToStepEvent += TutorialChangedStep;
    }

    private void OnDestroy()
    {
        TutorialHandler.Instance.ChangedToStepEvent -= TutorialChangedStep;
    }

    private void TutorialChangedStep(TutorialStepType step)
    {
        if (listenStep == step)
        {
            onSameStep?.Invoke();
        }
        else
        {
            onDifferentStep?.Invoke();
        }
    }
    
    protected virtual void OnSameStep(){}
    protected virtual void OnDifferentStep(){}
}