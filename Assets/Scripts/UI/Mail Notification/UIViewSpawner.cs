using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class UIViewSpawner : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject prefab;

    UIView canvas;

    public void OnShowMail()
    {
        Debug.Log("On Clicked");
        if (canvas == null)
        {
            Spawn();
        }
        else
        {
            canvas.Show();
        }
    }

    private void OnDestroy()
    {
        if(canvas)
            Addressables.ReleaseInstance(canvas.gameObject);
    }
    private bool isSpawning;

    public async void Spawn()
    {
        Debug.Log("On Clicked");
        
        // Tạm thời để tránh player ấn nút linh tinh
        if (isSpawning) return;
        
        isSpawning = true;
        LoadingUI.Show();
        await Task.Delay(1000);
        var obj = await prefab.InstantiateAsync().Task;
        canvas = obj.GetComponent<UIView>();
        canvas.Show();

        LoadingUI.Hide();
        isSpawning = false;
    }
}