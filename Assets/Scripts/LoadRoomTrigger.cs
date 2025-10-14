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
            LoadScene();
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
