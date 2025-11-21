using UnityEngine;
using UnityEngine.UI;

public class PlayButtonAudio : MonoBehaviour
{
    private Button btn;
    public AudioClip clip;
    public float pitch = 1;
    public float volume = 1;
    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnClick);

    }

    private void OnClick()
    {
        AudioManager.Instance.CreateSound(clip)
            .SetPitch(pitch)
            .SetVolume(volume)
            .With2DPreset().Play();
    }
}