using UnityEngine;

public static class PlayerLocator
{
    public static Vector3 WorldPosition;
    public static Quaternion WorldRotation;
    public static bool isSaved = false;


    public static void PlacePlayer(GameObject player, Vector3 pos, Quaternion rot)
    {
        var cc = player.GetComponent<CharacterController>();
        var rb = player.GetComponent<Rigidbody>();
        bool hadRB = rb != null;
        bool rbWasKinematic = false;

        if (hadRB)
        {
#if UNITY_2023_OR_NEWER
            rb.linearVelocity = Vector3.zero;
#else
            rb.linearVelocity = Vector3.zero;
#endif
            rb.angularVelocity = Vector3.zero;
            rbWasKinematic = rb.isKinematic;
            rb.isKinematic = true;
        }

        if (cc) cc.enabled = false;

        player.transform.SetPositionAndRotation(pos, rot);

        if (hadRB) rb.isKinematic = rbWasKinematic;
        if (cc) cc.enabled = true;
    }
}
