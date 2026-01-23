using UnityEngine;
using UnityEngine.Audio;

public class EntityFootstep : MonoBehaviour
{
    public CharacterController controller;   // hoặc Rigidbody
    public AudioSource audioSource;


    private Vector3 lastPosition;

    [SerializeField] private float maxVolume = 1f;

    void Start()
    {
        lastPosition = transform.position;
        audioSource.Play();
        audioSource.volume = 0;
    }

    private float lerpValue;
    private float lerpSpeed = 5;
    void Update()
    {
        lastPosition = transform.position;

        bool isFreeze = !controller.isGrounded || controller.velocity.magnitude < 0.1f;
        float targetValue = isFreeze ? 0 : 1;
        lerpValue = Mathf.Lerp(lerpValue, targetValue, Time.deltaTime * lerpSpeed);
        audioSource.volume = lerpValue;
    }
}
