using TMPro;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public class BigMapUI : MonoBehaviour
{
    [SerializeField] private AreaMapUI areaMapUI;
    public AreaMapUI AreaMapUI => areaMapUI;
    
    [SerializeField] private TextMeshProUGUI displayNameTxt;
    [SerializeField] private Image iconImg;
    [SerializeField] private RectTransform uiButton;
    private AreaMapData data;
    
    public void SetDisplayName(string displayName)
    {
        displayNameTxt.text = displayName;
    }

    public void SetData(AreaMapData bigMapData)
    {
        this.data = bigMapData;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (data == null)
        {
            Debug.LogError("AreaMapData is null",gameObject);
            return;
        }
        
        displayNameTxt.text = data.displayName;
        iconImg.sprite = data.displayIcon;
    }

    private float padding = 10;
    
    public void CalculatorHitbox(SplineContainer splineContainer,Camera mainCamera)
    {
        
    }
}