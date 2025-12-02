using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AvoidZoneBox : MonoBehaviour
{
    public Vector3 size = new Vector3(5, 5, 5);
    public float pushForce = 8f;

    private void Start()
    {
        var col = GetComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Vector3 dir = (other.transform.position - transform.position).normalized;
        other.transform.position += dir * pushForce * Time.deltaTime;
    }
}
