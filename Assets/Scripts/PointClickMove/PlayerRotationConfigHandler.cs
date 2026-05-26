using UnityEngine;

public class PlayerRotationConfigHandler : MonoBehaviour
{
    private const string SAVE_ROTATION_SENSITIVITY = "save_sensitivity_rotation";

    [SerializeField] private PlayerRotationConfig config;

    private void Awake()
    {
        Load();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    private void Save()
    {
        if (config == null)
        {
            Debug.LogError("[PlayerRotationConfigHandler] config is null");
            return;
        }

        PlayerPrefs.SetFloat(SAVE_ROTATION_SENSITIVITY, config.rotationMultiplier);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        if (config == null)
        {
            Debug.LogError("[PlayerRotationConfigHandler] config is null");
            return;
        }

        config.rotationMultiplier = PlayerPrefs.GetFloat(
            SAVE_ROTATION_SENSITIVITY,
            config.rotationMultiplier // fallback về giá trị default nếu chưa có save
        );
    }
}