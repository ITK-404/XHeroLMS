using UnityEngine;

public class AreaMapLocationByHouse : AreaMapLocation
{
    [SerializeField] private HouseHandler houseHandler;
    public override Vector3 GetItemWorldPosition()
    {
        if (houseHandler == null)
        {
            return base.GetItemWorldPosition();
        }

        return houseHandler.GetStandPoint();
    }
}