using UnityEngine;

public class ChairCheckPoint : MonoBehaviour
{
    public GameObject spriteCheckPoint;
    public GameObject checkPoint;
    public GameObject player;
    public void Show(bool isShow)
    {
        spriteCheckPoint.gameObject.SetActive(spriteCheckPoint);
    }

    private void LateUpdate()
    {
        var direction = player.transform.position - transform.position;
        direction.Normalize();
        spriteCheckPoint.transform.forward = direction;
    }

    private bool IsPlayer(GameObject other)
    {
        return other.CompareTag("Player");
    }
    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            Debug.Log("Enter dính player", other.gameObject);
            PlayerChairManager.Instance.TrySetChair(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            Debug.Log("Exit dính player",gameObject);
            PlayerChairManager.Instance.TryRemoveChair(this);
        }
    }

    public void Sitdown()
    {

    }
}