using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class DeleteAccountApi
{
    private readonly string baseUrl;
    private readonly string accessToken;
    private readonly string deleteUserPath;


    public DeleteAccountApi(string deleteUserPath, string baseUrl, string accessToken)
    {
        this.deleteUserPath = deleteUserPath;
        this.baseUrl = baseUrl;
        this.accessToken = accessToken;
    }

    public IEnumerator DeleteAccountRoutine(Action onSuccess, Action<string> onFail)
    {
        string baseUrl = LmsStore.Instance.baseUrl?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            onFail?.Invoke("BaseUrl empty");
            yield break;
        }

        string path = (deleteUserPath ?? "/users").Trim();
        if (!path.StartsWith("/")) path = "/" + path;

        string url = baseUrl + path;

        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Accept", "application/json");

            // Bearer token
            string token = TokenStore.AccessToken?.Trim();
            if (!string.IsNullOrEmpty(token))
            {
                if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = "Bearer " + token;
                www.SetRequestHeader("Authorization", token);
            }

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[PlayerPanelUI] Delete account success: " + www.downloadHandler.text);
                onSuccess?.Invoke();
                yield break;
            }

            long code = www.responseCode;
            string body = www.downloadHandler != null ? www.downloadHandler.text : "";
            string err = $"HTTP {code} | {www.error} | {body}";

            onFail?.Invoke(err);
        }
    }
}