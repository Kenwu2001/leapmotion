using UnityEngine;

[RequireComponent(typeof(FingerPath))]
public class FingerLineRenderer : MonoBehaviour
{
    public LineRenderer line;
    private FingerPath finger;

    void Awake()
    {
        finger = GetComponent<FingerPath>();
    }

    void LateUpdate()
    {
        if (line == null) return;

        int jointCount = finger.GetJointCount();
        line.positionCount = jointCount;
        for (int i = 0; i < jointCount; i++)
        {
            line.SetPosition(i, finger.GetJoint(i));
        }
    }
}