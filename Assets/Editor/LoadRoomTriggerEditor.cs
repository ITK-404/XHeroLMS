using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LoadRoomTrigger))]
[CanEditMultipleObjects]
public class LoadRoomTriggerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update(); // Đồng bộ data từ object thật
        // get field
        var loadTypeProp = serializedObject.FindProperty("loadType");
        var scenenameProp = serializedObject.FindProperty("sceneName");
        var courseIdProp  = serializedObject.FindProperty("courseId");
        var isUsingReviewModeProp = serializedObject.FindProperty("isUsingReviewMode");
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
        var returnPointId = serializedObject.FindProperty("returnPoint");
        EditorGUILayout.PropertyField(returnPointId, new GUIContent("Return Point"));
        
        EditorGUILayout.PropertyField(isUsingReviewModeProp, new GUIContent("isUsingReviewModeProp"));
        serializedObject.ApplyModifiedProperties(); // Lưu thay đổi + hỗ trợ Undo
    }
}