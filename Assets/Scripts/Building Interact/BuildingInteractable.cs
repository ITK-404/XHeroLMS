using UnityEngine;

public class BuildingInteractable : MonoBehaviour
{
    public Transform standPosition;

    public Transform GetStandTransform()
    {
        return standPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(standPosition.position, standPosition.forward);
    }
}
