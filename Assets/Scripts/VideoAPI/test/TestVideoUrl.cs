using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class TestVideoUrl : MonoBehaviour
{
    public string testUrl = "https://video.xheroapp.com/play-video?fileId=1JJBoPkOWZEzS8p2rCcUUlaWDkXpU1uJJ";

    IEnumerator Start()
    {
        using (var req = UnityWebRequest.Head(testUrl))
        {
            yield return req.SendWebRequest();
            Debug.Log("[TestVideoUrl]Result = " + req.result);
            Debug.Log("TestVideoUrl]Code = " + req.responseCode);
            Debug.Log("TestVideoUrl]Content-Type = " + req.GetResponseHeader("Content-Type"));
            Debug.Log("TestVideoUrl]Location = " + req.GetResponseHeader("Location"));
        }
    }
}
