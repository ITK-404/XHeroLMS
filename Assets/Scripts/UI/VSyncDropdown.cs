using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif

public class VSyncDropdown : MonoBehaviour
{
    // [Header("Chọn 1 trong 2 loại Dropdown (để trống cái còn lại)")]
    // [SerializeField] private Dropdown uiDropdown;           // UnityEngine.UI.Dropdown
    // #if TMP_PRESENT
    [SerializeField] private TMP_Dropdown tmpDropdown;      // TextMeshPro TMP_Dropdown
    // #endif

    [Header("Tùy chọn")]
    [Tooltip("Lưu lựa chọn vào PlayerPrefs")]
    [SerializeField] private bool saveToPlayerPrefs = true;
    private const string PlayerPrefsKey = "VSyncCount";

    private readonly List<string> options = new List<string>
    {
        "Don't Sync (0)",
        "Every VBlank (1)",
        "Every Second VBlank (2)"
    };

    private void Awake()
    {
        int initial = GetInitialVSync();
        ApplyVSync(initial, persist:false);

        if (tmpDropdown != null)
        {
            SetupTMPDropdown(tmpDropdown, initial);
            return;
        }

        Debug.LogWarning("[VSyncDropdown] Chưa gán Dropdown nào. Hãy kéo thả Dropdown vào Inspector.");
    }

    private int GetInitialVSync()
    {
        if (saveToPlayerPrefs && PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(PlayerPrefsKey), 0, 2);
        }
        return Mathf.Clamp(QualitySettings.vSyncCount, 0, 2);
    }

    private void ApplyVSync(int value, bool persist = true)
    {
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = Mathf.Clamp(value, 0, 2);

        if (persist && saveToPlayerPrefs)
        {
            PlayerPrefs.SetInt(PlayerPrefsKey, QualitySettings.vSyncCount);
            PlayerPrefs.Save();
        }

        Debug.Log($"[VSyncDropdown] vSyncCount = {QualitySettings.vSyncCount}");
    }
    private void SetupTMPDropdown(TMP_Dropdown dd, int initial)
    {
        dd.ClearOptions();
        dd.AddOptions(options);
        dd.value = Mathf.Clamp(initial, 0, 2);
        dd.RefreshShownValue();

        dd.onValueChanged.RemoveAllListeners();
        dd.onValueChanged.AddListener(SetVSyncFromIndex);
    }

    private void SetVSyncFromIndex(int index)
    {
        ApplyVSync(index, persist:true);
    }
}
