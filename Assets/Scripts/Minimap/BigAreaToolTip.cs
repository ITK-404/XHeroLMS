using System;
using UnityEngine;

public class BigAreaToolTip : MonoBehaviour
{
    [SerializeField] private BigAreaTooltipUI tooltipArea;
    [SerializeField] private PointClickSystem player;
    [SerializeField] private MinimapManager _minimapManager;

    private BigArea catchArea;

    private void Awake()
    {
        tooltipArea.OnClickFindPathAction += PlayerGoLocation;
    }

    private void OnDestroy()
    {
        tooltipArea.OnClickFindPathAction -= PlayerGoLocation;
    }

    private void PlayerGoLocation()
    {
        if (catchArea == null)
            return;
        _minimapManager.ToggleOffMinimap();
        player.MoveToPosition(catchArea.StandCheckPoint.position);
    }
    
    public void ShowTooltip(BigArea bigArea)
    {
        if (bigArea == null)
        {
            Debug.Log("This area is null, please checkout");
            return;
        }
        
        tooltipArea.Show();
        tooltipArea.ShowTooltip(bigArea);

        catchArea = bigArea;
    }
}