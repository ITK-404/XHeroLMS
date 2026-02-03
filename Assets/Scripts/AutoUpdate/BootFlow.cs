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

        if (intro != null)
            intro.OnAboutToEnterMain(); // optional hook

        if (mainSceneIsAddressable)
        {
#if ADDRESSABLES
            Debug.Log("[BootFlow] EnterMain after Preload DONE. Load main (Addressables): " + mainAddressableSceneKey);

            // Lưu ý:
            // - Nếu scene main + deps đã thuộc label preload (vd: "cloud") thì lúc này load sẽ chủ yếu từ cache.
            // - Nếu scene main KHÔNG nằm trong label preload, Addressables vẫn có thể phát sinh download.
            Addressables.LoadSceneAsync(mainAddressableSceneKey, LoadSceneMode.Single, activateOnLoad: true);
#else
            Debug.LogError("[BootFlow] ADDRESSABLES define OFF but mainSceneIsAddressable=true");
            if (intro != null) intro.ShowFatalFail("ADDRESSABLES define OFF, cannot load addressable scene.");
#endif
        }
        else
        {
            Debug.Log("[BootFlow] EnterMain after Preload DONE. Load main (BuildIndex): " + mainSceneBuildIndex);
            SceneManager.LoadScene(mainSceneBuildIndex, LoadSceneMode.Single);
        }
    }
}
