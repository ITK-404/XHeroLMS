using UnityEngine;
using UnityEngine.SceneManagement;
[RequireComponent(typeof(Rigidbody))]
public class LoadRoomTrigger : MonoBehaviour
{
    public string sceneName; 
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (other.CompareTag("Player") && !string.IsNullOrEmpty(sceneName))
            {
                LoadingTransition.Load(sceneName); 
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        
    }

    private void OnTriggerExit(Collider other)
    {
        
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
