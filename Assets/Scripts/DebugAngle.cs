using TMPro; // Import TextMeshPro namespace
using UnityEngine;
using System.Collections.Generic;

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
    public ControllerLocatorLeft controllerLocatorLeft;

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

      string clockwiseRaw = jointAngle != null ? jointAngle.isClockWise.ToString("F3") : "N/A";
      string rotationDirection = "N/A";
      if (jointAngle != null)
      {
        if (jointAngle.isClockWise > 0.1f)
          rotationDirection = "Clockwise";
        else if (jointAngle.isClockWise < -0.1f)
          rotationDirection = "Counterclockwise";
        else
          rotationDirection = "Neutral";
      }

      string planeActiveRaw = jointAngle != null ? jointAngle.isPlaneActive.ToString() : "N/A";
      string activeFingerRaw = jointAngle != null ? jointAngle.activeFinger : "N/A";
      string planeDebug = GetPlaneActivationDebug();
      string syncLogState = BuildSelectMotorLogSyncState();
      int fingerPriorityValue = SelectMotorCollider != null ? SelectMotorCollider.debugFingerPriority : -1;
      string controllerDistanceText = "N/A";
      if (controllerLocatorLeft != null)
      {
        controllerDistanceText = controllerLocatorLeft.isControllerSeparationValid
          ? controllerLocatorLeft.currentControllerSeparationDistance.ToString("F3") + " m"
          : "Controller pose unavailable";
      }

      string fingerPriorityText = fingerPriorityValue switch
      {
        0 => "0 (none)",
        1 => "1 (index priority, middle blocked)",
        2 => "2 (middle priority, index blocked)",
        _ => "N/A"
      };

      angleText.text =
        // "thumbGripperJoint1Max.y: " + clawModuleController.thumbGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
        //   "thumbGripperJoint1Min.y: " + clawModuleController.thumbGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
        //   "indexGripperJoint1Max.y: " + clawModuleController.indexGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
        //   "indexGripperJoint1Min.y: " + clawModuleController.indexGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
        //   "middleGripperJoint1Max.y: " + clawModuleController.middleGripperJoint1MaxRotationVector.y.ToString("F2") + "\n" +
        //   "middleGripperJoint1Min.y: " + clawModuleController.middleGripperJoint1MinRotationVector.y.ToString("F2") + "\n" +
        //   "thumbGripperJoint2Max.z: " + clawModuleController.thumbGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
        //   "thumbGripperJoint2Min.z: " + clawModuleController.thumbGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
        //   "indexGripperJoint2Max.z: " + clawModuleController.indexGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
        //   "indexGripperJoint2Min.z: " + clawModuleController.indexGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
        //   "middleGripperJoint2Max.z: " + clawModuleController.middleGripperJoint2MaxRotationVector.z.ToString("F2") + "\n" +
        //   "middleGripperJoint2Min.z: " + clawModuleController.middleGripperJoint2MinRotationVector.z.ToString("F2") + "\n" +
        //   "\n" +
        // "ThumbTip Touched:  " + (triggerRightThumbTip  != null ? triggerRightThumbTip.isRightThumbTipTouched.ToString()  : "N/A") + "\n" +
        // "IndexTip Touched:  " + (rightIndexTip          != null ? rightIndexTip.isRightIndexTipTouched.ToString()          : "N/A") + "\n" +
        // "MiddleTip Touched: " + (rightMiddleTip         != null ? rightMiddleTip.isRightMiddleTipTouched.ToString()        : "N/A") + "\n" +
        // "Priority Collider: " + (SelectMotorCollider    != null ? SelectMotorCollider.debugFingerPriority.ToString()       : "N/A") + "\n" +
        // "Index->Baseline Angle: " + (jointAngle != null ? jointAngle.indexToBaselineAngleOnPalm.ToString("F2") : "N/A") + "\n" +
        // "Middle->Baseline Angle: " + (jointAngle != null ? jointAngle.middleToBaselineAngleOnPalm.ToString("F2") : "N/A") + "\n" +
        // "modeSelect: " + (modeSwitching != null ? modeSwitching.modeSelect.ToString() : "N/A") + "\n" +
        // "modeManipulate: " + (modeSwitching != null ? modeSwitching.modeManipulate.ToString() : "N/A") + "\n" +
        // "isPlaneActive: " + planeActiveRaw + "\n" +
        // "activeFinger: " + activeFingerRaw + "\n" +
        // planeDebug + "\n" +
        "Clockwise Raw: " + clockwiseRaw + "\n" +
        "Rotation Direction: " + rotationDirection + "\n" +
        "Finger Priority: " + fingerPriorityText + "\n" +
        "L/R Controller Distance: " + controllerDistanceText;
        // syncLogState;
    }

    private string GetPlaneActivationDebug()
    {
      bool indexHasPair = HasIndexThumbPair(rightIndexTip);
      bool middleHasPair = HasIndexThumbPair(rightMiddleTip);
      bool thumbHasPair = HasIndexThumbPair(triggerRightThumbTip);

      string chosenSource = "None";
      if (indexHasPair)
        chosenSource = "IndexTrigger";
      else if (middleHasPair)
        chosenSource = "MiddleTrigger";
      else if (thumbHasPair)
        chosenSource = "ThumbTrigger";

      return "PlaneCheck IndexPair:" + indexHasPair +
           " MiddlePair:" + middleHasPair +
           " ThumbPair:" + thumbHasPair +
           " Source:" + chosenSource;
    }

    private bool HasIndexThumbPair(TriggerRightIndexTip trigger)
    {
      if (trigger == null)
        return false;

      Dictionary<string, Vector3> points = trigger.GetAllTouchedPoints();
      return points != null && points.ContainsKey("L_IndexTip") && points.ContainsKey("L_ThumbTip");
    }

    private bool HasIndexThumbPair(TriggerRightMiddleTip trigger)
    {
      if (trigger == null)
        return false;

      Dictionary<string, Vector3> points = trigger.GetAllTouchedPoints();
      return points != null && points.ContainsKey("L_IndexTip") && points.ContainsKey("L_ThumbTip");
    }

    private bool HasIndexThumbPair(TriggerRightThumbTip trigger)
    {
      if (trigger == null)
        return false;

      Dictionary<string, Vector3> points = trigger.GetAllTouchedPoints();
      return points != null && points.ContainsKey("L_IndexTip") && points.ContainsKey("L_ThumbTip");
    }
}
