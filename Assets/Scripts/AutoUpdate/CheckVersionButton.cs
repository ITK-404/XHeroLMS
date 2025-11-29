using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CheckVersionButton : MonoBehaviour
{
    private AutoUpdaterUI updater;
    public bool runInEditor = true;

    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
        if (!updater) updater = FindAnyObjectByType<AutoUpdaterUI>();

        updater=FindAnyObjectByType<AutoUpdaterUI>();
    }

    void OnDestroy()
    {
        GetComponent<Button>().onClick.RemoveListener(OnClick);
    }

    public void OnClick()
    {
        if (!updater)
        {
            Debug.LogWarning("[CheckVersionButton] AutoUpdaterUI not found in scene.");
            return;
        }
        updater.CheckNow(runInEditor);
    }
}
