using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class AutoReplayController : MonoBehaviour
{
    [SerializeField] private AskForReplayTutorialUI view;
    [FormerlySerializedAs("askForReplayAnimation")] [SerializeField] private SmokeTransitionAnimation smokeTransitionAnimation;

    private bool isShowBefore = false;    
    private void Awake()
    {
        view.OnClickedAcceptEvent += ReplayTutorial;
        view.OnClickedDeclineEvent += ContinueLearn;
    }

    private void Start()
    {
        view.Hide();
    }

    private void OnDestroy()
    {
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
        if (isShowBefore) return;
        isShowBefore = true;
        StartCoroutine(AskForReplayTutorial());
    }
    

    private void ReplayTutorial()
    {
        // view.Hide();
        view.SetInteractable(false);
        StartCoroutine(WaitForLoading());
        Debug.Log("[AutoReplayController] replay tutorial");
    }

    private IEnumerator WaitForLoading()
    {
        yield return smokeTransitionAnimation.StartTransitionAsync().ToCoroutine();
        LoadingTransition.Load_Scene(SceneManager.GetActiveScene().name);
    }
    

    private void ContinueLearn()
    {
        Debug.Log("[AutoReplayController] Continue learning");
        view.Hide();
    }
}