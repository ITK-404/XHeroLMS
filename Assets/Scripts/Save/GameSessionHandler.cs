using System;
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

    private void ClearCatchData()
    {
        // dọn data vị trí, scene đã lưu
        previousLoadingID = string.Empty;
        if (GameInitializer.Instance != null)
        {
            GameInitializer.Instance.SceneHistory.ClearHistory();
            GameInitializer.Instance.SceneLocationHandle.Clear();
            TutorialContext.ClearPlayedTutorialIds();
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
    public async UniTaskVoid StartSession()
    {
        config = await Addressables.LoadAssetAsync<GameSessionConfig>("GameSessionConfig").WithCancellation(destroyCancellationToken);
        if (config == null)
        {
            Debug.LogError($"Game Session config is null, load failed");
            return;
        }
        
        var userID = TokenStore.UserID;
        var lastSessionData = await GetLastSessionData(userID);
        Result sessionValidResult = await CheckSessionDataValid(lastSessionData);

        bool isResumeSession = sessionValidResult.IsSessionValid;
        if (isResumeSession)
        {
            TutorialContext.Load(lastSessionData.saveCourseData.tutorialIds);
        }
        // protect to load when load any scene
        if (SceneManager.GetActiveScene().name != DEFAULT_SCENE)
        {
            Debug.Log($"[GameSessionHandler] không load khi không đứng ở new scene");
            return;
        }
        
        Debug.Log("[GameSessionHandler] Bắt đầu game session");
        // Test        
        
        
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

    public async UniTask SaveSession()
    {
        if (config == null)
        {
            Debug.LogWarning("[GameSessionHandler] Skip save session because config is null.");
            return;
        }

        if (config.canSave == false) return;
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
        TutorialContext.Load(data.saveCourseData.tutorialIds);
        
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
