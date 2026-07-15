using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModeSwitching : MonoBehaviour
{
    private const int ThumbPaxiniMotorID = 13;
    private const int IndexPaxiniMotorID = 14;
    private const int MiddlePaxiniMotorID = 15;

    public JointAngle jointAngle;
    public SelectMotorCollider SelectMotorCollider;
    public ArmUIPlaneController armUIPlaneController;

    [Header("Input Priority")]
    [Tooltip("When enabled, projection motor touching can still select motors even while ArmUI plane is active.")]
    public bool allowProjectionSelectionWhenArmUIPlaneActive = true;

    public Renderer thumbJoint1Renderer;
    public Renderer thumbJoint2Renderer;
    public Renderer thumbJoint3Renderer;
    public Renderer thumbJoint4Renderer;

    public Renderer indexJoint1Renderer;
    public Renderer indexJoint2Renderer;
    public Renderer indexJoint3Renderer;
    public Renderer indexJoint4Renderer;

    public Renderer middleJoint1Renderer;
    public Renderer middleJoint2Renderer;
    public Renderer middleJoint3Renderer;
    public Renderer middleJoint4Renderer;

    public Renderer thumbPaxiniRenderer;
    public Renderer indexPaxiniRenderer;
    public Renderer middlePaxiniRenderer;

    public Renderer baseRenderer;

    private Color originalColor;
    private Color thumbPaxiniOriginalColor;
    private Color indexPaxiniOriginalColor;
    private Color middlePaxiniOriginalColor;
    public Color lightRedColor = new Color(1f, 0.5f, 0.5f, 1f); // Light red (temporary touch)
    public Color darkRedColor = Color.red; // Dark red (confirmed selection)
    
    [Header("Selection Timing")]
    [Tooltip("How many seconds to confirm selection (turn dark red)")]
    public float confirmationTime = 0.25f; // Confirm after exceeding this time
    [Tooltip("Faster confirmation time for fingertip motors (4, 8, 12) to allow quick switching between fingers")]
    public float fingertipConfirmationTime = 0.08f;
    
    public bool modeSelect = true;
    public bool motorSelected = false;
    public bool modeManipulate = false;
    public float currentHandSeparationDistance = 0f;

    [Header("Separation Source")]
    [Tooltip("OFF: use JointAngle.GetLIndexToIndex2Distance() and hand threshold. ON: use ControllerLocatorLeft.currentControllerSeparationDistance and controller threshold.")]
    public bool useControllerSeperationDistance = false;
    [Tooltip("Reference for controller-based separation distance when useControllerSeperationDistance is ON")]
    public ControllerLocatorLeft controllerlocatorleft;
    [Tooltip("Transition threshold used when useControllerSeperationDistance is OFF")]
    public float handSeparationThreshold = 0.16f;
    [Tooltip("Transition threshold used when useControllerSeperationDistance is ON")]
    public float controllerSeparationThreshold = 0.43f;
    
    public int lastTouchedMotorID = 0;
    public int currentRedMotorID = 0; // Currently touched motor (displayed in light/dark red)
    
    [Header("Confirmed Selection")]
    [Tooltip("ID of the confirmed motor (dark red) - this is retained after leaving modeSelect")]
    public int confirmedMotorID = 0; // Confirmed motor (dark red)
    
    private float touchStartTime = 0f; // Time when touch started
    private bool isConfirmed = false; // Whether the currently touched motor is confirmed (dark red)
    
    private bool hasEnteredCloseRange = false; // Track if we've entered below the active separation threshold during manipulation
    private bool hasSetManipulateColors = false; // Track if we've set manipulate colors
    private bool _wasModeSelectLastFrame = false;

    // Baseline snapshot captured when entering modeSelect.
    // Enforced so each finger-group state differs from baseline by at most one switched value.
    private bool _thumbBaselinePaxiniOn = false;
    private bool _indexBaselinePaxiniOn = false;
    private bool _middleBaselinePaxiniOn = false;

    public Material yellowMaterial;
    
    [Header("=== New Feature: Fingertip Priority Selection ===")]
    [Tooltip("Enable fingertip priority mode: Fingertip motors (4, 8, 12) must be confirmed before selecting other motors")]
    public bool useFingertipFirst = false;
    
    [Tooltip("Current selection phase")]
    public SelectionPhase currentPhase = SelectionPhase.SelectingFingertip;
    
    [Tooltip("ID of the confirmed fingertip motor")]
    public int confirmedFingertipID = 0;

    [Tooltip("Gray mode visuals for fingertip-first: ON = non-selectable motors are gray, OFF = keep original claw colors")]
    public bool grayMode = true;
    
    public Color grayColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray (disabled/unselectable)

    [Header("=== Single Motor Freeze (motors 1-12) ===")]
    [Tooltip("Time (seconds) of continuous holding after confirmation before a single motor becomes frozen")]
    public float singleMotorFreezeTime = 1.0f;
    [Tooltip("Color for a single frozen motor")]
    public Color singleFrozenColor = Color.yellow;
    [Tooltip("Time (seconds) of continuous hovering on a frozen (yellow) motor before the freeze is cancelled. Increase to make cancellation require a longer hold.")]
    public float unfreezeConfirmationTime = 1.5f;
    [Tooltip("Hint color shown while the user is holding to cancel a frozen motor. Original color is restored on completion.")]
    public Color unfreezeHintColor = new Color(1f, 0.4f, 0.7f, 1f);
    [Tooltip("[Debug] Per-motor freeze state (index 0-11 = motor ID 1-12)")]
    public bool[] singleMotorFrozen = new bool[12];

    // Single motor freeze \u2014 private state
    private bool _isUnfreezing = false;          // currently touching a frozen motor to unfreeze
    private int  _unfreezeTargetMotorID = 0;     // which motor is being unfrozen
    private bool _justFrozeWhileHolding = false; // true when motor just froze while finger still held
    private int  _justFrozeMotorID = 0;          // which motor just froze while held
    private bool _singleFreezeInProgress = false;// true during red→yellow lerp (blocks manipulate)
    private bool[] _pendingSingleMotorFreeze = new bool[12];
    private bool[] _pendingSingleMotorUnfreeze = new bool[12];

    // One-motor-per-round constraint: tracks which motor 1-12 changed mode this round.
    // Once set, no OTHER motor 1-12 may confirm or unfreeze until a new round begins.
    // Motors 13/14/15 (Paxini) are always free and never counted.
    private int _roundChangedMotorID = 0;

    // Baseline frozen state — captured each time the hand moves away with frozen motors.
    // Frozen motors in the baseline are NOT cleared by ClearSingleFrozenMotorInGroup,
    // so they persist into the next selection round (same as Paxini "on" persisting).
    private bool[] _singleMotorFrozenBaseline = new bool[12];
    private bool   _frozenBaselineCaptured          = false; // edge-trigger: true once per hand-away event (frozen motors present)
    private bool   _noFreezeRoundBaselineCaptured   = false; // edge-trigger: true once per hand-away event (no frozen motors, but round had a change)

    // Paxini group-sync: prev-state tracking for Paxini OFF → group unfreeze in Update()
    private bool _prevThumbPaxiniEnabled  = false;
    private bool _prevIndexPaxiniEnabled  = false;
    private bool _prevMiddlePaxiniEnabled = false;
    // Set by CheckAndAutoDisablePaxini so the Update() group-sync does NOT unfreeze all 4
    // (individual motor unfreeze only removes that motor; others stay frozen)
    private bool _suppressThumbPaxiniGroupUnfreeze  = false;
    private bool _suppressIndexPaxiniGroupUnfreeze  = false;
    private bool _suppressMiddlePaxiniGroupUnfreeze = false;

    // ── Pending Paxini visual states (all 15-motor one-change rule) ──────────────────────────────
    // Auto-ON: all 4 group motors froze → Paxini shows yellow visually but state stays OFF
    //          until hand moves away > 0.16 m, then commits to freeze-on.
    private bool _pendingThumbAutoOn  = false;
    private bool _pendingIndexAutoOn  = false;
    private bool _pendingMiddleAutoOn = false;
    // Auto-OFF: one group motor unfroze while Paxini was ON → Paxini shows original color
    //           but state stays ON until hand moves away > 0.16 m, then commits to freeze-off.
    private bool _pendingThumbAutoOff  = false;
    private bool _pendingIndexAutoOff  = false;
    private bool _pendingMiddleAutoOff = false;
    // Direct-OFF: user directly toggled Paxini OFF via freeze-zone → group motors show original
    //             color even if singleMotorFrozen=true; baseline-frozen motors become OFF at hand-away.
    private bool _pendingThumbDirectOff  = false;
    private bool _pendingIndexDirectOff  = false;
    private bool _pendingMiddleDirectOff = false;
    // Suppress the frozen-yellow colour for an entire group (used during pending direct-OFF)
    private bool _suppressThumbGroupYellow  = false;
    private bool _suppressIndexGroupYellow  = false;
    private bool _suppressMiddleGroupYellow = false;

    [Header("Debug - Paxini Group Sync")]
    [Tooltip("True on frames where FreezeGroupMotors was entered")]
    public bool debugEnteredFreezeGroupMotors = false;
    [Tooltip("True on frames where UnfreezeGroupMotors was entered")]
    public bool debugEnteredUnfreezeGroupMotors = false;
    [Tooltip("Last group-sync action name")]
    public string debugLastGroupSyncAction = "None";
    [Tooltip("Last group start used by group-sync action (1/5/9)")]
    public int debugLastGroupStart = 0;
    [Tooltip("Last Paxini motor ID resolved for group-sync action (13/14/15)")]
    public int debugLastGroupPaxiniID = 0;
    [Tooltip("Unity frame where the last group-sync action happened")]
    public int debugGroupSyncFrame = -1;
    [Tooltip("How many times FreezeGroupMotors has been entered")]
    public int debugFreezeGroupCallCount = 0;
    [Tooltip("How many times UnfreezeGroupMotors has been entered")]
    public int debugUnfreezeGroupCallCount = 0;

    [Header("Debug - Paxini Live Flags")]
    [Tooltip("Mirror of Update() local variable 'thumbOn' for runtime debugging")]
    public bool debugThumbOn = false;
    [Tooltip("Mirror of _suppressThumbPaxiniGroupUnfreeze for runtime debugging")]
    public bool debugSuppressThumbPaxiniGroupUnfreeze = false;

    [Header("Arm UI Proxy State")]
    public bool armUIProxyModeSelect = true;
    public bool armUIProxyModeManipulate = false;
    public int armUIProxyCurrentTouchedMotorID = 0;
    public int armUIProxyCurrentRedMotorID = 0;
    public int armUIProxyConfirmedMotorID = 0;
    public int armUIProxyConfirmedFingertipID = 0;
    public bool[] armUIProxySingleMotorFrozen = new bool[12];
    public bool armUIProxyThumbPaxiniOn = false;
    public bool armUIProxyIndexPaxiniOn = false;
    public bool armUIProxyMiddlePaxiniOn = false;
    public int armUIProxyRejectedMotorID = 0;
    public string armUIProxyRejectReason = "None";

    private bool _armUIInputIsInsideEnterPlane = false;
    private int _armUIInputTouchedMotorID = 0;
    private bool _armUIWasInsideEnterPlane = false;
    private int _armUILastTouchedMotorID = 0;
    private float _armUITouchStartTime = 0f;
    private bool _armUIIsConfirmed = false;
    private bool _armUIIsUnfreezing = false;
    private int _armUIUnfreezeTargetMotorID = 0;
    private bool _armUIJustFrozeWhileHolding = false;
    private int _armUIJustFrozeMotorID = 0;

    public enum SelectionPhase
    {
        SelectingFingertip,   // Phase 1: Selecting fingertip (4, 8, 12)
        SelectingMotor,       // Phase 2: Selecting the motor of the finger
        MotorConfirmed        // Final motor confirmed
    }
    
    void Start()
    {
        if (controllerlocatorleft == null)
        {
            controllerlocatorleft = FindObjectOfType<ControllerLocatorLeft>();
        }

        if (armUIPlaneController == null)
        {
            armUIPlaneController = FindObjectOfType<ArmUIPlaneController>();
        }

        if (SelectMotorCollider != null)
        {
            if (thumbPaxiniRenderer == null && SelectMotorCollider.triggerRightThumbTip != null)
                thumbPaxiniRenderer = SelectMotorCollider.triggerRightThumbTip.thumbPaxiniRenderer;
            if (indexPaxiniRenderer == null && SelectMotorCollider.triggerRightIndexTip != null)
                indexPaxiniRenderer = SelectMotorCollider.triggerRightIndexTip.indexPaxiniRenderer;
            if (middlePaxiniRenderer == null && SelectMotorCollider.triggerRightMiddleTip != null)
                middlePaxiniRenderer = SelectMotorCollider.triggerRightMiddleTip.middlePaxiniRenderer;
        }

        originalColor = thumbJoint1Renderer.material.color;
        thumbPaxiniOriginalColor = thumbPaxiniRenderer != null ? thumbPaxiniRenderer.material.color : originalColor;
        indexPaxiniOriginalColor = indexPaxiniRenderer != null ? indexPaxiniRenderer.material.color : originalColor;
        middlePaxiniOriginalColor = middlePaxiniRenderer != null ? middlePaxiniRenderer.material.color : originalColor;
        modeSelect = true;
        motorSelected = false;
        modeManipulate = false;
        _wasModeSelectLastFrame = modeSelect;
        
        // Initialize fingertip priority feature
        if (useFingertipFirst)
        {
            currentPhase = SelectionPhase.SelectingFingertip;
            confirmedFingertipID = 0;
        }
        
        ResetAllColors();

        // Apply initial modeSelect visuals.
        ApplyModeSelectBaseColors();

        CaptureModeSelectBaseline();

        singleMotorFrozen = new bool[12];
        _isUnfreezing = false;
        _unfreezeTargetMotorID = 0;
        _justFrozeWhileHolding = false;
        _justFrozeMotorID = 0;
        _singleFreezeInProgress = false;
        _roundChangedMotorID = 0;
        _singleMotorFrozenBaseline = new bool[12];
        _pendingSingleMotorFreeze = new bool[12];
        _pendingSingleMotorUnfreeze = new bool[12];
        _frozenBaselineCaptured = false;

        // Reset: grayMode off, all motors off, all Paxini freeze off
        grayMode = false;
        if (SelectMotorCollider != null)
        {
            SelectMotorCollider.ForcePaxiniOffForMotor(1);
            SelectMotorCollider.ForcePaxiniOffForMotor(5);
            SelectMotorCollider.ForcePaxiniOffForMotor(9);
        }
        ResetAllColors();
        CaptureModeSelectBaseline();
    }

    void Update()
    {
        UpdateCurrentHandSeparationDistance();
        float activeSeparationThreshold = GetActiveSeparationThreshold();
        bool isArmUIPlaneActive = IsArmUIPlaneActive();
        // Arm UI parity with claw selecting:
        // "away" means leaving EnterPlane only. Merely not touching a motor button
        // while still inside EnterPlane must remain in the same selecting round.
        bool isRoundAway = (!isArmUIPlaneActive && currentHandSeparationDistance > activeSeparationThreshold)
                || (isArmUIPlaneActive && !_armUIInputIsInsideEnterPlane);

        // Capture baseline once when entering modeSelect.
        if (modeSelect && !_wasModeSelectLastFrame)
        {
            CaptureModeSelectBaseline();
        }
        _wasModeSelectLastFrame = modeSelect;

        if (isArmUIPlaneActive)
        {
            if (SelectMotorCollider != null && !allowProjectionSelectionWhenArmUIPlaneActive)
            {
                SelectMotorCollider.ClearSelectionStateForArmUIPlane();
            }

            if (_armUIWasInsideEnterPlane != _armUIInputIsInsideEnterPlane)
            {
                if (_armUIInputIsInsideEnterPlane && modeSelect)
                {
                    ResetTransientSelectionStateForArmUIPlane();
                    CaptureModeSelectBaseline();
                }

                _armUIWasInsideEnterPlane = _armUIInputIsInsideEnterPlane;
            }
        }
        else
        {
            // Ensure ArmUI stale input never leaks into projection-driven selection/manipulation.
            _armUIInputIsInsideEnterPlane = false;
            _armUIInputTouchedMotorID = 0;
            _armUIWasInsideEnterPlane = false;
        }

        if (modeSelect)
        {
            int currentMotorID = GetCurrentTouchedMotorIDForSelection(isArmUIPlaneActive);

            // Motor switched
            if (currentMotorID != lastTouchedMotorID)
            {
                // Clear just-froze guard on any motor change
                _justFrozeWhileHolding = false;
                _justFrozeMotorID = 0;
                _singleFreezeInProgress = false;

                if (currentMotorID != 0)
                {
                    // Detect if the newly touched motor is currently single-frozen → unfreeze flow
                    bool isFrozenMotor = currentMotorID >= 1 && currentMotorID <= 12
                                        && singleMotorFrozen[currentMotorID - 1];

                    if (isFrozenMotor)
                    {
                        // Unfreeze flow: yellow → hint color over unfreezeConfirmationTime
                        _isUnfreezing = true;
                        _unfreezeTargetMotorID = currentMotorID;
                        touchStartTime = Time.time;
                        isConfirmed = false;
                        currentRedMotorID = currentMotorID;
                        // motorSelected intentionally NOT set: unfreeze ≠ motor selected

                        // FingertipFirst fix: a frozen fingertip (4/8/12) still needs to unlock
                        // the group so motors 1-3 / 5-7 / 9-11 remain selectable while the
                        // unfreeze gesture is in progress. Without this, isFingertipConfirmed
                        // stays false and IsMotorSelectable blocks the other group motors.
                        if (useFingertipFirst && SelectMotorCollider != null &&
                            (currentMotorID == 4 || currentMotorID == 8 || currentMotorID == 12))
                        {
                            confirmedFingertipID = currentMotorID;
                            currentPhase = SelectionPhase.MotorConfirmed;
                            SelectMotorCollider.OnFingertipConfirmed(currentMotorID);
                            SelectMotorCollider.CaptureFrozenLine(currentMotorID);
                        }

                        // Clear residual color on any previously-hovered motor before lerp starts.
                        UpdateMotorColors();
                    }
                    else
                    {
                        _isUnfreezing = false;
                        _unfreezeTargetMotorID = 0;

                        // Keep Paxini freeze state unchanged while switching hover/selection targets.
                        // This preserves the initial per-finger bool combination until the user
                        // explicitly toggles freeze itself.

                        // New motor touched
                        touchStartTime = Time.time;
                        isConfirmed = false;
                        currentRedMotorID = currentMotorID;

                        UpdateMotorColors();

                        if (!motorSelected)
                        {
                            motorSelected = true;
                        }
                    }
                }
                else
                {
                    // Left all motors
                    _isUnfreezing = false;
                    _unfreezeTargetMotorID = 0;
                    currentRedMotorID = 0;
                    UpdateMotorColors();
                }

                lastTouchedMotorID = currentMotorID;
            }
            else if (currentMotorID != 0)
            {
                // Continuously touching the same motor
                if (_justFrozeWhileHolding && currentMotorID == _justFrozeMotorID)
                {
                    // Motor just froze while still being held — wait for release before starting unfreeze
                }
                else if (_isUnfreezing)
                {
                    // Unfreeze confirmation: check if unfreezeConfirmationTime elapsed
                    if (!isConfirmed && (Time.time - touchStartTime) >= unfreezeConfirmationTime)
                    {
                        int fm = _unfreezeTargetMotorID;
                        // If a DIFFERENT motor already has a committed change, revert it first
                        // (selecting a new motor is always the highest priority).
                        if (fm >= 1 && fm <= 12
                            && _roundChangedMotorID != 0 && _roundChangedMotorID != fm)
                        {
                            RevertMotorToBaseline(_roundChangedMotorID);
                            _roundChangedMotorID = 0;
                        }
                        isConfirmed = true;
                        _pendingSingleMotorUnfreeze[fm - 1] = true;
                        _roundChangedMotorID = fm;
                        _frozenBaselineCaptured = false;
                        _noFreezeRoundBaselineCaptured = false;
                        _isUnfreezing = false;
                        _unfreezeTargetMotorID = 0;
                        currentRedMotorID = 0;
                        motorSelected = false;
                        confirmedMotorID = 0;
                        UpdateMotorColors();
                    }
                    // else: still holding on the frozen motor; per-frame section keeps it pink
                }
                else
                {
                    // Continuously touching the same motor - check if confirmation time exceeded
                    // Use faster confirmation time when crossing finger groups (e.g., from index to thumb)
                    bool isCrossFingerSwitch = confirmedMotorID != 0 && !IsSameFingerGroup(confirmedMotorID, currentMotorID);
                    float requiredConfirmTime = isCrossFingerSwitch ? fingertipConfirmationTime : confirmationTime;
                    if (!isConfirmed && (Time.time - touchStartTime) >= requiredConfirmTime)
                    {
                        if (isArmUIPlaneActive && IsPaxiniMotor(currentMotorID))
                        {
                            CommitArmUIPaxiniToggle(currentMotorID);
                            UpdateMotorColors();
                        }
                        else
                        {
                            // Selecting (confirming) a new motor is ALWAYS allowed.
                            // If a DIFFERENT motor already has a committed change this round, revert it first.
                            // Applies to all selectable motors, including Paxini (13/14/15),
                            // so each selecting round differs from baseline by at most ONE state.
                            if (_roundChangedMotorID != 0 && _roundChangedMotorID != currentMotorID)
                            {
                                RevertMotorToBaseline(_roundChangedMotorID);
                                _roundChangedMotorID = 0;
                            }

                            isConfirmed = true;

                            // Handle confirmation logic based on whether fingertip priority mode is enabled
                            if (useFingertipFirst)
                            {
                                HandleFingertipFirstConfirmation(currentMotorID);
                                EnforceGroupBaselineForConfirmedMotor(currentMotorID);
                            }
                            else
                            {
                                confirmedMotorID = currentMotorID;
                                EnforceGroupBaselineForConfirmedMotor(currentMotorID);
                            }

                            UpdateMotorColors();
                        }
                    }
                }
            }

            // ─── Per-frame: single motor freeze buildup (confirmed + still holding) ───
            if (isConfirmed && !_isUnfreezing
                && confirmedMotorID >= 1 && confirmedMotorID <= 12
                && currentMotorID == confirmedMotorID)
            {
                float elapsed = Time.time - touchStartTime - confirmationTime;
                if (elapsed > 0f)
                {
                    bool isStillBuildingFreeze = elapsed < singleMotorFreezeTime;
                    _singleFreezeInProgress = isStillBuildingFreeze;

                    if (isStillBuildingFreeze)
                    {
                        SetMotorColorDirect(confirmedMotorID, darkRedColor);
                    }
                    else
                    {
                        SetMotorColorDirect(confirmedMotorID, singleFrozenColor);
                    }

                    if (!isStillBuildingFreeze && !singleMotorFrozen[confirmedMotorID - 1] && !_pendingSingleMotorFreeze[confirmedMotorID - 1])
                    {
                        int frozenID = confirmedMotorID;
                        // If a DIFFERENT motor already has a committed change, revert it first.
                        if (frozenID >= 1 && frozenID <= 12
                            && _roundChangedMotorID != 0 && _roundChangedMotorID != frozenID)
                        {
                            RevertMotorToBaseline(_roundChangedMotorID);
                            _roundChangedMotorID = 0;
                        }
                        // Mark pending freeze for this round; state commits at round-away.
                        _pendingSingleMotorFreeze[frozenID - 1] = true;
                        _pendingSingleMotorUnfreeze[frozenID - 1] = false;
                        if (armUIPlaneController != null && armUIPlaneController.clawModuleController != null)
                        {
                            armUIPlaneController.clawModuleController.CaptureSingleMotorFreezeSnapshot(frozenID);
                        }
                        // Track round change only if net state differs from baseline
                        if (frozenID >= 1 && frozenID <= 12)
                            _roundChangedMotorID = !IsBaselineFrozenForMotor(frozenID) ? frozenID : 0;
                        confirmedMotorID = 0;
                        motorSelected = false;
                        currentRedMotorID = 0;
                        isConfirmed = false;
                        _singleFreezeInProgress = false;
                        _justFrozeWhileHolding = true;
                        _justFrozeMotorID = frozenID;
                        _frozenBaselineCaptured = false;
                        _noFreezeRoundBaselineCaptured = false;
                        SetMotorColorDirect(frozenID, singleFrozenColor);
                        UpdateMotorColors(); // Refresh reverted motor color
                        // Auto-enable Paxini if all 4 motors in this group are now frozen
                        CheckAndAutoEnablePaxini(frozenID);
                    }
                }
                else
                {
                    _singleFreezeInProgress = false;
                }
            }
            else
            {
                _singleFreezeInProgress = false;
            }

            // ─── Per-frame: unfreeze hover (frozen motor being touched) ───
            if (_isUnfreezing && _unfreezeTargetMotorID != 0 && currentMotorID == _unfreezeTargetMotorID)
            {
                SetMotorColorDirect(_unfreezeTargetMotorID, unfreezeHintColor);
            }

            EnforceSingleRoundDifferenceInvariant();
        }

        bool isConfirmedPaxiniMotor = IsPaxiniMotor(confirmedMotorID);

        // Paxini group-sync: detect Paxini OFF and restore the 1-12 motor freeze states back to
        // this round's baseline snapshot. This allows recovery from accidental Paxini toggles
        // without destroying an existing baseline freeze distribution.
        if (modeSelect && SelectMotorCollider != null)
        {
            bool thumbOn  = SelectMotorCollider.thumbFreezeEnabled;
            bool indexOn  = SelectMotorCollider.indexFreezeEnabled;
            bool middleOn = SelectMotorCollider.middleFreezeEnabled;
            debugThumbOn = thumbOn;

            // Paxini OFF→ON edge: clear stale suppress flags.
            // suppress is set by programmatic OFF paths (EnforceGroupBaseline, auto-OFF commit, etc.).
            // If it is never cleared and the user then manually turns Paxini OFF, the suppress would
            // block UnfreezeGroupMotors, leaving motors permanently frozen. Clearing on the ON edge
            // ensures the very next user-triggered OFF always goes through UnfreezeGroupMotors.
            if (!_prevThumbPaxiniEnabled  && thumbOn)  _suppressThumbPaxiniGroupUnfreeze  = false;
            if (!_prevIndexPaxiniEnabled  && indexOn)  _suppressIndexPaxiniGroupUnfreeze  = false;
            if (!_prevMiddlePaxiniEnabled && middleOn) _suppressMiddlePaxiniGroupUnfreeze = false;

            debugSuppressThumbPaxiniGroupUnfreeze = _suppressThumbPaxiniGroupUnfreeze;

            if (_prevThumbPaxiniEnabled && !thumbOn)
            {
                if (!_suppressThumbPaxiniGroupUnfreeze)
                {
                    Debug.Log($"!!!!!!!!!!!!!!!![SelectMotorCollider] Paxini group-sync: Thumb OFF → UnfreezeGroupMotors(1) frame={Time.frameCount}");
                    UnfreezeGroupMotors(1);
                }
                _suppressThumbPaxiniGroupUnfreeze = false;
            }
            if (_prevIndexPaxiniEnabled && !indexOn)
            {
                if (!_suppressIndexPaxiniGroupUnfreeze)
                {
                    UnfreezeGroupMotors(5);
                }
                _suppressIndexPaxiniGroupUnfreeze = false;
            }
            if (_prevMiddlePaxiniEnabled && !middleOn)
            {
                if (!_suppressMiddlePaxiniGroupUnfreeze)
                {
                    UnfreezeGroupMotors(9);
                }
                _suppressMiddlePaxiniGroupUnfreeze = false;
            }

            _prevThumbPaxiniEnabled  = thumbOn;
            _prevIndexPaxiniEnabled  = indexOn;
            _prevMiddlePaxiniEnabled = middleOn;
        }

        // For Paxini pseudo motors (13/14/15): never enter manipulate mode.
        // When hands separate, return to base select state but keep freeze (yellow) handled by TriggerRight*Tip.
        if (!isArmUIPlaneActive && modeSelect && motorSelected && isConfirmedPaxiniMotor)
        {
            float distance = currentHandSeparationDistance;
            if (distance > activeSeparationThreshold)
            {
                ReturnToBaseSelectStateAfterPaxini();
            }
        }

        // Single frozen motor + hand away → lock freeze into baseline.
        // IMPORTANT: runs even when motorSelected=true so that the user touching a new motor
        // in the same frame as the hand-away event still gets the baseline captured and
        // _roundChangedMotorID cleared before the confirmation logic fires next frame.
        if (modeSelect)
        {
            bool isHoldingArmUIPaxiniButton = isArmUIPlaneActive
                                           && _armUIInputIsInsideEnterPlane
                                           && IsPaxiniMotor(_armUIInputTouchedMotorID);

            bool hasAnyFrozen = false;
            for (int i = 0; i < 12; i++) { if (singleMotorFrozen[i]) { hasAnyFrozen = true; break; } }

            bool hasPendingSingleFreeze = false;
            for (int i = 0; i < 12; i++) { if (_pendingSingleMotorFreeze[i]) { hasPendingSingleFreeze = true; break; } }

            bool hasPendingSingleUnfreeze = false;
            for (int i = 0; i < 12; i++) { if (_pendingSingleMotorUnfreeze[i]) { hasPendingSingleUnfreeze = true; break; } }

            bool hasPendingPaxini = _pendingThumbAutoOn || _pendingIndexAutoOn || _pendingMiddleAutoOn
                                 || _pendingThumbAutoOff || _pendingIndexAutoOff || _pendingMiddleAutoOff
                                 || _pendingThumbDirectOff || _pendingIndexDirectOff || _pendingMiddleDirectOff;

            if (hasAnyFrozen || hasPendingPaxini || hasPendingSingleFreeze || hasPendingSingleUnfreeze)
            {
                if (isRoundAway)
                {
                    if (!_frozenBaselineCaptured)
                    {
                        _frozenBaselineCaptured = true;

                        // Commit pending single-motor freeze only after round-away.
                        for (int i = 0; i < 12; i++)
                        {
                            if (!_pendingSingleMotorFreeze[i])
                            {
                                continue;
                            }

                            _pendingSingleMotorFreeze[i] = false;
                            singleMotorFrozen[i] = true;
                            CheckAndAutoEnablePaxini(i + 1);
                        }

                        // Commit pending single-motor unfreeze only after round-away.
                        for (int i = 0; i < 12; i++)
                        {
                            if (!_pendingSingleMotorUnfreeze[i])
                            {
                                continue;
                            }

                            _pendingSingleMotorUnfreeze[i] = false;
                            int unfrozenMotorID = i + 1;
                            singleMotorFrozen[i] = false;
                            CheckAndAutoDisablePaxini(unfrozenMotorID);
                        }

                        // ── Commit pending Paxini auto-ON (all-4-frozen → Paxini freeze-on) ──────────
                        if (_pendingThumbAutoOn && SelectMotorCollider != null)
                        {
                            _pendingThumbAutoOn = false;
                            SelectMotorCollider.thumbPaxiniForceYellow = false;
                            for (int m = 1; m <= 4; m++) singleMotorFrozen[m - 1] = true;
                            SelectMotorCollider.ForcePaxiniOnForMotor(1);
                        }
                        if (_pendingIndexAutoOn && SelectMotorCollider != null)
                        {
                            _pendingIndexAutoOn = false;
                            SelectMotorCollider.indexPaxiniForceYellow = false;
                            for (int m = 5; m <= 8; m++) singleMotorFrozen[m - 1] = true;
                            SelectMotorCollider.ForcePaxiniOnForMotor(5);
                        }
                        if (_pendingMiddleAutoOn && SelectMotorCollider != null)
                        {
                            _pendingMiddleAutoOn = false;
                            SelectMotorCollider.middlePaxiniForceYellow = false;
                            for (int m = 9; m <= 12; m++) singleMotorFrozen[m - 1] = true;
                            SelectMotorCollider.ForcePaxiniOnForMotor(9);
                        }

                        // ── Commit pending Paxini auto-OFF (one-unfroze → Paxini freeze-off) ─────────
                        if (_pendingThumbAutoOff && SelectMotorCollider != null)
                        {
                            _suppressThumbPaxiniGroupUnfreeze = true;
                            _pendingThumbAutoOff = false;
                            SelectMotorCollider.thumbPaxiniForceOriginal = false;
                            SelectMotorCollider.ForcePaxiniOffForMotor(1);
                        }
                        if (_pendingIndexAutoOff && SelectMotorCollider != null)
                        {
                            _suppressIndexPaxiniGroupUnfreeze = true;
                            _pendingIndexAutoOff = false;
                            SelectMotorCollider.indexPaxiniForceOriginal = false;
                            SelectMotorCollider.ForcePaxiniOffForMotor(5);
                        }
                        if (_pendingMiddleAutoOff && SelectMotorCollider != null)
                        {
                            _suppressMiddlePaxiniGroupUnfreeze = true;
                            _pendingMiddleAutoOff = false;
                            SelectMotorCollider.middlePaxiniForceOriginal = false;
                            SelectMotorCollider.ForcePaxiniOffForMotor(9);
                        }

                        // ── Commit pending direct Paxini OFF (baseline-frozen motors → off) ──────────
                        // thumbFreezeEnabled is already false; un-freeze baseline-frozen motors.
                        if (_pendingThumbDirectOff)
                        {
                            _pendingThumbDirectOff = false; _suppressThumbGroupYellow = false;
                            for (int m = 1; m <= 4; m++) if (_singleMotorFrozenBaseline[m - 1]) singleMotorFrozen[m - 1] = false;
                        }
                        if (_pendingIndexDirectOff)
                        {
                            _pendingIndexDirectOff = false; _suppressIndexGroupYellow = false;
                            for (int m = 5; m <= 8; m++) if (_singleMotorFrozenBaseline[m - 1]) singleMotorFrozen[m - 1] = false;
                        }
                        if (_pendingMiddleDirectOff)
                        {
                            _pendingMiddleDirectOff = false; _suppressMiddleGroupYellow = false;
                            for (int m = 9; m <= 12; m++) if (_singleMotorFrozenBaseline[m - 1]) singleMotorFrozen[m - 1] = false;
                        }

                        // Capture current frozen state as the new round baseline.
                        CaptureModeSelectBaseline();
                        // Commit changed state first, then repaint from authoritative state
                        // so stale yellow/original colors cannot linger after hand-away.
                        UpdateMotorColors();
                        // Reset hover/selection transient state only when no motor is being touched
                        if (!motorSelected && !isHoldingArmUIPaxiniButton)
                        {
                            lastTouchedMotorID      = 0;
                            currentRedMotorID       = 0;
                            isConfirmed             = false;
                            touchStartTime          = 0f;
                            _isUnfreezing           = false;
                            _unfreezeTargetMotorID  = 0;
                            _justFrozeWhileHolding  = false;
                            _justFrozeMotorID       = 0;
                            _singleFreezeInProgress = false;
                        }
                    }
                }
                else
                {
                    // Hand came back close — allow the lock to re-trigger on the next away event
                    _frozenBaselineCaptured = false;
                }
            }
            else
            {
                _frozenBaselineCaptured = false;
                // If a round change was committed (e.g. an unfreeze) but no motors are
                // currently frozen, capture a fresh baseline when the hand moves away.
                // Without this, the stale frozen baseline would incorrectly re-freeze
                // the unfrozen motor the next time a different motor is confirmed.
                if (_roundChangedMotorID != 0 && isRoundAway)
                {
                    if (!_noFreezeRoundBaselineCaptured)
                    {
                        _noFreezeRoundBaselineCaptured = true;
                        CaptureModeSelectBaseline(); // refreshes _singleMotorFrozenBaseline and resets _roundChangedMotorID
                        UpdateMotorColors();
                    }
                }
                else
                {
                    _noFreezeRoundBaselineCaptured = false;
                }
            }
        }

        // Transition: Select → Manipulate (distance increases)
        // Only confirmed motor (dark red) can enter Manipulate mode
        // Fingertip priority mode: Must be in MotorConfirmed phase
        bool canEnterManipulate = modeSelect && motorSelected && confirmedMotorID != 0;
        if (isConfirmedPaxiniMotor)
        {
            canEnterManipulate = false;
        }
        if (useFingertipFirst)
        {
            canEnterManipulate = canEnterManipulate && currentPhase == SelectionPhase.MotorConfirmed;
        }
        if (SelectMotorCollider != null && SelectMotorCollider.suppressManipulateTransition)
        {
            canEnterManipulate = false;
        }
        bool armUIWantsEnterManipulate = isArmUIPlaneActive && !_armUIInputIsInsideEnterPlane;
        // Block manipulate while single-motor freeze is building up (red→yellow lerp in progress)
        if (_singleFreezeInProgress)
        {
            canEnterManipulate = false;
        }

        if (canEnterManipulate)
        {
            bool shouldEnterManipulate = false;
            if (isArmUIPlaneActive)
            {
                // Arm UI equivalent of "moved away": left enter plane.
                shouldEnterManipulate = armUIWantsEnterManipulate;
            }
            else
            {
                float distance = currentHandSeparationDistance;
                shouldEnterManipulate = distance > activeSeparationThreshold;
            }

            if (shouldEnterManipulate)
            {
                modeSelect = false;
                motorSelected = false;
                modeManipulate = true;
                hasEnteredCloseRange = false; // Reset when entering manipulate mode
                hasSetManipulateColors = false; // Reset color flag
                
                // Hide all debug visuals (spheres + LineRenderers) when entering manipulate mode
                if (SelectMotorCollider != null)
                {
                    SelectMotorCollider.HideAllDebugVisuals();
                }
            }
        }

        // Mode Manipulate: Track state and exit conditions
        if (modeManipulate)
        {
            // baseRenderer.material.color = Color.green; // Indicate Manipulate mode

            float distance = currentHandSeparationDistance;

            // Set colors only once when entering manipulate mode
            if (!hasSetManipulateColors)
            {
                // Reset all motors to original color first (no gray in manipulation mode)
                ResetAllColors();
                
                if (confirmedMotorID == 1)
                {
                    thumbJoint1Renderer.material.color = Color.red; // Keep red
                    thumbJoint2Renderer.material = yellowMaterial;
                    thumbJoint3Renderer.material = yellowMaterial;
                    thumbJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 2)
                {
                    thumbJoint2Renderer.material.color = Color.red; // Keep red
                    thumbJoint1Renderer.material = yellowMaterial;
                    thumbJoint3Renderer.material = yellowMaterial;
                    thumbJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 3)
                {
                    thumbJoint3Renderer.material.color = Color.red; // Keep red
                    thumbJoint1Renderer.material = yellowMaterial;
                    thumbJoint2Renderer.material = yellowMaterial;
                    thumbJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 4)
                {
                    thumbJoint4Renderer.material.color = Color.red; // Keep red
                    thumbJoint1Renderer.material = yellowMaterial;
                    thumbJoint2Renderer.material = yellowMaterial;
                    thumbJoint3Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 5)
                {
                    indexJoint1Renderer.material.color = Color.red; // Keep red
                    indexJoint2Renderer.material = yellowMaterial;
                    indexJoint3Renderer.material = yellowMaterial;
                    indexJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 6)
                {
                    indexJoint2Renderer.material.color = Color.red; // Keep red
                    indexJoint1Renderer.material = yellowMaterial;
                    indexJoint3Renderer.material = yellowMaterial;
                    indexJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 7)
                {
                    indexJoint3Renderer.material.color = Color.red; // Keep red
                    indexJoint1Renderer.material = yellowMaterial;
                    indexJoint2Renderer.material = yellowMaterial;
                    indexJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 8)
                {
                    indexJoint4Renderer.material.color = Color.red; // Keep red
                    indexJoint1Renderer.material = yellowMaterial;
                    indexJoint2Renderer.material = yellowMaterial;
                    indexJoint3Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 9)
                {
                    middleJoint1Renderer.material.color = Color.red; // Keep red
                    middleJoint2Renderer.material = yellowMaterial;
                    middleJoint3Renderer.material = yellowMaterial;
                    middleJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 10)
                {
                    middleJoint2Renderer.material.color = Color.red; // Keep red
                    middleJoint1Renderer.material = yellowMaterial;
                    middleJoint3Renderer.material = yellowMaterial;
                    middleJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 11)
                {
                    middleJoint3Renderer.material.color = Color.red; // Keep red
                    middleJoint1Renderer.material = yellowMaterial;
                    middleJoint2Renderer.material = yellowMaterial;
                    middleJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 12)
                {
                    middleJoint4Renderer.material.color = Color.red; // Keep red
                    middleJoint1Renderer.material = yellowMaterial;
                    middleJoint2Renderer.material = yellowMaterial;
                    middleJoint3Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == ThumbPaxiniMotorID)
                {
                    if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = Color.red;
                    thumbJoint1Renderer.material = yellowMaterial;
                    thumbJoint2Renderer.material = yellowMaterial;
                    thumbJoint3Renderer.material = yellowMaterial;
                    thumbJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == IndexPaxiniMotorID)
                {
                    if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = Color.red;
                    indexJoint1Renderer.material = yellowMaterial;
                    indexJoint2Renderer.material = yellowMaterial;
                    indexJoint3Renderer.material = yellowMaterial;
                    indexJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == MiddlePaxiniMotorID)
                {
                    if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = Color.red;
                    middleJoint1Renderer.material = yellowMaterial;
                    middleJoint2Renderer.material = yellowMaterial;
                    middleJoint3Renderer.material = yellowMaterial;
                    middleJoint4Renderer.material = yellowMaterial;
                }
                hasSetManipulateColors = true;
            }

            // Frozen motors keep yellow with HIGHEST PRIORITY every frame in modeManipulate
            for (int i = 0; i < 12; i++)
                if (singleMotorFrozen[i])
                    SetMotorColorDirect(i + 1, singleFrozenColor);

            // Track if we've entered close range (< active threshold) for manipulation
            if ((isArmUIPlaneActive && _armUIInputIsInsideEnterPlane)
                || (!isArmUIPlaneActive && distance < activeSeparationThreshold))
            {
                hasEnteredCloseRange = true;
            }

            // Only exit if we've performed manipulation (entered close range) 
            // and then moved back out (> active threshold)
            bool shouldExitManipulate = false;
            if (isArmUIPlaneActive)
            {
                // Arm UI equivalent of "close then away": re-entered then left enter plane.
                shouldExitManipulate = hasEnteredCloseRange && !_armUIInputIsInsideEnterPlane;
            }
            else
            {
                shouldExitManipulate = hasEnteredCloseRange && distance > activeSeparationThreshold;
            }

            if (shouldExitManipulate)
            {
                AutoFreezeConfirmedMotorAfterArmUIManipulate();

                modeSelect = true;
                motorSelected = false;
                modeManipulate = false;
                currentRedMotorID = 0;
                confirmedMotorID = 0;
                lastTouchedMotorID = 0;
                isConfirmed = false;
                touchStartTime = 0f;
                hasEnteredCloseRange = false;
                hasSetManipulateColors = false;
                _isUnfreezing = false;
                _unfreezeTargetMotorID = 0;
                _justFrozeWhileHolding = false;
                _justFrozeMotorID = 0;
                _singleFreezeInProgress = false;

                // Restore debug visuals based on current toggle states
                if (SelectMotorCollider != null)
                {
                    SelectMotorCollider.RestoreDebugVisuals();
                }

                // Reset fingertip priority mode state
                if (useFingertipFirst)
                {
                    currentPhase = SelectionPhase.SelectingFingertip;
                    confirmedFingertipID = 0;
                    SelectMotorCollider.ResetFingertipConfirmation();
                    SelectMotorCollider.ReleaseFrozenLine();
                }

                // Apply colors AFTER all state resets so frozen yellow is always preserved
                ResetAllColors();
                UpdateMotorColors();

                CaptureModeSelectBaseline();
            }
        }

        // Per-frame Paxini group sync:
        // 1) Paxini ON always keeps group motors visually yellow and angle-locked (SMC layer).
        // 2) singleMotorFrozen state is committed to full-group freeze ONLY after hand-away > 0.16m.
        // Guard: skip during active unfreeze animation so the lerp hint color is visible.
        if (SelectMotorCollider != null && !_isUnfreezing)
        {
            // State sync (modeSelect): commit full-group freeze only after hand-away threshold.
            // Skip when auto-OFF is pending (those groups must NOT be force-frozen here).
            if (modeSelect)
            {
                bool shouldCommitPaxiniFreeze = isRoundAway;
                bool committedStateChanged = false;
                if (shouldCommitPaxiniFreeze && SelectMotorCollider.thumbFreezeEnabled && !_pendingThumbAutoOff)
                {
                    for (int m = 1; m <= 4; m++)
                    {
                        if (!singleMotorFrozen[m - 1])
                        {
                            singleMotorFrozen[m - 1] = true;
                            committedStateChanged = true;
                        }
                    }
                }
                if (shouldCommitPaxiniFreeze && SelectMotorCollider.indexFreezeEnabled && !_pendingIndexAutoOff)
                {
                    for (int m = 5; m <= 8; m++)
                    {
                        if (!singleMotorFrozen[m - 1])
                        {
                            singleMotorFrozen[m - 1] = true;
                            committedStateChanged = true;
                        }
                    }
                }
                if (shouldCommitPaxiniFreeze && SelectMotorCollider.middleFreezeEnabled && !_pendingMiddleAutoOff)
                {
                    for (int m = 9; m <= 12; m++)
                    {
                        if (!singleMotorFrozen[m - 1])
                        {
                            singleMotorFrozen[m - 1] = true;
                            committedStateChanged = true;
                        }
                    }
                }

                // Keep state/color aligned in the same frame at threshold crossing.
                if (committedStateChanged)
                {
                    // Round-away Paxini commit changed authoritative state.
                    // Re-capture baseline in the same frame so next selecting round
                    // still has exactly one diff against baseline.
                    CaptureModeSelectBaseline();
                    UpdateMotorColors();
                }
            }
            // Color sync intentionally removed:
            // Paxini immediate color affects only Paxini motor (13/14/15).
            // Group motor colors are driven by singleMotorFrozen after round-away commit.
        }

        SyncArmUIProxyStateFromCanonical(isArmUIPlaneActive);
    }

    private void AutoFreezeConfirmedMotorAfterArmUIManipulate()
    {
        if (!IsArmUIPlaneActive() || armUIPlaneController == null)
        {
            return;
        }

        if (!armUIPlaneController.ShouldAutoFreezeConfirmedMotorOnManipulateExit())
        {
            return;
        }

        int motorID = confirmedMotorID;
        if (motorID < 1 || motorID > 12)
        {
            return;
        }

        if (_roundChangedMotorID != 0 && _roundChangedMotorID != motorID)
        {
            RevertMotorToBaseline(_roundChangedMotorID);
            _roundChangedMotorID = 0;
        }

        _pendingSingleMotorFreeze[motorID - 1] = true;
        _pendingSingleMotorUnfreeze[motorID - 1] = false;
        if (armUIPlaneController != null && armUIPlaneController.clawModuleController != null)
        {
            armUIPlaneController.clawModuleController.CaptureSingleMotorFreezeSnapshot(motorID);
        }
        _roundChangedMotorID = !IsBaselineFrozenForMotor(motorID) ? motorID : 0;
        _frozenBaselineCaptured = false;
        _noFreezeRoundBaselineCaptured = false;

        SetMotorColorDirect(motorID, singleFrozenColor);
        CheckAndAutoEnablePaxini(motorID);
        UpdateMotorColors();
    }

    private void UpdateCurrentHandSeparationDistance()
    {
        if (useControllerSeperationDistance)
        {
            if (controllerlocatorleft != null)
            {
                currentHandSeparationDistance = controllerlocatorleft.currentControllerSeparationDistance;
                return;
            }

            currentHandSeparationDistance = 0f;
            return;
        }

        if (jointAngle != null)
        {
            currentHandSeparationDistance = jointAngle.GetLIndexToIndex2Distance();
        }
    }

    private float GetActiveSeparationThreshold()
    {
        return useControllerSeperationDistance ? controllerSeparationThreshold : handSeparationThreshold;
    }

    private bool IsArmUIPlaneActive()
    {
        return armUIPlaneController != null && armUIPlaneController.useArmUIPlane;
    }

    public void ReceiveArmUIInput(bool isInsideEnterPlane, int touchedMotorID)
    {
        _armUIInputIsInsideEnterPlane = isInsideEnterPlane;
        _armUIInputTouchedMotorID = touchedMotorID;
    }

    public void ClearArmUIInput()
    {
        _armUIInputIsInsideEnterPlane = false;
        _armUIInputTouchedMotorID = 0;
        ResetArmUIProxyState();
    }

    private void ClearSelectionStateForArmUIPlane()
    {
        modeSelect = true;
        modeManipulate = false;
        lastTouchedMotorID = 0;
        currentRedMotorID = 0;
        confirmedMotorID = 0;
        motorSelected = false;
        isConfirmed = false;
        touchStartTime = 0f;
        _isUnfreezing = false;
        _unfreezeTargetMotorID = 0;
        _justFrozeWhileHolding = false;
        _justFrozeMotorID = 0;
        _singleFreezeInProgress = false;
        hasEnteredCloseRange = false;
        hasSetManipulateColors = false;

        if (useFingertipFirst)
        {
            currentPhase = SelectionPhase.SelectingFingertip;
            confirmedFingertipID = 0;
            if (SelectMotorCollider != null)
            {
                SelectMotorCollider.ResetFingertipConfirmation();
                SelectMotorCollider.ReleaseFrozenLine();
            }
        }

        if (SelectMotorCollider != null)
        {
            SelectMotorCollider.RestoreDebugVisuals();
        }

        UpdateMotorColors();
    }

    private void ResetTransientSelectionStateForArmUIPlane()
    {
        lastTouchedMotorID = 0;
        currentRedMotorID = 0;
        confirmedMotorID = 0;
        motorSelected = false;
        isConfirmed = false;
        touchStartTime = 0f;
        _isUnfreezing = false;
        _unfreezeTargetMotorID = 0;
        _justFrozeWhileHolding = false;
        _justFrozeMotorID = 0;
        _singleFreezeInProgress = false;
        hasEnteredCloseRange = false;
        hasSetManipulateColors = false;

        if (useFingertipFirst)
        {
            currentPhase = SelectionPhase.SelectingFingertip;
            confirmedFingertipID = 0;
            if (SelectMotorCollider != null)
            {
                SelectMotorCollider.ResetFingertipConfirmation();
                SelectMotorCollider.ReleaseFrozenLine();
            }
        }

        if (SelectMotorCollider != null)
        {
            SelectMotorCollider.RestoreDebugVisuals();
        }

        UpdateMotorColors();
    }

    private int GetCurrentTouchedMotorIDForSelection(bool isArmUIPlaneActive)
    {
        armUIProxyRejectedMotorID = 0;
        armUIProxyRejectReason = "None";

        if (!isArmUIPlaneActive)
        {
            return SelectMotorCollider != null ? SelectMotorCollider.currentTouchedMotorID : 0;
        }

        // Projection can remain selectable while ArmUI plane is active.
        if (allowProjectionSelectionWhenArmUIPlaneActive && SelectMotorCollider != null)
        {
            int projectionMotorID = SelectMotorCollider.currentTouchedMotorID;
            if (projectionMotorID != 0 && IsMotorSelectableForCurrentState(projectionMotorID))
            {
                return projectionMotorID;
            }
        }

        if (!_armUIInputIsInsideEnterPlane)
        {
            return 0;
        }

        if (IsMotorSelectableForCurrentState(_armUIInputTouchedMotorID))
        {
            return _armUIInputTouchedMotorID;
        }

        armUIProxyRejectedMotorID = _armUIInputTouchedMotorID;
        armUIProxyRejectReason = GetMotorRejectReasonForCurrentState(_armUIInputTouchedMotorID);
        return 0;
    }

    private bool IsMotorSelectableForCurrentState(int motorID)
    {
        if (motorID == 0)
        {
            return false;
        }

        if (!useFingertipFirst)
        {
            return true;
        }

        if (motorID == 4 || motorID == 8 || motorID == 12)
        {
            return true;
        }

        if (confirmedFingertipID == 0)
        {
            return false;
        }

        switch (confirmedFingertipID)
        {
            case 4:
                return (motorID >= 1 && motorID <= 4) || motorID == ThumbPaxiniMotorID;
            case 8:
                return (motorID >= 5 && motorID <= 8) || motorID == IndexPaxiniMotorID;
            case 12:
                return (motorID >= 9 && motorID <= 12) || motorID == MiddlePaxiniMotorID;
            default:
                return false;
        }
    }

    private string GetMotorRejectReasonForCurrentState(int motorID)
    {
        if (motorID == 0)
        {
            return "No raw touched motor";
        }

        if (!useFingertipFirst)
        {
            return "Selectable";
        }

        if (motorID == 4 || motorID == 8 || motorID == 12)
        {
            return "Selectable fingertip";
        }

        if (confirmedFingertipID == 0)
        {
            return "Blocked by fingertip-first: confirm 4/8/12 first";
        }

        switch (confirmedFingertipID)
        {
            case 4:
                return (motorID >= 1 && motorID <= 4) || motorID == ThumbPaxiniMotorID
                    ? "Selectable in thumb group"
                    : "Blocked by fingertip-first: thumb group only";
            case 8:
                return (motorID >= 5 && motorID <= 8) || motorID == IndexPaxiniMotorID
                    ? "Selectable in index group"
                    : "Blocked by fingertip-first: index group only";
            case 12:
                return (motorID >= 9 && motorID <= 12) || motorID == MiddlePaxiniMotorID
                    ? "Selectable in middle group"
                    : "Blocked by fingertip-first: middle group only";
            default:
                return "Blocked: invalid confirmed fingertip state";
        }
    }

    private bool IsCanonicalPaxiniOnForMotor(int motorID)
    {
        if (SelectMotorCollider == null)
        {
            return false;
        }

        if (motorID >= 1 && motorID <= 4) return SelectMotorCollider.thumbFreezeEnabled;
        if (motorID >= 5 && motorID <= 8) return SelectMotorCollider.indexFreezeEnabled;
        if (motorID >= 9 && motorID <= 12) return SelectMotorCollider.middleFreezeEnabled;
        if (motorID == ThumbPaxiniMotorID) return SelectMotorCollider.thumbFreezeEnabled;
        if (motorID == IndexPaxiniMotorID) return SelectMotorCollider.indexFreezeEnabled;
        if (motorID == MiddlePaxiniMotorID) return SelectMotorCollider.middleFreezeEnabled;
        return false;
    }

    private void CommitArmUIPaxiniToggle(int motorID)
    {
        int groupStart = GetGroupStartForPaxiniMotor(motorID);
        if (groupStart == 0 || SelectMotorCollider == null)
        {
            return;
        }

        if (IsCanonicalPaxiniOnForMotor(motorID))
        {
            SelectMotorCollider.ForcePaxiniOffForMotor(groupStart);
            UnfreezeGroupMotors(groupStart);
        }
        else
        {
            SelectMotorCollider.ForcePaxiniOnForMotor(groupStart);
            FreezeGroupMotors(groupStart);
        }

        motorSelected = false;
        currentRedMotorID = 0;
        confirmedMotorID = 0;
        isConfirmed = false;
        touchStartTime = 0f;
        _isUnfreezing = false;
        _unfreezeTargetMotorID = 0;
        _singleFreezeInProgress = false;
        _justFrozeWhileHolding = true;
        _justFrozeMotorID = motorID;
    }

    private int GetGroupStartForPaxiniMotor(int motorID)
    {
        if (motorID == ThumbPaxiniMotorID) return 1;
        if (motorID == IndexPaxiniMotorID) return 5;
        if (motorID == MiddlePaxiniMotorID) return 9;
        return 0;
    }

    private void SyncArmUIProxyStateFromCanonical(bool isArmUIPlaneActive)
    {
        armUIProxyModeSelect = isArmUIPlaneActive && modeSelect;
        armUIProxyModeManipulate = isArmUIPlaneActive && modeManipulate;
        armUIProxyCurrentTouchedMotorID = armUIProxyModeSelect && _armUIInputIsInsideEnterPlane && armUIProxyRejectedMotorID == 0
            ? _armUIInputTouchedMotorID
            : 0;
        armUIProxyCurrentRedMotorID = currentRedMotorID;
        armUIProxyConfirmedMotorID = confirmedMotorID;
        armUIProxyConfirmedFingertipID = confirmedFingertipID;
        armUIProxyThumbPaxiniOn = SelectMotorCollider != null && SelectMotorCollider.thumbFreezeEnabled;
        armUIProxyIndexPaxiniOn = SelectMotorCollider != null && SelectMotorCollider.indexFreezeEnabled;
        armUIProxyMiddlePaxiniOn = SelectMotorCollider != null && SelectMotorCollider.middleFreezeEnabled;

        if (armUIProxySingleMotorFrozen == null || armUIProxySingleMotorFrozen.Length != 12)
        {
            armUIProxySingleMotorFrozen = new bool[12];
        }

        for (int i = 0; i < 12; i++)
        {
            armUIProxySingleMotorFrozen[i] = singleMotorFrozen[i] || _pendingSingleMotorFreeze[i];
        }
    }

    private void UpdateArmUIProxyState()
    {
        armUIProxyModeSelect = _armUIInputIsInsideEnterPlane;
        armUIProxyModeManipulate = !_armUIInputIsInsideEnterPlane && armUIProxyConfirmedMotorID != 0;
        armUIProxyCurrentTouchedMotorID = armUIProxyModeSelect ? _armUIInputTouchedMotorID : 0;

        if (_armUIWasInsideEnterPlane != _armUIInputIsInsideEnterPlane)
        {
            if (_armUIInputIsInsideEnterPlane)
            {
                armUIProxyModeManipulate = false;
                armUIProxyCurrentRedMotorID = 0;
                armUIProxyConfirmedMotorID = 0;
                armUIProxyConfirmedFingertipID = 0;
                _armUIIsConfirmed = false;
                _armUITouchStartTime = 0f;
                _armUILastTouchedMotorID = 0;
                _armUIIsUnfreezing = false;
                _armUIUnfreezeTargetMotorID = 0;
                _armUIJustFrozeWhileHolding = false;
                _armUIJustFrozeMotorID = 0;
            }

            _armUIWasInsideEnterPlane = _armUIInputIsInsideEnterPlane;
        }

        if (armUIProxyModeSelect)
        {
            ProcessArmUIProxySelectionState();
        }

        ApplyArmUIProxyOutputs();
    }

    private void ProcessArmUIProxySelectionState()
    {
        bool isSelectable = IsArmUIProxyMotorSelectable(_armUIInputTouchedMotorID);
        int currentMotorID = isSelectable ? _armUIInputTouchedMotorID : 0;
        armUIProxyRejectedMotorID = isSelectable ? 0 : _armUIInputTouchedMotorID;
        armUIProxyRejectReason = GetArmUIProxyRejectReason(_armUIInputTouchedMotorID);

        if (currentMotorID != _armUILastTouchedMotorID)
        {
            _armUIJustFrozeWhileHolding = false;
            _armUIJustFrozeMotorID = 0;

            if (currentMotorID != 0)
            {
                bool isFrozenMotor = currentMotorID >= 1 && currentMotorID <= 12
                                    && (armUIProxySingleMotorFrozen[currentMotorID - 1] || IsArmUIProxyPaxiniOnForMotor(currentMotorID));

                if (isFrozenMotor)
                {
                    _armUIIsUnfreezing = true;
                    _armUIUnfreezeTargetMotorID = currentMotorID;
                    _armUITouchStartTime = Time.time;
                    _armUIIsConfirmed = false;
                    armUIProxyCurrentRedMotorID = currentMotorID;
                }
                else
                {
                    _armUIIsUnfreezing = false;
                    _armUIUnfreezeTargetMotorID = 0;
                    _armUITouchStartTime = Time.time;
                    _armUIIsConfirmed = false;
                    armUIProxyCurrentRedMotorID = currentMotorID;
                }
            }
            else
            {
                _armUIIsUnfreezing = false;
                _armUIUnfreezeTargetMotorID = 0;
                armUIProxyCurrentRedMotorID = 0;
            }

            _armUILastTouchedMotorID = currentMotorID;
            return;
        }

        if (currentMotorID == 0)
        {
            return;
        }

        if (_armUIJustFrozeWhileHolding && currentMotorID == _armUIJustFrozeMotorID)
        {
            return;
        }

        if (_armUIIsUnfreezing)
        {
            if (!_armUIIsConfirmed && (Time.time - _armUITouchStartTime) >= unfreezeConfirmationTime)
            {
                CommitArmUIProxyUnfreeze(_armUIUnfreezeTargetMotorID);
            }
            return;
        }

        bool isCrossFingerSwitch = armUIProxyConfirmedMotorID != 0 && !IsSameFingerGroup(armUIProxyConfirmedMotorID, currentMotorID);
        float requiredConfirmTime = isCrossFingerSwitch ? fingertipConfirmationTime : confirmationTime;
        if (!_armUIIsConfirmed && (Time.time - _armUITouchStartTime) >= requiredConfirmTime)
        {
            _armUIIsConfirmed = true;
            CommitArmUIProxyConfirmation(currentMotorID);
        }

        if (_armUIIsConfirmed && armUIProxyConfirmedMotorID >= 1 && armUIProxyConfirmedMotorID <= 12
            && currentMotorID == armUIProxyConfirmedMotorID)
        {
            float elapsed = Time.time - _armUITouchStartTime - confirmationTime;
            if (elapsed >= singleMotorFreezeTime && !armUIProxySingleMotorFrozen[armUIProxyConfirmedMotorID - 1])
            {
                int frozenID = armUIProxyConfirmedMotorID;
                armUIProxySingleMotorFrozen[frozenID - 1] = true;
                armUIProxyConfirmedMotorID = 0;
                armUIProxyCurrentRedMotorID = 0;
                _armUIIsConfirmed = false;
                _armUIIsUnfreezing = false;
                _armUIJustFrozeWhileHolding = true;
                _armUIJustFrozeMotorID = frozenID;
                CheckAndAutoEnableArmUIProxyPaxini(frozenID);
            }
        }
    }

    private void CommitArmUIProxyConfirmation(int motorID)
    {
        if (IsPaxiniMotor(motorID))
        {
            ToggleArmUIProxyPaxini(motorID);
            armUIProxyConfirmedMotorID = 0;
            armUIProxyCurrentRedMotorID = 0;
            _armUIIsConfirmed = false;
            return;
        }

        if (useFingertipFirst)
        {
            if (motorID == 4 || motorID == 8 || motorID == 12)
            {
                armUIProxyConfirmedFingertipID = motorID;
                armUIProxyConfirmedMotorID = motorID;
            }
            else
            {
                armUIProxyConfirmedMotorID = motorID;
            }
        }
        else
        {
            armUIProxyConfirmedMotorID = motorID;
        }
    }

    private void CommitArmUIProxyUnfreeze(int motorID)
    {
        if (motorID < 1 || motorID > 12)
        {
            return;
        }

        armUIProxySingleMotorFrozen[motorID - 1] = false;
        ClearArmUIProxyPaxiniForMotorGroup(motorID);
        _armUIIsUnfreezing = false;
        _armUIUnfreezeTargetMotorID = 0;
        armUIProxyCurrentRedMotorID = 0;
        armUIProxyConfirmedMotorID = 0;
        _armUIIsConfirmed = false;
    }

    private bool IsArmUIProxyMotorSelectable(int motorID)
    {
        if (motorID == 0)
        {
            return false;
        }

        if (!useFingertipFirst)
        {
            return true;
        }

        if (motorID == 4 || motorID == 8 || motorID == 12)
        {
            return true;
        }

        if (armUIProxyConfirmedFingertipID == 0)
        {
            return false;
        }

        switch (armUIProxyConfirmedFingertipID)
        {
            case 4:
                return (motorID >= 1 && motorID <= 4) || motorID == ThumbPaxiniMotorID;
            case 8:
                return (motorID >= 5 && motorID <= 8) || motorID == IndexPaxiniMotorID;
            case 12:
                return (motorID >= 9 && motorID <= 12) || motorID == MiddlePaxiniMotorID;
            default:
                return false;
        }
    }

    private string GetArmUIProxyRejectReason(int motorID)
    {
        if (motorID == 0)
        {
            return "No raw touched motor";
        }

        if (!useFingertipFirst)
        {
            return "Selectable";
        }

        if (motorID == 4 || motorID == 8 || motorID == 12)
        {
            return "Selectable fingertip";
        }

        if (armUIProxyConfirmedFingertipID == 0)
        {
            return "Blocked by fingertip-first: confirm 4/8/12 first";
        }

        switch (armUIProxyConfirmedFingertipID)
        {
            case 4:
                return (motorID >= 1 && motorID <= 4) || motorID == ThumbPaxiniMotorID
                    ? "Selectable in thumb group"
                    : "Blocked by fingertip-first: thumb group only";
            case 8:
                return (motorID >= 5 && motorID <= 8) || motorID == IndexPaxiniMotorID
                    ? "Selectable in index group"
                    : "Blocked by fingertip-first: index group only";
            case 12:
                return (motorID >= 9 && motorID <= 12) || motorID == MiddlePaxiniMotorID
                    ? "Selectable in middle group"
                    : "Blocked by fingertip-first: middle group only";
            default:
                return "Blocked: invalid confirmed fingertip state";
        }
    }

    private bool IsArmUIProxyPaxiniOnForMotor(int motorID)
    {
        if (motorID >= 1 && motorID <= 4) return armUIProxyThumbPaxiniOn;
        if (motorID >= 5 && motorID <= 8) return armUIProxyIndexPaxiniOn;
        if (motorID >= 9 && motorID <= 12) return armUIProxyMiddlePaxiniOn;
        if (motorID == ThumbPaxiniMotorID) return armUIProxyThumbPaxiniOn;
        if (motorID == IndexPaxiniMotorID) return armUIProxyIndexPaxiniOn;
        if (motorID == MiddlePaxiniMotorID) return armUIProxyMiddlePaxiniOn;
        return false;
    }

    private void ToggleArmUIProxyPaxini(int motorID)
    {
        if (motorID == ThumbPaxiniMotorID)
        {
            armUIProxyThumbPaxiniOn = !armUIProxyThumbPaxiniOn;
            ApplyArmUIProxyPaxiniToGroup(1, 4, armUIProxyThumbPaxiniOn);
        }
        else if (motorID == IndexPaxiniMotorID)
        {
            armUIProxyIndexPaxiniOn = !armUIProxyIndexPaxiniOn;
            ApplyArmUIProxyPaxiniToGroup(5, 8, armUIProxyIndexPaxiniOn);
        }
        else if (motorID == MiddlePaxiniMotorID)
        {
            armUIProxyMiddlePaxiniOn = !armUIProxyMiddlePaxiniOn;
            ApplyArmUIProxyPaxiniToGroup(9, 12, armUIProxyMiddlePaxiniOn);
        }
    }

    private void ApplyArmUIProxyPaxiniToGroup(int startMotorID, int endMotorID, bool isOn)
    {
        for (int motorID = startMotorID; motorID <= endMotorID; motorID++)
        {
            armUIProxySingleMotorFrozen[motorID - 1] = isOn;
        }
    }

    private void ClearArmUIProxyPaxiniForMotorGroup(int motorID)
    {
        if (motorID >= 1 && motorID <= 4) armUIProxyThumbPaxiniOn = false;
        else if (motorID >= 5 && motorID <= 8) armUIProxyIndexPaxiniOn = false;
        else if (motorID >= 9 && motorID <= 12) armUIProxyMiddlePaxiniOn = false;
    }

    private void CheckAndAutoEnableArmUIProxyPaxini(int motorID)
    {
        if (motorID >= 1 && motorID <= 4 && IsArmUIProxyGroupAllFrozen(1, 4)) armUIProxyThumbPaxiniOn = true;
        else if (motorID >= 5 && motorID <= 8 && IsArmUIProxyGroupAllFrozen(5, 8)) armUIProxyIndexPaxiniOn = true;
        else if (motorID >= 9 && motorID <= 12 && IsArmUIProxyGroupAllFrozen(9, 12)) armUIProxyMiddlePaxiniOn = true;
    }

    private bool IsArmUIProxyGroupAllFrozen(int startMotorID, int endMotorID)
    {
        for (int motorID = startMotorID; motorID <= endMotorID; motorID++)
        {
            if (!armUIProxySingleMotorFrozen[motorID - 1])
            {
                return false;
            }
        }

        return true;
    }

    private void ApplyArmUIProxyOutputs()
    {
        SyncArmUIProxyStateFromCanonical(IsArmUIPlaneActive());
    }

    public Color GetArmUIProxyMotorDisplayColor(int motorID, Color fallbackOriginalColor)
    {
        if (motorID == ThumbPaxiniMotorID || motorID == IndexPaxiniMotorID || motorID == MiddlePaxiniMotorID)
        {
            return GetCanonicalPaxiniDisplayColor(
                IsCanonicalPaxiniOnForMotor(motorID),
                (SelectMotorCollider != null && motorID == ThumbPaxiniMotorID && SelectMotorCollider.thumbPaxiniForceYellow)
                    || (SelectMotorCollider != null && motorID == IndexPaxiniMotorID && SelectMotorCollider.indexPaxiniForceYellow)
                    || (SelectMotorCollider != null && motorID == MiddlePaxiniMotorID && SelectMotorCollider.middlePaxiniForceYellow),
                (SelectMotorCollider != null && motorID == ThumbPaxiniMotorID && SelectMotorCollider.thumbPaxiniForceOriginal)
                    || (SelectMotorCollider != null && motorID == IndexPaxiniMotorID && SelectMotorCollider.indexPaxiniForceOriginal)
                    || (SelectMotorCollider != null && motorID == MiddlePaxiniMotorID && SelectMotorCollider.middlePaxiniForceOriginal),
                fallbackOriginalColor);
        }

        if (_isUnfreezing && motorID == _unfreezeTargetMotorID)
        {
            return unfreezeHintColor;
        }

        Color baseColor = fallbackOriginalColor;
        if (motorID >= 1 && motorID <= 12)
        {
            if (_pendingSingleMotorFreeze[motorID - 1])
            {
                return singleFrozenColor;
            }

            if (_pendingSingleMotorUnfreeze[motorID - 1])
            {
                return baseColor;
            }

            bool suppressGroupYellow = (motorID <= 4 && _suppressThumbGroupYellow)
                                     || (motorID >= 5 && motorID <= 8 && _suppressIndexGroupYellow)
                                     || (motorID >= 9 && _suppressMiddleGroupYellow);

            if (singleMotorFrozen[motorID - 1] && !suppressGroupYellow)
            {
                return singleFrozenColor;
            }

            if (useFingertipFirst && grayMode)
            {
                if (confirmedFingertipID == 0)
                {
                    baseColor = (motorID == 4 || motorID == 8 || motorID == 12) ? fallbackOriginalColor : grayColor;
                }
                else if (!IsMotorSelectableForCurrentState(motorID))
                {
                    baseColor = grayColor;
                }
            }
        }

        if (confirmedMotorID == motorID)
            return darkRedColor;

        if (currentRedMotorID == motorID && currentRedMotorID != confirmedMotorID)
            return isConfirmed ? darkRedColor : lightRedColor;

        if (motorID >= 1 && motorID <= 12)
        {
            return baseColor;
        }

        if (motorID == ThumbPaxiniMotorID)
        {
            return GetCanonicalPaxiniDisplayColor(
                SelectMotorCollider != null ? SelectMotorCollider.thumbFreezeEnabled : false,
                SelectMotorCollider != null ? SelectMotorCollider.thumbPaxiniForceYellow : false,
                SelectMotorCollider != null ? SelectMotorCollider.thumbPaxiniForceOriginal : false,
                fallbackOriginalColor);
        }

        if (motorID == IndexPaxiniMotorID)
        {
            return GetCanonicalPaxiniDisplayColor(
                SelectMotorCollider != null ? SelectMotorCollider.indexFreezeEnabled : false,
                SelectMotorCollider != null ? SelectMotorCollider.indexPaxiniForceYellow : false,
                SelectMotorCollider != null ? SelectMotorCollider.indexPaxiniForceOriginal : false,
                fallbackOriginalColor);
        }

        if (motorID == MiddlePaxiniMotorID)
        {
            return GetCanonicalPaxiniDisplayColor(
                SelectMotorCollider != null ? SelectMotorCollider.middleFreezeEnabled : false,
                SelectMotorCollider != null ? SelectMotorCollider.middlePaxiniForceYellow : false,
                SelectMotorCollider != null ? SelectMotorCollider.middlePaxiniForceOriginal : false,
                fallbackOriginalColor);
        }

        return baseColor;
    }

    private Color GetCanonicalPaxiniDisplayColor(bool isFreezeEnabled, bool forceYellow, bool forceOriginal, Color originalColor)
    {
        if (forceYellow)
        {
            return singleFrozenColor;
        }

        if (forceOriginal)
        {
            return originalColor;
        }

        if (isFreezeEnabled)
        {
                return singleFrozenColor;
        }

        return originalColor;
    }

    private void ResetArmUIProxyState()
    {
        armUIProxyModeSelect = false;
        armUIProxyModeManipulate = false;
        armUIProxyCurrentTouchedMotorID = 0;
        armUIProxyRejectedMotorID = 0;
        armUIProxyRejectReason = "None";
        _armUIWasInsideEnterPlane = false;

        if (armUIProxySingleMotorFrozen == null || armUIProxySingleMotorFrozen.Length != 12)
        {
            armUIProxySingleMotorFrozen = new bool[12];
            return;
        }

        System.Array.Copy(singleMotorFrozen, armUIProxySingleMotorFrozen, 12);
        armUIProxyCurrentRedMotorID = currentRedMotorID;
        armUIProxyConfirmedMotorID = confirmedMotorID;
        armUIProxyConfirmedFingertipID = confirmedFingertipID;
        armUIProxyThumbPaxiniOn = SelectMotorCollider != null && SelectMotorCollider.thumbFreezeEnabled;
        armUIProxyIndexPaxiniOn = SelectMotorCollider != null && SelectMotorCollider.indexFreezeEnabled;
        armUIProxyMiddlePaxiniOn = SelectMotorCollider != null && SelectMotorCollider.middleFreezeEnabled;
    }

    private void CaptureModeSelectBaseline()
    {
        if (SelectMotorCollider == null)
        {
            _thumbBaselinePaxiniOn = false;
            _indexBaselinePaxiniOn = false;
            _middleBaselinePaxiniOn = false;
            System.Array.Clear(_singleMotorFrozenBaseline, 0, 12);
            return;
        }

        _thumbBaselinePaxiniOn = SelectMotorCollider.thumbFreezeEnabled;
        _indexBaselinePaxiniOn = SelectMotorCollider.indexFreezeEnabled;
        _middleBaselinePaxiniOn = SelectMotorCollider.middleFreezeEnabled;

        // Snapshot current single-motor frozen state as the new baseline.
        // Motors in the baseline will NOT be cleared by ClearSingleFrozenMotorInGroup.
        System.Array.Copy(singleMotorFrozen, _singleMotorFrozenBaseline, 12);

        // Paxini ON in baseline means the entire finger group is baseline-frozen.
        // This keeps one-change-per-round comparisons/reverts consistent even when
        // singleMotorFrozen was not yet expanded to all 4 motors in earlier frames.
        if (_thumbBaselinePaxiniOn)
            for (int m = 1; m <= 4; m++) _singleMotorFrozenBaseline[m - 1] = true;
        if (_indexBaselinePaxiniOn)
            for (int m = 5; m <= 8; m++) _singleMotorFrozenBaseline[m - 1] = true;
        if (_middleBaselinePaxiniOn)
            for (int m = 9; m <= 12; m++) _singleMotorFrozenBaseline[m - 1] = true;

        // New round — reset the one-motor-per-round change tracker
        _roundChangedMotorID = 0;
    }

    private bool IsBaselineFrozenForMotor(int motorID)
    {
        if (motorID < 1 || motorID > 12)
            return false;

        return _singleMotorFrozenBaseline[motorID - 1];
    }

    private void EnforceGroupBaselineForConfirmedMotor(int motorID)
    {
        if (SelectMotorCollider == null) return;

        // If a normal motor is confirmed, keep this finger's Paxini at baseline state.
        // This prevents accumulating 2 switched values versus modeSelect baseline.
        if (motorID >= 1 && motorID <= 4)
        {
            if (_thumbBaselinePaxiniOn)
                SelectMotorCollider.ForcePaxiniOnForMotor(motorID);
            else
            {
                // Baseline-enforced OFF is a programmatic revert, not a user direct OFF.
                // Suppress group-sync OFF→UnfreezeGroupMotors side-effect for this frame.
                _suppressThumbPaxiniGroupUnfreeze = true;
                SelectMotorCollider.ForcePaxiniOffForMotor(motorID);
            }
        }
        else if (motorID >= 5 && motorID <= 8)
        {
            if (_indexBaselinePaxiniOn)
                SelectMotorCollider.ForcePaxiniOnForMotor(motorID);
            else
            {
                _suppressIndexPaxiniGroupUnfreeze = true;
                SelectMotorCollider.ForcePaxiniOffForMotor(motorID);
            }
        }
        else if (motorID >= 9 && motorID <= 12)
        {
            if (_middleBaselinePaxiniOn)
                SelectMotorCollider.ForcePaxiniOnForMotor(motorID);
            else
            {
                _suppressMiddlePaxiniGroupUnfreeze = true;
                SelectMotorCollider.ForcePaxiniOffForMotor(motorID);
            }
        }
    }
    
    /// <summary>
    /// Update motor colors:
    /// - The confirmed motor (`confirmedMotorID`) is shown in dark red
    /// - The currently touched but unconfirmed motor is shown in light red
    /// </summary>
    private void UpdateMotorColors()
    {
        // Reset all colors first
        if (useFingertipFirst)
        {
            // Fingertip-first mode: keep selection gate logic, visual base depends on grayMode.
            ApplyModeSelectBaseColors();
        }
        else
        {
            ResetAllColors();
        }

        // If there is a confirmed motor, show it in dark red
        if (confirmedMotorID != 0 && !IsPaxiniMotor(confirmedMotorID))
        {
            SetMotorColorDirect(confirmedMotorID, darkRedColor);
        }

        // If the currently touched motor is not the confirmed one, show it in light red
        if (currentRedMotorID != 0 && currentRedMotorID != confirmedMotorID && !IsPaxiniMotor(currentRedMotorID))
        {
            // Skip normal color for the motor being unfrozen (per-frame lerp handles yellow→original)
            if (_isUnfreezing && currentRedMotorID == _unfreezeTargetMotorID)
            {
                // Color handled by per-frame unfreeze lerp
            }
            else if (isConfirmed)
            {
                SetMotorColorDirect(currentRedMotorID, darkRedColor);
            }
            else
            {
                SetMotorColorDirect(currentRedMotorID, lightRedColor);
            }
        }

        // ★ Single frozen motor colors are applied LAST — highest priority, always overrides everything above.
        // Exception: when group yellow is suppressed (pending direct-OFF), show original instead.
        for (int i = 0; i < 12; i++)
        {
            if (singleMotorFrozen[i])
            {
                if (_pendingSingleMotorUnfreeze[i])
                {
                    continue;
                }

                int mID = i + 1;
                bool suppress = (mID <= 4 && _suppressThumbGroupYellow)
                             || (mID >= 5 && mID <= 8 && _suppressIndexGroupYellow)
                             || (mID >= 9 && _suppressMiddleGroupYellow);
                if (!suppress)
                    SetMotorColorDirect(mID, singleFrozenColor);
            }
            else if (_pendingSingleMotorFreeze[i])
            {
                SetMotorColorDirect(i + 1, singleFrozenColor);
            }
        }

        // Do not force group yellow from Paxini ON during selecting.
        // Group motor colors follow singleMotorFrozen and commit at round-away.
    }

    private bool IsSameFingerGroup(int motorA, int motorB)
    {
        return GetFingerGroupIndex(motorA) == GetFingerGroupIndex(motorB);
    }

    private bool IsPaxiniMotor(int motorID)
    {
        return motorID == ThumbPaxiniMotorID ||
               motorID == IndexPaxiniMotorID ||
               motorID == MiddlePaxiniMotorID;
    }

    private bool ShouldClearConfirmedPaxiniForNewMotor(int newMotorID)
    {
        if (!IsPaxiniMotor(confirmedMotorID)) return false;
        if (newMotorID == 0 || newMotorID == confirmedMotorID) return false;

        // Keep Paxini ON across different fingers (13/14/15 can be yellow at the same time).
        // Only clear when confirming another motor in the same finger group.
        return IsSameFingerGroup(confirmedMotorID, newMotorID);
    }

    private void ReturnToBaseSelectStateAfterPaxini()
    {
        modeSelect = true;
        modeManipulate = false;
        motorSelected = false;
        currentRedMotorID = 0;
        confirmedMotorID = 0;
        lastTouchedMotorID = 0;
        isConfirmed = false;
        touchStartTime = 0f;
        hasEnteredCloseRange = false;
        hasSetManipulateColors = false;

        if (useFingertipFirst && grayMode)
        {
            currentPhase = SelectionPhase.SelectingFingertip;
            confirmedFingertipID = 0;

            if (SelectMotorCollider != null)
            {
                SelectMotorCollider.ResetFingertipConfirmation();
                SelectMotorCollider.ReleaseFrozenLine();
            }

            // Return only 1-12 motors to base selectable colors.
            // Paxini color is managed by TriggerRight*Tip (freeze yellow can stay ON).
            thumbJoint1Renderer.material.color = grayColor;
            thumbJoint2Renderer.material.color = grayColor;
            thumbJoint3Renderer.material.color = grayColor;
            thumbJoint4Renderer.material.color = originalColor;

            indexJoint1Renderer.material.color = grayColor;
            indexJoint2Renderer.material.color = grayColor;
            indexJoint3Renderer.material.color = grayColor;
            indexJoint4Renderer.material.color = originalColor;

            middleJoint1Renderer.material.color = grayColor;
            middleJoint2Renderer.material.color = grayColor;
            middleJoint3Renderer.material.color = grayColor;
            middleJoint4Renderer.material.color = originalColor;
        }
        else
        {
            thumbJoint1Renderer.material.color = originalColor;
            thumbJoint2Renderer.material.color = originalColor;
            thumbJoint3Renderer.material.color = originalColor;
            thumbJoint4Renderer.material.color = originalColor;

            indexJoint1Renderer.material.color = originalColor;
            indexJoint2Renderer.material.color = originalColor;
            indexJoint3Renderer.material.color = originalColor;
            indexJoint4Renderer.material.color = originalColor;

            middleJoint1Renderer.material.color = originalColor;
            middleJoint2Renderer.material.color = originalColor;
            middleJoint3Renderer.material.color = originalColor;
            middleJoint4Renderer.material.color = originalColor;
        }

        // A new select cycle starts here while Paxini freeze may still be ON.
        // Refresh baseline so subsequent motor confirmations compare against
        // this returned base state instead of a stale older snapshot.
        CaptureModeSelectBaseline();

        // Re-apply single frozen motor colors after the reset above
        for (int i = 0; i < 12; i++)
        {
            if (singleMotorFrozen[i])
                SetMotorColorDirect(i + 1, singleFrozenColor);
        }
    }

    /// <summary>
    /// Clears single-motor freeze for all motors in the same finger group as motorID,
    /// EXCEPT motorID itself. Called when a motor is confirmed via normal selection.
    /// </summary>
    private void ClearSingleFrozenMotorInGroup(int motorID)
    {
        int gStart, gEnd;
        if      (motorID >= 1 && motorID <= 4)  { gStart = 1;  gEnd = 4;  }
        else if (motorID >= 5 && motorID <= 8)  { gStart = 5;  gEnd = 8;  }
        else if (motorID >= 9 && motorID <= 12) { gStart = 9;  gEnd = 12; }
        else return; // Paxini or out-of-range, no action
        for (int m = gStart; m <= gEnd; m++)
        {
            if (m == motorID) continue;
            // Respect the baseline: motors frozen in a previous round (baseline) are NOT cleared
            // here — they persist as a "locked" state until the user explicitly unfreezes them.
            // Only newly-frozen motors from the current round get cleared.
            if (!_singleMotorFrozenBaseline[m - 1])
                singleMotorFrozen[m - 1] = false;
        }
    }

    /// <summary>
    /// Reverts a motor's freeze state to its round-start baseline value and updates its color.
    /// Extended to handle Paxini motors (13/14/15) via RevertPaxiniToBaseline.
    /// </summary>
    private void RevertMotorToBaseline(int motorID)
    {
        if (motorID >= 1 && motorID <= 12)
        {
            bool baselineFrozen = _singleMotorFrozenBaseline[motorID - 1];
            _pendingSingleMotorFreeze[motorID - 1] = false;
            _pendingSingleMotorUnfreeze[motorID - 1] = false;
            if (armUIPlaneController != null && armUIPlaneController.clawModuleController != null)
            {
                armUIPlaneController.clawModuleController.ClearSingleMotorFreezeSnapshot(motorID);
            }
            singleMotorFrozen[motorID - 1] = baselineFrozen;
            SetMotorColorDirect(motorID, baselineFrozen ? singleFrozenColor : originalColor);
            // Check whether the group's pending auto states are still valid after this revert.
            int gStart = (motorID <= 4) ? 1 : (motorID <= 8) ? 5 : 9;
            CheckAndUpdatePendingAutoState(gStart);
        }
        else if (motorID == ThumbPaxiniMotorID)  RevertPaxiniToBaseline(1);
        else if (motorID == IndexPaxiniMotorID)  RevertPaxiniToBaseline(5);
        else if (motorID == MiddlePaxiniMotorID) RevertPaxiniToBaseline(9);
    }

    // ── Helper methods for pending Paxini state management ───────────────────────────────────────

    private int GetPaxiniIDForGroup(int groupStart)
    {
        if (groupStart == 1) return ThumbPaxiniMotorID;
        if (groupStart == 5) return IndexPaxiniMotorID;
        return MiddlePaxiniMotorID;
    }

    private bool GetBaselinePaxiniOn(int groupStart)
    {
        if (groupStart == 1) return _thumbBaselinePaxiniOn;
        if (groupStart == 5) return _indexBaselinePaxiniOn;
        return _middleBaselinePaxiniOn;
    }

    /// <summary>
    /// Activates the pending direct-OFF state for a group:
    /// suppresses frozen-yellow for the group, clears other pending states.
    /// </summary>
    private void SetPendingDirectOff(int groupStart)
    {
        if (SelectMotorCollider == null) return;
        if (groupStart == 1)
        {
            _pendingThumbDirectOff = true; _suppressThumbGroupYellow = false;
            _pendingThumbAutoOn = false; _pendingThumbAutoOff = false;
            SelectMotorCollider.thumbPaxiniForceYellow = false; SelectMotorCollider.thumbPaxiniForceOriginal = false;
        }
        else if (groupStart == 5)
        {
            _pendingIndexDirectOff = true; _suppressIndexGroupYellow = false;
            _pendingIndexAutoOn = false; _pendingIndexAutoOff = false;
            SelectMotorCollider.indexPaxiniForceYellow = false; SelectMotorCollider.indexPaxiniForceOriginal = false;
        }
        else
        {
            _pendingMiddleDirectOff = true; _suppressMiddleGroupYellow = false;
            _pendingMiddleAutoOn = false; _pendingMiddleAutoOff = false;
            SelectMotorCollider.middlePaxiniForceYellow = false; SelectMotorCollider.middlePaxiniForceOriginal = false;
        }
    }

    private void ClearPendingDirectOff(int groupStart)
    {
        if (groupStart == 1)      { _pendingThumbDirectOff  = false; _suppressThumbGroupYellow  = false; }
        else if (groupStart == 5) { _pendingIndexDirectOff  = false; _suppressIndexGroupYellow  = false; }
        else                      { _pendingMiddleDirectOff = false; _suppressMiddleGroupYellow = false; }
    }

    /// <summary>
    /// Clears ALL pending states for a group and resets force-visual flags in SelectMotorCollider.
    /// </summary>
    private void ClearAllPendingForGroup(int groupStart)
    {
        ClearPendingDirectOff(groupStart);
        if (SelectMotorCollider == null) return;
        if (groupStart == 1)
        {
            _pendingThumbAutoOn = false; _pendingThumbAutoOff = false;
            SelectMotorCollider.thumbPaxiniForceYellow = false; SelectMotorCollider.thumbPaxiniForceOriginal = false;
        }
        else if (groupStart == 5)
        {
            _pendingIndexAutoOn = false; _pendingIndexAutoOff = false;
            SelectMotorCollider.indexPaxiniForceYellow = false; SelectMotorCollider.indexPaxiniForceOriginal = false;
        }
        else
        {
            _pendingMiddleAutoOn = false; _pendingMiddleAutoOff = false;
            SelectMotorCollider.middlePaxiniForceYellow = false; SelectMotorCollider.middlePaxiniForceOriginal = false;
        }
    }

    /// <summary>
    /// Reverts Paxini freeze state for a group to baseline, clears pending flags and
    /// restores group motors to baseline frozen state.
    /// </summary>
    private void RevertPaxiniToBaseline(int groupStart)
    {
        if (SelectMotorCollider == null) return;
        bool baselinePaxiniOn = GetBaselinePaxiniOn(groupStart);
        // Suppress the ModeSwitching-side group-unfreeze detection that would re-call UnfreezeGroupMotors.
        if (!baselinePaxiniOn)
        {
            if (groupStart == 1)      _suppressThumbPaxiniGroupUnfreeze  = true;
            else if (groupStart == 5) _suppressIndexPaxiniGroupUnfreeze  = true;
            else                      _suppressMiddlePaxiniGroupUnfreeze = true;
        }
        if (baselinePaxiniOn)
            SelectMotorCollider.ForcePaxiniOnForMotor(groupStart);
        else
            SelectMotorCollider.ForcePaxiniOffForMotor(groupStart);

        ClearAllPendingForGroup(groupStart);

        // Revert the 4 group motors to baseline.
        int gEnd = groupStart + 3;
        for (int m = groupStart; m <= gEnd; m++)
            singleMotorFrozen[m - 1] = _singleMotorFrozenBaseline[m - 1];
    }

    /// <summary>
    /// After a group motor is reverted to baseline, verify that pending auto-ON/OFF
    /// conditions are still valid.  If not, cancel the pending state.
    /// </summary>
    private void CheckAndUpdatePendingAutoState(int gStart)
    {
        bool allFrozen = true;
        for (int m = gStart; m < gStart + 4; m++) if (!singleMotorFrozen[m - 1]) { allFrozen = false; break; }

        if (SelectMotorCollider == null) return;
        if (gStart == 1)
        {
            if (_pendingThumbAutoOn  && !allFrozen) { _pendingThumbAutoOn  = false; SelectMotorCollider.thumbPaxiniForceYellow   = false; }
            if (_pendingThumbAutoOff &&  allFrozen) { _pendingThumbAutoOff = false; SelectMotorCollider.thumbPaxiniForceOriginal = false; }
        }
        else if (gStart == 5)
        {
            if (_pendingIndexAutoOn  && !allFrozen) { _pendingIndexAutoOn  = false; SelectMotorCollider.indexPaxiniForceYellow   = false; }
            if (_pendingIndexAutoOff &&  allFrozen) { _pendingIndexAutoOff = false; SelectMotorCollider.indexPaxiniForceOriginal = false; }
        }
        else
        {
            if (_pendingMiddleAutoOn  && !allFrozen) { _pendingMiddleAutoOn  = false; SelectMotorCollider.middlePaxiniForceYellow   = false; }
            if (_pendingMiddleAutoOff &&  allFrozen) { _pendingMiddleAutoOff = false; SelectMotorCollider.middlePaxiniForceOriginal = false; }
        }
    }

    private int GetFingerGroupIndex(int motorID)
    {
        if (motorID >= 1 && motorID <= 4) return 0;
        if (motorID == ThumbPaxiniMotorID) return 0;

        if (motorID >= 5 && motorID <= 8) return 1;
        if (motorID == IndexPaxiniMotorID) return 1;

        if (motorID >= 9 && motorID <= 12) return 2;
        if (motorID == MiddlePaxiniMotorID) return 2;

        return -1;
    }

    private void EnforceSingleRoundDifferenceInvariant()
    {
        if (!modeSelect)
        {
            return;
        }

        List<int> diffs = new List<int>(15);

        for (int motorID = 1; motorID <= 12; motorID++)
        {
            bool baselineFrozen = _singleMotorFrozenBaseline[motorID - 1];
            bool pendingFreezeDiff = _pendingSingleMotorFreeze[motorID - 1];
            bool stateDiff = singleMotorFrozen[motorID - 1] != baselineFrozen;
            bool pendingUnfreezeDiff = _pendingSingleMotorUnfreeze[motorID - 1];
            if (pendingFreezeDiff || stateDiff || pendingUnfreezeDiff)
            {
                diffs.Add(motorID);
            }
        }

        if (SelectMotorCollider != null)
        {
            if (SelectMotorCollider.thumbFreezeEnabled != _thumbBaselinePaxiniOn)
            {
                diffs.Add(ThumbPaxiniMotorID);
            }

            if (SelectMotorCollider.indexFreezeEnabled != _indexBaselinePaxiniOn)
            {
                diffs.Add(IndexPaxiniMotorID);
            }

            if (SelectMotorCollider.middleFreezeEnabled != _middleBaselinePaxiniOn)
            {
                diffs.Add(MiddlePaxiniMotorID);
            }
        }

        if (diffs.Count <= 1)
        {
            _roundChangedMotorID = diffs.Count == 1 ? diffs[0] : 0;
            return;
        }

        int keeper = _roundChangedMotorID;
        bool keeperFound = false;
        for (int i = 0; i < diffs.Count; i++)
        {
            if (diffs[i] == keeper)
            {
                keeperFound = true;
                break;
            }
        }

        if (!keeperFound)
        {
            int preferredMotorID = currentRedMotorID != 0 ? currentRedMotorID : lastTouchedMotorID;
            for (int i = 0; i < diffs.Count; i++)
            {
                if (diffs[i] == preferredMotorID)
                {
                    keeper = preferredMotorID;
                    keeperFound = true;
                    break;
                }
            }
        }

        if (!keeperFound)
        {
            keeper = diffs[diffs.Count - 1];
        }

        for (int i = 0; i < diffs.Count; i++)
        {
            int diffMotorID = diffs[i];
            if (diffMotorID == keeper)
            {
                continue;
            }

            RevertMotorToBaseline(diffMotorID);
        }

        _roundChangedMotorID = keeper;
        UpdateMotorColors();
    }

    /// <summary>
    /// Clears the confirmed motor selection for a given finger (identified by motor range)
    /// and refreshes motor colors. Called by SelectMotorCollider when entering the freeze zone,
    /// so the previously dark-red motor reverts to its original color (Problem 2 fix).
    /// </summary>
    public void ClearConfirmedMotorForFinger(int minMotor, int maxMotor)
    {
        if (!modeSelect) return;
        if (confirmedMotorID >= minMotor && confirmedMotorID <= maxMotor)
        {
            confirmedMotorID = 0;
            motorSelected = false;
            currentRedMotorID = 0;
            isConfirmed = false;
            UpdateMotorColors();
        }
    }

    /// <summary>
    /// Returns true if all 4 motors in the given group are currently frozen.
    /// Used by SelectMotorCollider to gate manual Paxini toggle-ON.
    /// </summary>
    public bool IsGroupAllFrozen(int groupStart)
    {
        int gEnd = groupStart + 3;
        if (gEnd > 12) return false;
        for (int m = groupStart; m <= gEnd; m++)
            if (!singleMotorFrozen[m - 1]) return false;
        return true;
    }

    private void MarkGroupSyncDebug(bool isFreezeAction, int groupStart, bool validGroup)
    {
        debugEnteredFreezeGroupMotors = isFreezeAction;
        debugEnteredUnfreezeGroupMotors = !isFreezeAction;
        debugLastGroupSyncAction = isFreezeAction ? "FreezeGroupMotors" : "UnfreezeGroupMotors";
        if (!validGroup)
            debugLastGroupSyncAction += "(invalid group)";
        debugLastGroupStart = groupStart;
        debugLastGroupPaxiniID = (groupStart == 1) ? ThumbPaxiniMotorID
                             : (groupStart == 5) ? IndexPaxiniMotorID
                             : (groupStart == 9) ? MiddlePaxiniMotorID
                             : 0;
        debugGroupSyncFrame = Time.frameCount;

        if (isFreezeAction) debugFreezeGroupCallCount++;
        else debugUnfreezeGroupCallCount++;
    }

    /// <summary>
    /// Handles Paxini ON transition for a group (direct user selection via freeze zone).
    /// Enforces one-change-per-round (now including motors 13/14/15):
    ///   1. If another motor already changed this round, revert it first.
    ///   2. Revert the 4 group motors to the round baseline.
    ///   3. Track Paxini as the round change.
    ///   4. Visual yellow is handled per-frame by thumbFreezeEnabled=true.
    /// Full singleMotorFrozen commit still happens at hand-away (existing shouldCommitPaxiniFreeze path).
    /// </summary>
    public void FreezeGroupMotors(int groupStart)
    {
        int gEnd = groupStart + 3;
        bool validGroup = gEnd <= 12;
        MarkGroupSyncDebug(true, groupStart, validGroup);
        if (!validGroup) return;

        int paxiniID = GetPaxiniIDForGroup(groupStart);

        for (int m = groupStart; m <= gEnd; m++)
            singleMotorFrozen[m - 1] = _singleMotorFrozenBaseline[m - 1];
        {
            RevertMotorToBaseline(_roundChangedMotorID);
            _roundChangedMotorID = 0;
        }

        // Keep group motors unchanged during selecting.
        // Group state/color commit is deferred to round-away (> threshold).

        // Track Paxini as the round change (only if its state actually differs from baseline).
        bool baselinePaxiniOn = GetBaselinePaxiniOn(groupStart);
        _roundChangedMotorID = !baselinePaxiniOn ? paxiniID : 0;

        // Clear any pending direct-OFF for this group (can't be both ON and OFF simultaneously).
        ClearPendingDirectOff(groupStart);

        // Reset edge flag so the next hand-away can re-trigger baseline capture.
        _frozenBaselineCaptured = false;

        UpdateMotorColors();
    }

    /// <summary>
    /// Restores all 4 motors in the given group to the round-start baseline freeze snapshot (direct Paxini OFF).
    /// Enforces one-change-per-round extended to motors 13/14/15:
    ///   1. Revert any different motor's change from this round.
    ///   2. Revert group motors to baseline.
    ///   3. Track Paxini as round change.
    ///   4. Show original colour for all group motors (even frozen ones) — pending until hand-away.
    ///   5. At hand-away: baseline-frozen motors become off (committed by pending direct-off logic).
    /// </summary>
    public void UnfreezeGroupMotors(int groupStart)
    {
        Debug.Log($"UnfreezeGroupMotors({groupStart}) frame={Time.frameCount}");
        int gEnd = groupStart + 3;
        bool validGroup = gEnd <= 12;
        MarkGroupSyncDebug(false, groupStart, validGroup);
        if (!validGroup) return;

        int paxiniID = GetPaxiniIDForGroup(groupStart);
        if (_roundChangedMotorID != 0 && _roundChangedMotorID != paxiniID)
        {
            RevertMotorToBaseline(_roundChangedMotorID);
            _roundChangedMotorID = 0;
        }

        // Keep group motors unchanged during selecting.
        // Group state/color commit is deferred to round-away (> threshold).

        // Track Paxini as round change (if it was ON at baseline, turning OFF = change).
        bool baselinePaxiniOn = GetBaselinePaxiniOn(groupStart);
        _roundChangedMotorID = baselinePaxiniOn ? paxiniID : 0;

        // Set pending direct-OFF. Only Paxini color changes immediately;
        // group motors keep current color/state until hand-away commit.
        SetPendingDirectOff(groupStart);

        // Reset edge flag so hand-away can re-trigger.
        _frozenBaselineCaptured = false;

        UpdateMotorColors();
    }
    /// <summary>
    /// Called after a motor freezes: if all 4 in its group are now frozen, show Paxini yellow
    /// visually but do NOT commit freeze-on state yet.  State commits when hand moves away > 0.16 m.
    /// </summary>
    private void CheckAndAutoEnablePaxini(int frozenMotorID)
    {
        if (SelectMotorCollider == null) return;
        int gStart, gEnd;
        if      (frozenMotorID >= 1 && frozenMotorID <= 4)  { gStart = 1; gEnd = 4; }
        else if (frozenMotorID >= 5 && frozenMotorID <= 8)  { gStart = 5; gEnd = 8; }
        else if (frozenMotorID >= 9 && frozenMotorID <= 12) { gStart = 9; gEnd = 12; }
        else return;

        for (int m = gStart; m <= gEnd; m++)
        {
            int idx = m - 1;
            bool effectiveFrozen = (singleMotorFrozen[idx] || _pendingSingleMotorFreeze[idx])
                                && !_pendingSingleMotorUnfreeze[idx];
            if (!effectiveFrozen) return; // not all frozen yet
        }

        // All 4 frozen — mark pending auto-ON only.
        // Paxini color/state are committed when hand moves away (> threshold).
        if (gStart == 1)      { _pendingThumbAutoOn  = true; SelectMotorCollider.thumbPaxiniForceYellow   = false; SelectMotorCollider.thumbPaxiniForceOriginal   = false; }
        else if (gStart == 5) { _pendingIndexAutoOn  = true; SelectMotorCollider.indexPaxiniForceYellow   = false; SelectMotorCollider.indexPaxiniForceOriginal   = false; }
        else                  { _pendingMiddleAutoOn = true; SelectMotorCollider.middlePaxiniForceYellow  = false; SelectMotorCollider.middlePaxiniForceOriginal  = false; }
    }

    /// <summary>
    /// Called after a motor unfreezes: if Paxini was ON for its group, show Paxini original colour
    /// visually but do NOT commit freeze-off state yet.  State commits when hand moves away > 0.16 m.
    /// Also cancels a pending auto-ON if the all-4-frozen condition no longer holds.
    /// </summary>
    private void CheckAndAutoDisablePaxini(int unfrozenMotorID)
    {
        if (SelectMotorCollider == null) return;
        int gStart;
        if      (unfrozenMotorID >= 1 && unfrozenMotorID <= 4)  gStart = 1;
        else if (unfrozenMotorID >= 5 && unfrozenMotorID <= 8)  gStart = 5;
        else if (unfrozenMotorID >= 9 && unfrozenMotorID <= 12) gStart = 9;
        else return;

        bool paxiniOn = (gStart == 1 && SelectMotorCollider.thumbFreezeEnabled)
                     || (gStart == 5 && SelectMotorCollider.indexFreezeEnabled)
                     || (gStart == 9 && SelectMotorCollider.middleFreezeEnabled);

        if (paxiniOn)
        {
            // Paxini was committed ON — mark pending auto-OFF only.
            // Paxini color/state are committed when hand moves away (> threshold).
            if (gStart == 1)      { _pendingThumbAutoOff  = true; SelectMotorCollider.thumbPaxiniForceOriginal   = false; SelectMotorCollider.thumbPaxiniForceYellow   = false; }
            else if (gStart == 5) { _pendingIndexAutoOff  = true; SelectMotorCollider.indexPaxiniForceOriginal   = false; SelectMotorCollider.indexPaxiniForceYellow   = false; }
            else                  { _pendingMiddleAutoOff = true; SelectMotorCollider.middlePaxiniForceOriginal  = false; SelectMotorCollider.middlePaxiniForceYellow  = false; }
        }
        else
        {
            // Paxini was not committed ON — cancel pending auto-ON if the all-4-frozen condition is gone.
            if      (gStart == 1 && _pendingThumbAutoOn)  { _pendingThumbAutoOn  = false; SelectMotorCollider.thumbPaxiniForceYellow  = false; }
            else if (gStart == 5 && _pendingIndexAutoOn)  { _pendingIndexAutoOn  = false; SelectMotorCollider.indexPaxiniForceYellow  = false; }
            else if (gStart == 9 && _pendingMiddleAutoOn) { _pendingMiddleAutoOn = false; SelectMotorCollider.middlePaxiniForceYellow = false; }
        }
    }

    /// <summary>
    /// Set a single motor's color directly (do not reset other colors)
    /// </summary>
    private void SetMotorColorDirect(int motorID, Color color)
    {
        switch (motorID)
        {
            case 1: thumbJoint1Renderer.material.color = color; break;
            case 2: thumbJoint2Renderer.material.color = color; break;
            case 3: thumbJoint3Renderer.material.color = color; break;
            case 4: thumbJoint4Renderer.material.color = color; break;
            case 5: indexJoint1Renderer.material.color = color; break;
            case 6: indexJoint2Renderer.material.color = color; break;
            case 7: indexJoint3Renderer.material.color = color; break;
            case 8: indexJoint4Renderer.material.color = color; break;
            case 9: middleJoint1Renderer.material.color = color; break;
            case 10: middleJoint2Renderer.material.color = color; break;
            case 11: middleJoint3Renderer.material.color = color; break;
            case 12: middleJoint4Renderer.material.color = color; break;
            case ThumbPaxiniMotorID: if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = color; break;
            case IndexPaxiniMotorID: if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = color; break;
            case MiddlePaxiniMotorID: if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = color; break;
        }
    }
    
    private void SetMotorColor(int motorID, Color color)
    {
        // Reset all colors first
        ResetAllColors();
                
        // Set the specific motor's color
        switch (motorID)
        {
            case 1:
                thumbJoint1Renderer.material.color = color;
                break;
            case 2:
                thumbJoint2Renderer.material.color = color;
                break;
            case 3:
                thumbJoint3Renderer.material.color = color;
                break;
            case 4:
                thumbJoint4Renderer.material.color = color;
                break;
            case 5:
                indexJoint1Renderer.material.color = color;
                break;
            case 6:
                indexJoint2Renderer.material.color = color;
                break;
            case 7:
                indexJoint3Renderer.material.color = color;
                break;
            case 8:
                indexJoint4Renderer.material.color = color;
                break;
            case 9:
                middleJoint1Renderer.material.color = color;
                break;
            case 10:
                middleJoint2Renderer.material.color = color;
                break;
            case 11:
                middleJoint3Renderer.material.color = color;
                break;
            case 12:
                middleJoint4Renderer.material.color = color;
                break;
            case ThumbPaxiniMotorID:
                if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = color;
                break;
            case IndexPaxiniMotorID:
                if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = color;
                break;
            case MiddlePaxiniMotorID:
                if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = color;
                break;
            default:
                break;
        }
    }
    
    private void ResetAllColors()
    {
        thumbJoint1Renderer.material.color = originalColor;
        thumbJoint2Renderer.material.color = originalColor;
        thumbJoint3Renderer.material.color = originalColor;
        thumbJoint4Renderer.material.color = originalColor;
        
        indexJoint1Renderer.material.color = originalColor;
        indexJoint2Renderer.material.color = originalColor;
        indexJoint3Renderer.material.color = originalColor;
        indexJoint4Renderer.material.color = originalColor;
        
        middleJoint1Renderer.material.color = originalColor;
        middleJoint2Renderer.material.color = originalColor;
        middleJoint3Renderer.material.color = originalColor;
        middleJoint4Renderer.material.color = originalColor;

        if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
        if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
        if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
    }

    public void ResetExternalMotorColors()
    {
        ResetAllColors();
    }

    public void ApplyExternalMotorColor(int motorID, Color color)
    {
        SetMotorColorDirect(motorID, color);
    }

    public Color GetDefaultMotorColor(int motorID)
    {
        switch (motorID)
        {
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
                return originalColor;
            case ThumbPaxiniMotorID:
                return thumbPaxiniOriginalColor;
            case IndexPaxiniMotorID:
                return indexPaxiniOriginalColor;
            case MiddlePaxiniMotorID:
                return middlePaxiniOriginalColor;
            default:
                return originalColor;
        }
    }

    public Color GetMotorDisplayColor(int motorID, Color fallbackOriginalColor)
    {
        if (motorID == ThumbPaxiniMotorID || motorID == IndexPaxiniMotorID || motorID == MiddlePaxiniMotorID)
        {
            if (SelectMotorCollider != null)
            {
                if (motorID == ThumbPaxiniMotorID)
                {
                    if (SelectMotorCollider.thumbPaxiniForceOriginal)
                        return fallbackOriginalColor;
                    if (SelectMotorCollider.thumbPaxiniForceYellow || SelectMotorCollider.thumbFreezeEnabled)
                        return singleFrozenColor;
                }
                else if (motorID == IndexPaxiniMotorID)
                {
                    if (SelectMotorCollider.indexPaxiniForceOriginal)
                        return fallbackOriginalColor;
                    if (SelectMotorCollider.indexPaxiniForceYellow || SelectMotorCollider.indexFreezeEnabled)
                        return singleFrozenColor;
                }
                else
                {
                    if (SelectMotorCollider.middlePaxiniForceOriginal)
                        return fallbackOriginalColor;
                    if (SelectMotorCollider.middlePaxiniForceYellow || SelectMotorCollider.middleFreezeEnabled)
                        return singleFrozenColor;
                }
            }

            return fallbackOriginalColor;
        }

        if (_isUnfreezing && motorID == _unfreezeTargetMotorID && currentRedMotorID == motorID)
        {
            return unfreezeHintColor;
        }

        if (confirmedMotorID == motorID)
            return darkRedColor;

        if (currentRedMotorID == motorID && currentRedMotorID != confirmedMotorID)
            return isConfirmed ? darkRedColor : lightRedColor;

        Color baseColor = fallbackOriginalColor;

        if (motorID >= 1 && motorID <= 12)
        {
            if (useFingertipFirst && grayMode)
            {
                if (currentPhase == SelectionPhase.SelectingFingertip)
                {
                    baseColor = grayColor;
                }
                else
                {
                    switch (confirmedFingertipID)
                    {
                        case 4:
                            baseColor = (motorID >= 1 && motorID <= 4) ? fallbackOriginalColor : grayColor;
                            break;
                        case 8:
                            baseColor = (motorID >= 5 && motorID <= 8) ? fallbackOriginalColor : grayColor;
                            break;
                        case 12:
                            baseColor = (motorID >= 9 && motorID <= 12) ? fallbackOriginalColor : grayColor;
                            break;
                        default:
                            baseColor = grayColor;
                            break;
                    }
                }
            }

            bool suppress = (motorID <= 4 && _suppressThumbGroupYellow)
                         || (motorID >= 5 && motorID <= 8 && _suppressIndexGroupYellow)
                         || (motorID >= 9 && _suppressMiddleGroupYellow);
            if (singleMotorFrozen[motorID - 1] && !suppress)
                return singleFrozenColor;

            return baseColor;
        }

        return baseColor;
    }

    public Color GetPaxiniDisplayColor(int motorID, Color fallbackOriginalColor)
    {
        return GetMotorDisplayColor(motorID, fallbackOriginalColor);
    }

    private void ApplyModeSelectBaseColors()
    {
        if (useFingertipFirst && grayMode)
            UpdateGrayColors();
        else
            ResetAllColors();
    }

    /// <summary>
    /// Hard reset all selection/freeze/round trackers so the system returns to a clean
    /// baseline: motors 1-12 OFF, Paxini 13/14/15 freeze OFF, and original colors.
    /// </summary>
    public void HardResetToAllOffState()
    {
        modeSelect = true;
        motorSelected = false;
        modeManipulate = false;
        lastTouchedMotorID = 0;
        currentRedMotorID = 0;
        confirmedMotorID = 0;
        touchStartTime = 0f;
        isConfirmed = false;
        hasEnteredCloseRange = false;
        hasSetManipulateColors = false;
        _wasModeSelectLastFrame = true;

        _isUnfreezing = false;
        _unfreezeTargetMotorID = 0;
        _justFrozeWhileHolding = false;
        _justFrozeMotorID = 0;
        _singleFreezeInProgress = false;
        _roundChangedMotorID = 0;

        _frozenBaselineCaptured = false;
        _noFreezeRoundBaselineCaptured = false;

        _prevThumbPaxiniEnabled = false;
        _prevIndexPaxiniEnabled = false;
        _prevMiddlePaxiniEnabled = false;
        _suppressThumbPaxiniGroupUnfreeze = false;
        _suppressIndexPaxiniGroupUnfreeze = false;
        _suppressMiddlePaxiniGroupUnfreeze = false;

        // Clear all pending Paxini states
        _pendingThumbAutoOn  = false; _pendingIndexAutoOn  = false; _pendingMiddleAutoOn  = false;
        _pendingThumbAutoOff = false; _pendingIndexAutoOff = false; _pendingMiddleAutoOff = false;
        _pendingThumbDirectOff = false; _pendingIndexDirectOff = false; _pendingMiddleDirectOff = false;
        _suppressThumbGroupYellow  = false; _suppressIndexGroupYellow  = false; _suppressMiddleGroupYellow  = false;

        grayMode = false;

        if (singleMotorFrozen != null)
            System.Array.Clear(singleMotorFrozen, 0, singleMotorFrozen.Length);
        if (_singleMotorFrozenBaseline != null)
            System.Array.Clear(_singleMotorFrozenBaseline, 0, _singleMotorFrozenBaseline.Length);
        if (_pendingSingleMotorFreeze != null)
            System.Array.Clear(_pendingSingleMotorFreeze, 0, _pendingSingleMotorFreeze.Length);
        if (_pendingSingleMotorUnfreeze != null)
            System.Array.Clear(_pendingSingleMotorUnfreeze, 0, _pendingSingleMotorUnfreeze.Length);

        if (SelectMotorCollider != null)
        {
            SelectMotorCollider.ForcePaxiniOffForMotor(1);
            SelectMotorCollider.ForcePaxiniOffForMotor(5);
            SelectMotorCollider.ForcePaxiniOffForMotor(9);
            SelectMotorCollider.ResetFingertipConfirmation();
            SelectMotorCollider.ReleaseFrozenLine();
            SelectMotorCollider.RestoreDebugVisuals();
            // Clear force-visual override flags
            SelectMotorCollider.thumbPaxiniForceYellow    = false; SelectMotorCollider.thumbPaxiniForceOriginal  = false;
            SelectMotorCollider.indexPaxiniForceYellow    = false; SelectMotorCollider.indexPaxiniForceOriginal  = false;
            SelectMotorCollider.middlePaxiniForceYellow   = false; SelectMotorCollider.middlePaxiniForceOriginal = false;
        }

        if (useFingertipFirst)
        {
            currentPhase = SelectionPhase.SelectingFingertip;
            confirmedFingertipID = 0;
        }

        ResetAllColors();
        UpdateMotorColors();
        CaptureModeSelectBaseline();
    }
    
    /// <summary>
    /// Handle motor confirmation logic for fingertip-first mode
    /// </summary>
    private void HandleFingertipFirstConfirmation(int motorID)
    {
        switch (currentPhase)
        {
            case SelectionPhase.SelectingFingertip:
                // Phase 1: only accept fingertip motors (4, 8, 12)
                if (motorID == 4 || motorID == 8 || motorID == 12)
                {
                    confirmedFingertipID = motorID;
                    confirmedMotorID = motorID; // Record as the confirmed motor
                    
                    // Notify SelectMotorCollider that a fingertip has been confirmed
                    SelectMotorCollider.OnFingertipConfirmed(motorID);
                    
                    // Capture frozen line for this finger
                    SelectMotorCollider.CaptureFrozenLine(motorID);
                    
                    // Go directly to MotorConfirmed so that the user can
                    // separate hands immediately to enter manipulation mode.
                    // If they later touch a different motor on the same finger,
                    // the MotorConfirmed case below will handle the switch.
                    currentPhase = SelectionPhase.MotorConfirmed;
                    
                    // Update gray display (other motors of this finger are now selectable)
                    UpdateGrayColors();
                    
                    // Debug.Log($"[ModeSwitching] Fingertip motor {motorID} confirmed. Ready for manipulation (or select another motor).");
                }
                break;
                
            case SelectionPhase.SelectingMotor:
            case SelectionPhase.MotorConfirmed:
                // Check whether a new fingertip was selected (switch finger/change mind)
                if (motorID == 4 || motorID == 8 || motorID == 12)
                {
                    if (motorID != confirmedFingertipID)
                    {
                        // Switch to the new finger
                        confirmedFingertipID = motorID;
                        confirmedMotorID = motorID;
                        
                        // Notify SelectMotorCollider that the fingertip switched
                        SelectMotorCollider.OnFingertipConfirmed(motorID);
                        
                        // Capture frozen line for the new finger
                        SelectMotorCollider.CaptureFrozenLine(motorID);

                        // New fingertip is already confirmed, allow immediate manipulation.
                        currentPhase = SelectionPhase.MotorConfirmed;
                        
                        // Update gray display
                        UpdateGrayColors();
                        
                        // Debug.Log($"[ModeSwitching] Switched to new fingertip motor {motorID}. Ready for manipulation.");
                    }
                    else
                    {
                        // Still the same fingertip
                        confirmedMotorID = motorID;
                        currentPhase = SelectionPhase.MotorConfirmed;
                        // Debug.Log($"[ModeSwitching] Maintaining fingertip motor {motorID}");
                    }
                }
                else
                {
                    // Selecting another motor of the confirmed finger
                    confirmedMotorID = motorID;
                    EnforceGroupBaselineForConfirmedMotor(motorID);
                    currentPhase = SelectionPhase.MotorConfirmed;
                    // Debug.Log($"[ModeSwitching] Motor {motorID} confirmed. Can enter manipulate mode.");
                }
                break;
        }
    }
    
    /// <summary>
    /// Update gray display (unselectable motors)
    /// </summary>
    private void UpdateGrayColors()
    {
        if (!useFingertipFirst || !grayMode)
        {
            ResetAllColors();
            return;
        }
        
        if (currentPhase == SelectionPhase.SelectingFingertip)
        {
            // Phase 1: only fingertips (4, 8, 12) are selectable, others gray out
            thumbJoint1Renderer.material.color = grayColor;
            thumbJoint2Renderer.material.color = grayColor;
            thumbJoint3Renderer.material.color = grayColor;
            thumbJoint4Renderer.material.color = originalColor; // Motor 4 - fingertip selectable
            
            indexJoint1Renderer.material.color = grayColor;
            indexJoint2Renderer.material.color = grayColor;
            indexJoint3Renderer.material.color = grayColor;
            indexJoint4Renderer.material.color = originalColor; // Motor 8 - fingertip selectable
            
            middleJoint1Renderer.material.color = grayColor;
            middleJoint2Renderer.material.color = grayColor;
            middleJoint3Renderer.material.color = grayColor;
            middleJoint4Renderer.material.color = originalColor; // Motor 12 - fingertip selectable

            if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
            if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
            if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
        }
        else if (currentPhase == SelectionPhase.SelectingMotor || currentPhase == SelectionPhase.MotorConfirmed)
        {
            // Phase 2/3: motors of the confirmed finger are selectable, other fingers' fingertips remain selectable
            switch (confirmedFingertipID)
            {
                case 4: // Thumb
                    thumbJoint1Renderer.material.color = originalColor;
                    thumbJoint2Renderer.material.color = originalColor;
                    thumbJoint3Renderer.material.color = originalColor;
                    thumbJoint4Renderer.material.color = originalColor;
                    
                    indexJoint1Renderer.material.color = grayColor;
                    indexJoint2Renderer.material.color = grayColor;
                    indexJoint3Renderer.material.color = grayColor;
                    indexJoint4Renderer.material.color = originalColor; // Index fingertip remains selectable
                    
                    middleJoint1Renderer.material.color = grayColor;
                    middleJoint2Renderer.material.color = grayColor;
                    middleJoint3Renderer.material.color = grayColor;
                    middleJoint4Renderer.material.color = originalColor; // Middle fingertip remains selectable
                    if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
                    if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
                    if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
                    break;
                    
                case 8: // Index finger
                    thumbJoint1Renderer.material.color = grayColor;
                    thumbJoint2Renderer.material.color = grayColor;
                    thumbJoint3Renderer.material.color = grayColor;
                    thumbJoint4Renderer.material.color = originalColor; // Thumb fingertip remains selectable
                    
                    indexJoint1Renderer.material.color = originalColor;
                    indexJoint2Renderer.material.color = originalColor;
                    indexJoint3Renderer.material.color = originalColor;
                    indexJoint4Renderer.material.color = originalColor;
                    
                    middleJoint1Renderer.material.color = grayColor;
                    middleJoint2Renderer.material.color = grayColor;
                    middleJoint3Renderer.material.color = grayColor;
                    middleJoint4Renderer.material.color = originalColor; // Middle fingertip remains selectable
                    if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
                    if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
                    if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
                    break;
                    
                case 12: // Middle finger
                    thumbJoint1Renderer.material.color = grayColor;
                    thumbJoint2Renderer.material.color = grayColor;
                    thumbJoint3Renderer.material.color = grayColor;
                    thumbJoint4Renderer.material.color = originalColor; // Thumb fingertip remains selectable
                    
                    indexJoint1Renderer.material.color = grayColor;
                    indexJoint2Renderer.material.color = grayColor;
                    indexJoint3Renderer.material.color = grayColor;
                    indexJoint4Renderer.material.color = originalColor; // Index fingertip remains selectable
                    
                    middleJoint1Renderer.material.color = originalColor;
                    middleJoint2Renderer.material.color = originalColor;
                    middleJoint3Renderer.material.color = originalColor;
                    middleJoint4Renderer.material.color = originalColor;
                    if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
                    if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
                    if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
                    break;
            }
        }
    }
}
