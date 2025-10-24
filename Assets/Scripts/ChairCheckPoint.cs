using UnityEngine;

public class ChairCheckPoint : MonoBehaviour
{
    public GameObject spriteCheckPoint;
    public GameObject checkPoint;
    public GameObject player;
    public void Show(bool isShow)
    {
        spriteCheckPoint.gameObject.SetActive(isShow);
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

    public void Sitdown()
    {

    }
}
