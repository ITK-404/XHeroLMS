using UnityEngine;
using UnityEngine.Events;

public abstract class ListenTutorialStep : MonoBehaviour
{
    [SerializeField] private TutorialStepType listenStep;
    [SerializeField] private UnityEvent onSameStep;
    [SerializeField] private UnityEvent onDifferentStep;
    private void Start()
    {
        TutorialHandler.ChangedToStepEvent += TutorialChangedStep;
    }

    private void OnDestroy()
    {
        TutorialHandler.ChangedToStepEvent -= TutorialChangedStep;
    }

    private void TutorialChangedStep(TutorialStepType step)
    {
        Debug.Log($"HighlightTutorialStep change step");
        if (listenStep == step)
        {
            onSameStep?.Invoke();
            OnSameStep();
        }
        else
        {
            onDifferentStep?.Invoke();
            OnDifferentStep();
        }
    }
    
    protected virtual void OnSameStep(){}
    protected virtual void OnDifferentStep(){}
}