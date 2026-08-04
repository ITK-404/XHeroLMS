using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TutorialStepBehaviour))]
public class AutoParentUIElements : MonoBehaviour
{
    [SerializeField] private List<RectTransform> elements = new();

    private TutorialStepBehaviour behaviour;

    private void Awake()
    {
        behaviour = GetComponent<TutorialStepBehaviour>();

        if (behaviour == null)
        {
            Debug.LogError(
                $"[{GetType().Name}] TutorialStepBehaviour is missing!",
                this
            );

            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (behaviour == null)
        {
            behaviour = GetComponent<TutorialStepBehaviour>();
        }

        if (behaviour == null)
        {
            return;
        }

        behaviour.OnEnterStateEvent += OnEnterState;
        behaviour.OnExitStateEvent += OnExitState;
    }

    private void OnDisable()
    {
        if (behaviour == null)
        {
            return;
        }

        behaviour.OnEnterStateEvent -= OnEnterState;
        behaviour.OnExitStateEvent -= OnExitState;
    }

    private void OnEnterState()
    {
        // SetToNewParent();
        UpdateMasking();
    }

    private void UpdateMasking()
    {
        if (elements.Count > 0)
        {
            ClassTutorialFlow.Instance.SetInteractZone(elements[0]);
        }
    }
            
    private void ClearMasking() =>ClassTutorialFlow.Instance.ClearZone();
    
    private void OnExitState()
    {
        // SetToOldParent();
        ClearMasking();
    }
    
    public void AddElement(RectTransform element)
    {
        if (element == null || elements.Contains(element))
        {
            return;
        }
        elements.Add(element);
    }

    public void RemoveElement(RectTransform element)
    {
        if (element == null || !elements.Contains(element))
        {
            return;
        }
        elements.Remove(element);
    }
}