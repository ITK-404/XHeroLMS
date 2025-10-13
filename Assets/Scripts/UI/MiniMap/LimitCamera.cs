using UnityEngine;

public class LimitCamera:MonoBehaviour
{
    public GameObject player;

    /// <summary>
    /// LateUpdate is called every frame, if the Behaviour is enabled.
    /// It is called after all Update functions have been called.
    /// </summary>
    private void LateUpdate()
    {
        transform.position = new Vector3(player.transform.position.x, 40f, player.transform.position.z);
    }
}
