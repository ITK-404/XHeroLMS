using UnityEngine;

public class HouseHandler : MonoBehaviour
{
    [SerializeField] private OpenDoor[] openDoor;
    [SerializeField] private LoadRoomTrigger[] loadRoomTriggers;

    [Header("Editor")]
    [SerializeField] private string sceneNameToLoad;
    private void Awake()
    {
        if (TokenStore.IsAuthenticated)
        {
            Show();
        }
        else
        {
            Hide();
            LoginController.OnLoginComplete += Show;
        }
    }

    private void OnValidate()
    {
        loadRoomTriggers = GetComponentsInChildren<LoadRoomTrigger>();
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= Show;
    }

    private void Show()
    {
        foreach (var item in openDoor)
        {
            item.TriggerDoorCol.isTrigger = true;
        }
    }

    private void Hide()
    {
        foreach (var item in openDoor)
        {
            item.TriggerDoorCol.isTrigger = false;
        }
    }
    [ContextMenu("SetSceneToRoom")]
    private void SetSceneToRoom()
    {
        foreach (var item in loadRoomTriggers)
        {
            item.sceneName = sceneNameToLoad;
        }
    }
}