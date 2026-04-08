using UnityEngine;
using UnityEngine.UI;
using System.Collections;


#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class LoadTest : MonoBehaviour
{
    public string nameScene = "testScene";
    public Button button;

    public void Start()
    {
        button.onClick.AddListener(LoadScene);
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(LoadScene);
    }

    private void LoadScene()
    {
        // LoadingTransition.Load(nameScene);
        LoadingTransition.Load_Scene(nameScene);
    }

}
