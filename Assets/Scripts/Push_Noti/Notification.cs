using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;

public class Notification : MonoBehaviour
{
    private void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("[FCM] Firebase init failed: " + task.Result);
                return;
            }

            Debug.Log("[FCM] Firebase initialized");

            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

            FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseMessaging.MessageReceived += OnMessageReceived;

            FirebaseMessaging.GetTokenAsync().ContinueWithOnMainThread(tokenTask =>
            {
                if (tokenTask.IsCompleted && !tokenTask.IsFaulted)
                {
                    Debug.Log("[FCM] GetTokenAsync: " + tokenTask.Result);
                }
                else
                {
                    Debug.LogError("[FCM] GetTokenAsync failed: " + tokenTask.Exception);
                }
            });
        });
    }

    private void OnDestroy()
    {
        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
    }

    public void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log("[FCM] TokenReceived: " + token.Token);
    }

    public void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("[FCM] Message from: " + e.Message.From);
    }
}