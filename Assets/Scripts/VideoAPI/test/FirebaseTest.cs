using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseTest : MonoBehaviour
{
    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError("[FirebaseTest] Firebase dep error: " + task.Result);
                    return;
                }

                var app = FirebaseLoginQrPerCode.Instance
                              ? FirebaseLoginQrPerCode.Instance
                                    .GetType()
                                    .GetMethod("EnsureFirebaseApp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                    ?.Invoke(FirebaseLoginQrPerCode.Instance, null) as FirebaseApp
                              : FirebaseApp.DefaultInstance;

                var db = FirebaseDatabase.GetInstance(app);

                db.RootReference.Child("unity-test").GetValueAsync()
                    .ContinueWithOnMainThread(t =>
                    {
                        if (t.IsFaulted)
                        {
                            Debug.LogError("[FirebaseTest] GetValueAsync error: " + t.Exception);
                        }
                        else if (t.IsCompleted)
                        {
                            Debug.Log("[FirebaseTest] unity-test snapshot: " + t.Result.GetRawJsonValue());
                        }
                    });
            });
    }
}
