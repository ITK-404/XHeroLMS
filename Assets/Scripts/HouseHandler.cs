using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;

public class HouseHandler : MonoBehaviour
{
    [SerializeField] private OpenDoor[] openDoor;
    [SerializeField] private LoadRoomTrigger[] loadRoomTriggers;

    [Header("Editor")]
    [SerializeField] private string sceneNameToLoad;

    [SerializeField] private bool savePlayerPosition;

    [SerializeField] private Transform standPoint;
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

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= Show;
    }

    private void OnValidate()
    {
        loadRoomTriggers = GetComponentsInChildren<LoadRoomTrigger>();
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
#if UNITY_EDITOR
        foreach (var item in loadRoomTriggers)
        {
            // Record for Undo so user can revert
            Undo.RecordObject(item, "Set Scene To Room");
            item.sceneName = sceneNameToLoad;
            item.savePlayerPosition = savePlayerPosition;
            // Mark the component dirty so Unity serializes the change
            EditorUtility.SetDirty(item);
        }

        // Mark scene dirty so the Editor knows it needs saving
        EditorSceneManager.MarkSceneDirty(gameObject.scene);

#endif
    }

    public Vector3 GetStandPoint()
    {
        return standPoint.position;
    }
}