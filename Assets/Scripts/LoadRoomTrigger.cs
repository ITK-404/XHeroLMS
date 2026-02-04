using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class LoadRoomTrigger : MonoBehaviour
{
    [Header("Scene đích")]
    public string sceneName;

    [Header("Điểm dịch chuyển khi QUAY LẠI scene này")]
    [Tooltip("Kéo 1 GameObject/Empty làm mốc trả về khi scene này được load lại.")]
    public Transform returnPoint;

    [Header("Khôi phục khi scene mở")]
    public bool restoreOnSceneStart = true; // Bật để tự đặt Player khi scene load

    public bool snapToGround = true; // Snap nhẹ xuống nền để tránh rơi
    public Vector3 extraOffset = new Vector3(0f, 0.03f, 0f); // offset nhỏ khi đặt

    [Header("Debug")]
    public bool verbose = false;

    public bool savePlayerPosition = false;

    // Chặn đặt trùng nhiều lần trong cùng 1 scene (nếu có nhiều cổng)
    private static string _restoredSceneOnce = null;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void Start()
    {
        if (!restoreOnSceneStart) return;

        var curScene = SceneManager.GetActiveScene().name;
        if (_restoredSceneOnce == curScene) return;

        if (TravelContext.TryGetReturnPoint(curScene, out var pos, out var rot))
        {
            var player = GameObject.FindWithTag("Player");
            if (player)
            {
                // Snap nhẹ xuống mặt đất nếu cần
                if (snapToGround && Physics.Raycast(pos + Vector3.up * 1.5f, Vector3.down, out var hit, 3f))
                    pos.y = hit.point.y + 0.02f;

                PlayerLocator.PlacePlayer(player, pos + extraOffset, rot);
                _restoredSceneOnce = curScene;
                if (verbose) Debug.Log($"[LoadRoomTrigger] Restored player at {pos} in scene '{curScene}'");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isLoading) return;
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!TokenStore.IsAuthenticated && savePlayerPosition)
        {
            return;
        }
        if (savePlayerPosition)
        {

            StartCoroutine(TryEnterCourse());
        }
        else
        {
            // LoadingTransition.Load(sceneName);
            StartCoroutine(CoLoadSceneSmart(sceneName));
        }

        var keyScene = SceneManager.GetActiveScene().name; // key = scene hiện tại
        var savePos = (returnPoint ? returnPoint.position : transform.position) + extraOffset;
        var saveRot = (returnPoint ? returnPoint.rotation : transform.rotation);
        if (verbose) Debug.Log($"[LoadRoomTrigger] Save return for '{keyScene}' at {savePos}");

        TravelContext.SaveReturnPoint(keyScene, savePos, saveRot);
    }

    private bool isLoading = false;
    private IEnumerator TryEnterCourse()
    {
        isLoading = true;
        LoadingUI.Show(
                timeoutSeconds: 60f,
                timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
                timeoutHeader: "Lỗi Mạng"
            );
        SeoResolver.SetSeoCourse(sceneName);
        yield return new WaitForSecondsRealtime(1);
        yield return SeoResolver.LoadPrivateAndFillData();

        LoadingUI.Hide();

        if (SeoResolver.IsContainData())
        {
            Debug.Log("Đã tìm thấy seo URL để load");
            // LoadingTransition.Load(sceneName);
            StartCoroutine(CoLoadSceneSmart(sceneName));
        }
        else
        {
            Debug.Log("Không tìm thấy seo URL để load");
        }

        isLoading = false;
    }


    public static void ClearSceneRestoreFlag()
    {
        _restoredSceneOnce = null;
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
        LoadingTransition.Load(targetScene);
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