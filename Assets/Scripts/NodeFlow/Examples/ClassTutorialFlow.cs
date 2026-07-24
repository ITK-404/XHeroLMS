using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ClassTutorialFlow : FlowBase
{
    [Header("References")] 
    [SerializeField] private TutorialFlowBuilder nextBuilder;

    [SerializeField] private TutorialFlowBuilder builder;
    [Header("UI")] [SerializeField] private Image backgroundImg;
    [SerializeField] private RectTransform targetParent;
    [SerializeField] private AskForReplayTutorialUI askForReplayTutorialUI;
    [SerializeField] private AutoReplayController autoReplayController;

    protected override void Awake()
    {
        base.Awake();
        ShowBlockPanel(false);
    }

    private void InitSetup(GameObject builder)
    {
        foreach (var item in builder.GetComponentsInChildren<AutoParentUIElements>())
        {
            item.SetParent(targetParent);
        }
    }

    private void Start()
    {
        HandleFlow().Forget();
    }

    private async UniTask HandleFlow()
    {
        GameplayLock.Lock(GameplayLockReason.Animation, GameplayLockTarget.Movement);
        await UniTask.WaitForSeconds(2f);

        ShowBlockPanel(true);
        RunFlow().Forget();

        await UniTask.WaitForSeconds(2f);
        await UniTask.WaitUntil(() => !IsRunning(),
            PlayerLoopTiming.Update,
            this.GetCancellationTokenOnDestroy());

        ShowBlockPanel(false);
        
        // check next tutorial if exit 
        if (nextBuilder != null)
        {
            builder = nextBuilder;
            return;
        }
        ReplayTutorial().Forget();
    }

    private async UniTask ReplayTutorial()
    {
        var result = await askForReplayTutorialUI.ShowAsync();

        if (result)
        {
            StartCoroutine(autoReplayController.WaitForLoading());
        }
        else
        {
            askForReplayTutorialUI.Hide();
        }
    }

    private void ShowBlockPanel(bool isShow)
    {
        backgroundImg.DOKill();
        if (isShow)
        {
            backgroundImg.DOFade(0, 0);
            backgroundImg.gameObject.SetActive(true);
            backgroundImg.DOFade(0.95f, 0.3f);
        }
        else
        {
            backgroundImg.DOFade(0.95f, 0);
            backgroundImg.gameObject.SetActive(true);
            backgroundImg.DOFade(1, 0.3f).OnComplete(() => { backgroundImg.gameObject.SetActive(false); });
        }
    }

    protected override FlowNode CreateFlow()
    {
        InitSetup(builder.gameObject);
        var initializeNode = builder.BuildFlowNode();
        return initializeNode;
    }
    
}