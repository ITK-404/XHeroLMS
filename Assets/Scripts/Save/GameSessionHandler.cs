using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class GameSessionHandler : MonoBehaviour
{
    private const string DEFAULT_SCENE = "New Scene";
    
    [SerializeField] private GameSessionConfig config;
    private SceneLocationHandler sceneLocationHandler;
    
    private float lastSaveTime;

    private string savePath => config.BuildSavePath();
    private string previousLoadingID;

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

    public async UniTaskVoid StartSession()
    {
        config = await Addressables.LoadAssetAsync<GameSessionConfig>("GameSessionConfig").WithCancellation(destroyCancellationToken);
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

        var lastSessionData = await LoadLastSessionData();
        
        Result sessionValidResult = await IsSessionDataValid(lastSessionData);

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

    public async UniTask<GameSessionData> LoadLastSessionData()
    {
        if (config.canLoad == false) return null;
        
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Save file not found!");
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(savePath);
            var data = JsonUtility.FromJson<GameSessionData>(json);
            Debug.Log("Loaded from: " + savePath);
            return data;
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            throw;
        }
    }

    public async UniTask SaveSession()
    {
        if (config.canSave == false) return;
        Debug.Log($"[GameSessionHandler] Bắt đầu save game session");

        if (!TokenStore.IsAuthenticated) return;

        if (Time.time - lastSaveTime < config.MinSaveInterval) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        try
        {
            var rawData = GameSessionData.CaptureCurrentState(player);
            var data = JsonUtility.ToJson(rawData);
            await File.WriteAllTextAsync(savePath, data);
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
            throw;
        }

        Debug.Log($"[GameSessionHandler] Save Session data {savePath}");

        lastSaveTime = Time.time;
    }
    

    public async UniTask<Result> IsSessionDataValid(GameSessionData data)
    {
        bool isCorrectAccount = TokenStore.UserID == data.UserID;
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
        var seoId = data.CourseData.seoId;
        Debug.Log($"[GameSessionHandler] seo id là {seoId}");
        if (!string.IsNullOrEmpty(seoId))
        {
            Debug.Log("[GameSessionHandler] Fetech data và thử load khoá học");
            //fetech data xong moi laod
            SeoResolver.seoCourse = seoId;
            await SeoResolver.LoadPrivateAndFillData();
            // co the vao khoa hoc
            if (SeoResolver.canEnterCourse)
            {
                var sceneLocation = data.SceneLocation;
                var currentScene = SceneManager.GetActiveScene().name;
                // CAP NHAT VI TRI O SCENE DO
        
                sceneLocationHandler.TryAddOrUpdate(sceneLocation);
        
                if (sceneLocation.SceneName == currentScene)
                {
                    Debug.Log($"[GameSessionHandler] cùng scene hiện tại, load vị trí thôi");
                    sceneLocationHandler.LoadPlayerPosition(currentScene);
                }
                else
                {
                    Debug.Log($"[GameSessionHandler] khác scene hiện, load vị trí rồi load vị trí sau");
                    LoadingTransition.Load_Scene(sceneLocation.SceneName);
                }

                return;
            }
            Debug.Log("[GameSessionHandler] Không thể load khoá học");
        }
        
    }

    public void LoadDefaultSession()
    {
        // load to new scene
    }
}