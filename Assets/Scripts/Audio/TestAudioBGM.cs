using UnityEngine;
using UnityEngine.UI;

public class TestAudioBGM : MonoBehaviour
{
    private Button btn;
    private bool isOn = false;
    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(ToggleBGMAudio);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(ToggleBGMAudio);
    }

    private void ToggleBGMAudio()
    {
        isOn = !isOn;
        if (isOn)
        {
            AudioManager.Instance.Pause();
        }
        else
        {
            AudioManager.Instance.Resume();
        }
    }
}