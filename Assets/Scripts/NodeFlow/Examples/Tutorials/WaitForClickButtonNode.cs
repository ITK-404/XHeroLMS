using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

public class WaitForClickButtonNode : FlowNode
{
    private readonly Button button;
    public WaitForClickButtonNode(Button btn) : base("WaitForClickButtonNode")
    {
        this.button = btn;
    }

    public override async UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken)
    {
        await button.OnClickAsync(cancellationToken);
        // await button.OnClickAsync(button.destroyCancellationToken);

        return NodeResult.Completed;
    }
}