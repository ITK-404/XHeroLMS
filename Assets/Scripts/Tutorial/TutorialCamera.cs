using DG.Tweening;
using UnityEngine;

public class TutorialCamera : MonoBehaviour
{
    [SerializeField] private TutorialHandler _tutorialHandler;
    public GameObject player;
    public GameObject target;
    private void Awake()
    {
        _tutorialHandler = GetComponent<TutorialHandler>();
    }

    private void Start()
    {
        // if (_tutorialHandler.IsPlayedBefore())
        // {
        //     return;
        // }
        if (_tutorialHandler != null)
        {
            if (_tutorialHandler.IsPlayedBefore())
            {
                return;
            }
        }
        
        var direction = target.transform.position - player.transform.position;
        direction.Normalize();
        direction.y = 0;
        var targetRotation = Quaternion.LookRotation(direction);
        player.transform.DORotateQuaternion(targetRotation, 3f);
    }
}