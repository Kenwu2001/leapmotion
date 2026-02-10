using UnityEngine;

[RequireComponent(typeof(FingerPath))]
public class FingerLineRenderer : MonoBehaviour
{
    public LineRenderer line;
    
    [Header("Two-Point Mode")]
    [Tooltip("When enabled, only draw tip and base (2 points) instead of all joints")]
    public bool useTwoPointMode = false;
    
    private FingerPath finger;

    void Awake()
    {
        finger = GetComponent<FingerPath>();
    }

    void LateUpdate()
    {
        if (line == null) return;

        int jointCount = finger.GetJointCount();
        
        if (useTwoPointMode && jointCount >= 2)
        {
            // Two-point mode: only draw tip (joint 0) and base (last joint)
            line.positionCount = 2;
            line.SetPosition(0, finger.GetJoint(0));
            line.SetPosition(1, finger.GetJoint(jointCount - 1));
        }
        else
        {
            // Default: draw all joints
            line.positionCount = jointCount;
            for (int i = 0; i < jointCount; i++)
            {
                line.SetPosition(i, finger.GetJoint(i));
            }
        }
    }
}