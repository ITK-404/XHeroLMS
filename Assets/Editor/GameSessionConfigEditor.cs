using System;
using System.IO;
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
        var fileNameProp = serializedObject.FindProperty("saveFileName");
        if (GUILayout.Button("Load file save",GUILayout.Height(30)))
        {
            
            var path = script.BuildSavePath();
            EditorGUILayout.HelpBox("Thử nghiệm: ",MessageType.None);
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                script.previewData = JsonUtility.FromJson<GameSessionData>(json);
                error = "";
            }
            else
            {
                error = $"File có tên {fileNameProp.stringValue} không tồn tại";
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Height(30)))
        {
            script.previewData = new();
            error = "";
        }

        if (GUILayout.Button("Save", GUILayout.Height(30)))
        {
            if (script.previewData != null)
            {
                var path = script.BuildSavePath();
                var data = JsonUtility.ToJson(script.previewData);
                File.WriteAllTextAsync(path, data);
            }
        }

        if (!string.IsNullOrEmpty(error))
        {
            EditorGUILayout.HelpBox($"Có lỗi {error}",MessageType.Error);
            
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}