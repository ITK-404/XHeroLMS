using UnityEngine;

public class ChairCheckPoint : MonoBehaviour
{
    public GameObject spriteCheckPoint;
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

}