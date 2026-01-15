using UnityEngine;

public class BigAreaToolTip : MonoBehaviour
{
    [SerializeField] private PlotHandlerUI plotHandlerUI;
    [SerializeField] private BigAreaTooltipUI tooltipArea;
    private void Awake()
    {
        plotHandlerUI.OnClickShowBigArea += OnClickShowBigArea;
    }

    private void OnDestroy()
    {
        plotHandlerUI.OnClickShowBigArea -= OnClickShowBigArea;
    }

    private void OnClickShowBigArea()
    {
        var bigArea = AreaDisplayManager.Instance.SelectArea;

        if (bigArea == null)
        {
            Debug.Log("This area is null, please checkout");
            return;
        }
        
        tooltipArea.Show();
        tooltipArea.ShowTooltip(bigArea);
    }
}