using UnityEngine;

public interface IStorageService
{
    StorageInfo GetStorageInfo();
    bool IsAvailable();
}

public class StorageData
{
    public float TotalStorage;
    public float UsedStorage;
    public float UnusedStorage;
}

public class StorageInfo
{
    public float TotalMB { get; set; }
    public float UsedMB { get; set; }
    public float FreeMB { get; set; }

    // Computed — không cần set thủ công
    public float UsedPercent => TotalMB > 0 ? UsedMB / TotalMB * 100f : 0f;
}