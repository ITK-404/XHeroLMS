using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MailList : MonoBehaviour
{
    public enum MailFilter
    {
        Personal,
        System
    }

    [Header("UI Spawn")]
    [SerializeField] private MailElementVisualUI mailPrefab;
    [SerializeField] private bool clearOldItems = true;

    [Header("Optional")]
    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private NotificationsDetailLoader detailLoader;

    private readonly List<MailElementVisualUI> spawnedItems = new();

    private Transform currentContentParent;
    private MailFilter currentFilter = MailFilter.Personal;

    private void Awake()
    {
        if (detailLoader == null)
            detailLoader = FindFirstObjectByType<NotificationsDetailLoader>();
    }

    private void OnEnable()
    {
        NotificationsStaticStore.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        NotificationsStaticStore.OnChanged -= Refresh;
    }

    public void SetRenderTarget(Transform contentParent)
    {
        currentContentParent = contentParent;
        Refresh();
    }

    public void SetFilter(MailFilter filter)
    {
        currentFilter = filter;
        Refresh();
    }

    public void Refresh()
    {
        if (clearOldItems)
            ClearItems();

        if (NotificationsStaticStore.IsLoading)
        {
            return;
        }

        if (!string.IsNullOrEmpty(NotificationsStaticStore.LastError))
        {
            return;
        }

        if (!NotificationsStaticStore.HasData || NotificationsStaticStore.Items == null || NotificationsStaticStore.Items.Count == 0)
        {
            return;
        }

        int spawnedCount = 0;
        var items = NotificationsStaticStore.Items;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null)
                continue;

            if (!MatchFilter(item))
                continue;

            SpawnItem(item);
            spawnedCount++;
        }
    }

    private bool MatchFilter(NotificationMailItem item)
    {
        if (item == null)
            return false;

        string label = (item.label ?? "").Trim().ToLower();

        switch (currentFilter)
        {
            case MailFilter.Personal:
                return label == "personal";

            case MailFilter.System:
                return label == "system";

            default:
                return false;
        }
    }

    private void SpawnItem(NotificationMailItem data)
    {
        if (mailPrefab == null)
        {
            Debug.LogWarning("[MailList] mailPrefab đang null.");
            return;
        }

        if (currentContentParent == null)
        {
            Debug.LogWarning("[MailList] currentContentParent đang null khi SpawnItem.");
            return;
        }

        if (data == null)
            return;

        MailElementVisualUI ui = Instantiate(mailPrefab, currentContentParent);

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
            if (detailLoader == null)
                detailLoader = FindFirstObjectByType<NotificationsDetailLoader>();

            mainUI.SetDetailLoader(detailLoader);
            mainUI.Bind(data);
        }
        else
        {
            Debug.LogWarning("[MailList] Không tìm thấy MainElementUI trên prefab item.");
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
    }
}