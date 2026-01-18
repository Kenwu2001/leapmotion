using UnityEngine;

public class FingerPath : MonoBehaviour
{
    [Header("Joints in order (originally 5: J0 → J4, now can be 2 or more)")]
    public Transform[] joints = new Transform[5];

    public Vector3 GetJoint(int index)
    {
        if (index < 0 || index >= joints.Length)
        {
            Debug.LogError($"Joint index {index} out of range [0, {joints.Length - 1}]");
            return Vector3.zero;
        }
        return joints[index].position;
    }

    public int GetJointCount()
    {
        return joints.Length;
    }
}