using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ClassTutorialFlow : FlowBase
{
    [SerializeField] private List<TutorialStepBehaviour> tutorialList = new();
    [SerializeField] private Image backgroundImg;
    [SerializeField] private RectTransform targetParent;

    protected override void Awake()
    {
        base.Awake();
        tutorialList = GetComponentsInChildren<TutorialStepBehaviour>().ToList();
        InitSetup();
        ShowBlockPanel(false);
    }

    private void InitSetup()
    {
        foreach (var item in GetComponentsInChildren<AutoParentUIElements>())
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
        await UniTask.WaitForSeconds(2f);
        ShowBlockPanel(true);
        RunFlow().Forget();
        await UniTask.WaitForSeconds(2f);
        await UniTask.WaitUntil(() => !IsRunning(),
            PlayerLoopTiming.Update,
            this.GetCancellationTokenOnDestroy());
        ShowBlockPanel(false);
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
        if (tutorialList == null || tutorialList.Count == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] Tutorial list is empty.");
            return null;
        }

        Debug.Log($"[{GetType().Name}] Create tutorial flow. Total Steps: {tutorialList.Count}");

        FlowNode startNode = tutorialList[0].CreateTutorialNode();
        Debug.Log($"Start Node: {startNode.Name}");

        FlowNode currentNode = startNode;

        for (int i = 1; i < tutorialList.Count; i++)
        {
            FlowNode nextNode = tutorialList[i].CreateTutorialNode();

            Debug.Log(
                $"Link [{i - 1}] {currentNode.Name} -> [{i}] {nextNode.Name}"
            );

            currentNode.AddTransition(NodeResult.Completed, nextNode);
            currentNode = nextNode;
        }

        Debug.Log($"Tutorial flow created successfully.");

        return startNode;
    }
}