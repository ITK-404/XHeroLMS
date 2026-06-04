public static class StorageServiceFactory
{
    public static IStorageService Create()
    {
#if UNITY_EDITOR
        return new MockStorageService();
#elif UNITY_ANDROID
        return new AndroidStorageService();
#elif UNITY_IOS
        return new IOSStorageService();
#else
        UnityEngine.Debug.LogWarning("[StorageServiceFactory] Platform không được hỗ trợ, dùng Mock.");
        return new MockStorageService();
#endif
    }
}