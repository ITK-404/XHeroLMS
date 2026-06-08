using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GameInitializer : MonoBehaviour
{
    public static GameInitializer Instance;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var go = new GameObject("Game Initializer").AddComponent<GameInitializer>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        Instance = this;
        EnsureCoreRuntimeObjects();
        LoadingTransition.BindRuntime(this, sceneHistory, sceneLocationHandle);
        Debug.Log("[GameInitializer] Load các tác vụ ngầm");
    }

    private FPSSceneHandle fpsSceneHandle;

    private void Start()
    {
        LoginController.OnLoginComplete += LoginComplete;
        
        var ct = this.GetCancellationTokenOnDestroy();
        InitTask(ct).Forget();
    }


    private void OnDestroy()
    {
        
        UnbindEvent();
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

    public SceneNavigationHistory SceneHistory => sceneHistory;

    private SceneLocationHandler sceneLocationHandle;

    public SceneLocationHandler SceneLocationHandle => sceneLocationHandle;

    private GraphicsSettingsManager graphicsSettingsManager;
    
    private GameSessionHandler gameSessionHandle;
    public GameSessionHandler GameSessionHandler
    {
        get => gameSessionHandle;
    }

    private BatteryWarningHandler batteryWarningHandler;
    public BatteryWarningHandler BatteryWarningHandler => batteryWarningHandler;

    private PlayerRotationConfigHandler rotConfig;

    private async UniTaskVoid InitTask(CancellationToken ct)
    {
        await Addressables.InitializeAsync().WithCancellation(ct);
         var runner = this;

         FPSInit();
         
        // GAME OBJECT LOADING
        // TODO: UPDATE TO ADDRESSABLE BEFORE
        EnsureCoreRuntimeObjects();
        LoadingTransition.Init(runner, sceneHistory, sceneLocationHandle, ct).Forget();
        
        // ADDRESSABLE LOADING
        
        graphicsSettingsManager = await LoadAddressable<GraphicsSettingsManager>("GraphicsSettingsManager",true,ct);
        rotConfig = await LoadAddressable<PlayerRotationConfigHandler>("PlayerRotationConfigHandler",dontDestroyOnLoad:true,ct);
        batteryWarningHandler = await LoadAddressable<BatteryWarningHandler>("BatteryWarningHandler",dontDestroyOnLoad: true,ct);
        
        IOSReviewManager.CheckIOSReviewStatusAsync(ct).Forget();

        BindEvent();
    }

    private void EnsureCoreRuntimeObjects()
    {
        if (sceneHistory == null)
            sceneHistory = CreateGameObject<SceneNavigationHistory>(donDestroyOnLoad: true);

        if (sceneLocationHandle == null)
            sceneLocationHandle = CreateGameObject<SceneLocationHandler>(donDestroyOnLoad: true);
    }

    private void BindEvent()
    {
        // đảm bảo gọi sau khi các object khác init
        
        batteryWarningHandler.onBatteryLow.AddListener(HandleLowBattery);
    }

    private void UnbindEvent()
    {
        LoginController.OnLoginComplete -= LoginComplete;
        
        batteryWarningHandler.onBatteryLow.RemoveListener(HandleLowBattery);
        
        FPSHandler.Save();
        fpsSceneHandle.Dispose();
    }

    private void FPSInit()
    {
        // INIT
        // load save
        FPSHandler.Load();
        FPSHandler.ApplyFPS();
        // setup handle logic
        fpsSceneHandle = new FPSSceneHandle();
        fpsSceneHandle.Init();
    }
    
    private void HandleLowBattery()
    {
        graphicsSettingsManager.ApplyLowestPreset();
        FPSHandler.SetLowestFrameRate();
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

    

    private T CreateGameObject<T>(bool donDestroyOnLoad = false) where T : Component
    {
        var go = new GameObject(typeof(T).Name).AddComponent<T>();
        if (donDestroyOnLoad)
        {
            DontDestroyOnLoad(go.gameObject);
        }

        return go;
    }

    private async UniTask<T> LoadAddressable<T>(
        string address,
        bool dontDestroyOnLoad = false,
        CancellationToken ct = default) where T : Component
    {
        var prefab = await Addressables.LoadAssetAsync<GameObject>(address).WithCancellation(ct);
        var instance = Instantiate(prefab).GetComponent<T>();
    
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(instance.gameObject);
    
        return instance;
    }
}
