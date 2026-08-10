using System;
using System.Collections;
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

    [SerializeField] private SceneLessonUI sceneLessonUI;
    [SerializeField] private CourseListView courseListView;

    [SerializeField] private GameObject blockerCanvas;
    protected override void Awake()
    {
        base.Awake();
        Instance = this;
        courseListView = FindFirstObjectByType<CourseListView>();
        shaderMaskingUI.Hide();
        sceneLessonUI.OnLoadCourseDone += CourseDataLoadDone;
    }
    
    protected override void OnDestroy()
    {
        base.OnDestroy();
        Instance = null;
        sceneLessonUI.OnLoadCourseDone -= CourseDataLoadDone;
    }

    private void CourseDataLoadDone(LmsCoursePrivate lmsCoursePrivate)
    {
        StartCoroutine(WaitForLoading());
    }

    private IEnumerator WaitForLoading()
    {
        LoadingUI.Show();
        yield return new WaitForSecondsRealtime(2f);
        LoadingUI.Hide();
        Debug.Log($"[ClassTutorialFlow] courseData load xong {isReplayTutorial}");
        ClearZone();
        TryStartTutorialFlow();
    }

    private void Start()
    {
        Debug.Log($"[ClassTutorialFlow] check flag isReplayTutorial {isReplayTutorial}");
        // if (isReplayTutorial)
        // {
        //     Debug.Log($"[ClassTutorialFlow] bắt đầu lại tutorial");
        //     ClearZone();
        //     TryStartTutorialFlow();
        // }
    }

    public void SetStateBlocker(bool blockerState)
    {
        blockerCanvas.gameObject.SetActive(blockerState);
    }
    
    private static bool isReplayTutorial = false;
    private void TryStartTutorialFlow()
    {
        if (IsTutorialPlayed() && isReplayTutorial == false)
        {
            SetStateBlocker(false);
            Debug.Log("[ClassTutorialFlow] Tutorial đã play hoặc isReplayTutorial đang true");
            return;
        }
        if (!IsCourseValid())
        {
            SetStateBlocker(false);
            Debug.Log("[ClassTutorialFlow] Không có course để chạy tutorial");
            return;
        }
        
        Debug.Log($"Tutorial is played {tutorialContext.IsPlayed}");
        HandleFlow().Forget();
    }
    
    private bool IsCourseValid()
    {
        if (courseListView.VideoLessons == null || courseListView.VideoLessons.Count == 0)
        {
            return false;
        }
        return true;
    }
    
    private bool IsTutorialPlayed()
    {
        // data must be load
        tutorialContext.Load();
        return tutorialContext.IsPlayed;
    }

    private async UniTask HandleFlow()
    {
        // SETUP
        SetStateBlocker(false);
        
        GameplayLock.Lock(GameplayLockReason.Tutorial, GameplayLockTarget.Movement);
        shaderMaskingUI.Show();
        
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
        GameplayLock.Unlock(GameplayLockReason.Tutorial);
        
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
            Debug.Log($"[ClassTutorialFlow] bắt đầu lại tutorial");
            // for sure
            tutorialContext.ResetTutorial();
            StartCoroutine(autoReplayController.WaitForLoading());
            isReplayTutorial = true;
        }
        else
        {
            Debug.Log($"[ClassTutorialFlow] Không bắt đầu lại tutorial");
            tutorialContext.MarkAsPlayed();
            askForReplayTutorialUI.Hide();
            isReplayTutorial = false;
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