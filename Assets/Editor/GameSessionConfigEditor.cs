using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameSessionConfig))]
public class GameSessionConfigEditor : Editor
{
    private string error = "";

    private void OnEnable()
    {
        error = "";
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        var script = (GameSessionConfig)target;
        serializedObject.ApplyModifiedProperties();
    }
}

// Đặt trong Editor/
public class SaveFileViewer : EditorWindow
{
    private (GameSessionData data, string path)[] _slots = new (GameSessionData, string)[3];

    [MenuItem("Tools/Save File Viewer")]
    public static void Open() => GetWindow<SaveFileViewer>("Save Viewer");

    private void OnEnable() => LoadAllSlots();

    private void LoadAllSlots()
    {
        string dir = Application.persistentDataPath + "/Saves";

        if (!Directory.Exists(dir))
        {
            _slots = new (GameSessionData, string)[3];
            return;
        }

        var files = new DirectoryInfo(dir)
            .GetFiles("save_*.json")
            .OrderByDescending(f => f.LastWriteTime)
            .Take(3)
            .ToArray();

        _slots = new (GameSessionData, string)[3];
        for (int i = 0; i < files.Length; i++)
        {
            var data = JsonUtility.FromJson<GameSessionData>(File.ReadAllText(files[i].FullName));
            if (data != null && !string.IsNullOrEmpty(data.UserID))
                _slots[i] = (data, files[i].FullName);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Save File Viewer", EditorStyles.boldLabel);
        GUILayout.Space(6);

        // Toolbar
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh")) LoadAllSlots();
        if (GUILayout.Button("Open Folder"))
            EditorUtility.RevealInFinder(Application.persistentDataPath + "/Saves");
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 3 slot ngang nhau
        for (int i = 0; i < 3; i++)
            DrawSlot(_slots[i]);
    }

    private void DrawSlot((GameSessionData data, string path) slot)
    {
        EditorGUILayout.BeginVertical("box");

        if (slot.data == null)
        {
            EditorGUILayout.HelpBox("Empty", MessageType.None);
        }
        else
        {
            DrawObject(slot.data);
            
            GUILayout.Space(4);
            
            GUI.color = Color.red;
            if (GUILayout.Button("Delete"))
            {
                if (EditorUtility.DisplayDialog("Delete Save",
                        $"Xoá save {slot.data.UserID}?", "Xoá", "Huỷ"))
                {
                    File.Delete(slot.path);
                    LoadAllSlots();
                }
            }
            GUI.color = Color.white;
        }

        EditorGUILayout.EndVertical();
    }
    
    private void DrawObject(object obj, int indent = 0)
    {
        if (obj == null) return;

        string pad = new string(' ', indent * 12);

        foreach (var field in obj.GetType().GetFields())
        {
            var value = field.GetValue(obj);

            // Nested list
            if (value is IList<GameSessionData> list)
            {
                EditorGUILayout.LabelField($"{pad}{field.Name}", $"[{list.Count} items]");
                foreach (var item in list)
                    DrawObject(item, indent + 1);
            }
            // Nested object (có [Serializable])
            else if (field.FieldType.IsClass && field.FieldType != typeof(string))
            {
                EditorGUILayout.LabelField($"{pad}{field.Name}", EditorStyles.boldLabel);
                DrawObject(value, indent + 1);
            }
            else
            {
                EditorGUILayout.LabelField($"{pad}{field.Name}", value?.ToString() ?? "null");
            }
        }
    }
}