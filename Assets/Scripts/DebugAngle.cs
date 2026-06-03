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
            ? "thumbGripperJoint1Max.y: N/A\nthumbGripperJoint1Min.y: N/A\n" +
              "indexGripperJoint1Max.y: N/A\nindexGripperJoint1Min.y: N/A\n" +
              "middleGripperJoint1Max.y: N/A\nmiddleGripperJoint1Min.y: N/A\n" +
              "thumbGripperJoint2Max.z: N/A\nthumbGripperJoint2Min.z: N/A\n" +
              "indexGripperJoint2Max.z: N/A\nindexGripperJoint2Min.z: N/A\n" +
              "middleGripperJoint2Max.z: N/A\nmiddleGripperJoint2Min.z: N/A"
            : "thumbGripperJoint1Max.y: " + clawModuleController.thumbGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
              "thumbGripperJoint1Min.y: " + clawModuleController.thumbGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
              "indexGripperJoint1Max.y: " + clawModuleController.indexGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
              "indexGripperJoint1Min.y: " + clawModuleController.indexGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
              "middleGripperJoint1Max.y: " + clawModuleController.middleGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
              "middleGripperJoint1Min.y: " + clawModuleController.middleGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
              "thumbGripperJoint2Max.z: " + clawModuleController.thumbGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
              "thumbGripperJoint2Min.z: " + clawModuleController.thumbGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
              "indexGripperJoint2Max.z: " + clawModuleController.indexGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
              "indexGripperJoint2Min.z: " + clawModuleController.indexGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
              "middleGripperJoint2Max.z: " + clawModuleController.middleGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
              "middleGripperJoint2Min.z: " + clawModuleController.middleGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
              "\n" +
              "ThumbTip Touched:  " + (triggerRightThumbTip  != null ? triggerRightThumbTip.isRightThumbTipTouched.ToString()  : "N/A") + "\n" +
              "IndexTip Touched:  " + (rightIndexTip          != null ? rightIndexTip.isRightIndexTipTouched.ToString()          : "N/A") + "\n" +
              "MiddleTip Touched: " + (rightMiddleTip         != null ? rightMiddleTip.isRightMiddleTipTouched.ToString()        : "N/A") + "\n" +
              "Priority Collider: " + (SelectMotorCollider    != null ? SelectMotorCollider.debugFingerPriority.ToString()       : "N/A");
    }
}
