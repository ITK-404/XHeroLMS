using UnityEngine;

public class LanguageGroupManagerUI : MonoBehaviour
{
    [SerializeField] private LanguageElementUI[] elementUis;
    
    [SerializeField] private LanguageElementUI elementPrefab;
    [SerializeField] private Transform container;

    // ToggleSwitchGroupManager đã có sẵn trên cùng GO hoặc child
    private ToggleSwitchGroupManager toggleGroupManager;

    private void Awake()
    {
        toggleGroupManager = GetComponent<ToggleSwitchGroupManager>();
        SpawnElements();
    }

    private void SpawnElements()
    {
        for (int i = 0; i < elementUis.Length; i++)
        {
            var element = elementUis[i];
            element.OnDeactivated();
            // Wire visual callbacks vào UnityEvent của ToggleSwitch
            var ts = element.ToggleSwitch;
            ts.onToggleOn.AddListener(element.OnActivated);
            ts.onToggleOff.AddListener(element.OnDeactivated);
        }
        elementUis[0].OnActivated();
        
        toggleGroupManager.Setup();
        toggleGroupManager.Init();
    }
}