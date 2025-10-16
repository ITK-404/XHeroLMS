using UnityEngine;

public class OpenDoor : MonoBehaviour
{
    public Animator doorAnimator;
    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            // open
            doorAnimator.SetBool("IsOpen", true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            // close
            doorAnimator.SetBool("IsOpen", false);
        }
    }

    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player");
    }
}
