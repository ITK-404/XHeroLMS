using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class IOSReviewManager
{
    [Serializable]
    public class AppReviewConfig
    {
        public bool isInReview;
        public string versionInReview;
    }

    [Serializable]
    public class ReviewConfigData
    {
        public AppReviewConfig xheroApp;
        public AppReviewConfig lmsApp;
    }

    // --- API response mapping (match JSON exactly) ---
    [Serializable]
    private class ApiResponse
    {
        public bool status;
        public ApiData data;
    }

    [Serializable]
    private class ApiData
    {
        public string _id;
        public string key;
        public ReviewConfigData data; // <-- config thật nằm ở đây
        public int __v;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        // Fire-and-forget ở startup (không block load scene)
        _ = CheckIOSReviewStatusAsync();
    }

    public static async Task CheckIOSReviewStatusAsync()
    {
        try
        {
            ReviewConfigData config = await FetchIOSReviewStatusAsync();
            if (config?.xheroApp == null)
            {
                AppDataGlobal.isInReviewMode = false;
                Debug.Log("Review Mode: INACTIVE (no config / missing xheroApp)");
                return;
            }

            bool isInReview = config.lmsApp.isInReview;
            string versionInReview = config.lmsApp.versionInReview;

            if (isInReview && !string.IsNullOrEmpty(versionInReview))
            {
                string appVersion = Application.version;
                Debug.Log($"Check Review Mode: API(v{versionInReview}) vs App(v{appVersion})");

                if (appVersion == versionInReview)
                {
                    AppDataGlobal.isInReviewMode = true;
                    Debug.Log("Review Mode: ACTIVE");
                    return;
                }
            }

            AppDataGlobal.isInReviewMode = false;
            Debug.Log("Review Mode: INACTIVE");
        }
        catch (Exception e)
        {
            Debug.LogError($"Check Review Mode Error: {e}");
            AppDataGlobal.isInReviewMode = false;
        }
    }

    public static async Task<ReviewConfigData> FetchIOSReviewStatusAsync()
    {
        const string url = "https://apis-dev.xheroapp.com/config?key=ios-in-review"; // fix API dev 

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;

            var operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
Debug.Log("Review Mode: "+jsonResponse);
                var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

                // Validate minimal fields
                if (response == null || response.data == null)
                {
                    Debug.LogError("Parse failed: response or response.data is null");
                    return null;
                }

                // Optional: log status
                Debug.Log($"API status: {response.status}, key: {response.data.key}");

                return response.data.data; // <-- trả về ReviewConfigData
            }

            Debug.LogError($"Request failed: {request.error} | Code: {request.responseCode} | URL: {url}");
            return null;
        }
    }
}

// Global data class (đặt ở file riêng)

public static class AppDataGlobal
{
    private static bool _isInReviewMode = true;
    public static event Action<bool> OnReviewModeChanged;

    public static bool isInReviewMode
    {
        get => _isInReviewMode;
        set
        {
            if (_isInReviewMode == value) return;
            _isInReviewMode = value;
            OnReviewModeChanged?.Invoke(_isInReviewMode);
        }
    }
}
