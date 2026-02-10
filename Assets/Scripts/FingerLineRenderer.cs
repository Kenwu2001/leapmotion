using UnityEngine;

[RequireComponent(typeof(FingerPath))]
public class FingerLineRenderer : MonoBehaviour
{
    public LineRenderer line;
    
    [Header("Auto-Sync")]
    [Tooltip("Reference to SelectMotorCollider to auto-sync visualization with projection mode")]
    public SelectMotorCollider selectMotorCollider;
    
    [Tooltip("Which fingertip this line renderer represents (4=Thumb, 8=Index, 12=Middle)")]
    public int fingertipID = 0;
    
    [HideInInspector] public bool useTwoPointMode = false;
    
    private FingerPath finger;

    void Awake()
    {
        finger = GetComponent<FingerPath>();
    }

    void LateUpdate()
    {
        if (line == null) return;
        
        // Respect showDebugLines toggle and force-hidden state from SelectMotorCollider
        if (selectMotorCollider != null && (!selectMotorCollider.showDebugLines || selectMotorCollider.isVisualsForceHidden))
        {
            line.enabled = false;
            return;
        }
        
        // If line was disabled by FingerRendererManager (wrong mode), don't override
        if (!line.enabled) return;

        int jointCount = finger.GetJointCount();
        
        if (selectMotorCollider != null)
        {
            switch (selectMotorCollider.projectionMode)
            {
                case ProjectionMode.FrozenLine:
                    useTwoPointMode = true;
                    // If frozen is active for THIS finger, show frozen positions
                    if (selectMotorCollider.isFrozenLineActive && selectMotorCollider.frozenFingerID == fingertipID)
                    {
                        Vector3 tipWorld, baseWorld;
                        selectMotorCollider.GetFrozenWorldPositions(out tipWorld, out baseWorld);
                        if (tipWorld != Vector3.zero || baseWorld != Vector3.zero)
                        {
                            line.positionCount = 2;
                            line.SetPosition(0, tipWorld);
                            line.SetPosition(1, baseWorld);
                            return; // Done, skip normal drawing
                        }
                    }
                    // Not frozen yet or different finger → show live 2-point
                    if (jointCount >= 2)
                    {
                        line.positionCount = 2;
                        line.SetPosition(0, finger.GetJoint(0));
                        line.SetPosition(1, finger.GetJoint(jointCount - 1));
                    }
                    return;
                    
                case ProjectionMode.TwoPoint:
                    useTwoPointMode = true;
                    break;
                    
                default: // FivePoint
                    useTwoPointMode = false;
                    break;
            }
        }
        
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