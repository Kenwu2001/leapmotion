using UnityEngine;

public class FingerPath : MonoBehaviour
{
    [Header("5 joints in order J0 → J4")]
    public Transform[] joints = new Transform[5];

    public Vector3 GetJoint(int index)
    {
        return joints[index].position;
    }
}