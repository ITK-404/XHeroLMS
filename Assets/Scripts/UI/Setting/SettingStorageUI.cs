using TMPro;
using UnityEngine;
using UnityEngine.UI;
 
public class SettingStorageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI totalStorageText;
    [SerializeField] private TextMeshProUGUI usedStorageText;
    [SerializeField] private TextMeshProUGUI unusedStorageText;
    [SerializeField] private Slider storageUsedSlider;
 
    private IStorageService storageService;
 
    private void Awake()
    {
        storageService = StorageServiceFactory.Create();
        Refresh();
    }
 
    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (storageService == null || !storageService.IsAvailable())
        {
            Debug.LogWarning("[SettingStorageUI] StorageService không khả dụng.");
            return;
        }

        Debug.Log($"Setting Storage UI: Update refresh");
        
        StorageInfo info = storageService.GetStorageInfo();
        UpdateUI(info);
    }
 
    private void UpdateUI(StorageInfo info)
    {
        totalStorageText.text   = $"{Format(info.UsedMB)} / {Format(info.TotalMB)}";
        usedStorageText.text    = "Đã sử dụng " + Format(info.UsedMB);
        unusedStorageText.text  = "Còn trống "  + Format(info.FreeMB);
        storageUsedSlider.value = info.UsedPercent / 100f;
    }
 
    private static string Format(float mb)
        => mb >= 1024f ? $"{mb / 1024f:F1} GB" : $"{mb:F1} MB";
}