using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target;

    [Header("Offset")]
    public Vector3 localOffset = Vector3.zero; // Apply offset in target's local space
    public bool followRotation = true;

    void LateUpdate()
    {
        if (!target) return;

        // Follow position (with local offset)
        transform.position = target.TransformPoint(localOffset);

        if (followRotation)
        {
            // Only use target's X rotation
            Vector3 targetEuler = target.rotation.eulerAngles;

            // Only follow X axis rotation, others fixed to 0
            transform.rotation = Quaternion.Euler(targetEuler.x, 0f, 0f);
        }
    }
}