using TMPro;
using UnityEngine;

public class PlotAreaUI : MonoBehaviour
{
    public TextMeshProUGUI displayText;
    public TextMeshProUGUI titleText;
    
    public AreaMapUI AreaMapUI;

    public GameObject container;

    public float scaleUI = 1;
    
    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}