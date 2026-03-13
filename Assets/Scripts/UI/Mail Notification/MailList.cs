using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MailList : MonoBehaviour
{
    [Header("UI Spawn")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private MailElementVisualUI mailPrefab;
    [SerializeField] private bool clearOldItems = true;

    [Header("Optional")]
    [SerializeField] private TextMeshProUGUI emptyText;

    private readonly List<MailElementVisualUI> spawnedItems = new();

    private void OnEnable()
    {
        NotificationsStaticStore.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        NotificationsStaticStore.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        if (clearOldItems)
            ClearItems();

        if (NotificationsStaticStore.IsLoading)
        {
            SetEmpty(true, "Đang tải...");
            return;
        }

        if (!string.IsNullOrEmpty(NotificationsStaticStore.LastError))
        {
            SetEmpty(true, NotificationsStaticStore.LastError);
            return;
        }

        if (!NotificationsStaticStore.HasData)
        {
            SetEmpty(true, "Không có thông báo.");
            return;
        }

        SetEmpty(false, "");

        var items = NotificationsStaticStore.Items;
        for (int i = 0; i < items.Count; i++)
        {
            SpawnItem(items[i]);
        }
    }

    private void SpawnItem(NotificationMailItem data)
    {
        if (mailPrefab == null || contentParent == null || data == null)
            return;

        MailElementVisualUI ui = Instantiate(mailPrefab, contentParent);

        string timeText = BuildTimeText(data.time);
        string readState = data.isRead ? "Đã đọc" : "Chưa đọc";

        ui.BindData(
            title: data.title,
            description: data.text,
            timeText: timeText,
            readStateText: readState
        );

        MainElementUI mainUI = ui.GetComponent<MainElementUI>();
        if (mainUI != null)
        {
            mainUI.Bind(data);
        }
        else
        {
            Debug.LogWarning("[MailList] Prefab chưa có MainElementUI.");
        }

        spawnedItems.Add(ui);
    }

    private string BuildTimeText(NotificationMailTime t)
    {
        if (t == null) return "";

        if (!string.IsNullOrEmpty(t.time))
            return t.time;

        if (!string.IsNullOrEmpty(t.day))
            return t.day;

        return "";
    }

    private void ClearItems()
    {
        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
                Destroy(spawnedItems[i].gameObject);
        }
        spawnedItems.Clear();

        if (contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
        {
            Destroy(contentParent.GetChild(i).gameObject);
        }
    }

    private void SetEmpty(bool show, string msg)
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(show);
            emptyText.text = msg;
        }
    }
}