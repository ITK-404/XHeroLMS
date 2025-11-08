using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExamExportJson : MonoBehaviour
{
    public string jsonFolderName = "ExamLogs";
    public bool prettyPrintJson = true;
    
    string GetJsonDir()
    {
        var dir = Path.Combine(Application.persistentDataPath, jsonFolderName);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }

    static string MakeSafeFilename(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s;
    }

    static string PrettyJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        var indent = 0;
        var quoted = false;
        var sb = new StringBuilder();

        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];

            switch (ch)
            {
                case '"':
                    sb.Append(ch);
                    bool escaped = false;
                    int j = i;
                    while (j > 0 && json[--j] == '\\') escaped = !escaped;
                    if (!escaped) quoted = !quoted;
                    break;

                case '{':
                case '[':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.Append('\n');
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;

                case '}':
                case ']':
                    if (!quoted)
                    {
                        sb.Append('\n');
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                    }
                    else sb.Append(ch);
                    break;

                case ',':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;

                case ':':
                    sb.Append(quoted ? ":" : ": ");
                    break;

                default:
                    sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }

    public void SaveJsonToFile(string baseName, string json)
    {
        try
        {
            var dir = GetJsonDir();
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var file = MakeSafeFilename($"{baseName}_{stamp}.json");
            var path = Path.Combine(dir, file);

            var data = prettyPrintJson ? PrettyJson(json) : json;
            File.WriteAllText(path, data, new UTF8Encoding(encoderShouldEmitUTF8Identifier:false));

            Debug.Log($"[ExamUI] JSON saved: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExamUI] SaveJsonToFile error: {ex}");
        }
    }
}
