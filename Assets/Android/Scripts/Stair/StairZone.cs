using UnityEngine;

public class StairZone : MonoBehaviour
{
    public Vector3 standPoint;

    public float radius = 1;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.rebeccaPurple;
        Gizmos.DrawWireSphere(transform.position + standPoint, 1);
    }
}
