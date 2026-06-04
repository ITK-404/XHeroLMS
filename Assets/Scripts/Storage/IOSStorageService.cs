using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_IOS
public class IOSStorageService : IStorageService
{
    // Khai báo 2 hàm native viết trong file .mm (Objective-C)
    [DllImport("__Internal")] private static extern float _GetTotalDiskSpaceMB();
    [DllImport("__Internal")] private static extern float _GetFreeDiskSpaceMB();
 
    public bool IsAvailable() => Application.platform == RuntimePlatform.IPhonePlayer;
 
    public StorageInfo GetStorageInfo()
    {
        float totalMB = _GetTotalDiskSpaceMB();
        float freeMB  = _GetFreeDiskSpaceMB();
 
        return new StorageInfo
        {
            TotalMB = totalMB,
            FreeMB  = freeMB,
            UsedMB  = totalMB - freeMB,
        };
    }
}
#endif