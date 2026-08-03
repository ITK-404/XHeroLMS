using UnityEngine;

[RequireComponent(typeof(TutorialStepBehaviour))]
public class FocusHandTutorial : MonoBehaviour
{
    private TutorialStepBehaviour behaviour;
    
    [SerializeField] private RectTransform targetAnchor;
    [SerializeField] private string focusHandDescription = "Nhấn vào đây";
    
    private void Awake()
    {
        behaviour = GetComponent<TutorialStepBehaviour>();
        
        behaviour.OnEnterStateEvent += OnEnterState;
        behaviour.OnExitStateEvent += OnExitState;
    }

    private void OnDestroy()
    {
        behaviour.OnEnterStateEvent -= OnEnterState;
        behaviour.OnExitStateEvent -= OnExitState;
    }

    private void OnExitState()
    {
        FocusHandManager.Instance.Hide();
    }

    private void OnEnterState()
    {
        if (targetAnchor == null)
        {
            Debug.LogError("This rect transform is null");
            return;
        }
        FocusHandManager.Instance.SetToTargetRect(targetAnchor,focusHandDescription);
    }
}