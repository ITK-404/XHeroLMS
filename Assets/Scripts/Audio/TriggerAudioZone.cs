using UnityEngine;

public class TriggerAudioZone : MonoBehaviour
{
    [SerializeField] private AudioClip audioClip;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioManager.Instance.PlayMusic(audioClip);
        }   
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            AudioManager.Instance.StopMusic(0.2f);
        }
    }

}
