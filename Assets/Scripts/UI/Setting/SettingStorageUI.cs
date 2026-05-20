using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingStorageUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI totalStorageText;
    [SerializeField] private TextMeshProUGUI usedStorageText;
    [SerializeField] private TextMeshProUGUI unusedStorageText;
    [SerializeField] private Slider storageUsedSlider;

    private StorageData data;

    private void Awake()
    {
        StorageData mockData = new StorageData
        {
            TotalStorage = 4096f,   // 2 GB
            UsedStorage  = 1300f,   // 1.3 GB
            UnusedStorage = 748f,   // 748 MB
        };

        BindData(mockData);
        UpdateUI(this.data);
    }

    private void BindData(StorageData data)
    {
        this.data = data;
    }

    private void OnEnable()
    {
        if (data != null)
        {
            UpdateUI(data);
        }
    }

    public void UpdateUI(StorageData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SettingStorageUI] StorageData is null");
            return;
        }

        totalStorageText.text = $"{FormatStorage(data.UsedStorage)} / {FormatStorage(data.TotalStorage)}";
        usedStorageText.text = "Đã sử dụng " + FormatStorage(data.UsedStorage);
        unusedStorageText.text = "Còn trống " + FormatStorage(data.UnusedStorage);

        storageUsedSlider.value = data.TotalStorage > 0
            ? data.UsedStorage / data.TotalStorage
            : 0f;
    }

    private string FormatStorage(float valueInMB)
    {
        if (valueInMB >= 1024f)
            return $"{valueInMB / 1024f:F1} GB";

        return $"{valueInMB:F1} MB";
    }

    public class StorageData
    {
        public float TotalStorage;
        public float UsedStorage;
        public float UnusedStorage;
    }
}