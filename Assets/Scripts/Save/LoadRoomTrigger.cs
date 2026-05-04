using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;


public class LoadRoomTrigger : MonoBehaviour
{
    [Header("Scene đích")] [SceneDropdown] public string sceneName;

    [SceneSeoDropdown] public string courseId;

    [Header("Điểm dịch chuyển khi QUAY LẠI scene này")]
    [Tooltip("Kéo 1 GameObject/Empty làm mốc trả về khi scene này được load lại.")]
    public Transform returnPoint;

    [Header("Khôi phục khi scene mở")] public bool snapToGround = true; // Snap nhẹ xuống nền để tránh rơi
    public Vector3 extraOffset = new Vector3(0f, 0.03f, 0f); // offset nhỏ khi đặt

    [Header("Debug")] public bool verbose = false;

    private static bool isEnter = false;

    [FormerlySerializedAs("savePlayerPosition")]
    public bool loadByCourse = false;

    public bool isUsingReviewMode = false;
    public enum LoadType
    {
        Lock = 0,
        Course = 1,
        Scene = 2,
        Previous = 3
    }

    [SerializeField] private LoadType loadType = LoadType.Scene;
    // Chặn đặt trùng nhiều lần trong cùng 1 scene (nếu có nhiều cổng)

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        isEnter = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        #if UNITY_EDITOR || PLATFORM_IOS
        if (AppDataGlobal.isInReviewMode && isUsingReviewMode)
        {
            return;
        }
        #endif
        if (isEnter) return;
        if (isLoading) return;
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!TokenStore.IsAuthenticated && loadByCourse)
        {
            return;
        }

        isEnter = true;
        LoadByType(loadType);
    }

    private void LoadByType(LoadType currentType)
    {
        switch (currentType)
        {
            case LoadType.Lock:
                break;
            case LoadType.Course:
                SavePositionToLoad();
                StartCoroutine(TryEnterCourse());
                break;
            case LoadType.Scene:
                SavePositionToLoad();
                LoadingTransition.Load_Scene(sceneName);
                break;
            case LoadType.Previous:
                LoadingTransition.LoadPreviousSceneOrDefault();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SavePositionToLoad()
    {
        Vector3 spawnPosition = returnPoint.transform.position + extraOffset;
        Quaternion rotation = returnPoint.transform.rotation;
        LoadingTransition.SavePosition(spawnPosition, rotation);
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
            SavePositionToLoad();
            LoadingTransition.Load_Scene(sceneName);
        }
        else
        {
            Debug.Log("Không tìm thấy seo URL để load");
        }

        isLoading = false;
    }
}