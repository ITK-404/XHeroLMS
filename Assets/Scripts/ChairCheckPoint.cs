using UnityEngine;

public class ChairCheckPoint : MonoBehaviour
{
    public GameObject spriteCheckPoint;

    public void Show(bool isShow)
    {
        spriteCheckPoint.gameObject.SetActive(spriteCheckPoint);
    }


}