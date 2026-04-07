using System.Collections;
using UnityEngine;
using Firebase;

public class FirebaseDebugInfo : MonoBehaviour
{
    private IEnumerator Start()
    {
        Debug.Log("========== FIREBASE DEBUG START ==========");
        Debug.Log("[DEBUG] Application.identifier: " + Application.identifier);

        float timeout = 10f;
        float elapsed = 0f;

        while (FirebaseApp.DefaultInstance == null && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (FirebaseApp.DefaultInstance == null)
        {
            Debug.LogError("[DEBUG] FirebaseApp.DefaultInstance is still NULL after waiting.");
            yield break;
        }

        FirebaseApp app = FirebaseApp.DefaultInstance;

        Debug.Log("[DEBUG] Firebase App Name: " + app.Name);
        Debug.Log("[DEBUG] Project ID: " + app.Options.ProjectId);
        Debug.Log("[DEBUG] App ID: " + app.Options.AppId);
        Debug.Log("[DEBUG] API Key: " + app.Options.ApiKey);
        Debug.Log("========== FIREBASE DEBUG END ==========");
    }
}