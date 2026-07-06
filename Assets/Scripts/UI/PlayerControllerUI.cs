using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public class PlayerControllerUI : MonoBehaviour
{
    private const string CanvasLoginAddress = "Assets/Shaders/Prefabs_UI/Login/CanvasLogin.prefab";

    [SerializeField] public Button loginBtn;

    public Action OnLoginBtnClicked;

    private Coroutine loadCanvasLoginRoutine;

#if ADDRESSABLES
    private static AsyncOperationHandle<GameObject>? canvasLoginPrefabHandle;
#endif

    private void Awake()
    {
        if (loginBtn != null)
            loginBtn.onClick.AddListener(ClickLoginBtn);
    }

    private void OnDestroy()
    {
        if (loginBtn != null)
            loginBtn.onClick.RemoveListener(ClickLoginBtn);
    }

    private void ClickLoginBtn()
    {
        int listenerCount = OnLoginBtnClicked != null ? OnLoginBtnClicked.GetInvocationList().Length : 0;
        Debug.Log("[PlayerControllerUI] Login button clicked. listenerCount=" + listenerCount);

        if (OnLoginBtnClicked != null)
        {
            OnLoginBtnClicked.Invoke();
            return;
        }

        if (TryOpenExistingLoginPanel())
            return;

        if (loadCanvasLoginRoutine == null)
            loadCanvasLoginRoutine = StartCoroutine(LoadCanvasLoginAndOpen());
    }

    private static bool TryOpenExistingLoginPanel()
    {
        OpenClosePanel panel = UnityEngine.Object.FindAnyObjectByType<OpenClosePanel>(FindObjectsInactive.Include);
        if (panel == null)
            return false;

        panel.OpenFromExternalLoginButton();
        return true;
    }

    private IEnumerator LoadCanvasLoginAndOpen()
    {
#if ADDRESSABLES
        Debug.LogWarning("[PlayerControllerUI] OpenClosePanel missing in scene. Loading CanvasLogin from Addressables.");

        if (!canvasLoginPrefabHandle.HasValue || !canvasLoginPrefabHandle.Value.IsValid())
            canvasLoginPrefabHandle = Addressables.LoadAssetAsync<GameObject>(CanvasLoginAddress);

        AsyncOperationHandle<GameObject> handle = canvasLoginPrefabHandle.Value;
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError("[PlayerControllerUI] Cannot load CanvasLogin prefab: " + CanvasLoginAddress);
            loadCanvasLoginRoutine = null;
            yield break;
        }

        GameObject instance = Instantiate(handle.Result);
        instance.name = "CanvasLogin";

        yield return null;

        if (!TryOpenExistingLoginPanel())
            Debug.LogError("[PlayerControllerUI] CanvasLogin loaded but OpenClosePanel is still missing.");
#else
        Debug.LogError("[PlayerControllerUI] OpenClosePanel missing and ADDRESSABLES define is off.");
        loadCanvasLoginRoutine = null;
        yield break;
#endif

        loadCanvasLoginRoutine = null;
    }
}
