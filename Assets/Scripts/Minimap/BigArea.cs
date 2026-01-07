using UnityEngine;

public class BigArea : MonoBehaviour
{
    private AreaMapLocation location;
    public AreaMapLocation Location => location;
    [SerializeField] private AreaMapData mapData;
    public AreaMapData Data => mapData;
    private void Awake()
    {
        location = GetComponent<AreaMapLocation>();
        LoadForDebug();
    }

    private void LoadForDebug()
    {
        if (mapData != null)
        {
            gameObject.name = "Big Area: " + mapData.displayName;
        }
    }

}