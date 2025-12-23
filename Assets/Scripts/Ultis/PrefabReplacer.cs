using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;

public class PrefabReplacer : EditorWindow
{
    public GameObject prefabToInstantiate;
    public List<GameObject> targetObjects = new List<GameObject>();

    [MenuItem("Tools/Prefab Replacer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabReplacer>("Prefab Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Cấu hình thay thế Prefab", EditorStyles.boldLabel);

        // Ô kéo Prefab mẫu vào
        prefabToInstantiate = (GameObject)EditorGUILayout.ObjectField("Prefab để tạo clone", prefabToInstantiate, typeof(GameObject), false);

        // Hiển thị danh sách các mục tiêu (có thể kéo thả trực tiếp)
        ScriptableObject target = this;
        SerializedObject so = new SerializedObject(target);
        SerializedProperty stringsProperty = so.FindProperty("targetObjects");
        EditorGUILayout.PropertyField(stringsProperty, new GUIContent("Danh sách vật thể đích"), true);
        so.ApplyModifiedProperties();

        GUILayout.Space(10);

        if (GUILayout.Button("Chạy Tool (Tạo Clone)"))
        {
            ReplaceObjects();
        }
    }

    private void ReplaceObjects()
    {
        if (prefabToInstantiate == null)
        {
            Debug.LogError("Vui lòng kéo Prefab mẫu vào!");
            return;
        }

        if (targetObjects == null || targetObjects.Count == 0)
        {
            Debug.LogError("Danh sách vật thể đích đang trống!");
            return;
        }

        // Đăng ký Undo để có thể Ctrl+Z nếu làm sai
        Undo.IncrementCurrentGroup();

        foreach (GameObject obj in targetObjects)
        {
            if (obj == null) continue;

            // Tạo clone từ Prefab tại vị trí và góc quay của vật thể cũ
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToInstantiate);
            newObj.transform.position = obj.transform.position;
            newObj.transform.rotation = obj.transform.rotation;
            newObj.transform.SetParent(obj.transform.parent); // Giữ nguyên cấu trúc phân cấp nếu muốn

            Undo.RegisterCreatedObjectUndo(newObj, "Create Clone");
        }

        Debug.Log($"Đã tạo xong {targetObjects.Count} clone của prefab.");
    }
}
#endif