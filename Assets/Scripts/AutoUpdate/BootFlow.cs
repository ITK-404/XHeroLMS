using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

/// <summary>
/// Attach vào 1 GameObject trong BootstrapScene.
/// Script này là "cửa duy nhất" để vào Main.
/// </summary>
[DefaultExecutionOrder(-20000)]
public class BootFlow : MonoBehaviour
{
    public static BootFlow Instance { get; private set; }

    [Header("Bootstrap References")]
    public AddressablesPreload preload;     // có thể kéo trong inspector, hoặc để null để auto-create
    public IntroManager intro;              // kéo IntroManager trong scene

    [Header("Main Scene")]
    public bool mainSceneIsAddressable = true;

    [Tooltip("Nếu mainSceneIsAddressable=true -> key scene Addressables")]
    public string mainAddressableSceneKey = "NewScene";

    [Tooltip("Nếu mainSceneIsAddressable=false -> build index của Scene main (thường là 1)")]
    public int mainSceneBuildIndex = 1;

    [Header("Behavior")]
    public bool allowEnterMainWhenPreloadFailed = true;
    public float minHoldBeforeEnterMain = 0.05f;

    private bool _loadingMain;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // đảm bảo preload singleton tồn tại
        if (preload == null) preload = AddressablesPreload.Instance;
        if (preload == null)
        {
            var go = new GameObject("[AddressablesPreload]");
            DontDestroyOnLoad(go);
            preload = go.AddComponent<AddressablesPreload>();
        }

        // đảm bảo IntroManager biết preload (nếu bạn muốn)
        if (intro != null)
            intro.SetExternalPreload(preload);
    }

    private void Start()
    {
        StartCoroutine(CoBoot());
    }

    private IEnumerator CoBoot()
    {
        // Chờ preload được tạo ra (trong case BootFlow chạy sớm hơn)
        while (preload == null)
        {
            preload = AddressablesPreload.Instance;
            yield return null;
        }

        // Chờ AddressablesPreload tải xong toàn bộ data (label) hoặc fail
        // => Không quan tâm chuyện "download scene" nữa, mọi thứ nằm ở preload flow.
while (!preload.IsCloudFullyDownloaded && !preload.HasFailed)
    yield return null;


        if (preload.HasFailed && !allowEnterMainWhenPreloadFailed)
        {
            if (intro != null)
                intro.ShowFatalFail(preload.LastError);
            yield break;
        }

        // giữ 1 chút cho UI fill 100% (đẹp)
        if (intro != null) intro.ForceProgress(1f);
        if (minHoldBeforeEnterMain > 0) yield return new WaitForSecondsRealtime(minHoldBeforeEnterMain);

        // vào main
        EnterMain();
    }

public void EnterMain()
{
    if (_loadingMain) return;
    _loadingMain = true;

    StartCoroutine(CoEnterMain());
}

private IEnumerator CoEnterMain()
{
    if (intro != null)
        intro.OnAboutToEnterMain();

#if ADDRESSABLES
    if (mainSceneIsAddressable)
    {
        Debug.Log("[BootFlow] Checking remaining download size for scene: " + mainAddressableSceneKey);

        // check size còn lại của scene + deps
        var sizeHandle = Addressables.GetDownloadSizeAsync(mainAddressableSceneKey);
        yield return sizeHandle;

        long remainBytes = sizeHandle.Result;
        Addressables.Release(sizeHandle);

        Debug.Log($"[BootFlow] Scene remaining bytes = {remainBytes}");

        // nếu còn -> download trước
        if (remainBytes > 0)
        {
            Debug.Log("[BootFlow] Pre-downloading scene dependencies...");

            var dl = Addressables.DownloadDependenciesAsync(mainAddressableSceneKey, false);

            while (!dl.IsDone)
            {
                float p = dl.PercentComplete;
                if (intro != null)
                    intro.ForceProgress(p); // nếu bạn muốn UI progress

                yield return null;
            }

            if (dl.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("[BootFlow] Scene dependency download FAILED");
                if (intro != null)
                    intro.ShowFatalFail("Scene download failed.");
                yield break;
            }

            Addressables.Release(dl);
        }

        // load scene (giờ chắc chắn từ cache)
        Debug.Log("[BootFlow] Loading main scene from cache...");
        Addressables.LoadSceneAsync(mainAddressableSceneKey, LoadSceneMode.Single, true);
        yield break;
    }
#endif

    // fallback build index
    Debug.Log("[BootFlow] Load main by BuildIndex: " + mainSceneBuildIndex);
    SceneManager.LoadScene(mainSceneBuildIndex, LoadSceneMode.Single);
}

}
