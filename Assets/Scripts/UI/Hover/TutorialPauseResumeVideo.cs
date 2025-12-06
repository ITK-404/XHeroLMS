using UnityEngine;
using UnityEngine.UI;

public class TutorialPauseResumeVideo : MonoBehaviour
{
    public VideoPlayerControllerPro videoPlayer;
    private Button btn;
    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            if (TutorialHandler.Instance.CurrentStep == TutorialStepType.CloseLesson)
            {
                TutorialHandler.Instance.SetCurrentStep(TutorialStepType.PauseVideo);
                GetComponent<VideoPlayPauseButtonHover>()?.HandleSprite(false);
            }
            else if (TutorialHandler.Instance.CurrentStep == TutorialStepType.PauseVideo)
            {
                TutorialHandler.Instance.SetCurrentStep(TutorialStepType.ResumeVideo);
                GetComponent<VideoPlayPauseButtonHover>()?.HandleSprite(true);
            }
            else if (TutorialHandler.Instance.CurrentStep == TutorialStepType.ResumeVideo)
            {
                TutorialHandler.Instance.SetCurrentStep(TutorialStepType.Skip);
            }
        });
    }

}