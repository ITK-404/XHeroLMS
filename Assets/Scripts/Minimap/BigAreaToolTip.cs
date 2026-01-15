using UnityEngine;

public class BigAreaToolTip : MonoBehaviour
{
    [SerializeField] private BigAreaTooltipUI tooltipArea;

    public void ShowTooltip(BigArea bigArea)
    {
        if (bigArea == null)
        {
            Debug.Log("This area is null, please checkout");
            return;
        }
        
        tooltipArea.Show();
        tooltipArea.ShowTooltip(bigArea);
    }
}