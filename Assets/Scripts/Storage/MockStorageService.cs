public class MockStorageService : IStorageService
{
    private readonly StorageInfo _mockData;
 
    public MockStorageService(float totalMB = 4096f, float usedMB = 1300f)
    {
        _mockData = new StorageInfo
        {
            TotalMB = totalMB,
            UsedMB  = usedMB,
            FreeMB  = totalMB - usedMB,
        };
    }
 
    public bool IsAvailable() => true;
    public StorageInfo GetStorageInfo() => _mockData;
}