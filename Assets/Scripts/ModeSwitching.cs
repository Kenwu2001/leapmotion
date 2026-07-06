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
    
    public int lastTouchedMotorID = 0;
    public int currentRedMotorID = 0; // Currently touched motor (displayed in light/dark red)
    
    [Header("Confirmed Selection")]
    [Tooltip("ID of the confirmed motor (dark red) - this is retained after leaving modeSelect")]
    public int confirmedMotorID = 0; // Confirmed motor (dark red)
    
    private float touchStartTime = 0f; // Time when touch started
    private bool isConfirmed = false; // Whether the currently touched motor is confirmed (dark red)
    
    private bool hasEnteredCloseRange = false; // Track if we've entered < 0.16f during manipulation
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
    [Tooltip("Time (seconds) of continuous holding after confirmation to freeze a single motor (red \u2192 yellow)")]
    public float singleMotorFreezeTime = 1.0f;
    [Tooltip("Color for a single frozen motor")]
    public Color singleFrozenColor = Color.yellow;
    [Tooltip("Time (seconds) of continuous hovering on a frozen (yellow) motor before the freeze is cancelled. Increase to make cancellation require a longer hold.")]
    public float unfreezeConfirmationTime = 1.5f;
    [Tooltip("Hint color the frozen motor lerps toward while the user is holding to cancel. Signals that the freeze is about to be removed. Original color is restored on completion.")]
    public Color unfreezeHintColor = new Color(1f, 0.4f, 0.7f, 1f);
    [Tooltip("[Debug] Per-motor freeze state (index 0-11 = motor ID 1-12)")]
    public bool[] singleMotorFrozen = new bool[12];

    // Single motor freeze \u2014 private state
    private bool _isUnfreezing = false;          // currently touching a frozen motor to unfreeze
    private int  _unfreezeTargetMotorID = 0;     // which motor is being unfrozen
    private bool _justFrozeWhileHolding = false; // true when motor just froze while finger still held
    private int  _justFrozeMotorID = 0;          // which motor just froze while held
    private bool _singleFreezeInProgress = false;// true during red→yellow lerp (blocks manipulate)

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

    public enum SelectionPhase
    {
        SelectingFingertip,   // Phase 1: Selecting fingertip (4, 8, 12)
        SelectingMotor,       // Phase 2: Selecting the motor of the finger
        MotorConfirmed        // Final motor confirmed
    }
    
    void Start()
    {
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
        if (jointAngle != null)
        {
            currentHandSeparationDistance = jointAngle.GetLIndexToIndex2Distance();
        }

        // Capture baseline once when entering modeSelect.
        if (modeSelect && !_wasModeSelectLastFrame)
        {
            CaptureModeSelectBaseline();
        }
        _wasModeSelectLastFrame = modeSelect;

        if (modeSelect && SelectMotorCollider != null)
        {
            int currentMotorID = SelectMotorCollider.currentTouchedMotorID;

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
                        singleMotorFrozen[fm - 1] = false;
                        // Track round change only if net state differs from baseline
                        if (fm >= 1 && fm <= 12)
                            _roundChangedMotorID = _singleMotorFrozenBaseline[fm - 1] ? fm : 0;
                        _isUnfreezing = false;
                        _unfreezeTargetMotorID = 0;
                        currentRedMotorID = 0;
                        motorSelected = false;
                        confirmedMotorID = 0;
                        // Auto-disable Paxini BEFORE UpdateMotorColors so the visual sync
                        // sees thumbFreezeEnabled=false and does not repaint the unfrozen motor yellow.
                        CheckAndAutoDisablePaxini(fm);
                        UpdateMotorColors();
                    }
                    // else: still lerping yellow→hint color (per-frame section handles color)
                }
                else
                {
                    // Continuously touching the same motor - check if confirmation time exceeded
                    // Use faster confirmation time when crossing finger groups (e.g., from index to thumb)
                    bool isCrossFingerSwitch = confirmedMotorID != 0 && !IsSameFingerGroup(confirmedMotorID, currentMotorID);
                    float requiredConfirmTime = isCrossFingerSwitch ? fingertipConfirmationTime : confirmationTime;
                    if (!isConfirmed && (Time.time - touchStartTime) >= requiredConfirmTime)
                    {
                        // Selecting (confirming) a new motor is ALWAYS allowed.
                        // If a DIFFERENT motor already has a committed change this round, revert it first.
                        if (currentMotorID >= 1 && currentMotorID <= 12
                            && _roundChangedMotorID != 0 && _roundChangedMotorID != currentMotorID)
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

            // ─── Per-frame: single motor freeze buildup (confirmed + still holding → red→yellow) ───
            if (isConfirmed && !_isUnfreezing
                && confirmedMotorID >= 1 && confirmedMotorID <= 12
                && currentMotorID == confirmedMotorID)
            {
                float elapsed = Time.time - touchStartTime - confirmationTime;
                if (elapsed > 0f)
                {
                    float t = Mathf.Clamp01(elapsed / singleMotorFreezeTime);
                    _singleFreezeInProgress = (t < 1f);
                    SetMotorColorDirect(confirmedMotorID, Color.Lerp(darkRedColor, singleFrozenColor, t));

                    if (t >= 1f && !singleMotorFrozen[confirmedMotorID - 1])
                    {
                        int frozenID = confirmedMotorID;
                        // If a DIFFERENT motor already has a committed change, revert it first.
                        if (frozenID >= 1 && frozenID <= 12
                            && _roundChangedMotorID != 0 && _roundChangedMotorID != frozenID)
                        {
                            RevertMotorToBaseline(_roundChangedMotorID);
                            _roundChangedMotorID = 0;
                        }
                        // Motor is now frozen!
                        singleMotorFrozen[frozenID - 1] = true;
                        // Track round change only if net state differs from baseline
                        if (frozenID >= 1 && frozenID <= 12)
                            _roundChangedMotorID = !_singleMotorFrozenBaseline[frozenID - 1] ? frozenID : 0;
                        confirmedMotorID = 0;
                        motorSelected = false;
                        currentRedMotorID = 0;
                        isConfirmed = false;
                        _singleFreezeInProgress = false;
                        _justFrozeWhileHolding = true;
                        _justFrozeMotorID = frozenID;
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

            // ─── Per-frame: unfreeze lerp (frozen motor being touched → yellow→unfreezeHintColor) ───
            if (_isUnfreezing && _unfreezeTargetMotorID != 0 && currentMotorID == _unfreezeTargetMotorID)
            {
                float t = Mathf.Clamp01((Time.time - touchStartTime) / unfreezeConfirmationTime);
                SetMotorColorDirect(_unfreezeTargetMotorID, Color.Lerp(singleFrozenColor, unfreezeHintColor, t));
            }
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

            if (_prevThumbPaxiniEnabled && !thumbOn)
            {
                if (!_suppressThumbPaxiniGroupUnfreeze)
                {
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
        if (modeSelect && motorSelected && isConfirmedPaxiniMotor)
        {
            float distance = currentHandSeparationDistance;
            if (distance > 0.16f)
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
            bool hasAnyFrozen = false;
            for (int i = 0; i < 12; i++) { if (singleMotorFrozen[i]) { hasAnyFrozen = true; break; } }

            bool hasPendingPaxini = _pendingThumbAutoOn || _pendingIndexAutoOn || _pendingMiddleAutoOn
                                 || _pendingThumbAutoOff || _pendingIndexAutoOff || _pendingMiddleAutoOff
                                 || _pendingThumbDirectOff || _pendingIndexDirectOff || _pendingMiddleDirectOff;

            if (hasAnyFrozen || hasPendingPaxini)
            {
                if (currentHandSeparationDistance > 0.16f)
                {
                    if (!_frozenBaselineCaptured)
                    {
                        _frozenBaselineCaptured = true;

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
                        if (!motorSelected)
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
                if (_roundChangedMotorID != 0 && currentHandSeparationDistance > 0.16f)
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
        // Block manipulate while single-motor freeze is building up (red→yellow lerp in progress)
        if (_singleFreezeInProgress)
        {
            canEnterManipulate = false;
        }

        if (canEnterManipulate)
        {
            float distance = currentHandSeparationDistance;

            if (distance > 0.16f)
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

            // Track if we've entered close range (< 0.16f) for manipulation
            if (distance < 0.16f)
            {
                hasEnteredCloseRange = true;
            }

            // Only exit if we've performed manipulation (entered close range) 
            // and then moved back out (> 0.16f)
            if (hasEnteredCloseRange && distance > 0.16f)
            {
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
                bool shouldCommitPaxiniFreeze = currentHandSeparationDistance > 0.16f;
                if (shouldCommitPaxiniFreeze && SelectMotorCollider.thumbFreezeEnabled && !_pendingThumbAutoOff)
                    for (int m = 1; m <= 4; m++) singleMotorFrozen[m - 1] = true;
                if (shouldCommitPaxiniFreeze && SelectMotorCollider.indexFreezeEnabled && !_pendingIndexAutoOff)
                    for (int m = 5; m <= 8; m++) singleMotorFrozen[m - 1] = true;
                if (shouldCommitPaxiniFreeze && SelectMotorCollider.middleFreezeEnabled && !_pendingMiddleAutoOff)
                    for (int m = 9; m <= 12; m++) singleMotorFrozen[m - 1] = true;
            }
            // Color sync (both modes): paint all group motors yellow — final authority.
            // Skip when auto-OFF is pending (group shows individual frozen/original colours instead).
            if (SelectMotorCollider.thumbFreezeEnabled && !_pendingThumbAutoOff)
                for (int m = 1; m <= 4; m++) SetMotorColorDirect(m, singleFrozenColor);
            if (SelectMotorCollider.indexFreezeEnabled && !_pendingIndexAutoOff)
                for (int m = 5; m <= 8; m++) SetMotorColorDirect(m, singleFrozenColor);
            if (SelectMotorCollider.middleFreezeEnabled && !_pendingMiddleAutoOff)
                for (int m = 9; m <= 12; m++) SetMotorColorDirect(m, singleFrozenColor);
        }
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

        // New round — reset the one-motor-per-round change tracker
        _roundChangedMotorID = 0;
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
        if (confirmedMotorID != 0)
        {
            SetMotorColorDirect(confirmedMotorID, darkRedColor);
        }

        // If the currently touched motor is not the confirmed one, show it in light red
        if (currentRedMotorID != 0 && currentRedMotorID != confirmedMotorID)
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
                int mID = i + 1;
                bool suppress = (mID <= 4 && _suppressThumbGroupYellow)
                             || (mID >= 5 && mID <= 8 && _suppressIndexGroupYellow)
                             || (mID >= 9 && _suppressMiddleGroupYellow);
                if (!suppress)
                    SetMotorColorDirect(mID, singleFrozenColor);
            }
        }

        // Paxini visual sync: when Paxini is ON for a group, all 4 group motors must appear yellow.
        // Skip when pending auto-OFF (Paxini visually shows original while state transitions).
        if (SelectMotorCollider != null)
        {
            if (SelectMotorCollider.thumbFreezeEnabled && !_pendingThumbAutoOff)
                for (int m = 1; m <= 4; m++) SetMotorColorDirect(m, singleFrozenColor);
            if (SelectMotorCollider.indexFreezeEnabled && !_pendingIndexAutoOff)
                for (int m = 5; m <= 8; m++) SetMotorColorDirect(m, singleFrozenColor);
            if (SelectMotorCollider.middleFreezeEnabled && !_pendingMiddleAutoOff)
                for (int m = 9; m <= 12; m++) SetMotorColorDirect(m, singleFrozenColor);
        }
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
            _pendingThumbDirectOff = true; _suppressThumbGroupYellow = true;
            _pendingThumbAutoOn = false; _pendingThumbAutoOff = false;
            SelectMotorCollider.thumbPaxiniForceYellow = false; SelectMotorCollider.thumbPaxiniForceOriginal = false;
        }
        else if (groupStart == 5)
        {
            _pendingIndexDirectOff = true; _suppressIndexGroupYellow = true;
            _pendingIndexAutoOn = false; _pendingIndexAutoOff = false;
            SelectMotorCollider.indexPaxiniForceYellow = false; SelectMotorCollider.indexPaxiniForceOriginal = false;
        }
        else
        {
            _pendingMiddleDirectOff = true; _suppressMiddleGroupYellow = true;
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
        if (gEnd > 12) return;

        int paxiniID = GetPaxiniIDForGroup(groupStart);

        // One-change-per-round: if a DIFFERENT motor already changed this round, revert it first.
        if (_roundChangedMotorID != 0 && _roundChangedMotorID != paxiniID)
        {
            RevertMotorToBaseline(_roundChangedMotorID);
            _roundChangedMotorID = 0;
        }

        // Revert the 4 group motors to the round-entry baseline so this Paxini toggle
        // is the only net change from baseline.
        for (int m = groupStart; m <= gEnd; m++)
            singleMotorFrozen[m - 1] = _singleMotorFrozenBaseline[m - 1];

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
        int gEnd = groupStart + 3;
        if (gEnd > 12) return;

        int paxiniID = GetPaxiniIDForGroup(groupStart);

        // One-change-per-round: if a DIFFERENT motor already changed this round, revert it first.
        if (_roundChangedMotorID != 0 && _roundChangedMotorID != paxiniID)
        {
            RevertMotorToBaseline(_roundChangedMotorID);
            _roundChangedMotorID = 0;
        }

        // Revert group motors to baseline.
        for (int m = groupStart; m <= gEnd; m++)
            singleMotorFrozen[m - 1] = _singleMotorFrozenBaseline[m - 1];

        // Track Paxini as round change (if it was ON at baseline, turning OFF = change).
        bool baselinePaxiniOn = GetBaselinePaxiniOn(groupStart);
        _roundChangedMotorID = baselinePaxiniOn ? paxiniID : 0;

        // Set pending direct-OFF: suppresses frozen-yellow on group motors until hand-away.
        // At hand-away, baseline-frozen motors transition to off state.
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
            if (!singleMotorFrozen[m - 1]) return; // not all frozen yet

        // All 4 frozen — show Paxini yellow visually (pending, state stays freeze-OFF).
        if (gStart == 1)      { _pendingThumbAutoOn  = true; SelectMotorCollider.thumbPaxiniForceYellow   = true; SelectMotorCollider.thumbPaxiniForceOriginal   = false; }
        else if (gStart == 5) { _pendingIndexAutoOn  = true; SelectMotorCollider.indexPaxiniForceYellow   = true; SelectMotorCollider.indexPaxiniForceOriginal   = false; }
        else                  { _pendingMiddleAutoOn = true; SelectMotorCollider.middlePaxiniForceYellow  = true; SelectMotorCollider.middlePaxiniForceOriginal  = false; }
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
            // Paxini was committed ON — show original colour (pending, state stays freeze-ON).
            if (gStart == 1)      { _pendingThumbAutoOff  = true; SelectMotorCollider.thumbPaxiniForceOriginal   = true; SelectMotorCollider.thumbPaxiniForceYellow   = false; }
            else if (gStart == 5) { _pendingIndexAutoOff  = true; SelectMotorCollider.indexPaxiniForceOriginal   = true; SelectMotorCollider.indexPaxiniForceYellow   = false; }
            else                  { _pendingMiddleAutoOff = true; SelectMotorCollider.middlePaxiniForceOriginal  = true; SelectMotorCollider.middlePaxiniForceYellow  = false; }
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

    public Color GetPaxiniDisplayColor(int motorID, Color fallbackOriginalColor)
    {
        // Single frozen motors (1-12): return frozen color
        if (motorID >= 1 && motorID <= 12 && singleMotorFrozen[motorID - 1])
            return singleFrozenColor;

        // Paxini renderers (13/14/15): always return original color.
        // Yellow is handled by the showFreezeColor path in TriggerRight*Tip (bypasses this function).
        // Gray/red must never affect Paxini — they are not part of the 1-12 motor selection system.
        switch (motorID)
        {
            case ThumbPaxiniMotorID:
                return thumbPaxiniRenderer != null ? thumbPaxiniOriginalColor : fallbackOriginalColor;
            case IndexPaxiniMotorID:
                return indexPaxiniRenderer != null ? indexPaxiniOriginalColor : fallbackOriginalColor;
            case MiddlePaxiniMotorID:
                return middlePaxiniRenderer != null ? middlePaxiniOriginalColor : fallbackOriginalColor;
        }

        // Motors 1-12: apply gray/red logic as normal
        Color baseColor = fallbackOriginalColor;

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

        if (confirmedMotorID == motorID)
            return darkRedColor;

        if (currentRedMotorID == motorID && currentRedMotorID != confirmedMotorID)
            return isConfirmed ? darkRedColor : lightRedColor;

        return baseColor;
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
