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
    public Transform clawIndexFingerTip;
    public Transform R_IndexTriggerTip;
    public ThreeFingerCollisionDetector threeFingerCollisionDetector;
    public SelectMotorCollider SelectMotorCollider;
    public ModeSwitching modeSwitching;
    public PaxiniValue paxiniValue;
    public TcpSender tcpSender;
    public TriggerRightWrist triggerRightWrist;
    public ControllerLocatorLeft controllerLocatorLeft;
    public ArmUIPlaneController armUIPlaneController;
    // public VirtualLeftHandButtonInteractor virtualLeftHandButtonInteractor;

    public GameObject r_wrist;

    private bool _lastIndexRotationColliderMode = false;
    private bool _lastMiddleRotationColliderMode = false;
    private float _latchedIndexEntryAngle = 0f;
    private bool _hasLatchedIndexEntryAngle = false;
    private float _latchedMiddleEntryAngle = 0f;
    private bool _hasLatchedMiddleEntryAngle = false;

    // middle 59 58 57
    // index 302 303 304

    private string BuildMotorFreezeStateText()
    {
        if (modeSwitching == null || SelectMotorCollider == null)
            return "[Motor Freeze State]\nN/A\n";

        bool[] frozen = modeSwitching.singleMotorFrozen;
        bool thumbPaxini  = SelectMotorCollider.thumbFreezeEnabled;
        bool indexPaxini  = SelectMotorCollider.indexFreezeEnabled;
        bool middlePaxini = SelectMotorCollider.middleFreezeEnabled;

        string MotorState(int id)
        {
            if (id < 1 || id > 12) return "?";
            if (frozen[id - 1]) return "freeze";
            if (modeSwitching.confirmedMotorID == id) return "on";
            return "off";
        }

        string PaxState(bool on) => on ? "freeze on" : "freeze off";

        return "[Motor Freeze State]\n" +
            $"(m1,m2,m3,m4,m13) = ({MotorState(1)},{MotorState(2)},{MotorState(3)},{MotorState(4)},{PaxState(thumbPaxini)})\n" +
            $"(m5,m6,m7,m8,m14) = ({MotorState(5)},{MotorState(6)},{MotorState(7)},{MotorState(8)},{PaxState(indexPaxini)})\n" +
            $"(m9,m10,m11,m12,m15) = ({MotorState(9)},{MotorState(10)},{MotorState(11)},{MotorState(12)},{PaxState(middlePaxini)})\n";
    }

    private string BuildPaxiniGroupSyncDebugText()
    {
      if (SelectMotorCollider == null)
        return "\n[Paxini Group Sync]\nSelectMotorCollider: N/A\n";

      return "\n[Paxini Group Sync]\n" +
        "DebugText: " + SelectMotorCollider.debugGroupSyncText + "\n" +
        "Frame: " + SelectMotorCollider.debugGroupSyncFrame + "\n";
    }

    private string BuildFreezeEdgeDebugText()
    {
      if (SelectMotorCollider == null)
        return "\n[Freeze Edge Debug]\nSelectMotorCollider: N/A\n";

      return "\n[Freeze Edge Debug]\n" +
        "_prevThumbFreezeEnabled: " + SelectMotorCollider.debugPrevThumbFreezeEnabled + "\n" +
        "thumbFreezeEnabled: " + SelectMotorCollider.thumbFreezeEnabled + "\n" +
        "_prevIndexFreezeEnabled: " + SelectMotorCollider.debugPrevIndexFreezeEnabled + "\n" +
        "indexFreezeEnabled: " + SelectMotorCollider.indexFreezeEnabled + "\n" +
        "_prevMiddleFreezeEnabled: " + SelectMotorCollider.debugPrevMiddleFreezeEnabled + "\n" +
        "middleFreezeEnabled: " + SelectMotorCollider.middleFreezeEnabled + "\n";
    }

    private string BuildThumbFreezeCompareDebugText()
    {
      if (modeSwitching == null || SelectMotorCollider == null)
        return "\n[Thumb Freeze Compare]\nN/A\n";

      bool thumbOn = modeSwitching.debugThumbOn;
      bool thumbFreezeEnabled = SelectMotorCollider.thumbFreezeEnabled;

      return "\n[Thumb Freeze Compare]\n" +
        "ModeSwitching.thumbOn: " + thumbOn + "\n" +
        "SelectMotorCollider.thumbFreezeEnabled: " + thumbFreezeEnabled + "\n" +
        "Same: " + (thumbOn == thumbFreezeEnabled) + "\n";
    }

    private string BuildSuppressThumbPaxiniDebugText()
    {
      if (modeSwitching == null)
        return "\n[Suppress Thumb Paxini]\nN/A\n";

      return "\n[Suppress Thumb Paxini]\n" +
        "_suppressThumbPaxiniGroupUnfreeze: " + modeSwitching.debugSuppressThumbPaxiniGroupUnfreeze + "\n";
    }

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

    private string BuildNewTouchConditionDebug()
    {
      if (jointAngle == null)
        return "\n[NEW TOUCH CONDITIONS]\nJointAngle: N/A";

      Dictionary<string, Vector3> indexPoints = rightIndexTip != null ? rightIndexTip.GetAllTouchedPoints() : null;
      Dictionary<string, Vector3> middlePoints = rightMiddleTip != null ? rightMiddleTip.GetAllTouchedPoints() : null;
      Dictionary<string, Vector3> thumbPoints = triggerRightThumbTip != null ? triggerRightThumbTip.GetAllTouchedPoints() : null;

      bool hasLIndex0 = jointAngle.joints != null && jointAngle.joints.ContainsKey("L_index0");

      bool indexPointsNotNull = indexPoints != null;
      bool middlePointsNotNull = middlePoints != null;
      bool thumbPointsNotNull = thumbPoints != null;

      bool indexHasLThumbTip = indexPointsNotNull && indexPoints.ContainsKey("L_ThumbTip");
      bool middleHasLThumbTip = middlePointsNotNull && middlePoints.ContainsKey("L_ThumbTip");
      bool thumbHasLThumbTip = thumbPointsNotNull && thumbPoints.ContainsKey("L_ThumbTip");

      bool indexHasLIndexTip = indexPointsNotNull && indexPoints.ContainsKey("L_IndexTip");
      bool middleHasLIndexTip = middlePointsNotNull && middlePoints.ContainsKey("L_IndexTip");
      bool thumbHasLIndexTip = thumbPointsNotNull && thumbPoints.ContainsKey("L_IndexTip");

      bool indexLegacyTouch = indexHasLIndexTip && indexHasLThumbTip;
      bool middleLegacyTouch = middleHasLIndexTip && middleHasLThumbTip;
      bool thumbLegacyTouch = thumbHasLIndexTip && thumbHasLThumbTip;

      bool indexFormula = jointAngle.indexNewRotationMode && hasLIndex0 && indexPointsNotNull && indexHasLThumbTip;
      bool middleFormula = jointAngle.middleNewRotationMode && hasLIndex0 && middlePointsNotNull && middleHasLThumbTip;
      bool thumbFormula = jointAngle.thumbNewRotationMode && hasLIndex0 && thumbPointsNotNull && thumbHasLThumbTip;

      return "\n!!!!!!!!!!!!!!!!!!!!![NEW TOUCH CONDITIONS]" +
        "\nindexNewTouch: " + jointAngle.indexNewTouch +
        "\nindexNewRotationMode: " + jointAngle.indexNewRotationMode +
        "\nhasLIndex0: " + hasLIndex0 +
        "\nindexTouchPoints != null: " + indexPointsNotNull +
        "\nindexTouchPoints.ContainsKey(\"L_ThumbTip\"): "; 
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
      string leftToThumbAngle = jointAngle != null ? jointAngle.leftIndexTipToRightThumbTipAngle.ToString("F2") : "N/A";
      string leftToIndexAngle = jointAngle != null ? jointAngle.leftIndexTipToRightIndexTipAngle.ToString("F2") : "N/A";
      string leftToMiddleAngle = jointAngle != null ? jointAngle.leftIndexTipToRightMiddleTipAngle.ToString("F2") : "N/A";

      bool currentIndexRotationColliderMode = jointAngle != null && jointAngle.indexRotationColliderMode;
      if (currentIndexRotationColliderMode && !_lastIndexRotationColliderMode && jointAngle != null)
      {
        _latchedIndexEntryAngle = jointAngle.leftIndexTipToRightIndexTipAngle;
        _hasLatchedIndexEntryAngle = true;
      }
      _lastIndexRotationColliderMode = currentIndexRotationColliderMode;

      bool currentMiddleRotationColliderMode = jointAngle != null && jointAngle.middleRotationColliderMode;
      if (currentMiddleRotationColliderMode && !_lastMiddleRotationColliderMode && jointAngle != null)
      {
        _latchedMiddleEntryAngle = jointAngle.leftIndexTipToRightMiddleTipAngle;
        _hasLatchedMiddleEntryAngle = true;
      }
      _lastMiddleRotationColliderMode = currentMiddleRotationColliderMode;
      
      // Index Rotation Collider Debug Info
      string indexRotationColliderModeText = jointAngle != null ? jointAngle.indexRotationColliderMode.ToString() : "N/A";
      string enterRightIndexWithOldRotationText = jointAngle != null ? jointAngle.enterRightIndexWithOldRotation.ToString() : "N/A";
      string enterRightIndexWithNewRotationText = jointAngle != null ? jointAngle.enterRightIndexWithNewRotation.ToString() : "N/A";
      string entryAngleText = _hasLatchedIndexEntryAngle ? _latchedIndexEntryAngle.ToString("F2") : "N/A";

      // Middle Rotation Collider Debug Info
      string middleRotationColliderModeText = jointAngle != null ? jointAngle.middleRotationColliderMode.ToString() : "N/A";
      string enterRightMiddleWithOldRotationText = jointAngle != null ? jointAngle.enterRightMiddleWithOldRotation.ToString() : "N/A";
      string enterRightMiddleWithNewRotationText = jointAngle != null ? jointAngle.enterRightMiddleWithNewRotation.ToString() : "N/A";
      string middleEntryAngleText = _hasLatchedMiddleEntryAngle ? _latchedMiddleEntryAngle.ToString("F2") : "N/A";
      
      string planeDebug = GetPlaneActivationDebug();
      string syncLogState = BuildSelectMotorLogSyncState();
      string newTouchConditionDebug = BuildNewTouchConditionDebug();
      int fingerPriorityValue = SelectMotorCollider != null ? SelectMotorCollider.debugFingerPriority : -1;
      string lIndexToIndex2DistanceText = jointAngle != null
        ? jointAngle.GetLIndexToIndex2Distance().ToString("F3") + " m"
        : "N/A";
      string controllerDistanceText = "N/A";
      if (controllerLocatorLeft != null)
      {
        controllerDistanceText = controllerLocatorLeft.isControllerSeparationValid
          ? controllerLocatorLeft.currentControllerSeparationDistance.ToString("F3") + " m"
          : "Controller pose unavailable";
      }

      // string albowButtonStateText = "N/A";
      // if (virtualLeftHandButtonInteractor != null)
      // {
      //   albowButtonStateText = virtualLeftHandButtonInteractor.buttonAlbow.isOn ? "ON" : "OFF";
      // }

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
        1 => "1 (thumb priority, index/middle blocked)",
        2 => "2 (index priority, thumb/middle blocked)",
        3 => "3 (middle priority, thumb/index blocked)",
        _ => "N/A"
      };

      string allJointLocalEulerText = BuildAllJointLocalEulerText();
      string mappedMotorAngleText = BuildMappedMotorAngleText();
      string touchSnappedText = BuildTouchSnappedText();
      string tcpSenderDebugText = BuildTcpSenderDebugText();
      string armUiColliderDebugText = BuildArmUIColliderDebugText();

      //TODO: print here
      angleText.text =
        // allJointLocalEulerText +
        // mappedMotorAngleText +
        // touchSnappedText +
        // tcpSenderDebugText +
        // "L_index_c -> Index2 Distance: " + lIndexToIndex2DistanceText + "\n" +
        // "L/R Controller Distance: " + controllerDistanceText + "\n" +
        // "Albow Button: " + albowButtonStateText + "\n" +
        BuildMotorFreezeStateText() +
        // BuildFingertipFirstDebugText() +
        // BuildPaxiniGroupSyncDebugText() +
        // BuildFreezeEdgeDebugText() +
        // BuildThumbFreezeCompareDebugText() +
        // BuildSuppressThumbPaxiniDebugText() +
        armUiColliderDebugText;
        // "L/R Controller Distance: " + controllerDistanceText + "\n" +
        // "Clockwise Raw: " + clockwiseRaw + "\n" +
        // "Rotation Direction: " + rotationDirection + "\n";
        // "Finger Priority: " + fingerPriorityText + "\n" +
        // "L_IndexTipSmall -> R_thumb_b Angle: " + leftToThumbAngle + " deg\n" +
        // "L_IndexTipSmall -> R_index_c Angle: " + leftToIndexAngle + " deg\n" +
        // "L_IndexTipSmall -> R_middle_c Angle: " + leftToMiddleAngle + " deg\n" +
        // "\n[Index Rotation Collider Debug]\n" +
        // "indexRotationColliderMode: " + indexRotationColliderModeText + "\n" +
        // "enterRightIndexWithOldRotation: " + enterRightIndexWithOldRotationText + "\n" +
        // "enterRightIndexWithNewRotation: " + enterRightIndexWithNewRotationText + "\n" +
        // "Entry Angle (L_IndexTipSmall -> R_index_c): " + entryAngleText + " deg\n" +
        // "\n[Middle Rotation Collider Debug]\n" +
        // "middleRotationColliderMode: " + middleRotationColliderModeText + "\n" +
        // "enterRightMiddleWithOldRotation: " + enterRightMiddleWithOldRotationText + "\n" +
        // "enterRightMiddleWithNewRotation: " + enterRightMiddleWithNewRotationText + "\n" +
        // "Entry Angle (L_IndexTipSmall -> R_middle_c): " + middleEntryAngleText + " deg\n";
        // "[ThumbOnly Mode]: " + thumbOnlyModeText +
        // "\nThumb Legacy Touch: " + (jointAngle != null ? jointAngle.thumbLegacyTouch.ToString() : "N/A") +
        // "\nIndex Legacy Touch: " + (jointAngle != null ? jointAngle.indexLegacyTouch.ToString() : "N/A") + "\n" +
        // "Middle Legacy Touch: " + (jointAngle != null ? jointAngle.middleLegacyTouch.ToString() : "N/A") +
        // "Plane Active: " + planeActiveRaw + "\n" +
        // "Active Finger: " + activeFingerRaw + "\n" +
        // planeDebug +
        // "\n***indexRotationDebug*** " + jointAngle.indexRotationDebug + "\n" +
        // "\n***middleRotationDebug*** " + jointAngle.middleRotationDebug + "\n" +
        // "\n***thumbRotationDebug*** " + jointAngle.thumbRotationDebug + "\n";
        // syncLogState;
    }

    private string BuildFingertipFirstDebugText()
    {
        if (modeSwitching == null || SelectMotorCollider == null)
            return "\n[FingertipFirst]\nN/A\n";

        if (!modeSwitching.useFingertipFirst)
            return "\n[FingertipFirst]\ndisabled\n";

        string phase = modeSwitching.currentPhase.ToString();
        int msConfirmedTip = modeSwitching.confirmedFingertipID;
        bool smcConfirmed  = SelectMotorCollider.isFingertipConfirmed;
        int smcTipID       = SelectMotorCollider.confirmedFingertipMotorID;
        string range       = SelectMotorCollider.selectableMotorRange;
        int touchedMotor   = SelectMotorCollider.currentTouchedMotorID;

        return "\n[FingertipFirst]\n" +
            $"Phase: {phase}\n" +
            $"MS confirmedFingertipID: {msConfirmedTip}\n" +
            $"SMC isFingertipConfirmed: {smcConfirmed}\n" +
            $"SMC confirmedFingertipMotorID: {smcTipID}\n" +
            $"SelectableRange: {range}\n" +
            $"currentTouchedMotorID: {touchedMotor}\n";
    }

    private string BuildTouchSnappedText()
    {
      if (paxiniValue == null)
      {
        return "[TouchSnapped]\nPaxiniValue: N/A\n";
      }

      string rxAgeText = paxiniValue.HasReceivedPayload
        ? paxiniValue.SecondsSinceLastPayload.ToString("F2") + "s"
        : "N/A";

      return "[TouchSnapped]\n" +
        "Rx Packets: " + paxiniValue.ReceivedPacketCount + " | Parse Errors: " + paxiniValue.JsonParseErrorCount + " | Last Rx Age: " + rxAgeText + "\n" +
        "Thresholds -> Zero <= " + paxiniValue.paxiniZeroThreshold.ToString("F2") + ", SnapOn > " + paxiniValue.paxiniSnapOnThreshold.ToString("F2") + "\n" +
        "Fz Thumb/Index/Middle: " + paxiniValue.LatestFzThumb.ToString("F2") + " / " + paxiniValue.LatestFzIndex.ToString("F2") + " / " + paxiniValue.LatestFzMiddle.ToString("F2") + "\n" +
        "Zero Flags T/I/M: " + paxiniValue.isThumbPaxiniZero + " / " + paxiniValue.isIndexPaxiniZero + " / " + paxiniValue.isMiddlePaxiniZero + "\n" +
        "Thumb isTouchSnapped: " + paxiniValue.isThumbTouchSnapped + "\n" +
        "Index isTouchSnapped: " + paxiniValue.isIndexTouchSnapped + "\n" +
        "Middle isTouchSnapped: " + paxiniValue.isMiddleTouchSnapped + "\n";
    }

    private string BuildTcpSenderDebugText()
    {
      if (tcpSender == null)
      {
        return "[TcpSender]\nTcpSender: N/A\n";
      }

      return "[TcpSender]\n" +
        "isSending: " + tcpSender.isSending + "\n" +
        "Rotation Mode: " + tcpSender.rotationMode + "\n" +
        "Position: (" +
          tcpSender.debug_pos_x.ToString("F3") + ", " +
          tcpSender.debug_pos_y.ToString("F3") + ", " +
          tcpSender.debug_pos_z.ToString("F3") + ")\n" +
        "Euler: (" +
          tcpSender.debug_euler_x.ToString("F3") + ", " +
          tcpSender.debug_euler_y.ToString("F3") + ", " +
          tcpSender.debug_euler_z.ToString("F3") + ")\n";
    }

    private string BuildArmUIColliderDebugText()
    {
      if (armUIPlaneController == null)
      {
        return "\n[Arm UI Collider]\nArmUIPlaneController: N/A\n";
      }

      return "\n[Arm UI Collider]\n" +
        "Touched Button: " + armUIPlaneController.currentTouchedButton + "\n" +
        "Touched Collider: " + armUIPlaneController.currentTouchedCollider + "\n" +
        "Interaction: " + armUIPlaneController.interactionDebug + "\n";
    }

    private string BuildAllJointLocalEulerText()
    {
      if (clawModuleController == null)
      {
        return "[12 Joints LocalEulerAngles]\nClawModuleController: N/A\n";
      }

      return "[12 Joints LocalEulerAngles]\n" +
        GetLocalEulerText("Thumb1", clawModuleController.ThumbAngle1Center) + "\n" +
        GetLocalEulerText("Thumb2", clawModuleController.ThumbAngle2Center) + "\n" +
        GetLocalEulerText("Thumb3", clawModuleController.ThumbAngle3Center) + "\n" +
        GetLocalEulerText("Thumb4", clawModuleController.ThumbAngle4Center) + "\n" +
        GetLocalEulerText("Index1", clawModuleController.IndexAngle1Center) + "\n" +
        GetLocalEulerText("Index2", clawModuleController.IndexAngle2Center) + "\n" +
        GetLocalEulerText("Index3", clawModuleController.IndexAngle3Center) + "\n" +
        GetLocalEulerText("Index4", clawModuleController.IndexAngle4Center) + "\n" +
        GetLocalEulerText("Middle1", clawModuleController.MiddleAngle1Center) + "\n" +
        GetLocalEulerText("Middle2", clawModuleController.MiddleAngle2Center) + "\n" +
        GetLocalEulerText("Middle3", clawModuleController.MiddleAngle3Center) + "\n" +
        GetLocalEulerText("Middle4", clawModuleController.MiddleAngle4Center) + "\n";
    }

    private string BuildMappedMotorAngleText()
    {
      if (clawModuleController == null)
      {
        return "\n[Mapped Motor Angles]\nClawModuleController: N/A\n";
      }

      return "\n[Mapped Motor Angles]\n" +
        "Thumb1 mapped Y: " + FormatAngle(clawModuleController.thumbMappedYDebug) + "\n" +
        "Index1 mapped Y: " + FormatAngle(clawModuleController.indexMappedYDebug) + "\n" +
        "Middle1 mapped Y: " + FormatAngle(clawModuleController.middleMappedYDebug) + "\n";
    }

    private string GetLocalEulerText(string label, Transform t)
    {
      if (t == null)
        return label + ": N/A";

      Vector3 e = t.localEulerAngles;
      return label + ": (" + e.x.ToString("F2") + ", " + e.y.ToString("F2") + ", " + e.z.ToString("F2") + ")";
    }

    private string FormatAngle(float angle)
    {
      return float.IsNaN(angle) ? "N/A" : angle.ToString("F2");
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
