using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class SaveManager
{
    private const int MAX_SLOTS = 3;
    private string SaveDir => Application.persistentDataPath + "/Saves";
    private const string SAVE_PREFIX = "save_";
    private readonly object _saveLock = new object();

    public void SaveGameSession(GameSessionData data)
    {
        if (string.IsNullOrEmpty(data.UserID))
            throw new ArgumentException("UserID cannot be null or empty");

        lock (_saveLock)
        {
            Directory.CreateDirectory(SaveDir);

            var files = GetSaveFilesSortedByDate();

            // Có file cùng UserID -> ghi đè
            var existing = files.FirstOrDefault(f => GetAccountId(f.Name) == data.UserID);
            if (existing != null)
            {
                File.WriteAllText(existing.FullName, JsonUtility.ToJson(data));
                return;
            }

            // Full slot -> xoá cũ nhất
            if (files.Length >= MAX_SLOTS)
                File.Delete(files[0].FullName);

            // Tạo mới
            string newPath = Path.Combine(SaveDir, $"{SAVE_PREFIX}{data.UserID}.json");
            File.WriteAllText(newPath, JsonUtility.ToJson(data));
        }
    }

    public List<GameSessionData> LoadAllGameSession()
    {
        if (!Directory.Exists(SaveDir)) return new List<GameSessionData>();

        return GetSaveFilesSortedByDate()
            .Reverse()
            .Select(f => JsonUtility.FromJson<GameSessionData>(File.ReadAllText(f.FullName)))
            .Where(d => d != null && !string.IsNullOrEmpty(d.UserID))
            .ToList();
    }

    private FileInfo[] GetSaveFilesSortedByDate()
    {
        if (!Directory.Exists(SaveDir)) return Array.Empty<FileInfo>();

        return new DirectoryInfo(SaveDir)
            .GetFiles($"{SAVE_PREFIX}*.json")
            .OrderBy(f => f.LastWriteTime)
            .ToArray();
    }

    private string GetAccountId(string fileName) =>
        fileName.Replace(SAVE_PREFIX, "").Replace(".json", "");
}