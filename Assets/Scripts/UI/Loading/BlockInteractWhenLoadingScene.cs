using UnityEngine;

public class BlockInteractWhenLoadingScene : MonoBehaviour
{
    private LoadingScreenController loadingController;
    
    
    private void Awake()
    {
        loadingController = GetComponent<LoadingScreenController>();
        loadingController.OnStartLoadingEvent += LoadingControllerOnOnStartLoadingEvent;
        loadingController.OnEndLoadingEvent += LoadingControllerOnOnEndLoadingEvent;
    }
    
    private void OnDestroy()
    {
        loadingController.OnStartLoadingEvent -= LoadingControllerOnOnStartLoadingEvent;
        loadingController.OnEndLoadingEvent -= LoadingControllerOnOnEndLoadingEvent;
    }

    private void LoadingControllerOnOnStartLoadingEvent()
    {
        GameplayLock.Lock(GameplayLockReason.Loading,GameplayLockTarget.BookInteract);
    }
    
    private void LoadingControllerOnOnEndLoadingEvent()
    {
        GameplayLock.Unlock(GameplayLockReason.Loading);
    }


}