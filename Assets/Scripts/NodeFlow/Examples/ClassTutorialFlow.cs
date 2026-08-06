using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ClassTutorialFlow : FlowBase
{
    public static ClassTutorialFlow Instance;
    
    [Header("References")]
    [SerializeField]
    private TutorialFlowBuilder nextBuilder; // default next, dùng khi không có logic context riêng
    [Header("Tutorial")]
    [SerializeField] private TutorialFlowBuilder builder;
    [SerializeField] private AskForReplayTutorialUI askForReplayTutorialUI;
    [SerializeField] private AutoReplayController autoReplayController;

    [Header("Focus Masking")] 
    [SerializeField] private ShaderMaskingUI shaderMaskingUI;
    [SerializeField] private TutorialFocusRaycastFilter focusFiler;
    public TutorialClickArea blockingArea;
    [SerializeField] private TutorialContext tutorialContext;
    protected override void Awake()
    {
        base.Awake();
        Instance = this;

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        Instance = null;
    }

    private void Start()
    {
        ClearZone();
        tutorialContext.Load();
        Debug.Log($"Tutorial is played {tutorialContext.IsPlayed}");
        if (tutorialContext.IsPlayed)
        {
            return;
        }
        HandleFlow().Forget();
    }

    private async UniTask HandleFlow()
    {
        // GameplayLock.Lock(GameplayLockReason.Animation, GameplayLockTarget.Movement);
        await UniTask.WaitForSeconds(2f);
        RunFlow().Forget();

        await UniTask.WaitForSeconds(2f);
        await UniTask.WaitUntil(() => !IsRunning(),
            PlayerLoopTiming.Update,
            this.GetCancellationTokenOnDestroy());
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
        var next = nextBuilder;
        nextBuilder = null;
        return next;
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
            // for sure
            tutorialContext.ResetTutorial();
            StartCoroutine(autoReplayController.WaitForLoading());
        }
        else
        {
            tutorialContext.MarkAsPlayed();
            askForReplayTutorialUI.Hide();
        }
    }

    protected override FlowNode CreateFlow()
    {
        var initializeNode = builder.BuildFlowNode();
        return initializeNode;
    }

    protected override CutsceneContext CreateGameContext()
    {
        var cutsceneContext = new CutsceneContext();
        cutsceneContext.Set(nameof(ClassTutorialFlow), this);
        return cutsceneContext;
    }

    public void SetInteractZone(RectTransform rectTransform)
    {
        shaderMaskingUI.SetTarget(rectTransform);
        focusFiler.SetTarget(rectTransform);
    }

    public void ClearZone()
    {
        shaderMaskingUI.ClearTargetAndTurnOff();
        focusFiler.ClearTarget();
        blockingArea.DeActive();
    }
}