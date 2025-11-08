using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class LessonSender : MonoBehaviour
{
   [SerializeField] private LessonUI lessonUI;

   

    [Serializable]
    public class Payload
    {
        public string lesson;
        public string lessonType;
        public string progressTime;
    }
    [ContextMenu("Test item")]
    private void Test()
    {
        StartCoroutine(PostResultLesson(lessonUI.lessonID));
    }
    public IEnumerator PostResultLesson(string courseId)
    {
        // URL endpoint (thay bằng API thật của bạn)
        string url = $"https://apis-dev.xheroapp.com/lms/result-lesson/{courseId}";

        // Tạo dữ liệu cần gửi
        Payload data = new Payload
        {
            lesson = courseId,
            lessonType = lessonUI.type,
            progressTime = "5"
        };

        // Chuyển object sang JSON
        string jsonBody = JsonUtility.ToJson(data);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);

        // Tạo request
        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        // request.SetRequestHeader("Content-Type", "application/json");
        // Nếu API yêu cầu token thì thêm:
        request.SetRequestHeader("Authorization", TokenStore.AccessToken);

        // Gửi request
        yield return request.SendWebRequest();

        // Kiểm tra kết quả
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Gửi thành công!");
            Debug.Log("Response: " + request.downloadHandler.text);
        }
        else
        {
            Debug.LogError($"❌ Lỗi: {request.responseCode} - {request.error}");
            Debug.LogError(request.downloadHandler.text);
        }
    }
}