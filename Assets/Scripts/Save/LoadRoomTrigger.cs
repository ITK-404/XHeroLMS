using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;


public class LoadRoomTrigger : MonoBehaviour
{
    [Header("Scene đích")]
    [SceneDropdown] public string sceneName;

    [SceneSeoDropdown] public string courseId;
    [Header("Điểm dịch chuyển khi QUAY LẠI scene này")]
    [Tooltip("Kéo 1 GameObject/Empty làm mốc trả về khi scene này được load lại.")]
    public Transform returnPoint;

    [Header("Khôi phục khi scene mở")]
    public bool snapToGround = true; // Snap nhẹ xuống nền để tránh rơi
    public Vector3 extraOffset = new Vector3(0f, 0.03f, 0f); // offset nhỏ khi đặt

    [Header("Debug")]
    public bool verbose = false;

    [FormerlySerializedAs("savePlayerPosition")] public bool loadByCourse = false;
    public enum LoadType
    {
        Course,
        Scene,
        Previous
    }

    [SerializeField] private LoadType loadType = LoadType.Scene;
    // Chặn đặt trùng nhiều lần trong cùng 1 scene (nếu có nhiều cổng)

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isLoading) return;
        if (!other.CompareTag("Player")) return;
        if (string.IsNullOrEmpty(sceneName)) return;
        if (!TokenStore.IsAuthenticated && loadByCourse)
        {
            return;
        }

        LoadByType(loadType);
    }

    private void LoadByType(LoadType currentType)
    {
        switch (currentType)
        {
            case LoadType.Course:
                StartCoroutine(TryEnterCourse());
                break;
            case LoadType.Scene:
                LoadingTransition.Load_Scene(sceneName);
                break;
            case LoadType.Previous:
                LoadingTransition.LoadPreviousSceneOrDefault();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
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
            LoadingTransition.Load_Scene(sceneName);
        }
        else
        {
            Debug.Log("Không tìm thấy seo URL để load");
        }

        isLoading = false;
    }
}
[CustomEditor(typeof(LoadRoomTrigger))]
public class LoadRoomTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // Đồng bộ data từ object thật
        // get field
        var loadTypeProp = serializedObject.FindProperty("loadType");
        var scenenameProp = serializedObject.FindProperty("sceneName");
        var courseIdProp  = serializedObject.FindProperty("courseId");
        // ... vẽ fields ở đây
        var style = new GUIStyle(EditorStyles.helpBox);
        style.wordWrap = true;
        EditorGUILayout.LabelField("Note", "Tuỳ chỉnh field này để thiết lập cách logic được chạy",style);
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(loadTypeProp);
        var currentType = (LoadRoomTrigger.LoadType)loadTypeProp.enumValueIndex;
        switch (currentType)
        {
            case LoadRoomTrigger.LoadType.Course:
                EditorGUILayout.PropertyField(courseIdProp, new GUIContent("Course ID"));
                break;
            case LoadRoomTrigger.LoadType.Scene:
                EditorGUILayout.PropertyField(scenenameProp, new GUIContent("Scene Name"));
                break;
            case LoadRoomTrigger.LoadType.Previous:
                EditorGUILayout.HelpBox("Sẽ load scene trước đó", MessageType.Info);
                break;
        }
        
        serializedObject.ApplyModifiedProperties(); // Lưu thay đổi + hỗ trợ Undo
    }
}