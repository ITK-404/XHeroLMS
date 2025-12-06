using UnityEngine;

public class ChairCheckPoint : MonoBehaviour
{
    public GameObject spriteCheckPoint;
    public GameObject checkPoint;
    public GameObject examCheckPoint;
    public GameObject player;
    public void Show(bool isShow)
    {
        spriteCheckPoint.gameObject.SetActive(isShow);
    }

    private void LateUpdate()
    {
        if(player == null)
        {
            return;
        }

        var direction = player.transform.position - transform.position;

        if (direction.y < 0f)
            direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
            spriteCheckPoint.transform.forward = direction.normalized;
    }
    private bool IsPlayer(GameObject other)
    {
        return other.CompareTag("Player");
    }


    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            //Debug.Log("Enter d�nh player", other.gameObject);
            PlayerChairManager.Instance.TrySetChair(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other.gameObject))
        {
            //Debug.Log("Exit d�nh player",gameObject);
            PlayerChairManager.Instance.TryRemoveChair(this);
        }
    }
}
