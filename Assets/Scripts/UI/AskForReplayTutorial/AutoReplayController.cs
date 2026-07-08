using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoReplayController : MonoBehaviour
{
    [SerializeField] private TutorialHandler tutorialHandler;
    [SerializeField] private AskForReplayTutorialUI view;

    private void Awake()
    {
        if(tutorialHandler)
            tutorialHandler.OnCompleteTutorial += DelayShowUI;
        
        view.OnClickedAcceptEvent += ReplayTutorial;
        view.OnClickedDeclineEvent += ContinueLearn;
    }

    private void Start()
    {
        view.Hide();
    }

    private void OnDestroy()
    {
        if(tutorialHandler)
            tutorialHandler.OnCompleteTutorial -= DelayShowUI;
        
        view.OnClickedAcceptEvent -= ReplayTutorial;
        view.OnClickedDeclineEvent -= ContinueLearn;
    }

    private IEnumerator AskForReplayTutorial()
    {
        yield return new WaitForSeconds(2f);
        view.Show();
    }

    private void DelayShowUI()
    {
        StartCoroutine(AskForReplayTutorial());
    }
    

    private void ReplayTutorial()
    {
        view.Hide();
        LoadingTransition.Load_Scene(SceneManager.GetActiveScene().name);
        tutorialHandler.ResetKey();
        Debug.Log("[AutoReplayController] replay tutorial");
    }

    private void ContinueLearn()
    {
        Debug.Log("[AutoReplayController] Continue learning");
        view.Hide();
    }
}