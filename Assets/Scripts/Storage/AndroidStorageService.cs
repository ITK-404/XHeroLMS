#if UNITY_ANDROID
using UnityEngine;
 
public class AndroidStorageService : IStorageService
{
    public bool IsAvailable() => Application.platform == RuntimePlatform.Android;
 
    public StorageInfo GetStorageInfo()
    {
        // StatFs đọc thông tin filesystem từ đường dẫn ứng dụng
        using var statFs = new AndroidJavaObject(
            "android.os.StatFs",
            Application.persistentDataPath
        );
 
        long blockSize   = statFs.Call<long>("getBlockSizeLong");
        long totalBlocks = statFs.Call<long>("getBlockCountLong");
        long freeBlocks  = statFs.Call<long>("getAvailableBlocksLong");
 
        float totalMB = blockSize * totalBlocks  / (1024f * 1024f);
        float freeMB  = blockSize * freeBlocks   / (1024f * 1024f);
 
        return new StorageInfo
        {
            TotalMB = totalMB,
            FreeMB  = freeMB,
            UsedMB  = totalMB - freeMB,
        };
    }
}
#endif
