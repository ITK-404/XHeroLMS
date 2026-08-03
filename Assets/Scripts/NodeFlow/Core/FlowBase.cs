using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class FlowBase : MonoBehaviour
{
    private bool isRunning = false;
    private CutsceneFlowRunner flowRunner;
    private CancellationTokenSource cancellationTokenSource;

    protected virtual void Awake()
    {
    }

    protected virtual void OnDestroy()
    {
        Dispose();
    }

    private void Dispose()
    {
        cancellationTokenSource?.Cancel();
        cancellationTokenSource?.Dispose();
    }

    public bool IsRunning() => isRunning;

    protected virtual CutsceneContext CreateGameContext()
    {
        return new CutsceneContext();
    }
    
    public async UniTaskVoid RunFlow()
    {
        if (isRunning)
        {
            Debug.LogError($"[FlowBase] You are trying to start a running flow");
            return;
        }

        cancellationTokenSource?.Dispose(); // dispose CTS cũ nếu có, tránh leak
        cancellationTokenSource = new CancellationTokenSource();

        CutsceneContext context = CreateGameContext();
        FlowNode startNode = CreateFlow();
        flowRunner = new CutsceneFlowRunner();
        isRunning = true;

        
        Debug.Log("[FlowBase] Flow Starting");

        try
        {
            await flowRunner.RunAsync(startNode, context, cancellationTokenSource.Token);
            Debug.Log("[FlowBase] Flow completed");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[FlowBase] Flow cancelled");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlowBase] Flow error: {ex}");
        }
        finally
        {
            isRunning = false; // LUÔN reset, dù thành công, bị cancel, hay lỗi
        }
    }

    protected abstract FlowNode CreateFlow();
}