using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class IOSReviewManager : MonoBehaviour
{
    [Serializable]
    public class XheroConfig
    {
        public bool isInReview;
        public string versionInReview;
    }

    [Serializable]
    public class ReviewConfigData
    {
        public XheroConfig xheroApp;
    }

    public async void CheckIOSReviewStatus()
    {
        try
        {
            var data = await FetchIOSReviewStatus();
            Debug.Log($"Review Config: {data}");

            if (data != null)
            {
                var reviewConfig = JsonUtility.FromJson<ReviewConfigData>(data);
                
                if (reviewConfig?.xheroApp != null)
                {
                    bool isInReview = reviewConfig.xheroApp.isInReview;
                    string versionInReview = reviewConfig.xheroApp.versionInReview;

                 
                       if (isInReview && !string.IsNullOrEmpty(versionInReview))
                                        {    string appVersion = Application.version;
                        Debug.Log($"Check Review Mode: API(v{versionInReview}) vs App(v{appVersion})");

                        if (appVersion == versionInReview)
                        {
                            AppDataGlobal.isInReviewMode = true;
                            Debug.Log("Review Mode: ACTIVE");
                            return;
                        }
                    }
                }
            }

            AppDataGlobal.isInReviewMode = false;
            Debug.Log("Review Mode: INACTIVE");
        }
        catch (Exception e)
        {
            Debug.LogError($"Check Review Mode Error: {e.Message}");
            AppDataGlobal.isInReviewMode = false;
        }
    }

    public async System.Threading.Tasks.Task<string> FetchIOSReviewStatus()
    {
        string url = "YOUR_BASE_URL/config?key=ios-in-review";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Add headers if needed
            // request.SetRequestHeader("Authorization", "Bearer YOUR_TOKEN");
            
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                await System.Threading.Tasks.Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                
                // Parse nested response structure
                var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);
                
                return JsonUtility.ToJson(response.data.data);
            }
            else
            {
                Debug.LogError($"Request failed: {request.error}");
                return null;
            }
        }
    }

    [Serializable]
    private class ApiResponse
    {
        public DataWrapper data;
    }

    [Serializable]
    private class DataWrapper
    {
        public ReviewConfigData data;
    }
}

// Global data class (đặt ở file riêng)
public static class AppDataGlobal
{
    public static bool isInReviewMode = false;
}