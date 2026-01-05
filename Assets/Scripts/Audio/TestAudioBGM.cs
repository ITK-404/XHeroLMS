using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TestAudioBGM : MonoBehaviour
{
    [Header("Icon")]
    public Image iconImage;
    public Sprite iconOn;   // loa bật
    public Sprite iconOff;  // loa tắt

    private Button btn;
    private bool isOn = true; // mặc định BGM đang bật

    private void Start()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(ToggleBGMAudio);
        LoadByData();
        UpdateIcon();
    }

    private void LoadByData()
    {
        if (!AudioManager.Instance)
        {
            return;
        }
        isOn = AudioManager.Instance.settings.MusicVolume >= 0.5f;
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
            AudioManager.Instance.Resume();
        }
        else
        {
            AudioManager.Instance.Pause();
        }

        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (!iconImage) return;

        iconImage.sprite = isOn ? iconOn : iconOff;
    }
}
