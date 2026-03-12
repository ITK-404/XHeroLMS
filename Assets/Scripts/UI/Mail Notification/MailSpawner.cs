using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class MailSpawner : MonoBehaviour
{
    [SerializeField] private AssetReferenceGameObject mailCanvasPrefab;
    [SerializeField] private Button mailBtn;

    GameObject canvas;

    private void Awake()
    {
        mailBtn.onClick.AddListener(OnShowMail);
    }

    private void OnShowMail()
    {
        if (canvas == null)
        {
            Spawn();
        }
        else
        {
            canvas.gameObject.SetActive(true);
        }
    }

    private void OnDestroy()
    {
        mailBtn.onClick.RemoveListener(OnShowMail);
        if(canvas)
            Addressables.ReleaseInstance(canvas);
    }
    private bool isSpawning;

    public async void Spawn()
    {
        // Tạm thời để tránh player ấn nút linh tinh
        if (isSpawning) return;

        isSpawning = true;
        LoadingUI.Show();
        await Task.Delay(1000);
        canvas = await mailCanvasPrefab.InstantiateAsync().Task;
        LoadingUI.Hide();
        isSpawning = false;
    }
}