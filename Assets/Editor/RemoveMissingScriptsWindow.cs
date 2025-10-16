// File: Assets/Editor/RemoveMissingScriptsWindow.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Object = UnityEngine.Object;

public class RemoveMissingScriptsWindow : EditorWindow
{
    bool scanEntireScene = false;
    bool includeInactive = true;
    int removedCount = 0;
    Vector2 scroll;

    [MenuItem("Tools/Remove Missing Scripts")]
    public static void OpenWindow()
    {
        GetWindow<RemoveMissingScriptsWindow>("Remove Missing Scripts");
    }

    void OnGUI()
    {
        GUILayout.Label("Remove Missing Scripts Tool (editor)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        scanEntireScene = EditorGUILayout.ToggleLeft("Scan entire active scene (otherwise uses Selection)", scanEntireScene);
        includeInactive = EditorGUILayout.ToggleLeft("Include inactive GameObjects", includeInactive);

        EditorGUILayout.Space();
        if (GUILayout.Button("Scan and Remove"))
        {
            removedCount = 0;
            if (scanEntireScene)
            {
                removedCount = ProcessScene();
            }
            else
            {
                removedCount = ProcessSelection();
            }

            // mark scene dirty so user can save after changes
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Done", $"Removed {removedCount} missing script entries.", "OK");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Last run results:", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(100));
        EditorGUILayout.LabelField($"Removed missing script entries: {removedCount}");
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This removes missing script entries from GameObjects' serialized component lists. It does not try to infer or recover deleted code. Always keep a backup or commit before running if you care about your job security.", MessageType.Info);
    }

    int ProcessSelection()
    {
        GameObject[] objs = Selection.gameObjects;
        if (objs == null || objs.Length == 0)
        {
            EditorUtility.DisplayDialog("Nothing selected", "Select one or more GameObjects or enable 'Scan entire scene'.", "OK");
            return 0;
        }

        int total = 0;
        foreach (var go in objs)
        {
            total += ProcessGameObjectRecursive(go);
        }
        return total;
    }

    int ProcessScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.isLoaded)
        {
            Debug.LogWarning("No active loaded scene.");
            return 0;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        int total = 0;
        foreach (var r in roots)
        {
            total += ProcessGameObjectRecursive(r);
        }
        return total;
    }

    int ProcessGameObjectRecursive(GameObject go)
    {
        int removed = 0;
        if (go == null) return 0;

        if (includeInactive || go.activeInHierarchy)
        {
            removed += RemoveMissingScriptsFromGameObject(go);
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            removed += ProcessGameObjectRecursive(go.transform.GetChild(i).gameObject);
        }

        return removed;
    }

    int RemoveMissingScriptsFromGameObject(GameObject go)
    {
        // register undo so user can revert
        Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");

        SerializedObject so = new SerializedObject(go);
        SerializedProperty prop = so.FindProperty("m_Component");
        if (prop == null || !prop.isArray) return 0;

        int removed = 0;
        // iterate backwards when removing array elements
        for (int i = prop.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty compRef = prop.GetArrayElementAtIndex(i).FindPropertyRelative("component");
            Object target = compRef.objectReferenceValue;
            if (target == null)
            {
                // delete the array element (removes the missing component slot)
                prop.DeleteArrayElementAtIndex(i);
                removed++;
            }
        }

        if (removed > 0)
        {
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(go);
        }

        return removed;
    }
}
