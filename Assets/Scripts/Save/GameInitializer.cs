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

        var sceneHistory = CreateGameObject<SceneNavigationHistory>(donDestroyOnLoad: true);
        var sceneLocationHandle = CreateGameObject<SceneLocationHandler>(donDestroyOnLoad: true);
        IOSReviewManager.CheckIOSReviewStatusAsync(ct).Forget();
        LoadingTransition.Init(runner, sceneHistory, ct).Forget();
        
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