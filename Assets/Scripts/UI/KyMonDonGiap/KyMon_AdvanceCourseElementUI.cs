using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class KyMon_AdvanceCourseElementUI : MonoBehaviour
{
    public event Action<bool> OnSelectStateChanged;
    [SerializeField] private Toggle toggle;

    public UnityEvent OnToggleOn;
    public UnityEvent OnToggleOff;

    private void Awake()
    {
        HandleContentUI(false);
        toggle.onValueChanged.AddListener(ToggleChanged);
    }

    private void ToggleChanged(bool isSelect)
    {
        OnSelectStateChanged?.Invoke(isSelect);
        HandleContentUI(isSelect);
    }

    private void HandleContentUI(bool isSelect)
    {
        if (isSelect)
        {
            OnToggleOn?.Invoke();
        }
        else
        {
            OnToggleOff?.Invoke();
        }
    }
}