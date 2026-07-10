using UnityEngine;
using UnityEngine.UI;

public class KyMon_AutoAddToggleGroup : MonoBehaviour
{
    [SerializeField] private ToggleGroup toggleGroup;
    [SerializeField] private Toggle[] toggles;

    [ContextMenu("SearchToggle")]
    private void SearchToggleGroupAndFill()
    {
        toggles = GetComponentsInChildren<Toggle>();
        foreach (var toggle in toggles)
        {
            toggle.group = toggleGroup;
        }
    }
}