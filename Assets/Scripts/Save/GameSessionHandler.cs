using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class GameSessionHandler : MonoBehaviour
{
    private const string DEFAULT_SCENE = "IntroScene";
    
    [SerializeField] private GameSessionConfig config;
    private SceneLocationHandler sceneLocationHandler;
    
    private float lastSaveTime;
    private UniTask<GameSessionConfig>? configLoadTask;
    private float nextPeriodicSaveTime;

    private string previousLoadingID;
    private SaveManager saveManager;

    private void Awake()
    {
        saveManager = new();
        EventHub.OnPlayerLogout += ClearCatchData;
        EventHub.OnPlayerDeleteAccount += ClearCatchData;
    }

    private void OnDestroy()
    {
        EventHub.OnPlayerLogout -= ClearCatchData;
        EventHub.OnPlayerDeleteAccount -= ClearCatchData;
    }

    public async UniTask InitializeConfig(CancellationToken cancellationToken = default)
    {
        await LoadConfig(cancellationToken);
    }

    private async UniTask LoadConfig(CancellationToken cancellationToken)
    {
        if (config != null)
            return;

        if (!configLoadTask.HasValue)
        {
            configLoadTask = Addressables
                .LoadAssetAsync<GameSessionConfig>("GameSessionConfig")
                .WithCancellation(cancellationToken);
        }

        try
        {
            config = await configLoadTask.Value;

            if (config != null)
            {
                Debug.Log("[GameSessionHandler] GameSessionConfig loaded. "
                          + "canSave=" + config.canSave
                          + ", canLoad=" + config.canLoad
                          + ", interval=" + config.MinSaveInterval);
            }
            else
            {
                Debug.LogWarning("[GameSessionHandler] GameSessionConfig loaded as null.");
            }
        }
        catch (Exception e)
        {
            configLoadTask = null;
            Debug.LogWarning("[GameSessionHandler] GameSessionConfig load failed: " + e.Message);
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextPeriodicSaveTime)
            return;

        nextPeriodicSaveTime = Time.unscaledTime + 1f;

        if (config == null || !config.canSave)
            return;

        TrySaveSession("periodic", false);
    }

    private void ClearCatchData()
    {
        // dọn data vị trí, scene đã lưu
        previousLoadingID = string.Empty;
        if (GameInitializer.Instance != null)
        {
            GameInitializer.Instance.SceneHistory.ClearHistory();
            GameInitializer.Instance.SceneLocationHandle.Clear();
        }
    }

    public void Init(SceneLocationHandler sceneLocationHandler)
    {
        this.sceneLocationHandler = sceneLocationHandler;
    }

    private bool EnsureSceneLocationHandler()
    {
        if (sceneLocationHandler != null)
            return true;

        if (GameInitializer.Instance != null)
            sceneLocationHandler = GameInitializer.Instance.SceneLocationHandle;

        if (sceneLocationHandler == null)
            sceneLocationHandler = FindObjectOfType<SceneLocationHandler>();

        if (sceneLocationHandler != null)
            return true;

        Debug.LogWarning("[GameSessionHandler] SceneLocationHandler is null.");
        return false;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            TrySaveSession("focus_lost", true);
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            TrySaveSession("pause", true);
    }

    private void OnApplicationQuit()
    {
        TrySaveSession("quit", true);
    }

    public struct Result
    {
        public bool IsSessionValid;
    }
    /// <summary>
    /// NOTE: LUỒNG HIỆN TẠI CỦA LOAD SESSION
    /// 1. LOADING CÁC GAME SESSION ĐƯỢC LƯU
    /// 2. KIỂM TRA ACCOUNT HIỆN TẠI CÓ NẰM TRONG GAME SESSION ĐƯỢC LƯU KHÔNG
    ///     2.1 KIỂM TRA SESSION CÓ HỢP LỆ KHÔNG
    /// 3. NẾU CÓ THÌ LOAD LẠI SESSION TRƯỚC ĐÓ
    /// </summary>
    public async UniTaskVoid StartSession()
    {
        await LoadConfig(destroyCancellationToken);

        if (config == null)
        {
            Debug.LogError($"Game Session config is null, load failed");
            return;
        }
        // protect to load when load any scene
        if (SceneManager.GetActiveScene().name != DEFAULT_SCENE)
        {
            Debug.Log($"[GameSessionHandler] không load khi không đứng ở new scene");
            return;
        }
        
        Debug.Log("[GameSessionHandler] Bắt đầu game session");
        // Test        
        var userID = TokenStore.UserID;
        var lastSessionData = await GetLastSessionData(userID);
        
        Result sessionValidResult = await CheckSessionDataValid(lastSessionData);

        bool isResumeSession = sessionValidResult.IsSessionValid;
        await UniTask.SwitchToMainThread();
        if (isResumeSession)
        {
            Debug.Log($"[GameSessionHandler] Tiếp tục session");
            // update user id
            LoadGameSessionData(lastSessionData).Forget();
            previousLoadingID = lastSessionData.UserID;
        }
        else
        {
            Debug.Log($"[GameSessionHandler] không thể tiếp tục session");
            LoadDefaultSession();
        }
    }

    public async UniTask<GameSessionData> GetLastSessionData(string userID)
    {
        if (config == null) return null;
        if (config.canLoad == false) return null;
        var saves = saveManager.LoadAllGameSession();
        if (saves == null || saves.Count == 0)
        {
            Debug.Log("[GameSessionHandler] Máy này chưa có lưu game session data");
            return null;
        }
        foreach (var item in saves)
        {
            if (item.UserID == userID)
            {
                Debug.Log("[GameSessionHandler] Tìm thấy game session data của account id này");
                return item;
            }
        }
        Debug.Log("[GameSessionHandler] Không tìm thấy data của account id này");
        return null;
    }

    public UniTask SaveSession()
    {
        TrySaveSession("explicit", true);
        return UniTask.CompletedTask;
    }

    private bool TrySaveSession(string reason, bool force)
    {
        if (config == null)
        {
            Debug.LogWarning("[GameSessionHandler] Skip save session because config is null. reason=" + reason);
            return false;
        }

        if (!config.canSave)
            return false;

        float minInterval = Mathf.Max(0.5f, config.MinSaveInterval);
        if (!force && Time.unscaledTime - lastSaveTime < minInterval)
            return false;

        string currentScene = SceneNameAliases.ToSavedSceneName(SceneManager.GetActiveScene().name);
        if (!SceneNameAliases.CanUseSavedSceneForResume(currentScene))
            return false;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return false;

        try
        {
            var rawData = GameSessionData.CaptureCurrentState(player);
            if (string.IsNullOrWhiteSpace(rawData.UserID))
                rawData.UserID = GameSessionData.LocalGuestUserID;

            saveManager.SaveGameSession(rawData);
            lastSaveTime = Time.unscaledTime;
            nextPeriodicSaveTime = Time.unscaledTime + minInterval;

            Debug.Log("[GameSessionHandler] Saved session. reason="
                      + reason
                      + ", user="
                      + rawData.UserID
                      + ", scene="
                      + rawData.SceneLocation.SceneName
                      + ", position="
                      + rawData.SceneLocation.Position
                      + ", rotation="
                      + rawData.SceneLocation.Rotation);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[GameSessionHandler] Save session failed: " + e);
            return false;
        }
    }
    

    public async UniTask<Result> CheckSessionDataValid(GameSessionData data)
    {
        bool isCorrectAccount =data != null && TokenStore.UserID == data.UserID;
        bool alreadyLoaded = previousLoadingID == TokenStore.UserID;

        bool isResumeSession = isCorrectAccount && !alreadyLoaded;
        var result = new Result
        {
            IsSessionValid = isResumeSession
        };
        return result;
    }

    public async UniTaskVoid LoadGameSessionData(GameSessionData data)
    {
        // load by session data
        if (data.saveCourseData != null && !string.IsNullOrEmpty(data.saveCourseData.seoId))
        {
            var seoId = data.saveCourseData.seoId;
            
            Debug.Log("[GameSessionHandler] Fetech data và thử load khoá học");
            //fetech data xong moi laod
            SeoResolver.seoCourse = seoId;
            await SeoResolver.LoadPrivateAndFillData();
            // co the vao khoa hoc
            if (!SeoResolver.canEnterCourse)
            {
                return;
            }
            Debug.Log("[GameSessionHandler] Không thể load khoá học");
        }
        
        LoadSceneBySession(data);
    }

    private bool LoadSceneBySession(GameSessionData data)
    {
        if (data == null || data.SceneLocation == null)
        {
            return false;
        }

        if (!EnsureSceneLocationHandler())
            return false;

        var sceneLocation = data.SceneLocation;
        var currentScene = SceneManager.GetActiveScene().name;
        // CAP NHAT VI TRI O SCENE DO
        Debug.Log($"[GameSessionHandler]Không có khoá học");
        sceneLocationHandler.TryAddOrUpdate(sceneLocation);
        
        if (sceneLocation.SceneName == currentScene)
        {
            Debug.Log($"[GameSessionHandler] cùng scene hiện tại, load vị trí thôi");
            sceneLocationHandler.LoadPlayerPosition(currentScene);
        }
        else
        {
            Debug.Log($"[GameSessionHandler] khác scene hiện, load vị trí rồi load vị trí sau");
            // LoadingTransition.Load_Scene(sceneLocation.SceneName);
        }

        return true;
    }

    public async UniTask<bool> LoadGameSessionData2(GameSessionData data)
    {
        if (data == null)
        {
            return false;
        }

        if (data.SceneLocation == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(data.SceneLocation.SceneName))
        {
            return false;
        }

        // Tái dùng cùng logic fetch data như LoadGameSessionData,
        // nhưng hàm này trả bool để BootFlow biết khi nào xong.
        if (data.saveCourseData != null && !string.IsNullOrEmpty(data.saveCourseData.seoId))
        {
            var seoId = data.saveCourseData.seoId;
            SeoResolver.seoCourse = seoId;
            await SeoResolver.LoadPrivateAndFillData();
            if (!SeoResolver.canEnterCourse)
            {
                return false;
            }
        }

        return LoadSceneBySession(data);
    }

    public void LoadDefaultSession()
    {
        // load to new scene
    }
}
