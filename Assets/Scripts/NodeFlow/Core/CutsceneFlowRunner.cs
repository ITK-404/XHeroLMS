using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class CutsceneFlowRunner
{
    public async UniTask RunAsync(
        FlowNode entryNode,
        CutsceneContext context,
        CancellationToken cancellationToken
    )
    {
        FlowNode currentNode = entryNode;

        while (currentNode != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Debug.Log(
                $"[Cutscene] Execute node: {currentNode.Name}"
            );

            string result = await currentNode.ExecuteAsync(
                context,
                cancellationToken
            );

            Debug.Log(
                $"[Cutscene] Node result: {result}"
            );

            if (!currentNode.TryGetNextNode(
                    result,
                    out FlowNode nextNode))
            {
                Debug.Log(
                    $"[Cutscene] Không có transition cho result '{result}'. Flow kết thúc."
                );

                break;
            }

            currentNode = nextNode;
        }
    }
}