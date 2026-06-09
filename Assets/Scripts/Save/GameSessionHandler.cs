using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class GameSessionHandler : MonoBehaviour
{
    private const string DEFAULT_SCENE = "New Scene";
    private const string LOADING_SCENE = "LoadingScene";
    
    [SerializeField] private GameSessionConfig config;
    private SceneLocationHandler sceneLocationHandler;
    
    private float lastSaveTime;

    private string previousLoadingID;
    private SaveManager saveManager;
    private bool isStartSessionRunning;
    private bool isRestoringSession;
    private string restoredSessionKey;

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

    private void ClearCatchData()
    {
        // dọn data vị trí, scene đã lưu
        previousLoadingID = string.Empty;
        restoredSessionKey = string.Empty;
        isStartSessionRunning = false;
        isRestoringSession = false;
        GameInitializer.Instance.SceneHistory.ClearHistory();
        GameInitializer.Instance.SceneLocationHandle.Clear();
    }

    public void Init(SceneLocationHandler sceneLocationHandler)
    {
        this.sceneLocationHandler = sceneLocationHandler;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            SaveSession().Forget();
        }
    }

    private void OnApplicationQuit()
    {
        SaveSession().Forget();
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
    public async UniTask StartSession()
    {
        if (isStartSessionRunning)
        {
            Debug.LogError($"Game Session config is null, load failed");
            return;
        }

        isStartSessionRunning = true;

        try
        {
            config = await Addressables.LoadAssetAsync<GameSessionConfig>("GameSessionConfig")
                .WithCancellation(destroyCancellationToken);

            if (config == null)
            {
                Debug.LogError("Load failed");
                return;
            }

            if (!config.canLoad)
                return;

            if (!TokenStore.IsAuthenticated || string.IsNullOrWhiteSpace(TokenStore.UserID))
            {
            Debug.Log($"[GameSessionHandler] không load khi không đứng ở new scene");
                return;
            }

            Debug.Log("[GameSessionHandler] Bắt đầu game session");

            string userID = TokenStore.UserID;
            GameSessionData lastSessionData = await GetLastSessionData(userID);
            Result sessionValidResult = await CheckSessionDataValid(lastSessionData);

            if (!sessionValidResult.IsSessionValid)
            {
                LoadDefaultSession();
                return;
            }

            if (!CanRestoreInCurrentSceneState(lastSessionData))
                return;

            MarkSessionRestoreStarted(lastSessionData);
            isRestoringSession = true;

            await UniTask.SwitchToMainThread();
            Debug.Log($"[GameSessionHandler] Tiếp tục session.");
            await LoadGameSessionData(lastSessionData);
        }
        finally
        {
            isRestoringSession = false;
            isStartSessionRunning = false;
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
            if (item != null && item.UserID == userID)
            {
                Debug.Log("[GameSessionHandler] Tìm thấy game session data của account id này");
                return item;
            }
        }
        Debug.Log("[GameSessionHandler] Không tìm thấy data của account id này");
        return null;
    }

    public async UniTask SaveSession()
    {
        if (config == null || config.canSave == false) return;
        if (isRestoringSession) return;
        if (Time.time - lastSaveTime < config.MinSaveInterval) return;
        if (SceneNameEquals(SceneManager.GetActiveScene().name, LOADING_SCENE)) return;
        Debug.Log($"[GameSessionHandler] Bắt đầu save game session");

        if (!TokenStore.IsAuthenticated) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        try
        {
            var rawData = GameSessionData.CaptureCurrentState(player);
            saveManager.SaveGameSession(rawData);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            throw;
        }
        lastSaveTime = Time.time;
    }
    

    public async UniTask<Result> CheckSessionDataValid(GameSessionData data)
    {
        bool isCorrectAccount = data != null && TokenStore.UserID == data.UserID;
        bool hasSceneData = data != null && data.HasValidScene;
        bool alreadyLoaded =
            previousLoadingID == TokenStore.UserID ||
            (!string.IsNullOrWhiteSpace(restoredSessionKey) && restoredSessionKey == BuildSessionKey(data));

        bool isResumeSession = isCorrectAccount && hasSceneData && !alreadyLoaded;
        var result = new Result
        {
            IsSessionValid = isResumeSession
        };
        return result;
    }

    public async UniTask LoadGameSessionData(GameSessionData data)
    {
        if (data == null || !data.HasValidScene)
            return;

        // load by session data
        if (data.HasCourseData)
        {
            var seoId = data.CourseData.seoId;
            
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
        
        await LoadSceneBySession(data);
    }

    private async UniTask LoadSceneBySession(GameSessionData data)
    {
        var sceneLocation = data.SceneLocation;
        if (sceneLocation == null || string.IsNullOrWhiteSpace(sceneLocation.SceneName))
            return;
        var currentScene = SceneManager.GetActiveScene().name;
        // CAP NHAT VI TRI O SCENE DO
        Debug.Log($"[GameSessionHandler]Không có khoá học");
        sceneLocationHandler.TryAddOrUpdate(sceneLocation);
        
        if (SceneNameEquals(sceneLocation.SceneName, currentScene))
        {
            Debug.Log($"[GameSessionHandler] cùng scene hiện tại, load vị trí thôi");
            await ApplySceneLocationWhenPlayerReady(currentScene);
        }
        else if (IsLoadingTargetScene(sceneLocation.SceneName))
        {
            await WaitForSceneAndApplyLocation(sceneLocation.SceneName);
        }
        else
        {
            Debug.Log($"[GameSessionHandler] khác scene hiện, load vị trí rồi load vị trí sau");
            LoadingTransition.Load_Scene(sceneLocation.SceneName);
        }
    }

    private void MarkSessionRestoreStarted(GameSessionData data)
    {
        previousLoadingID = data.UserID;
        restoredSessionKey = BuildSessionKey(data);
    }

    private bool CanRestoreInCurrentSceneState(GameSessionData data)
    {
        if (data == null || !data.HasValidScene)
            return false;

        string activeScene = SceneManager.GetActiveScene().name;
        string targetScene = data.SceneLocation.SceneName;

        if (SceneNameEquals(activeScene, DEFAULT_SCENE))
            return true;

        if (SceneNameEquals(activeScene, targetScene))
            return true;

        if (IsLoadingTargetScene(targetScene))
            return true;

        return false;
    }

    private bool IsLoadingTargetScene(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
               !string.IsNullOrWhiteSpace(LoadingTransition.TargetSceneName) &&
               SceneNameEquals(LoadingTransition.TargetSceneName, sceneName) &&
               !SceneNameEquals(SceneManager.GetActiveScene().name, sceneName);
    }

    private async UniTask WaitForSceneAndApplyLocation(string sceneName)
    {
        for (int i = 0; i < 600; i++)
        {
            if (SceneNameEquals(SceneManager.GetActiveScene().name, sceneName))
            {
                await ApplySceneLocationWhenPlayerReady(sceneName);
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
        }
    }

    private async UniTask ApplySceneLocationWhenPlayerReady(string sceneName)
    {
        for (int i = 0; i < 180; i++)
        {
            if (SceneNameEquals(SceneManager.GetActiveScene().name, sceneName) &&
                GameObject.FindGameObjectWithTag("Player") != null)
            {
                sceneLocationHandler.LoadPlayerPosition(sceneName);
                return;
            }

            await UniTask.Yield(PlayerLoopTiming.Update, destroyCancellationToken);
        }
    }

    private static string BuildSessionKey(GameSessionData data)
    {
        if (data == null)
            return string.Empty;

        string sceneName = data.SceneLocation != null ? data.SceneLocation.SceneName : "";
        string seoId = data.CourseData != null ? data.CourseData.seoId : "";
        return $"{data.UserID}|{data.SaveVersion}|{sceneName}|{seoId}";
    }

    private static bool SceneNameEquals(string a, string b)
    {
        return NormalizeSceneName(a) == NormalizeSceneName(b);
    }

    private static string NormalizeSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return "";

        return sceneName
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .Trim()
            .ToLowerInvariant();
    }

    public void LoadDefaultSession()
    {
        // load to new scene
    }
}