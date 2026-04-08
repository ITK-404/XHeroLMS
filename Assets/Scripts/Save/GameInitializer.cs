using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var go = new GameObject("Game Initializer").AddComponent<GameInitializer>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        // Init call here
        Debug.Log($"[GameInitializer] Load các tác vụ ngầm");
        var ct = this.GetCancellationTokenOnDestroy();
        var runner = this;
        
        IOSReviewManager.CheckIOSReviewStatusAsync(ct).Forget();
        LoadingTransition.Init(runner,ct).Forget();
    }
}