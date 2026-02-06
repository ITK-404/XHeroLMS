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
        StartCoroutine(CoLoadSceneSmart(nameScene));
    }

    private IEnumerator CoLoadSceneSmart(string targetScene)
    {
#if ADDRESSABLES
        // Nếu scene là addressable (cloud) -> dùng LoadAssetBundle
        bool isCloud = false;
        yield return CoCheckIsCloudScene(targetScene, r => isCloud = r);

        if (isCloud)
            LoadingTransition.LoadAssetBundle(targetScene);
        else
            // LoadingTransition.Load(targetScene);
            StartCoroutine(CoLoadSceneSmart(targetScene));
#else
    LoadingTransition.Load(targetScene);
    yield break;
#endif
    }

#if ADDRESSABLES
    // Check: sceneName có tồn tại như 1 addressable scene không?
    private IEnumerator CoCheckIsCloudScene(string sceneKeyOrName, System.Action<bool> result)
    {
        // var h = Addressables.LoadResourceLocationsAsync(sceneKeyOrName, typeof(SceneInstance));
        var h = Addressables.LoadResourceLocationsAsync(sceneKeyOrName);

        yield return h;

        bool ok = (h.Status == AsyncOperationStatus.Succeeded && h.Result != null && h.Result.Count > 0);

        // Release handle (tránh leak)
        Addressables.Release(h);

        result?.Invoke(ok);
    }
#endif
}
