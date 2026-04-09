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
    private const float MinSaveInterval = 1f;
    private  string savePath => Application.persistentDataPath + "/save.json";

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
        
        Debug.Log("[GameSessionHandler] Bắt đầu game session");
        // Test        
        if (config.canLoad == false) return;

        GameSessionData data = await LoadSessionData();

        Result sessionValidResult = await IsSessionDataValid(data);

        bool isResumeSession = IsSameAccountID() && sessionValidResult.IsSessionValid;
        
        await UniTask.SwitchToMainThread();
        if (isResumeSession)
        {
            LoadGameSessionData(data);
        }
        else
        {
            LoadDefaultSession();
        }
    }

    public async UniTask<GameSessionData> LoadSessionData()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("Save file not found!");
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

        if (Time.time - lastSaveTime < MinSaveInterval) return;

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
            IsSessionValid = true
        };
        return result;
    }

    public void LoadGameSessionData(GameSessionData data)
    {
        // load by session data
        var sceneLocation = data.SceneLocation;
        var currentScene = SceneManager.GetActiveScene().name;
        // CAP NHAT VI TRI O SCENE DO
        sceneLocationHandler.TryAddOrUpdate(sceneLocation.SceneName, sceneLocation.Position, sceneLocation.Rotation);
        
        if (sceneLocation.SceneName == currentScene)
        {
            sceneLocationHandler.LoadPlayerPosition(currentScene);
        }
        else
        {
            LoadingTransition.Load_Scene(sceneLocation.SceneName);
        }
    }

    public void LoadDefaultSession()
    {
        // load to new scene
    }
}