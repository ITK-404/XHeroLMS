using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Transform player;

    void Update()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;

        // Nếu player ở bên trái hoặc phải so với image
        if (toPlayer.x < 0)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }
}
