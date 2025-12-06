using UnityEngine;
using UnityEngine.UI;

public class TutorialButtonHandle : MonoBehaviour
{
    public TutorialStepType previousStep;
    public TutorialStepType nextStep;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnDoneTutorial);
        }
    }
    private void OnDestroy()
    {
        if (btn != null)
        {
            btn.onClick.RemoveListener(OnDoneTutorial);
        }
    }
    private void OnDoneTutorial()
    {
        if (TutorialHandler.Instance.CurrentStep == previousStep)
        {
            TutorialHandler.Instance.SetCurrentStep(nextStep);
        }
    }
}