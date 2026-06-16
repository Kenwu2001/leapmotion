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

      // Thumb-Only Rotation Mode debug
      string thumbOnlyModeText = "N/A";
      // if (jointAngle != null)
      // {
      //   bool modeOn = jointAngle.newRotationMode;
      //   if (modeOn)
      //   {
      //     thumbOnlyModeText = "ENABLED" +
      //       "\n  L_index0(L_index_a) pos: " + jointAngle.indexTipPos.ToString("F3") +
      //       "\n  L_ThumbTip pos:          " + jointAngle.thumbTipPos.ToString("F3") +
      //       "\n  Plane Active: " + jointAngle.isPlaneActive +
      //       "\n  Direction: " + rotationDirection +
      //       "\n  isClockWise: " + clockwiseRaw;
      //   }
      //   else
      //   {
      //     thumbOnlyModeText = "disabled";
      //   }
      // }

      string fingerPriorityText = fingerPriorityValue switch
      {
        0 => "0 (none)",
        1 => "1 (index priority, middle blocked)",
        2 => "2 (middle priority, index blocked)",
        _ => "N/A"
      };

      string newTouchDebug = "";
      if (jointAngle != null)
      {
        newTouchDebug = "\n[NEW TOUCH MODE]" +
          "\n  indexNewTouch: " + jointAngle.indexNewTouch +
          "\n  middleNewTouch: " + jointAngle.middleNewTouch +
          "\n  thumbNewTouch: " + jointAngle.thumbNewTouch;
      }

      angleText.text =
        "Clockwise Raw: " + clockwiseRaw + "\n" +
        "Rotation Direction: " + rotationDirection + "\n" +
        "Finger Priority: " + fingerPriorityText + "\n" +
        "L/R Controller Distance: " + controllerDistanceText + "\n" +
        "[ThumbOnly Mode]: " + thumbOnlyModeText +
        // "\nThumb Legacy Touch: " + (jointAngle != null ? jointAngle.thumbLegacyTouch.ToString() : "N/A") +
        // "\nIndex Legacy Touch: " + (jointAngle != null ? jointAngle.indexLegacyTouch.ToString() : "N/A") + "\n" +
        // "Middle Legacy Touch: " + (jointAngle != null ? jointAngle.middleLegacyTouch.ToString() : "N/A") +
        newTouchDebug;
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
