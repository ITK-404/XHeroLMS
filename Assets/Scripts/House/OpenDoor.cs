using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    private static readonly int IsOpen = Animator.StringToHash("IsOpen");
    public Animator doorAnimator;
    public Collider TriggerDoorCol;
    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            // open
            Debug.Log("Door trigger enter",gameObject);
            doorAnimator.SetBool(IsOpen, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            // close
            Debug.Log("Door trigger exit",gameObject);
            doorAnimator.SetBool(IsOpen, false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }
}
