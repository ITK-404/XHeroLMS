using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
        Debug.Log("[GameInitializer] Load các tác vụ ngầm");
        var ct = this.GetCancellationTokenOnDestroy();
  
        InitTask(ct).Forget();
    }

private async UniTaskVoid InitTask(CancellationToken ct)
    {
        await Addressables.InitializeAsync().WithCancellation(ct);
         var runner = this;

        var sceneHistory = CreateGameObject<SceneNavigationHistory>(donDestroyOnLoad: true);
        var sceneLocationHandle = CreateGameObject<SceneLocationHandler>(donDestroyOnLoad: true);
        var gameSessionHandle = CreateGameObject<GameSessionHandler>(donDestroyOnLoad: true);

        IOSReviewManager.CheckIOSReviewStatusAsync(ct).Forget();
        LoadingTransition.Init(runner, sceneHistory, sceneLocationHandle, ct).Forget();

        gameSessionHandle.Init(sceneLocationHandle);

        // BUG NOTE:
        // StartSession chạy quá sớm trong boot flow và có thể tranh quyền điều hướng scene,
        // gây kẹt hoặc đè lên flow load scene hiện tại.
        // Tạm thời disable để xác nhận nguyên nhân và tránh chặn scene mới.
        gameSessionHandle.StartSession().Forget();

    }

    private T CreateGameObject<T>(bool donDestroyOnLoad = false) where T : Component
    {
        var go = new GameObject(typeof(T).Name).AddComponent<T>();
        if (donDestroyOnLoad)
        {
            DontDestroyOnLoad(go.gameObject);
        }

        return go;
    }
}