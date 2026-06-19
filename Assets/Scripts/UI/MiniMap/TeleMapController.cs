using Pathfinding;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class TeleMapController : MonoBehaviour
{
    [Header("Refs")]
    public Camera mapCamera;        // camera bản đồ (top-down)
    public Camera playerCamera;     // camera người chơi (optional)
    public Transform player;        // transform người chơi
    private CinemachineBrain brain;
    [Header("Raycast")]
    public LayerMask raycastMask = ~0;

    [Header("Placement")]
    public bool preservePlayerY = false;
    public float yLift = 0.02f;

    // state
    public static bool _mapActive;
    float _savedPlayerY;
    float _playerCamDepth;

    CursorGameManager cursorMgr;

    void Awake()
    {
        brain = playerCamera ? playerCamera.GetComponent<CinemachineBrain>() : null;
        if (playerCamera) _playerCamDepth = playerCamera.depth;
        if (mapCamera) mapCamera.gameObject.SetActive(false);
        
        cursorMgr = FindAnyObjectByType<CursorGameManager>();
    }

    private bool IsBlendingCamera()
    {
        return brain != null && brain.IsBlending;
    }

    void Update()
    {
        if (AddressableAdditiveSceneLoader.IsAnyBoxLoadVisible)
        {
            if (_mapActive)
                ToggleMap(false);

            return;
        }

        if (IsBlendingCamera() || BuildingCameraManager.Instance.IsFocus())
        {
            return;
        }
        if (InputBlocker.IsBlocked())
        return;
        // Nhấn phím M để bật/tắt map
        if (Input.GetKeyDown(KeyCode.M)) ToggleMap();

        // Cuộn chuột về sau (scroll down) để mở map
        if (Input.mouseScrollDelta.y < -0.5f && !_mapActive)
            ToggleMap(true);

        if (!_mapActive) return;

        if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetMouseButtonDown(0)) TryTeleportAtMouse();
        if (Input.GetKeyDown(KeyCode.Escape)) ToggleMap(false);
    }

    public void ToggleMap(bool? force = null)
    {
        if (!mapCamera || !player)
        {
            Debug.LogWarning("[TeleMap] Thiếu mapCamera hoặc player.");
            return;
        }

        bool targetActive = force.HasValue ? force.Value : !_mapActive;

        if (targetActive && AddressableAdditiveSceneLoader.IsAnyBoxLoadVisible)
        {
            Debug.Log("[TeleMap] Map is blocked while boxLoad is visible.");
            return;
        }

        _mapActive = targetActive;
        mapCamera.gameObject.SetActive(_mapActive);

        if (_mapActive)
        {
            _savedPlayerY = player.position.y;
            if (playerCamera) playerCamera.depth = _playerCamDepth - 10f;
            mapCamera.depth = _playerCamDepth + 10f;
            if (cursorMgr) cursorMgr.SetUIOpen(true);
        }
        else
        {
            if (playerCamera) playerCamera.depth = _playerCamDepth;
            mapCamera.depth = _playerCamDepth - 10f;
            if (cursorMgr) cursorMgr.SetUIOpen(false);
        }
    }

void TryTeleportAtMouse()
{
    if (!mapCamera) return;

    Ray ray = mapCamera.ScreenPointToRay(Input.mousePosition);

    RaycastHit[] hits = Physics.RaycastAll(
        ray, 10000f, raycastMask, QueryTriggerInteraction.Collide);

    if (hits == null || hits.Length == 0) return;

    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    foreach (var h in hits)
    {
        var col = h.collider;
        if (!col) continue;
        
        if (IsTaggedItemsRecursive(col.transform))
        {
            return;
        }

        if (col.CompareTag("Ground"))
        {
            Vector3 target = h.point;
            target.y = preservePlayerY ? (_savedPlayerY + yLift) : (target.y + yLift);
            TeleportPlayer(target);
            ToggleMap(false);
            return;
        }
    }

    return;
}

bool IsTaggedItemsRecursive(Transform t)
{
    while (t != null)
    {
        if (t.CompareTag("Items")) return true;
        t = t.parent;
    }
    return false;
}

    void TeleportPlayer(Vector3 targetPos)
    {
        if (!player) return;
        player.GetComponent<PointClickSystem>().TeleportDelay(targetPos);
        //        var cc = player.GetComponent<CharacterController>();
        //        var AIPath = player.GetComponent<IAstarAI>();
        //        AIPath.destination = targetPos;
        //        if (cc)
        //        {
        //            bool was = cc.enabled;
        //            cc.enabled = false;
        //            player.position = targetPos;
        //            cc.enabled = was;
        //            return;
        //        }

        //        var rb = player.GetComponent<Rigidbody>();
        //        if (rb && !rb.isKinematic)
        //        {
        //#if UNITY_6000_0_OR_NEWER
        //            rb.linearVelocity = Vector3.zero;
        //#else
        //            rb.velocity = Vector3.zero;
        //#endif
        //            rb.angularVelocity = Vector3.zero;
        //            rb.MovePosition(targetPos);
        //            return;
        //        }

        player.position = targetPos;
    }
}
