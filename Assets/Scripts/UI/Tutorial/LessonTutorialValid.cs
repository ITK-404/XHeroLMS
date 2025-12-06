using UnityEngine;

public class LessonTutorialValid : MonoBehaviour
{
    private ToggleBaseUI toggleBaseUI;

    private void Awake()
    {
        toggleBaseUI = GetComponent<ToggleBaseUI>();
        toggleBaseUI.OnValueChange += OnToggleValueChanged;
    }

    private void OnDestroy()
    {
        toggleBaseUI.OnValueChange -= OnToggleValueChanged;
    }

    private void OnToggleValueChanged(ToggleBaseUI.State state)
    {
        bool isOn = state == ToggleBaseUI.State.Active;
        // mặc định là đang tắt
        if (isOn)
        {
            if (TutorialHandler.Instance.CurrentStep == TutorialStepType.OpenLesson)
            {
                TutorialHandler.Instance.SetCurrentStep(TutorialStepType.CloseLesson);
            }
        }
        else
        {
            if (TutorialHandler.Instance.CurrentStep == TutorialStepType.CloseLesson)
            {
                TutorialHandler.Instance.SetCurrentStep(TutorialStepType.PauseVideo);
            }
        }
    }
}
