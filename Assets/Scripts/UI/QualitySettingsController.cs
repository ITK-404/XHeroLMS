using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class QualitySettingsController : MonoBehaviour
{
    [Header("Buttons (assign in Inspector)")]
    public Button lowButton;
    public Button medButton;
    public Button highButton;
    public Button ultraButton;

    [Header("Optional")]
    [Tooltip("Tự log ra console khi đổi chất lượng (debug).")]
    public bool logOnChange = true;

    void Start()
    {
        if (lowButton)   lowButton.onClick.AddListener(() => SetQuality("Low"));
        if (medButton)   medButton.onClick.AddListener(() => SetQuality("Med"));
        if (highButton)  highButton.onClick.AddListener(() => SetQuality("High"));
        if (ultraButton) ultraButton.onClick.AddListener(() => SetQuality("Ultra"));
    }
    
    public void SetQuality(string levelName)
    {
        int index = QualitySettings.GetQualityLevel();
        string[] names = QualitySettings.names;

        for (int i = 0; i < names.Length; i++)
        {
            if (string.Equals(names[i], levelName, System.StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        QualitySettings.SetQualityLevel(index, true);
        if (logOnChange)
            Debug.Log($"[Quality] Changed to: {QualitySettings.names[index]} (index {index})");
    }
}
