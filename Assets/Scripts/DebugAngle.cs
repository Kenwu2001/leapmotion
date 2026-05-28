using TMPro; // Import TextMeshPro namespace
using UnityEngine;

public class DebugAngle : MonoBehaviour
{
    public TextMeshPro angleText; // Manually drag into Inspector
    public JointAngle jointAngle; // Drag in the angle source script
    public ClawModuleController clawModuleController;
    public TriggerRightThumbTip triggerRightThumbTip;
    public TriggerRightIndexTip rightIndexTip;
    public TriggerRightMiddleTip rightMiddleTip;
    public TriggerIndexInnerExtension triggerIndexInnerExtension;
    public TriggerThumbInnerExtension triggerThumbInnerExtension;
    public Transform clawIndexFingerTip;
    public Transform R_IndexTriggerTip;
    public ThreeFingerCollisionDetector threeFingerCollisionDetector;
    public RetargetIndex retargetIndex;
    public SelectMotorCollider SelectMotorCollider;
    public ModeSwitching modeSwitching;
    public PaxiniValue paxiniValue;
    public TcpSender tcpSender;
    public TriggerRightWrist triggerRightWrist;
    // public ControllerLocatorLeft controllerLocatorLeft;

    public GameObject r_wrist;

    // middle 59 58 57
    // index 302 303 304

    void Update()
    {
        if (angleText == null)
            return;

        angleText.text = clawModuleController == null
            ? "thumbGripperJoint1MaxRotationVector.y: N/A\n" +
              "thumbGripperJoint1MinRotationVector.y: N/A"
            : "thumbGripperJoint1MaxRotationVector.y: " + clawModuleController.thumbGripperJoint1MaxRotationVector.y.ToString("F4") + "\n" +
              "thumbGripperJoint1MinRotationVector.y: " + clawModuleController.thumbGripperJoint1MinRotationVector.y.ToString("F4");
    }
}
