using UnityEngine;
using UnityEngine.UI;

public class LoadTest : MonoBehaviour
{
    [Header("Target Scene")]
    [Tooltip("Tên scene hoặc Addressable Address. Không dùng label cloud nếu cloud chứa nhiều scene.")]
    [SerializeField] private string nameScene = "testScene";

    [Header("UI")]
    [SerializeField] private Button button;

    private bool isLoading;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(LoadScene);
        else
            Debug.LogWarning("[LoadTest] Button is null.");
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(LoadScene);
    }

    private void LoadScene()
    {
        if (isLoading)
            return;

        if (string.IsNullOrWhiteSpace(nameScene))
        {
            Debug.LogError("[LoadTest] nameScene is empty.");
            return;
        }

        isLoading = true;

        if (button != null)
            button.interactable = false;

        string targetScene = nameScene.Trim();

        Debug.Log($"[LoadTest] Request load scene: {targetScene}");

        // Flow mới:
        // LoadingTransition sẽ check scene có phải Addressables không.
        // LoadingScreenController sẽ gọi AddressablesPreload để tải + giải nén đúng scene đó.
        LoadingTransition.Load_Scene(targetScene);
    }
}