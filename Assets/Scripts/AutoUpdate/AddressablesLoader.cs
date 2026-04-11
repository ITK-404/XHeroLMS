using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class AddressablesLoader
{
    private static UniTask _initTask;
    private static bool _initialized;

    public static UniTask EnsureInitialized()
    {
        if (_initialized) return UniTask.CompletedTask;

        if (_initTask.Status == UniTaskStatus.Pending)
            return _initTask;

        _initTask = Init();
        return _initTask;
    }

    private static async UniTask Init()
    {
        await Addressables.InitializeAsync().Task;
        _initialized = true;
    }
}
