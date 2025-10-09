using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ExitButtonController : MonoBehaviour
{
    [Header("Button to trigger Exit")]
    public Button exitButton;

    [Header("Options")]
    [Tooltip("Có in log khi thoát (debug trong Editor).")]
    public bool logOnExit = true;

    void Start()
    {
        if (exitButton)
            exitButton.onClick.AddListener(ExitGame);
        else
            Debug.LogWarning("[ExitButtonController] Chưa gán Button thoát!");
    }

    public void ExitGame()
    {
        if (logOnExit)
            Debug.Log("[ExitButtonController] Exiting game...");

#if UNITY_EDITOR
        // Khi đang chạy trong Editor -> dừng Play mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Khi đang build (Windows, Android, v.v) -> thoát app
        Application.Quit();
#endif
    }
}
