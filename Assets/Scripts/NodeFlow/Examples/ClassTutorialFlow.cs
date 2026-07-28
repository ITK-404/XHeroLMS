using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ClassTutorialFlow : FlowBase
{
    [Header("References")]
    [SerializeField] private TutorialFlowBuilder nextBuilder; // default next, dùng khi không có logic context riêng

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

    private void InitSetup(GameObject builderGO)
    {
        foreach (var item in builderGO.GetComponentsInChildren<AutoParentUIElements>())
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
        // GameplayLock.Lock(GameplayLockReason.Animation, GameplayLockTarget.Movement);
        ShowBlockPanel(true);
        await UniTask.WaitForSeconds(2f);
        RunFlow().Forget();

        await UniTask.WaitForSeconds(2f);
        await UniTask.WaitUntil(() => !IsRunning(),
            PlayerLoopTiming.Update,
            this.GetCancellationTokenOnDestroy());

        ShowBlockPanel(false);

        // Flow hiện tại đã chạy xong tại đây.
        var next = ResolveNextBuilder();
        if (next != null)
        {
            builder = next;
            HandleFlow().Forget(); // chạy tiếp tutorial kế tiếp
            return;
        }

        ReplayTutorial().Forget();
        // Không có next tutorial -> dừng, không tự động chạy replay.
        // ReplayTutorial() chỉ nên được gọi từ nơi khác (vd: nút Replay ngoài UI), nếu cần.
    }

    /// <summary>
    /// Quyết định tutorial tiếp theo. Hardcode logic theo context ngay tại đây.
    /// Trả về null nghĩa là không có next -> HandleFlow dừng luôn, không tự chạy gì thêm.
    /// </summary>
    private TutorialFlowBuilder ResolveNextBuilder()
    {
        return nextBuilder;
    }

    private bool SomeContextCondition()
    {
        // TODO: thay bằng check thật, ví dụ:
        // return QuestManager.Instance.IsCompleted("QuestA");
        return false;
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