using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LoadRoomTrigger : MonoBehaviour
{
    [Header("Scene đích")]
    public string sceneName;

    [Header("Điểm dịch chuyển khi QUAY LẠI scene này")]
    [Tooltip("Kéo 1 GameObject/Empty làm mốc trả về khi scene này được load lại.")]
    public Transform returnPoint;

    [Header("Khôi phục khi scene mở")]
    public bool restoreOnSceneStart = true;   // Bật để tự đặt Player khi scene load
    public bool snapToGround = true;          // Snap nhẹ xuống nền để tránh rơi
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

                PlacePlayer(player, pos + extraOffset, rot);
                _restoredSceneOnce = curScene;
                if (verbose) Debug.Log($"[LoadRoomTrigger] Restored player at {pos} in scene '{curScene}'");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(sceneName)) return;

        if (savePlayerPosition)
        {
            var keyScene = SceneManager.GetActiveScene().name; // key = scene hiện tại
            var savePos = (returnPoint ? returnPoint.position : transform.position) + extraOffset;
            var saveRot = (returnPoint ? returnPoint.rotation : transform.rotation);
            TravelContext.SaveReturnPoint(keyScene, savePos, saveRot);
            if (verbose) Debug.Log($"[LoadRoomTrigger] Save return for '{keyScene}' at {savePos}");
        }

        if (savePlayerPosition)
        {
            StartCoroutine(TryEnterCourse());
        }
        else
        {
            LoadingTransition.Load(sceneName);
        }

    }
    
    private IEnumerator TryEnterCourse()
    {
        LoadingUI.Show();
        SeoResolver.SetSeoCourse(sceneName);
        yield return new WaitForSecondsRealtime(1);
        yield return SeoResolver.LoadPrivateAndFillData();
        
        LoadingUI.Hide();

        if (SeoResolver.IsContainData())
        {
            LoadingTransition.Load(SeoResolver.DefaultScene);
        }
    }

    private void PlacePlayer(GameObject player, Vector3 pos, Quaternion rot)
    {
        var cc = player.GetComponent<CharacterController>();
        var rb = player.GetComponent<Rigidbody>();
        bool hadRB = rb != null;
        bool rbWasKinematic = false;

        if (hadRB)
        {
#if UNITY_2023_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.linearVelocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rbWasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }
        if (cc) cc.enabled = false;

        player.transform.SetPositionAndRotation(pos, rot);

        if (hadRB) rb.isKinematic = rbWasKinematic;
        if (cc) cc.enabled = true;
    }

    public static void ClearSceneRestoreFlag()
    {
        _restoredSceneOnce = null;
    }
}
