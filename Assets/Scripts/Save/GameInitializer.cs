using System;
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
        LoginController.OnLoginComplete += LoginComplete;
    }

    private void Start()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        InitTask(ct).Forget();
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= LoginComplete;
    }

    private float _lastLoginCompleteTime = -999f;
    private const float LOGIN_COOLDOWN = 5f;

    private void LoginComplete()
    {
        if (Time.time - _lastLoginCompleteTime < LOGIN_COOLDOWN)
        {
            Debug.Log("[GameSessionHandler] LoginComplete bị chặn, cooldown chưa hết");
            return;
        }
    
        _lastLoginCompleteTime = Time.time;
        PlaySession().Forget();
    }
    
    private SceneNavigationHistory sceneHistory;
    private SceneLocationHandler sceneLocationHandle;
    private async UniTaskVoid InitTask(CancellationToken ct)
    {
        await Addressables.InitializeAsync().WithCancellation(ct);
         var runner = this;
        
        sceneHistory = CreateGameObject<SceneNavigationHistory>(donDestroyOnLoad: true);
        sceneLocationHandle = CreateGameObject<SceneLocationHandler>(donDestroyOnLoad: true);

        IOSReviewManager.CheckIOSReviewStatusAsync(ct).Forget();
        LoadingTransition.Init(runner, sceneHistory, sceneLocationHandle, ct).Forget();
    }

    private async UniTaskVoid PlaySession()
    {
        // BUG NOTE:
        // StartSession chạy quá sớm trong boot flow và có thể tranh quyền điều hướng scene,
        // gây kẹt hoặc đè lên flow load scene hiện tại.
        // Tạm thời disable để xác nhận nguyên nhân và tránh chặn scene mới.
        
        await AddressablesLoader.EnsureInitialized();
        if (gameSessionHandle == null)
        {
            gameSessionHandle = CreateGameObject<GameSessionHandler>(donDestroyOnLoad: true);
            gameSessionHandle.Init(sceneLocationHandle);
        }
        if(gameSessionHandle)
            gameSessionHandle.StartSession().Forget();
    }

    private GameSessionHandler gameSessionHandle;

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