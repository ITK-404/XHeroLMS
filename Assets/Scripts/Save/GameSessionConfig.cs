using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "GameSessionConfig", menuName = "Game Session Config")]
public class GameSessionConfig : ScriptableObject
{
    public bool canSave = false;
    public bool canLoad = false;

    public float MinSaveInterval = 1f;
    [SerializeField] string saveFileName = "gamesessiondata";
    public GameSessionData previewData = null;

    public string BuildSavePath()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, Path.ChangeExtension(saveFileName, ".json"));
        return fullPath;
    }
}

