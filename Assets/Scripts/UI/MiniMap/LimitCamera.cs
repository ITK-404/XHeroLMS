using UnityEngine;

public class LimitCamera:MonoBehaviour
{
    public GameObject player;
    [SerializeField] private float heighPositionY = 40f;
    /// <summary>
    /// LateUpdate is called every frame, if the Behaviour is enabled.
    /// It is called after all Update functions have been called.
    /// </summary>
    private void LateUpdate()
    {
        transform.position = new Vector3(player.transform.position.x, heighPositionY, player.transform.position.z);
    }
}
