using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class GameSessionHandler : MonoBehaviour
{
    [SerializeField] private GameSessionConfig config;
    private SceneLocationHandler sceneLocationHandler;
    
    private float lastSaveTime;

    private string savePath => config.BuildSavePath();

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

    // private void OnApplicationQuit()
    // {
    //     SaveSession().Forget();
    // }

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
        Debug.Log("[GameSessionHandler] Bắt đầu game session");
        // Test        

        GameSessionData lastSessionData = await LoadLastSessionData();
        
        Result sessionValidResult = await IsSessionDataValid(lastSessionData);

        bool isResumeSession = IsSameAccountID() && sessionValidResult.IsSessionValid;
        
        await UniTask.SwitchToMainThread();
        if (isResumeSession)
        {
            LoadGameSessionData(lastSessionData);
        }
        else
        {
            LoadDefaultSession();
        }
    }

    public async UniTask<GameSessionData> LoadLastSessionData()
    {
        if (config.canLoad == false) return null;
        
        if (!File.Exists(savePath))
        {
            Debug.LogError("Save file not found!");
            return null;
        }
        string json = await File.ReadAllTextAsync(savePath);
        GameSessionData data = JsonUtility.FromJson<GameSessionData>(json);
        Debug.Log("Loaded from: " + savePath);
        return data;
    }

    public async UniTask SaveSession()
    {
        if (config.canSave == false) return;
        Debug.Log($"[GameSessionHandler] Bắt đầu save game session");

        if (!TokenStore.IsAuthenticated) return;

        if (Time.time - lastSaveTime < config.MinSaveInterval) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var rawData = GameSessionData.CaptureCurrentState(player);
        var data = JsonUtility.ToJson(rawData);
        await File.WriteAllTextAsync(savePath, data);

        Debug.Log($"[GameSessionHandler] Save Session data {savePath}");

        lastSaveTime = Time.time;
    }

    public bool IsSameAccountID() => true;

    public async UniTask<Result> IsSessionDataValid(GameSessionData data)
    {
        var result = new Result
        {
            IsSessionValid = data != null
        };
        return result;
    }

    public void LoadGameSessionData(GameSessionData data)
    {
        // load by session data
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
    }

    public void LoadDefaultSession()
    {
        // load to new scene
    }
}