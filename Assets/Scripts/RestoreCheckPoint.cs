using UnityEngine;

public class RestoreCheckPoint : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private void Start()
    {
        if (PlayerLocator.isSaved)
        {
            var pos = PlayerLocator.WorldPosition;
            var rot = PlayerLocator.WorldRotation;
            PlayerLocator.PlacePlayer(player, pos, rot);
            PlayerLocator.isSaved = false;
        }
    }
}

