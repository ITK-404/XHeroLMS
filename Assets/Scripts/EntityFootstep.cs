using UnityEngine;
using UnityEngine.Audio;

public class EntityFootstep : MonoBehaviour
{
    public CharacterController controller;   // hoặc Rigidbody
    public AudioSource audioSource;
    public AudioClip[] footstepClips;

    public float stepDistance = 2.0f;        // đi bao nhiêu mét thì phát 1 bước
    private float accumulatedDistance = 0f;

    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;
    }
    
    void Update()
    {
        float dist = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (!controller.isGrounded || controller.velocity.magnitude < 0.1f)
        {
            return;
        }

        accumulatedDistance += dist;

        if (accumulatedDistance >= stepDistance)
        {
            PlayFootstep();
            accumulatedDistance = 0f;
        }
    }

    void PlayFootstep()
    {
        if (footstepClips.Length == 0) return;

        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        audioSource.PlayOneShot(clip);
    }
}
