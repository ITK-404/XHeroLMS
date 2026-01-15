using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AreaDropdownController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private AreaDisplayManager areaDisplayManager;

    [Header("Options")]
    [SerializeField] private bool includeAllOption = true;
    [SerializeField] private string allOptionLabel = "Tất cả khu vực";
    [SerializeField] private bool autoBuildWhenAreasReady = true;
    [SerializeField] private bool fallbackBuildNextFrame = true;

    // Khi đổi dropdown thì highlight/focus ngay
    [SerializeField] private bool autoSelectOnChange = true;

    // Nếu true: khi chọn khu vực bằng click minimap (OnShowFocusArea) thì dropdown sẽ tự sync caption/value
    [SerializeField] private bool syncWithAreaSelection = true;

    [Header("UI - Progress")]
    [SerializeField] private TMP_Text progressTmp;
    [SerializeField] private string progressFormat = "Đã mở khóa <color=#F9DF99>{0}/{1} ({2:0}%)</color>";

    private CourseMapBrowserUI _courseMap;

    [Header("Events")]
    public Action<BigArea> OnAreaSelected;

    // runtime
    private readonly List<BigArea> _areas = new();
    private bool _isBuilding;
    private bool _didBuildAtLeastOnce;
    private bool _isSyncingFromManager;

    private void Reset()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void Awake()
    {
        if (dropdown == null) dropdown = GetComponent<TMP_Dropdown>();

        if (dropdown != null)
        {
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }
    }

    private void OnEnable()
    {
        ResolveManager();
        SubscribeManager(true);

        if (fallbackBuildNextFrame && !_didBuildAtLeastOnce)
            StartCoroutine(BuildNextFrameIfNeeded());
    }

    private void OnDisable()
    {
        SubscribeManager(false);
    }

    private void ResolveManager()
    {
        if (areaDisplayManager == null)
            areaDisplayManager = AreaDisplayManager.Instance;
    }

    private void SubscribeManager(bool subscribe)
    {
        if (areaDisplayManager == null) return;

        // OnAreasReady (để build list)
        areaDisplayManager.OnAreasReady -= HandleAreasReady;
        if (subscribe)
            areaDisplayManager.OnAreasReady += HandleAreasReady;

        // NEW: OnShowFocusArea (để sync caption/value khi click minimap)
        if (syncWithAreaSelection)
        {
            areaDisplayManager.OnShowFocusArea -= HandleFocusAreaChanged;
            if (subscribe)
                areaDisplayManager.OnShowFocusArea += HandleFocusAreaChanged;
        }
    }

    private void HandleAreasReady(BigArea[] _)
    {
        if (!autoBuildWhenAreasReady) return;
        BuildFromAreaDisplayManager();
    }

    private IEnumerator BuildNextFrameIfNeeded()
    {
        yield return null;
        if (_didBuildAtLeastOnce) yield break;

        ResolveManager();
        if (areaDisplayManager != null && areaDisplayManager.BigAreas != null && areaDisplayManager.BigAreas.Length > 0)
            BuildFromAreaDisplayManager();
        else
            BuildFromList(null);
    }

    public void BuildFromAreaDisplayManager()
    {
        ResolveManager();

        if (dropdown == null)
        {
            Debug.LogError("[AreaDropdown] dropdown is null.");
            return;
        }

        if (areaDisplayManager == null)
        {
            Debug.LogError("[AreaDropdown] AreaDisplayManager.Instance is null.");
            BuildFromList(null);
            return;
        }

        BuildFromList(areaDisplayManager.BigAreas);
    }

    private void BuildFromList(BigArea[] src)
    {
        _areas.Clear();

        if (src != null)
        {
            for (int i = 0; i < src.Length; i++)
            {
                if (src[i] != null) _areas.Add(src[i]);
            }
        }

        _isBuilding = true;

        dropdown.ClearOptions();
        var options = new List<TMP_Dropdown.OptionData>();

        if (includeAllOption)
            options.Add(new TMP_Dropdown.OptionData(allOptionLabel));

        for (int i = 0; i < _areas.Count; i++)
            options.Add(new TMP_Dropdown.OptionData(GetAreaLabel(_areas[i])));

        dropdown.AddOptions(options);

        // sync theo SelectArea nếu có
        int defaultIndex = GetDropdownIndexForArea(areaDisplayManager != null ? areaDisplayManager.SelectArea : null);

        dropdown.SetValueWithoutNotify(defaultIndex);
        dropdown.RefreshShownValue();

        _isBuilding = false;
        _didBuildAtLeastOnce = true;

        // optional: apply selection ngay (sẽ highlight)
        if (autoSelectOnChange)
            ApplySelectionByDropdownIndex(defaultIndex);
    }

    private void OnDropdownChanged(int value)
    {
        if (_isBuilding) return;
        if (_isSyncingFromManager) return; // NEW: tránh loop khi manager sync ngược về dropdown
        if (!autoSelectOnChange) return;

        ApplySelectionByDropdownIndex(value);
    }

    private void ApplySelectionByDropdownIndex(int dropdownIndex)
    {
        ResolveManager();
        if (areaDisplayManager == null) return;

        BigArea selectedArea = null;

        if (includeAllOption)
        {
            // 0 = all/reset
            if (dropdownIndex <= 0)
            {
                areaDisplayManager.ResetArea();
                OnAreaSelected?.Invoke(null);

                // đảm bảo caption đúng
                dropdown.RefreshShownValue();
                return;
            }

            int areaIdx = dropdownIndex - 1;
            if (areaIdx >= 0 && areaIdx < _areas.Count)
                selectedArea = _areas[areaIdx];
        }
        else
        {
            if (dropdownIndex >= 0 && dropdownIndex < _areas.Count)
                selectedArea = _areas[dropdownIndex];
        }

        areaDisplayManager.HighlightSingleArea(selectedArea);
        OnAreaSelected?.Invoke(selectedArea);

        // đảm bảo caption đúng
        dropdown.RefreshShownValue();
    }

    // ===================== NEW: Sync when click minimap =====================
    private void HandleFocusAreaChanged(BigArea selectedArea)
    {
        if (dropdown == null) return;

        // nếu chưa build list thì chưa map được index
        if (!_didBuildAtLeastOnce) return;

        int idx = GetDropdownIndexForArea(selectedArea);

        _isSyncingFromManager = true;
        dropdown.SetValueWithoutNotify(idx);
        dropdown.RefreshShownValue();
        _isSyncingFromManager = false;
    }

    private int GetDropdownIndexForArea(BigArea area)
    {
        if (area == null)
            return includeAllOption ? 0 : 0;

        int listIndex = _areas.IndexOf(area);
        if (listIndex < 0)
            return includeAllOption ? 0 : 0;

        return includeAllOption ? (listIndex + 1) : listIndex;
    }

    private string GetAreaLabel(BigArea area)
    {
        if (area == null) return "(null)";

        if (area.Data != null && !string.IsNullOrEmpty(area.Data.displayName))
            return area.Data.displayName;

        return area.gameObject != null ? area.gameObject.name : "(unknown)";
    }

    [ContextMenu("Rebuild Dropdown")]
    private void RebuildContextMenu()
    {
        BuildFromAreaDisplayManager();
    }
}
