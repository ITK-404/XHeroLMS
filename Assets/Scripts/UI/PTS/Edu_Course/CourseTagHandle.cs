using System;
using UnityEngine;
using UnityEngine.UI;

public enum CourseTag
{
    Offline,
    Online,
    Zoom
}

public class CourseTagHandle : MonoBehaviour
{
    [SerializeField] private Image offlineImg;
    [SerializeField] private Image onlineImg;
    [SerializeField] private Image zoomImg;

    [SerializeField] private CourseTag _tag;

    [Header("Auto bind from CourseDetailStaticStore")]
    [SerializeField] private bool useLearningModeFromDetailStore = true;
    [SerializeField] private bool autoRefreshOnEnable = true;
    [SerializeField] private bool listenStoreChanged = true;
    [SerializeField] private bool hideWhenUnknown = true;
    [SerializeField] private bool debugLog = false;

    private bool _subscribedToDetailStore;

    private void Awake()
    {
        if (!useLearningModeFromDetailStore)
            ApplyTag(_tag);
    }

    private void OnEnable()
    {
        TrySubscribeDetailStore();

        if (useLearningModeFromDetailStore && autoRefreshOnEnable)
            RefreshFromDetailStore();
    }

    private void OnDisable()
    {
        UnsubscribeDetailStore();
    }

    public void Show(CourseTag newTag)
    {
        SetDetailStoreBinding(false);
        ApplyTag(newTag);
    }

    public void ShowLearningMode(string learningMode)
    {
        SetDetailStoreBinding(false);

        if (TryMapLearningModeToTag(learningMode, out var tag))
        {
            ApplyTag(tag);
            return;
        }

        if (hideWhenUnknown)
            SetAllInactive();
        else
            ApplyTag(_tag);
    }

    public void SetDetailStoreBinding(bool enabled)
    {
        useLearningModeFromDetailStore = enabled;
        autoRefreshOnEnable = enabled;
        listenStoreChanged = enabled;

        if (!enabled)
        {
            UnsubscribeDetailStore();
            return;
        }

        TrySubscribeDetailStore();

        if (isActiveAndEnabled && autoRefreshOnEnable)
            RefreshFromDetailStore();
    }

    private void ApplyTag(CourseTag newTag)
    {
        _tag = newTag;

        SetAllInactive();

        var image = GetImage(newTag);
        if (image != null)
            image.gameObject.SetActive(true);
    }

    public void RefreshFromDetailStore()
    {
        string learningMode = null;

        if (CourseDetailStaticStore.CurrentDetail != null)
            learningMode = CourseDetailStaticStore.CurrentDetail.learningMode;

        if (debugLog)
            Debug.Log($"[CourseTagHandle] learningMode from detail store = {learningMode}");

        if (TryMapLearningModeToTag(learningMode, out var tag))
        {
            ApplyTag(tag);
        }
        else
        {
            if (hideWhenUnknown)
                SetAllInactive();
            else
                ApplyTag(_tag);
        }
    }

    [ContextMenu("Update Current Tag")]
    private void UpdateCurrentTag()
    {
        if (useLearningModeFromDetailStore)
            RefreshFromDetailStore();
        else
            ApplyTag(_tag);
    }

    private void TrySubscribeDetailStore()
    {
        if (!useLearningModeFromDetailStore || !listenStoreChanged || _subscribedToDetailStore)
            return;

        CourseDetailStaticStore.OnChanged += RefreshFromDetailStore;
        _subscribedToDetailStore = true;
    }

    private void UnsubscribeDetailStore()
    {
        if (!_subscribedToDetailStore)
            return;

        CourseDetailStaticStore.OnChanged -= RefreshFromDetailStore;
        _subscribedToDetailStore = false;
    }

    private bool TryMapLearningModeToTag(string learningMode, out CourseTag tag)
    {
        tag = CourseTag.Offline;

        if (string.IsNullOrWhiteSpace(learningMode))
            return false;

        string mode = learningMode.Trim().ToLowerInvariant();

        if (mode.Contains("zoom"))
        {
            tag = CourseTag.Zoom;
            return true;
        }

        if (mode.Contains("offline") || mode.Contains("onsite") || mode.Contains("on-site"))
        {
            tag = CourseTag.Offline;
            return true;
        }

        if (mode.Contains("online"))
        {
            tag = CourseTag.Online;
            return true;
        }

        return false;
    }

    private void SetAllInactive()
    {
        if (offlineImg != null) offlineImg.gameObject.SetActive(false);
        if (onlineImg != null) onlineImg.gameObject.SetActive(false);
        if (zoomImg != null) zoomImg.gameObject.SetActive(false);
    }

    private Image GetImage(CourseTag courseTag)
    {
        switch (courseTag)
        {
            case CourseTag.Offline:
                return offlineImg;
            case CourseTag.Online:
                return onlineImg;
            case CourseTag.Zoom:
                return zoomImg;
        }

        return null;
    }
}
