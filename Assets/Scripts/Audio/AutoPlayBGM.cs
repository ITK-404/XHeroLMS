using UnityEngine;

public class AutoPlayBGM : MonoBehaviour
{
    [SerializeField] private AudioClip backgroundMusic;
    private void Start()
    {
        AudioManager.Instance.PlayMusic(backgroundMusic);
    }

}