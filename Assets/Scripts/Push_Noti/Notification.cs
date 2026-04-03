using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;
using System.Threading.Tasks;

public class Notification : MonoBehaviour
{
    private bool _firebaseInitialized = false;

    private async void Start()
    {
        await InitializeFirebaseAsync();
    }

    private async Task InitializeFirebaseAsync()
    {
        // Check & fix dependencies
        var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (dependencyStatus != DependencyStatus.Available)
        {
            Debug.LogError("[FCM] Firebase init failed: " + dependencyStatus);
            return;
        }

        Debug.Log("[FCM] Firebase initialized");

        // Đăng ký event trước
        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

        _firebaseInitialized = true;

        // Xin quyền notification (quan trọng trên iOS)
        await RequestPermissionAsync();

        // Lấy token
        await GetTokenAsync();
    }

    private async Task RequestPermissionAsync()
    {
        Debug.Log("[FCM] Requesting permission...");

        await FirebaseMessaging.RequestPermissionAsync();
    }

    private async Task GetTokenAsync()
    {
        try
        {
            var token = await FirebaseMessaging.GetTokenAsync();
            Debug.Log("[FCM] Token: " + token);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[FCM] GetTokenAsync failed: " + ex.Message);
        }
    }

    private void OnDestroy()
    {
        if (_firebaseInitialized)
        {
            FirebaseMessaging.TokenReceived -= OnTokenReceived;
            FirebaseMessaging.MessageReceived -= OnMessageReceived;
        }
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log("[FCM] TokenReceived: " + token.Token);
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("[FCM] Message from: " + e.Message.From);

        var notification = e.Message.Notification;
        if (notification != null)
        {
            Debug.Log("[FCM] Title: " + notification.Title);
            Debug.Log("[FCM] Body: " + notification.Body);
        }

        if (e.Message.Data != null && e.Message.Data.Count > 0)
        {
            foreach (var kvp in e.Message.Data)
            {
                Debug.Log($"[FCM] Data: {kvp.Key} = {kvp.Value}");
            }
        }
    }
}