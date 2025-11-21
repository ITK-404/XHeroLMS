using System.Collections;
using UnityEngine;

public class AutoPlayBGM : MonoBehaviour
{
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private int delayTime;
    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delayTime);
        AudioManager.Instance.PlayMusic(backgroundMusic);
    }

}