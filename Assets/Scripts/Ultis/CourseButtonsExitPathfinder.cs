using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CourseButtonsExitPathfinder : MonoBehaviour
{
    [SerializeField] private Button[] rotationButtons;
    [SerializeField] private Button exitBtn;
    [SerializeField] private Transform target;
    [SerializeField] private PointClickSystem player;

    private void Awake()
    {
        // rotation
        foreach (var button in rotationButtons)
        {
            button.onClick.AddListener(RotateToTarget);
        }
        exitBtn.onClick.AddListener(FindNodeForPathfinding);
    }

    private void OnDestroy()
    {
        // rotation
        foreach (var button in rotationButtons)
        {
            button.onClick.RemoveListener(RotateToTarget);
        }
        exitBtn.onClick.RemoveListener(FindNodeForPathfinding);
    }

    private void FindNodeForPathfinding()
    {
        if (player == null)
        {
            Debug.LogError("Player is null cannot move to it");
            return;
        }
        player.GetComponent<PointClickSystem>().MoveToPosition(target.transform.position);
    }

    private void RotateToTarget()
    {
        var directionToTarget = target.transform.position - player.transform.position;
        directionToTarget.y = 0;
        var lookQuaternion = Quaternion.LookRotation(directionToTarget);
        player.transform.DORotateQuaternion(lookQuaternion, 2);
    }
}