using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MailList : MonoBehaviour
{
    public enum MailFilter
    {
        Personal,
        System
    }

    private string currentDetailLoadingId;

    [Header("UI Spawn")]
    [SerializeField] private MailElementVisualUI mailPrefab;

    [Header("Optional")]
    [SerializeField] private NotificationsDetailLoader detailLoader;

    [Header("State")]
    [SerializeField] private Transform emptyMailState;

    private readonly Dictionary<string, MailElementVisualUI> spawnedItemMap = new();
    private readonly List<string> removeBuffer = new();

    private Transform currentContentParent;
    private MailFilter currentFilter = MailFilter.System;

    private NotificationMailItem _currentSelectedItem;
    private bool hasInitializedSelection = false;
    private bool forceSelectFirstOnNextRefresh = false;

    private Coroutine resetScrollRoutine;
    private bool pendingRefresh;

    private void Awake()
    {
        if (detailLoader == null)
            detailLoader = FindFirstObjectByType<NotificationsDetailLoader>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        NotificationsStaticStore.OnChanged += Refresh;

        hasInitializedSelection = false;
        forceSelectFirstOnNextRefresh = true;
        _currentSelectedItem = null;
        pendingRefresh = true;

        ResetScrollToTopDeferred();
        TryConsumePendingRefresh();
    }

    private void OnDisable()
    {
        NotificationsStaticStore.OnChanged -= Refresh;

        if (resetScrollRoutine != null)
        {
            StopCoroutine(resetScrollRoutine);
            resetScrollRoutine = null;
        }
    }

    public void SetRenderTarget(Transform contentParent)
    {
        currentContentParent = contentParent;

        ClearAllItems();
        ClearSelectionAndDetail(true);

        hasInitializedSelection = false;
        forceSelectFirstOnNextRefresh = true;
        pendingRefresh = true;

        ResetScrollToTopDeferred();
        TryConsumePendingRefresh();
    }

    public void SetFilter(MailFilter filter)
    {
        currentFilter = filter;

        ClearAllItems();
        ClearSelectionAndDetail(true);

        hasInitializedSelection = false;
        forceSelectFirstOnNextRefresh = true;
        pendingRefresh = true;

        ResetScrollToTopDeferred();
        TryConsumePendingRefresh();
    }

public void ForceResetToFirstItem()
{
    ClearSelectionAndDetail(true);
    hasInitializedSelection = false;
    forceSelectFirstOnNextRefresh = true;
    pendingRefresh = true;

    ResetScrollToTopDeferred();

    if (!NotificationsStaticStore.IsLoading && IsReadyToRefresh())
        Refresh();
    else
        TryConsumePendingRefresh();
}
    public void Refresh()
    {
        if (emptyMailState != null)
            emptyMailState.gameObject.SetActive(false);

        if (NotificationsStaticStore.IsLoading)
        {
            pendingRefresh = true;
            return;
        }

        if (!IsReadyToRefresh())
        {
            pendingRefresh = true;
            return;
        }

        if (!string.IsNullOrEmpty(NotificationsStaticStore.LastError))
        {
            pendingRefresh = false;
            RemoveAllSpawnedItems();
            ClearSelectionAndDetail(true);

            if (emptyMailState != null)
                emptyMailState.gameObject.SetActive(true);

            return;
        }

        pendingRefresh = false;

        var items = NotificationsStaticStore.Items;

        if (items == null || items.Count == 0)
        {
            RemoveAllSpawnedItems();
            ClearSelectionAndDetail(true);

            if (emptyMailState != null)
                emptyMailState.gameObject.SetActive(true);

            return;
        }

        HashSet<string> aliveIds = new();
        int visibleIndex = 0;
        NotificationMailItem firstMatchedItem = null;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null)
                continue;

            if (!MatchFilter(item))
                continue;

            if (firstMatchedItem == null)
                firstMatchedItem = item;

            string id = GetItemId(item);
            if (string.IsNullOrEmpty(id))
                continue;

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
            RemoveItem(removeBuffer[i]);

        bool hasAnyVisibleItem = visibleIndex > 0;

        if (emptyMailState != null)
            emptyMailState.gameObject.SetActive(!hasAnyVisibleItem);

        if (!hasAnyVisibleItem)
        {
            ClearSelectionAndDetail(true);
            return;
        }

        AutoSelectFirstItem(firstMatchedItem);
    }

    private void AutoSelectFirstItem(NotificationMailItem firstItem)
    {
        if (firstItem == null)
        {
            ClearSelectionAndDetail(true);
            return;
        }

        string currentId = GetItemId(_currentSelectedItem);

        bool shouldResetToFirst =
            forceSelectFirstOnNextRefresh ||
            !hasInitializedSelection ||
            _currentSelectedItem == null ||
            string.IsNullOrEmpty(currentId) ||
            !spawnedItemMap.ContainsKey(currentId) ||
            !MatchFilter(_currentSelectedItem);

        if (shouldResetToFirst)
        {
            _currentSelectedItem = firstItem;
            hasInitializedSelection = true;
            forceSelectFirstOnNextRefresh = false;

            ResetScrollToTopDeferred();
            UpdateDetailView(firstItem);
            return;
        }

        for (int i = 0; i < NotificationsStaticStore.Items.Count; i++)
        {
            var item = NotificationsStaticStore.Items[i];
            if (item == null)
                continue;

            if (!MatchFilter(item))
                continue;

            if (GetItemId(item) == currentId)
            {
                _currentSelectedItem = item;
                UpdateDetailView(item);
                return;
            }
        }

        _currentSelectedItem = firstItem;
        hasInitializedSelection = true;
        forceSelectFirstOnNextRefresh = false;

        ResetScrollToTopDeferred();
        UpdateDetailView(firstItem);
    }

    public void SelectItem(NotificationMailItem item)
    {
        if (item == null)
            return;

        _currentSelectedItem = item;
        hasInitializedSelection = true;
        forceSelectFirstOnNextRefresh = false;

        UpdateDetailView(item);
    }

private void UpdateDetailView(NotificationMailItem item)
{
    if (item == null)
    {
        ClearSelectionAndDetail(true);
        return;
    }

    var contentView = MailContentView.Instance;
    if (contentView == null)
        contentView = FindFirstObjectByType<MailContentView>(FindObjectsInactive.Include);

    if (contentView != null)
        contentView.ShowPreview(item);

    if (NotificationsStaticStore.IsLoading)
    {
        pendingRefresh = true;
        return;
    }

    if (detailLoader == null)
        detailLoader = FindFirstObjectByType<NotificationsDetailLoader>(FindObjectsInactive.Include);

    if (detailLoader == null || string.IsNullOrWhiteSpace(item._id))
        return;

    if (currentDetailLoadingId == item._id &&
        NotificationsDetailStaticStore.IsLoading)
        return;

    currentDetailLoadingId = item._id;
    detailLoader.LoadById(item._id);
}

private void ClearSelectionAndDetail(bool resetDetailStore)
{
    _currentSelectedItem = null;
    currentDetailLoadingId = null;

    if (resetDetailStore)
        NotificationsDetailStaticStore.Reset();

    var contentView = MailContentView.Instance;
    if (contentView == null)
        contentView = FindFirstObjectByType<MailContentView>(FindObjectsInactive.Include);

    if (contentView != null)
        contentView.ResetView();
}

    private ScrollRect GetScrollRect()
    {
        if (currentContentParent == null)
            return null;

        return currentContentParent.GetComponentInParent<ScrollRect>(true);
    }

    private void ResetScrollToTopDeferred()
    {
        if (resetScrollRoutine != null)
        {
            StopCoroutine(resetScrollRoutine);
            resetScrollRoutine = null;
        }

        resetScrollRoutine = StartCoroutine(CoResetScrollToTop());
    }

    private IEnumerator CoResetScrollToTop()
    {
        yield return null;

        var sr = GetScrollRect();
        if (sr == null)
            yield break;

        Canvas.ForceUpdateCanvases();

        if (currentContentParent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        sr.StopMovement();
        sr.verticalNormalizedPosition = 1f;
        sr.velocity = Vector2.zero;

        yield return null;

        Canvas.ForceUpdateCanvases();

        if (currentContentParent is RectTransform contentRect2)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect2);

        sr.StopMovement();
        sr.verticalNormalizedPosition = 1f;
        sr.velocity = Vector2.zero;

        resetScrollRoutine = null;
    }

    private bool MatchFilter(NotificationMailItem item)
    {
        if (item == null)
            return false;

        string label = (item.label ?? "").Trim().ToLower();

        return currentFilter switch
        {
            MailFilter.Personal => label == "personal",
            MailFilter.System => label == "system",
            _ => false
        };
    }

    private string GetItemId(NotificationMailItem data)
    {
        return data == null ? string.Empty : (data._id ?? "").Trim();
    }

    private MailElementVisualUI CreateItemUI(NotificationMailItem data)
    {
        if (data == null)
            return null;

        var ui = Instantiate(mailPrefab, currentContentParent);
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

        string readState = data.isRead ? "Đã đọc" : "Chưa đọc";

        ui.BindData(data.title, data.text, readState);
        ui.SetTimeFromApi(data.time);

        var mainUI = ui.GetComponent<MainElementUI>();
        if (mainUI != null)
        {
            if (detailLoader == null)
                detailLoader = FindFirstObjectByType<NotificationsDetailLoader>(FindObjectsInactive.Include);

            mainUI.SetDetailLoader(detailLoader);
            mainUI.Bind(data);
        }
    }

    private void RemoveItem(string id)
    {
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

    private bool IsReadyToRefresh()
    {
        return mailPrefab != null && currentContentParent != null;
    }

private void TryConsumePendingRefresh()
{
    if (!pendingRefresh)
        return;

    if (NotificationsStaticStore.IsLoading)
        return;

    if (!IsReadyToRefresh())
        return;

    Refresh();
}
}