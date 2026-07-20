using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


public class CutsceneFlowMockTest : MonoBehaviour
{
    private CutsceneFlowRunner flowRunner;
    private CancellationTokenSource cancellationTokenSource;

    private void Start()
    {
        RunTestFlow().Forget();
    }

    private async UniTaskVoid RunTestFlow()
    {
        cancellationTokenSource = new CancellationTokenSource();

        CutsceneContext context = new CutsceneContext();

        FlowNode startNode = CreateMinigameFlow();

        flowRunner = new CutsceneFlowRunner();

        try
        {
            await flowRunner.RunAsync(
                startNode,
                context,
                cancellationTokenSource.Token
            );

            Debug.Log("[Cutscene] Flow completed");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[Cutscene] Flow cancelled");
        }
    }

    private FlowNode CreateMinigameFlow()
    {
        var startNode = new DebugFlowNode
        (
            "Cutscene đổ ly nước"
        );

        var popupChoice = new MockChoicePopupNode("Bạn sẽ làm gì");
        var repairChoice = new DebugFlowNode("Player chọn sửa tranh");
        var getNewPaperNode = new DebugFlowNode("Lấy tờ giấy mới, chờ 1s");

        startNode.AddTransition(NodeResult.Completed, popupChoice);

        popupChoice.AddTransition(NodeResult.Accept, repairChoice);
        popupChoice.AddTransition(NodeResult.Decline, getNewPaperNode);

        return startNode;
    }

    private FlowNode CreateTestFlow()
    {
        var startNode = new DebugFlowNode(
            "Bắt đầu mở hộp"
        );

        var popupNode = new MockChoicePopupNode(
            "Bạn có muốn nhận phần thưởng không?"
        );

        var acceptNode = new DebugFlowNode(
            "Player đã chọn nhận quà"
        );

        var showRewardNode = new DebugFlowNode(
            "Hiện reward item"
        );

        var rewardDelayNode = new WaitFlowNode(
            1f
        );

        var addRewardNode = new DebugFlowNode(
            "Thêm reward vào Inventory"
        );

        var declineNode = new DebugFlowNode(
            "Player đã từ chối phần thưởng"
        );

        var rejectCutsceneNode = new DebugFlowNode(
            "Play cutscene từ chối"
        );

        var endNode = new DebugFlowNode(
            "Kết thúc cutscene"
        );

        startNode.AddTransition(
            NodeResult.Completed,
            popupNode
        );

        popupNode.AddTransition(
            NodeResult.Accept,
            acceptNode
        );

        popupNode.AddTransition(
            NodeResult.Decline,
            declineNode
        );

        acceptNode.AddTransition(
            NodeResult.Completed,
            showRewardNode
        );

        showRewardNode.AddTransition(
            NodeResult.Completed,
            rewardDelayNode
        );

        rewardDelayNode.AddTransition(
            NodeResult.Completed,
            addRewardNode
        );

        addRewardNode.AddTransition(
            NodeResult.Completed,
            endNode
        );

        declineNode.AddTransition(
            NodeResult.Completed,
            rejectCutsceneNode
        );

        rejectCutsceneNode.AddTransition(
            NodeResult.Completed,
            endNode
        );

        return startNode;
    }

    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }
}

public static class NodeResult
{
    public const string Completed = "Completed";
    public const string Accept = "Accept";
    public const string Decline = "Decline";
    public const string Cancel = "Cancel";
}

public class DebugFlowNode : FlowNode
{
    private readonly string message;

    public DebugFlowNode(string message)
        : base($"Debug: {message}")
    {
        this.message = message;
    }

    public override UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken
    )
    {
        Debug.Log($"[Cutscene Debug] {message}");

        return UniTask.FromResult(
            NodeResult.Completed
        );
    }
}

public class WaitFlowNode : FlowNode
{
    private readonly float duration;

    public WaitFlowNode(float duration)
        : base($"Wait {duration} seconds")
    {
        this.duration = duration;
    }

    public override async UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken
    )
    {
        await UniTask.Delay(
            TimeSpan.FromSeconds(duration),
            cancellationToken: cancellationToken
        );

        return NodeResult.Completed;
    }
}

public class MockChoicePopupNode : FlowNode
{
    private readonly string message;

    public MockChoicePopupNode(string message)
        : base("Mock Choice Popup")
    {
        this.message = message;
    }

    public override async UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken
    )
    {
        Debug.Log(
            $"[Mock Popup] {message}\n" +
            "Nhấn Y để Accept, N để Decline"
        );

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Input.GetKeyDown(KeyCode.Y))
            {
                context.Set("PlayerChoice", NodeResult.Accept);

                return NodeResult.Accept;
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                context.Set("PlayerChoice", NodeResult.Decline);

                return NodeResult.Decline;
            }

            await UniTask.Yield(
                PlayerLoopTiming.Update,
                cancellationToken
            );
        }
    }
}