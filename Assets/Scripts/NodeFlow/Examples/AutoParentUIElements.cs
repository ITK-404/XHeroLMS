using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(TutorialStepBehaviour))]
public class AutoParentUIElements : MonoBehaviour
{
    [SerializeField] private List<RectTransform> elements = new();
    [SerializeField] private Transform newParent;

    private TutorialStepBehaviour behaviour;

    private List<Transform> oldParents = new();
    private List<int> oldSiblingIndexes = new();

    private bool isInitialized;

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
            return;
        }

        InitOriginalHierarchy();
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
        SetToNewParent();
    }

    private void OnExitState()
    {
        SetToOldParent();
    }

    private void InitOriginalHierarchy()
    {
        oldParents.Clear();
        oldSiblingIndexes.Clear();

        foreach (RectTransform element in elements)
        {
            if (element == null)
            {
                oldParents.Add(null);
                oldSiblingIndexes.Add(-1);
                continue;
            }

            oldParents.Add(element.parent);
            oldSiblingIndexes.Add(element.GetSiblingIndex());
        }

        isInitialized = true;
    }

    private void SetToNewParent()
    {
        if (!isInitialized)
        {
            InitOriginalHierarchy();
        }

        if (newParent == null)
        {
            Debug.LogError(
                $"[{GetType().Name}] newParent is null!",
                this
            );

            return;
        }

        foreach (RectTransform element in elements)
        {
            if (element == null)
            {
                continue;
            }

            element.SetParent(newParent, true);
        }
    }

    public void SetToOldParent()
    {
        if (!isInitialized)
        {
            Debug.LogWarning(
                $"[{GetType().Name}] Original hierarchy has not been initialized.",
                this
            );

            return;
        }

        int count = Mathf.Min(
            elements.Count,
            oldParents.Count,
            oldSiblingIndexes.Count
        );

        for (int i = 0; i < count; i++)
        {
            RectTransform element = elements[i];
            Transform oldParent = oldParents[i];

            if (element == null)
            {
                continue;
            }

            element.SetParent(oldParent, true);

            int siblingIndex = oldSiblingIndexes[i];

            if (oldParent != null && siblingIndex >= 0)
            {
                int maxIndex = oldParent.childCount - 1;
                element.SetSiblingIndex(Mathf.Min(siblingIndex, maxIndex));
            }
        }
    }

    public void SetParent(Transform parent)
    {
        newParent = parent;
    }
    
    public void AddElement(RectTransform element)
    {
        if (element == null || elements.Contains(element))
        {
            return;
        }

        elements.Add(element);
        oldParents.Add(element.parent);
        oldSiblingIndexes.Add(element.GetSiblingIndex());

        // Chuyển UI mới vào parent tutorial ngay.
        if (newParent != null)
        {
            element.SetParent(newParent, true);
        }
    }
}