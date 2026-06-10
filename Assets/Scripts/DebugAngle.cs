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

    private string BuildSelectMotorLogSyncState()
    {
      if (SelectMotorCollider == null)
      {
        return "\n[SyncLogs]\nSelectMotorCollider: N/A";
      }

      bool freezeFeatureEnabled = SelectMotorCollider.enableFreezeMotorFeature;
      bool hasModeSwitching = SelectMotorCollider.modeSwitching != null;

      // Mirror the same condition branches used by SelectMotorCollider debug logs.
      bool aaaa1111 = !freezeFeatureEnabled || !hasModeSwitching;
      bool aaaa2222 = !aaaa1111;
      bool bbbb = hasModeSwitching;
      bool cccc1111 = !hasModeSwitching;
      bool cccc2222 = hasModeSwitching;

      return "\n[SyncLogs]" +
           "\nAAAA1111: " + aaaa1111 +
           "\nAAAA2222: " + aaaa2222 +
           "\nBBBB: " + bbbb +
           "\nCCCC1111: " + cccc1111 +
           "\nCCCC2222: " + cccc2222;
    }

    void Update()
    {
        if (angleText == null)
            return;

        string syncLogState = BuildSelectMotorLogSyncState();

        angleText.text = 
              // "thumbGripperJoint1Max.y: " + clawModuleController.thumbGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
              // "thumbGripperJoint1Min.y: " + clawModuleController.thumbGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
              // "indexGripperJoint1Max.y: " + clawModuleController.indexGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
              // "indexGripperJoint1Min.y: " + clawModuleController.indexGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
              // "middleGripperJoint1Max.y: " + clawModuleController.middleGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
              // "middleGripperJoint1Min.y: " + clawModuleController.middleGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
              // "thumbGripperJoint2Max.z: " + clawModuleController.thumbGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
              // "thumbGripperJoint2Min.z: " + clawModuleController.thumbGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
              // "indexGripperJoint2Max.z: " + clawModuleController.indexGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
              // "indexGripperJoint2Min.z: " + clawModuleController.indexGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
              // "middleGripperJoint2Max.z: " + clawModuleController.middleGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
              // "middleGripperJoint2Min.z: " + clawModuleController.middleGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
              // "\n" +
              // "ThumbTip Touched:  " + (triggerRightThumbTip  != null ? triggerRightThumbTip.isRightThumbTipTouched.ToString()  : "N/A") + "\n" +
              // "IndexTip Touched:  " + (rightIndexTip          != null ? rightIndexTip.isRightIndexTipTouched.ToString()          : "N/A") + "\n" +
              // "MiddleTip Touched: " + (rightMiddleTip         != null ? rightMiddleTip.isRightMiddleTipTouched.ToString()        : "N/A") + "\n" +
              //   "Priority Collider: " + (SelectMotorCollider    != null ? SelectMotorCollider.debugFingerPriority.ToString()       : "N/A") + "\n" +
              //   "Index->Baseline Angle: " + (jointAngle != null ? jointAngle.indexToBaselineAngleOnPalm.ToString("F2") : "N/A") + "\n" +
              //   "Middle->Baseline Angle: " + (jointAngle != null ? jointAngle.middleToBaselineAngleOnPalm.ToString("F2") : "N/A") +
                syncLogState;
    }
}
