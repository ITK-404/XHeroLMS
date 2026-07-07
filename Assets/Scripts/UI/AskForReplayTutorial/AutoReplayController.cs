using System;
using UnityEngine;

public class AutoReplayController : MonoBehaviour
{
    [SerializeField] private TutorialHandler tutorialHandler;
    [SerializeField] private AskForReplayTutorialUI view;

    private void Awake()
    {
        if(tutorialHandler)
            tutorialHandler.OnCompleteTutorial += AskForReplayTutorial;
        
        view.OnClickedAcceptEvent += ReplayTutorial;
        view.OnViewOpened += ContinueLearn;
    }

    private void Start()
    {
        view.Hide();
    }

    private void OnDestroy()
    {
        if(tutorialHandler)
            tutorialHandler.OnCompleteTutorial -= AskForReplayTutorial;
        
        view.OnClickedAcceptEvent -= ReplayTutorial;
        view.OnViewOpened -= ContinueLearn;
    }

    private void AskForReplayTutorial()
    {
        view.Show();
    }

    private void ReplayTutorial()
    {
        view.Hide();
    }

    private void ContinueLearn()
    {
        view.Hide();
    }
}