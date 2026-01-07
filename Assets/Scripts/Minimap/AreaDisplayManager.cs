using System;
using UnityEngine;

public class AreaDisplayManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private BigArea[] bigAreasLocation;
    [SerializeField] private BigMapUI bigMapUIPrefab;
    [Header("Others")]
    [SerializeField] private GameObject uiAreaContainer;
    [SerializeField] private GameObject locationAreaContainer;
    [SerializeField] private Camera wrapperCamera;


    private void Awake()
    {
        Hide();
    }

    private void Start()
    {
        bigAreasLocation = locationAreaContainer.GetComponentsInChildren<BigArea>();

        if (bigAreasLocation == null) return;

        foreach (var bigMap in bigAreasLocation)
        {
            var bigMapUI = Instantiate(bigMapUIPrefab, uiAreaContainer.transform);
            bigMapUI.SetData(bigMap.Data);
            
            var areaUI = bigMapUI.AreaMapUI;
            areaUI.Setup(wrapperCamera, bigMap.Location);
        }
    }

    public void Show()
    {
        uiAreaContainer.gameObject.SetActive(true);   
    }

    public void Hide()
    {
        uiAreaContainer.gameObject.SetActive(false);
    }
}