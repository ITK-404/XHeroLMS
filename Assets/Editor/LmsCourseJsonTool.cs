#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class LmsCourseJsonTool : EditorWindow
{
    private string inputPath = "";
    private string outputFileName = "courses.json";

    [MenuItem("Tools/LMS/Import Course JSON")]
    public static void ShowWindow()
    {
        GetWindow<LmsCourseJsonTool>("Import Course JSON");
    }

    void OnGUI()
    {
        GUILayout.Label("LMS Course JSON Importer", EditorStyles.boldLabel);
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        inputPath = EditorGUILayout.TextField("Input JSON Path", inputPath);
        if (GUILayout.Button("...", GUILayout.Width(30)))
        {
            string p = EditorUtility.OpenFilePanel("Chọn file JSON", "", "json");
            if (!string.IsNullOrEmpty(p)) inputPath = p;
        }
        GUILayout.EndHorizontal();

        outputFileName = EditorGUILayout.TextField("Output File Name", outputFileName);

        GUILayout.Space(8);
        if (GUILayout.Button("Convert + Import to StreamingAssets", GUILayout.Height(30)))
        {
            ImportAndConvertJson();
        }

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Tool này sẽ đọc file JSON gốc, chuyển `_id.$oid` -> `_id` và `seo.url` -> `seo`, " +
            "rồi lưu bản sạch vào Assets/StreamingAssets/" + outputFileName,
            MessageType.Info);
    }

    void ImportAndConvertJson()
    {
        if (string.IsNullOrEmpty(inputPath) || !File.Exists(inputPath))
        {
            EditorUtility.DisplayDialog("Lỗi", "Chưa chọn hoặc không tìm thấy file JSON!", "OK");
            return;
        }

        string folderPath = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string outPath = Path.Combine(folderPath, outputFileName);

        try
        {
            string raw = File.ReadAllText(inputPath);

            // Bước 1: replace _id
            raw = Regex.Replace(raw, "\"_id\"\\s*:\\s*\\{\\s*\"\\$oid\"\\s*:\\s*\"([^\"]+)\"\\s*\\}", "\"_id\": \"$1\"");
            // Bước 2: replace seo
            raw = Regex.Replace(raw, "\"seo\"\\s*:\\s*\\{\\s*\"url\"\\s*:\\s*\"([^\"]+)\"\\s*\\}", "\"seo\": \"$1\"");

            // (optional) thêm sceneName placeholder
            if (!Regex.IsMatch(raw, "\"sceneName\""))
            {
                raw = Regex.Replace(raw, "\\{", "{ \"sceneName\": \"<scene_01>\",", RegexOptions.Multiline);
            }

            File.WriteAllText(outPath, raw);
            AssetDatabase.Refresh();

            Debug.Log($"Đã convert + import JSON vào: {outPath}");
            EditorUtility.DisplayDialog("Thành công",
                "Đã chuyển và lưu JSON vào StreamingAssets!\n\n" + outPath,
                "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Import thất bại: " + ex.Message);
            EditorUtility.DisplayDialog("Lỗi", "Ghi file thất bại:\n" + ex.Message, "OK");
        }
    }
}
#endif
