using System.Collections.Generic;
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

    [Header("Optional")]
    [SerializeField] private NotificationsDetailLoader detailLoader;

    private readonly Dictionary<string, MailElementVisualUI> spawnedItemMap = new();
    private readonly List<string> removeBuffer = new();

    private Transform currentContentParent;
    // private MailFilter currentFilter = MailFilter.Personal;
    private MailFilter currentFilter = MailFilter.System;


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
        if (currentContentParent == contentParent)
            return;

        currentContentParent = contentParent;
        ClearAllItems();
        Refresh();
    }

    public void SetFilter(MailFilter filter)
    {
        Debug.Log("[MailList] SetFilter = " + filter);

        currentFilter = filter;
        ClearAllItems();
        Refresh();
    }

    public void Refresh()
    {
        if (mailPrefab == null)
        {
            Debug.LogWarning("[MailList] mailPrefab đang null.");
            return;
        }

        if (currentContentParent == null)
        {
            Debug.LogWarning("[MailList] currentContentParent đang null khi Refresh.");
            return;
        }

        if (NotificationsStaticStore.IsLoading)
        {
            return;
        }

        if (!string.IsNullOrEmpty(NotificationsStaticStore.LastError))
        {
            return;
        }

        var items = NotificationsStaticStore.Items;
        if (items == null || items.Count == 0)
        {
            RemoveAllSpawnedItems();
            return;
        }

        HashSet<string> aliveIds = new();
        int visibleIndex = 0;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null)
                continue;

            if (!MatchFilter(item))
                continue;

            string id = GetItemId(item);
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[MailList] Notification item không có id hợp lệ, bỏ qua.");
                continue;
            }

            aliveIds.Add(id);

            if (spawnedItemMap.TryGetValue(id, out var existingUI) && existingUI != null)
            {
                UpdateItemUI(existingUI, item);

                if (existingUI.transform.parent != currentContentParent)
                    existingUI.transform.SetParent(currentContentParent, false);

                existingUI.transform.SetSiblingIndex(visibleIndex);
            }
            else
            {
                var newUI = CreateItemUI(item);
                if (newUI != null)
                {
                    spawnedItemMap[id] = newUI;
                    newUI.transform.SetSiblingIndex(visibleIndex);
                }
            }

            visibleIndex++;
        }

        removeBuffer.Clear();

        foreach (var kvp in spawnedItemMap)
        {
            if (!aliveIds.Contains(kvp.Key))
                removeBuffer.Add(kvp.Key);
        }

        for (int i = 0; i < removeBuffer.Count; i++)
        {
            RemoveItem(removeBuffer[i]);
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

    private string GetItemId(NotificationMailItem data)
    {
        if (data == null)
            return string.Empty;

        return (data._id ?? "").Trim();
    }

    private MailElementVisualUI CreateItemUI(NotificationMailItem data)
    {
        if (data == null)
            return null;

        MailElementVisualUI ui = Instantiate(mailPrefab, currentContentParent);
        ApplyDataToUI(ui, data);
        return ui;
    }

    private void UpdateItemUI(MailElementVisualUI ui, NotificationMailItem data)
    {
        if (ui == null || data == null)
            return;

        ApplyDataToUI(ui, data);
    }

    private void ApplyDataToUI(MailElementVisualUI ui, NotificationMailItem data)
    {
        if (ui == null || data == null)
            return;

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

    private void RemoveItem(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        if (!spawnedItemMap.TryGetValue(id, out var ui))
            return;

        if (ui != null)
            Destroy(ui.gameObject);

        spawnedItemMap.Remove(id);
    }

    private void RemoveAllSpawnedItems()
    {
        removeBuffer.Clear();

        foreach (var kvp in spawnedItemMap)
            removeBuffer.Add(kvp.Key);

        for (int i = 0; i < removeBuffer.Count; i++)
            RemoveItem(removeBuffer[i]);
    }

    private void ClearAllItems()
    {
        RemoveAllSpawnedItems();
    }
}