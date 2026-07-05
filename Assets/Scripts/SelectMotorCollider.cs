using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProjectionMode
{
    FivePoint,   // Original multi-segment projection (5 joints)
    TwoPoint,    // Two-point projection (tip + base, 1 segment)
    FrozenLine   // Frozen two-point projection (locked to wrist)
}

public class SelectMotorCollider : MonoBehaviour
{
    private const int ThumbPaxiniMotorID = 13;
    private const int IndexPaxiniMotorID = 14;
    private const int MiddlePaxiniMotorID = 15;

    public Collider thumbSelectionRetargetingCollider;
    public Collider thumbManipulationRetargetingCollider;
    public Collider indexSelectionRetargetingCollider;
    public Collider indexManipulationRetargetingCollider;
    public Collider middleSelectionRetargetingCollider;
    public Collider middleManipulationRetargetingCollider;

    [Header("12 Motor Colliders (for Thumb only - motors 1-4)")]
    [Tooltip("Drag and drop 12 collider GameObjects here (index 0-11 = motor ID 1-12)")]
    public GameObject[] motorColliders = new GameObject[12];
    
    [Header("Target Tag")]
    public string targetTag = "L_IndexTipSmall";
    
    [Header("Switch Settings")]
    [Tooltip("Minimum time (seconds) between motor switches to prevent rapid switching")]
    public float switchCooldown = 0f;

    [Header("=== Thumb Projection Settings ===")]
    [Tooltip("Use projection-based detection for thumb instead of colliders (motors 1-4)")]
    public bool useThumbProjection = false;
    
    [Tooltip("Right hand thumb FingerPath with joints (segments = motors 1-4)")]
    public FingerPath rightThumbPath;
    
    [Tooltip("Claw thumb FingerPath")]
    public FingerPath clawThumbPath;
    
    [Tooltip("TriggerRightThumbTip to detect when thumb is being touched")]
    public TriggerRightThumbTip triggerRightThumbTip;

    [Header("Index Finger Projection Settings")]
    [Tooltip("Left hand point for projection (L_IndexTipSmall transform)")]
    public Transform leftHandPoint;
    
    [Tooltip("Right hand index FingerPath with 5 joints (4 segments = motors 5-8)")]
    public FingerPath rightIndexPath;
    
    [Tooltip("Claw index FingerPath")]
    public FingerPath clawIndexPath;
    
    [Tooltip("TriggerRightIndexTip to detect when index finger is being touched")]
    public TriggerRightIndexTip triggerRightIndexTip;

    [Header("Middle Finger Projection Settings")]
    [Tooltip("Right hand middle FingerPath with 5 joints (4 segments = motors 9-12)")]
    public FingerPath rightMiddlePath;
    
    [Tooltip("Claw middle FingerPath")]
    public FingerPath clawMiddlePath;
    
    [Tooltip("TriggerRightMiddleTip to detect when middle finger is being touched")]
    public TriggerRightMiddleTip triggerRightMiddleTip;

    [Tooltip("Reference to ModeSwitching — required for freeze gate (Problem 1), confirmed-motor color clear (Problem 2), and mutual exclusion (Problem 3)")]
    public ModeSwitching modeSwitching;

    [Header("=== Priority Colliders (Thumb/Index/Middle disambiguation) ===")]
    [Tooltip("Small collider near thumb fingertip — last OnTriggerEnter sets thumb priority, blocking index/middle projection")]
    public GameObject thumbPriorityCollider;

    [Tooltip("Small collider near index fingertip — last OnTriggerEnter sets index priority, blocking thumb/middle projection")]
    public GameObject indexPriorityCollider;

    [Tooltip("Small collider near middle fingertip — last OnTriggerEnter sets middle priority, blocking thumb/index projection")]
    public GameObject middlePriorityCollider;

    [Tooltip("[Debug] Current finger priority: 0=none, 1=thumb priority, 2=index priority, 3=middle priority")]
    public int debugFingerPriority = 0;

    [Header("=== New Feature: Fingertip Priority Selection ===")]
    [Tooltip("Enable fingertip-first mode: Fingertip motors (4, 8, 12) must be confirmed before other motors can be selected")]
    public bool useFingertipFirst = false;
    
    [Tooltip("Confirmed fingertip motor ID (4=Thumb, 8=Index, 12=Middle, 0=None)")]
    public int confirmedFingertipMotorID = 0;
    
    [Tooltip("Whether the fingertip has been confirmed")]
    public bool isFingertipConfirmed = false;
    
    [Tooltip("Current selectable motor range")]
    public string selectableMotorRange = "All (1-12)";

    [Header("=== Projection Mode ===")]
    [Tooltip("Select projection method: FivePoint (5 joints), TwoPoint (tip+base), FrozenLine (locked to wrist)")]
    public ProjectionMode projectionMode = ProjectionMode.FivePoint;

    // Convenience properties for backward compatibility
    public bool useTwoPointProjection => projectionMode == ProjectionMode.TwoPoint;
    public bool useFrozenTwoPoint => projectionMode == ProjectionMode.FrozenLine;
    
    [Tooltip("R_Wrist transform (right hand wrist bone)")]
    public Transform rWristTransform;
    
    [Header("Frozen Line Visualization (Auto-Created)")]
    [HideInInspector] public LineRenderer frozenThumbLineRenderer;
    [HideInInspector] public LineRenderer frozenIndexLineRenderer;
    [HideInInspector] public LineRenderer frozenMiddleLineRenderer;
    
    [HideInInspector] public Transform frozenThumbTipSphere;
    [HideInInspector] public Transform frozenThumbBaseSphere;
    [HideInInspector] public Transform frozenIndexTipSphere;
    [HideInInspector] public Transform frozenIndexBaseSphere;
    [HideInInspector] public Transform frozenMiddleTipSphere;
    [HideInInspector] public Transform frozenMiddleBaseSphere;
    
    [Tooltip("Color of the frozen projection line")]
    public Color frozenLineColor = Color.cyan;
    [Tooltip("Width of the frozen projection line")]
    public float frozenLineWidth = 0.003f;
    [Tooltip("Radius of the frozen endpoint spheres")]
    public float frozenSphereRadius = 0.005f;
    [Tooltip("Color of the frozen endpoint spheres")]
    public Color frozenSphereColor = Color.cyan;

    // ─── Debug ────────────────────────────────────────────────────────────────

    [Header("=== Debug Settings ===")]
    [Tooltip("Toggle to show/hide debug spheres")]
    public bool showDebugSpheres = true;
    
    [Tooltip("Key to toggle debug spheres on/off")]
    public KeyCode debugToggleKey = KeyCode.D;
    
    [Tooltip("Toggle to show/hide debug LineRenderers (5-point, 2-point, frozen)")]
    public bool showDebugLines = true;
    
    [Tooltip("Key to toggle debug LineRenderers on/off")]
    public KeyCode debugLineToggleKey = KeyCode.L;
    
    [Tooltip("Reference to FingerRendererManager (for toggling 5-point/2-point line renderers)")]
    public FingerRendererManager fingerRendererManager;

    [Header("Debug Spheres - Thumb")]
    [Tooltip("Sphere to show projection on right hand thumb")]
    public Transform thumbRightFingerProjectionSphere;
    [Tooltip("Sphere to show projection on claw thumb")]
    public Transform thumbClawProjectionSphere;

    [Header("Debug Spheres - Index")]
    [Tooltip("Sphere to show projection on right hand index finger")]
    public Transform indexRightFingerProjectionSphere;
    [Tooltip("Sphere to show projection on claw index finger")]
    public Transform indexClawProjectionSphere;

    [Header("Debug Spheres - Middle")]
    [Tooltip("Sphere to show projection on right hand middle finger")]
    public Transform middleRightFingerProjectionSphere;
    [Tooltip("Sphere to show projection on claw middle finger")]
    public Transform middleClawProjectionSphere;

    [Header("Debug Info")]
    [Tooltip("Currently touched motor ID (0 = none)")]
    public int currentTouchedMotorID = 0;
    
    [Tooltip("Touch position on the active motor")]
    public Vector3 touchPosition = Vector3.zero;
    
    [Tooltip("Is any motor currently being touched?")]
    public bool isAnyMotorTouched = false;

    [Header("Debug - Thumb Projection")]
    [Tooltip("Current segment index on thumb (0-3)")]
    public int thumbSegmentIndex = -1;
    
    [Tooltip("Projection position on claw thumb")]
    public Vector3 thumbClawProjectionPosition = Vector3.zero;

    [Header("Debug - Index Projection")]
    [Tooltip("Current segment index on index finger (0-3)")]
    public int indexSegmentIndex = -1;
    
    [Tooltip("Projection position on claw index finger")]
    public Vector3 indexClawProjectionPosition = Vector3.zero;

    [Header("Debug - Middle Projection")]
    [Tooltip("Current segment index on middle finger (0-3)")]
    public int middleSegmentIndex = -1;
    
    [Tooltip("Projection position on claw middle finger")]
    public Vector3 middleClawProjectionPosition = Vector3.zero;

    [Header("Debug - Two-Point Projection")]
    [Tooltip("Thumb projection percentage (0=tip, 100=base)")]
    public float thumbProjectionPercent = 0f;
    
    [Tooltip("Index finger projection percentage (0=tip, 100=base)")]
    public float indexProjectionPercent = 0f;
    
    [Tooltip("Middle finger projection percentage (0=tip, 100=base)")]
    public float middleProjectionPercent = 0f;

    [Header("Debug - Frozen Projection")]
    [Tooltip("Whether a frozen line is currently active")]
    public bool isFrozenLineActive = false;
    [Tooltip("Which finger is frozen (0=none, 4=thumb, 8=index, 12=middle)")]
    public int frozenFingerID = 0;

    // Priority collider state: 0=none, 1=thumb, 2=index, 3=middle
    private int _fingerPriority = 0;

    // Track which motor is currently touched (1-12, 0 = none)
    private int activeTouchedMotorID = 0;
    private Vector3 activeTouchPosition = Vector3.zero;
    
    // Switch cooldown tracking
    private float lastSwitchTime = -999f;
    
    // Child trigger components for each motor (only used for motors 1-4 Thumb)
    private MotorTriggerDetector[] triggerDetectors = new MotorTriggerDetector[12];
    
    // Thumb projection state
    private int thumbProjectionMotorID = 0;
    private Vector3 thumbProjectionPosition = Vector3.zero;
    private Vector3 thumbClawPosition = Vector3.zero;
    
    // Index finger projection state
    private int indexProjectionMotorID = 0;
    private Vector3 indexProjectionPosition = Vector3.zero;
    private Vector3 indexClawPosition = Vector3.zero;
    
    // Middle finger projection state
    private int middleProjectionMotorID = 0;
    private Vector3 middleProjectionPosition = Vector3.zero;
    private Vector3 middleClawPosition = Vector3.zero;
    
    // Frozen two-point state
    private Vector3 frozenTipLocalPos;   // tip position relative to R_Wrist
    private Vector3 frozenBaseLocalPos;  // base position relative to R_Wrist
    private bool frozenCaptured = false; // whether frozen positions have been captured
    private int frozenFingertipID = 0;   // which fingertip was frozen (4, 8, 12)
    
    // When true, all visuals are force-hidden (e.g. during modeManipulate)
    private bool _visualsForceHidden = false;
    public bool isVisualsForceHidden => _visualsForceHidden;

    // ─── New Feature: Freeze Motor ────────────────────────────────────────────

    [Header("=== New Feature: Freeze Motor ===")]
    [Tooltip("Enable freeze motor feature: 0-20% zone toggles freeze ON/OFF for the selected finger's 4 motors")]
    public bool enableFreezeMotorFeature = false;

    [Tooltip("Allow keyboard/Inspector manual freeze toggles even when enableFreezeMotorFeature is OFF")]
    public bool allowManualFreezeWhenFeatureDisabled = true;

    [Header("Manual Freeze Controls (works when freeze feature is OFF)")]
    [Tooltip("Enable keyboard toggles for finger freeze when enableFreezeMotorFeature is OFF")]
    public bool enableManualFreezeKeyboard = true;
    [Tooltip("Keyboard key to toggle thumb freeze (motors 1-4)")]
    public KeyCode thumbManualFreezeKey = KeyCode.Alpha1;
    [Tooltip("Keyboard key to toggle index freeze (motors 5-8)")]
    public KeyCode indexManualFreezeKey = KeyCode.Alpha2;
    [Tooltip("Keyboard key to toggle middle freeze (motors 9-12)")]
    public KeyCode middleManualFreezeKey = KeyCode.Alpha3;

    [Tooltip("Inspector one-shot toggle for thumb freeze (auto-reset next frame)")]
    public bool inspectorToggleThumbFreeze = false;
    [Tooltip("Inspector one-shot toggle for index freeze (auto-reset next frame)")]
    public bool inspectorToggleIndexFreeze = false;
    [Tooltip("Inspector one-shot toggle for middle freeze (auto-reset next frame)")]
    public bool inspectorToggleMiddleFreeze = false;

    [Tooltip("[Debug] Thumb motors freeze enabled")]
    public bool thumbFreezeEnabled = false;
    [Tooltip("[Debug] Index motors freeze enabled")]
    public bool indexFreezeEnabled = false;
    [Tooltip("[Debug] Middle motors freeze enabled")]
    public bool middleFreezeEnabled = false;

    [Tooltip("[Debug] Thumb currently in freeze zone (0-20%)")]
    public bool thumbInFreezeZone = false;
    [Tooltip("[Debug] Index currently in freeze zone (0-20%)")]
    public bool indexInFreezeZone = false;
    [Tooltip("[Debug] Middle currently in freeze zone (0-20%)")]
    public bool middleInFreezeZone = false;

    [Tooltip("Suppresses Select→Manipulate transition when freeze zone was last action")]
    public bool suppressManipulateTransition = false;

    // Freeze feature — edge trigger state
    private bool _thumbFreezeCanTrigger = true;
    private bool _indexFreezeCanTrigger = true;
    private bool _middleFreezeCanTrigger = true;

    // Freeze feature — per-finger suppress tracking
    private bool _suppressFromThumbFreeze = false;
    private bool _suppressFromIndexFreeze = false;
    private bool _suppressFromMiddleFreeze = false;

    // Freeze feature — color lerp state
    private Color _thumbFreezeLerpStart  = Color.black;
    private Color _thumbFreezeLerpTarget = Color.black;
    private float _thumbFreezeColorLerpT = 1f;

    private Color _indexFreezeLerpStart  = Color.black;
    private Color _indexFreezeLerpTarget = Color.black;
    private float _indexFreezeColorLerpT = 1f;

    private Color _middleFreezeLerpStart   = Color.black;
    private Color _middleFreezeLerpTarget  = Color.black;
    private float _middleFreezeLerpColorT  = 1f;

    // Freeze feature — gate: unlocks once finger's fingertip motor (4/8/12) is actively selected,
    // resets when the finger leaves its trigger zone entirely.
    // Gate must be unlocked before the 0-20% freeze zone can trigger a toggle (Problem 1).
    private bool _thumbFreezeGateUnlocked  = false;
    private bool _indexFreezeGateUnlocked  = false;
    private bool _middleFreezeGateUnlocked = false;

    // Freeze feature — pending: lerp has started but toggle is not committed until lerp completes
    private bool _thumbFreezePending  = false;
    private bool _indexFreezePending  = false;
    private bool _middleFreezePending = false;

    // Paxini group-sync: track previous Paxini ON states to detect user-driven toggle OFF → bulk unfreeze
    private bool _prevThumbFreezeEnabled  = false;
    private bool _prevIndexFreezeEnabled  = false;
    private bool _prevMiddleFreezeEnabled = false;
    // Set before any programmatic (non-user-initiated) *FreezeEnabled = false to suppress bulk unfreeze.
    private bool _suppressThumbBulkUnfreeze  = false;
    private bool _suppressIndexBulkUnfreeze  = false;
    private bool _suppressMiddleBulkUnfreeze = false;
    // Set before any programmatic *FreezeEnabled = true to suppress bulk freeze (motors already handled).
    private bool _suppressThumbBulkFreeze  = false;
    private bool _suppressIndexBulkFreeze  = false;
    private bool _suppressMiddleBulkFreeze = false;

    private void Start()
    {
        // Setup trigger detectors for motor colliders 1-4 (Thumb only) - skip if using projection mode
        if (!useThumbProjection)
        {
            for (int i = 0; i < 4; i++) // Only motors 1-4
            {
                if (motorColliders[i] != null)
                {
                    // Add or get the detector component
                    MotorTriggerDetector detector = motorColliders[i].GetComponent<MotorTriggerDetector>();
                    if (detector == null)
                    {
                        detector = motorColliders[i].AddComponent<MotorTriggerDetector>();
                    }
                    
                    // Initialize with motorID 1-4 (i+1)
                    detector.Initialize(i + 1, targetTag, this);
                    triggerDetectors[i] = detector;
                }
            }
        }
        
        // Hide thumb debug spheres initially
        if (thumbRightFingerProjectionSphere != null)
            thumbRightFingerProjectionSphere.gameObject.SetActive(false);
        if (thumbClawProjectionSphere != null)
            thumbClawProjectionSphere.gameObject.SetActive(false);
        
        // Hide index finger debug spheres initially
        if (indexRightFingerProjectionSphere != null)
            indexRightFingerProjectionSphere.gameObject.SetActive(false);
        if (indexClawProjectionSphere != null)
            indexClawProjectionSphere.gameObject.SetActive(false);
        
        // Hide middle finger debug spheres initially
        if (middleRightFingerProjectionSphere != null)
            middleRightFingerProjectionSphere.gameObject.SetActive(false);
        if (middleClawProjectionSphere != null)
            middleClawProjectionSphere.gameObject.SetActive(false);
        
        // Auto-find FingerRendererManager if not assigned
        if (fingerRendererManager == null)
        {
            fingerRendererManager = FindObjectOfType<FingerRendererManager>();
            // if (fingerRendererManager != null)
            //     Debug.Log("[SelectMotorCollider] Auto-found FingerRendererManager");
        }
        
        // Auto-create frozen line renderers and endpoint spheres
        CreateFrozenVisuals();
        
        // Hide frozen endpoint spheres and lines initially
        HideFrozenVisuals();
        
        // Initialize fingertip-first feature state
        if (useFingertipFirst)
        {
            confirmedFingertipMotorID = 0;
            isFingertipConfirmed = false;
            UpdateSelectableMotorRangeText();
        }

        // Initialize freeze feature color lerp state (OFF = black, fully arrived)
        _thumbFreezeLerpStart  = Color.black;
        _thumbFreezeLerpTarget = Color.black;
        _thumbFreezeColorLerpT = 1f;
        _indexFreezeLerpStart  = Color.black;
        _indexFreezeLerpTarget = Color.black;
        _indexFreezeColorLerpT = 1f;
        _middleFreezeLerpStart  = Color.black;
        _middleFreezeLerpTarget = Color.black;
        _middleFreezeLerpColorT = 1f;

        // Setup priority collider detectors (thumb/index/middle disambiguation)
        if (thumbPriorityCollider != null)
        {
            var det = thumbPriorityCollider.GetComponent<PriorityColliderDetector>();
            if (det == null) det = thumbPriorityCollider.AddComponent<PriorityColliderDetector>();
            det.Initialize(1, targetTag, this);
        }
        if (indexPriorityCollider != null)
        {
            var det = indexPriorityCollider.GetComponent<PriorityColliderDetector>();
            if (det == null) det = indexPriorityCollider.AddComponent<PriorityColliderDetector>();
            det.Initialize(2, targetTag, this);
        }
        if (middlePriorityCollider != null)
        {
            var det = middlePriorityCollider.GetComponent<PriorityColliderDetector>();
            if (det == null) det = middlePriorityCollider.AddComponent<PriorityColliderDetector>();
            det.Initialize(3, targetTag, this);
        }
    }

    private void Update()
    {
        HandleManualFreezeControls();

        // Toggle debug spheres with key press
        if (Input.GetKeyDown(debugToggleKey))
        {
            showDebugSpheres = !showDebugSpheres;
            // Debug.Log($"[SelectMotorCollider] Debug spheres: {(showDebugSpheres ? "ON" : "OFF")}");
            
            // If turning off, hide all spheres immediately
            if (!showDebugSpheres)
            {
                HideAllDebugSpheres();
            }
        }
        
        // Toggle debug LineRenderers with key press
        if (Input.GetKeyDown(debugLineToggleKey))
        {
            showDebugLines = !showDebugLines;
            // Debug.Log($"[SelectMotorCollider] Debug lines: {(showDebugLines ? "ON" : "OFF")}");
            
            if (!showDebugLines)
            {
                // Hide all line renderers immediately
                HideAllLineRenderers();
            }
            else
            {
                // Restore FingerRendererManager visibility
                if (fingerRendererManager != null)
                {
                    fingerRendererManager.SetLineRenderersVisible(true);
                }
            }
        }

        // Hard block: during manipulate mode, selection and pseudo-motor effects must be disabled.
        if (IsManipulateModeActive())
        {
            ClearSelectionStateForManipulate();

            // Keep debug fields coherent while blocked.
            currentTouchedMotorID = 0;
            touchPosition = Vector3.zero;
            isAnyMotorTouched = false;

            // Keep freeze display state updated while manipulating.
            UpdateFreezeColors();
            return;
        }
        
        // Handle Thumb projection-based selection (motors 1-4) if enabled
        if (useThumbProjection)
        {
            // Problem 3: block thumb if another finger is confirmed AND still physically touched
            if (enableFreezeMotorFeature && IsBlockedByOtherFinger(1, 4))
                ClearFingerProjectionState(1);
            else
            {
                switch (projectionMode)
                {
                    case ProjectionMode.FrozenLine:
                        if (frozenCaptured && frozenFingertipID == 4)
                            UpdateFrozenProjection(1, 4, clawThumbPath);
                        else
                            UpdateThumbProjection_TwoPoint(); // fallback before frozen capture
                        break;
                    case ProjectionMode.TwoPoint:
                        UpdateThumbProjection_TwoPoint();
                        break;
                    default: // FivePoint
                        UpdateThumbProjection();
                        break;
                }
            }
        }
        
        // Handle Index finger projection-based selection (motors 5-8)
        // Problem 3: block index if another finger is confirmed AND still physically touched
        if (enableFreezeMotorFeature && IsBlockedByOtherFinger(5, 8))
            ClearFingerProjectionState(5);
        else
        {
            switch (projectionMode)
            {
                case ProjectionMode.FrozenLine:
                    if (frozenCaptured && frozenFingertipID == 8)
                        UpdateFrozenProjection(5, 8, clawIndexPath);
                    else
                        UpdateIndexFingerProjection_TwoPoint(); // fallback before frozen capture
                    break;
                case ProjectionMode.TwoPoint:
                    UpdateIndexFingerProjection_TwoPoint();
                    break;
                default: // FivePoint
                    UpdateIndexFingerProjection();
                    break;
            }
        }
        
        // Handle Middle finger projection-based selection (motors 9-12)
        // Problem 3: block middle if another finger is confirmed AND still physically touched
        if (enableFreezeMotorFeature && IsBlockedByOtherFinger(9, 12))
            ClearFingerProjectionState(9);
        else
        {
            switch (projectionMode)
            {
                case ProjectionMode.FrozenLine:
                    if (frozenCaptured && frozenFingertipID == 12)
                        UpdateFrozenProjection(9, 12, clawMiddlePath);
                    else
                        UpdateMiddleFingerProjection_TwoPoint(); // fallback before frozen capture
                    break;
                case ProjectionMode.TwoPoint:
                    UpdateMiddleFingerProjection_TwoPoint();
                    break;
                default: // FivePoint
                    UpdateMiddleFingerProjection();
                    break;
            }
        }
        
        // Update frozen line visuals (skip if force-hidden during manipulate mode)
        if (projectionMode == ProjectionMode.FrozenLine && frozenCaptured && !_visualsForceHidden)
        {
            UpdateFrozenVisuals();
        }
        
        // Update debug info
        currentTouchedMotorID = activeTouchedMotorID;
        touchPosition = activeTouchPosition;
        isAnyMotorTouched = activeTouchedMotorID != 0;

        // Update freeze feature color animations
        UpdateFreezeColors();

        // Paxini group-sync: detect user-driven Paxini state changes and cascade to group motors.
        // OFF→ON: force-freeze all 4 group motors. ON→OFF: force-unfreeze all 4 group motors.
        if (_prevThumbFreezeEnabled && !thumbFreezeEnabled)
        {
            if (!_suppressThumbBulkUnfreeze && modeSwitching != null)
                modeSwitching.UnfreezeGroupMotors(1);
            _suppressThumbBulkUnfreeze = false;
        }
        else if (!_prevThumbFreezeEnabled && thumbFreezeEnabled)
        {
            if (!_suppressThumbBulkFreeze && modeSwitching != null)
                modeSwitching.FreezeGroupMotors(1);
            _suppressThumbBulkFreeze = false;
        }
        if (_prevIndexFreezeEnabled && !indexFreezeEnabled)
        {
            if (!_suppressIndexBulkUnfreeze && modeSwitching != null)
                modeSwitching.UnfreezeGroupMotors(5);
            _suppressIndexBulkUnfreeze = false;
        }
        else if (!_prevIndexFreezeEnabled && indexFreezeEnabled)
        {
            if (!_suppressIndexBulkFreeze && modeSwitching != null)
                modeSwitching.FreezeGroupMotors(5);
            _suppressIndexBulkFreeze = false;
        }
        if (_prevMiddleFreezeEnabled && !middleFreezeEnabled)
        {
            if (!_suppressMiddleBulkUnfreeze && modeSwitching != null)
                modeSwitching.UnfreezeGroupMotors(9);
            _suppressMiddleBulkUnfreeze = false;
        }
        else if (!_prevMiddleFreezeEnabled && middleFreezeEnabled)
        {
            if (!_suppressMiddleBulkFreeze && modeSwitching != null)
                modeSwitching.FreezeGroupMotors(9);
            _suppressMiddleBulkFreeze = false;
        }
        _prevThumbFreezeEnabled  = thumbFreezeEnabled;
        _prevIndexFreezeEnabled  = indexFreezeEnabled;
        _prevMiddleFreezeEnabled = middleFreezeEnabled;
    }

    private void HandleManualFreezeControls()
    {
        if (enableFreezeMotorFeature || !allowManualFreezeWhenFeatureDisabled)
        {
            inspectorToggleThumbFreeze = false;
            inspectorToggleIndexFreeze = false;
            inspectorToggleMiddleFreeze = false;
            return;
        }

        if (enableManualFreezeKeyboard)
        {
            if (Input.GetKeyDown(thumbManualFreezeKey))
            {
                ToggleFingerFreezeManual(1);
            }
            if (Input.GetKeyDown(indexManualFreezeKey))
            {
                ToggleFingerFreezeManual(5);
            }
            if (Input.GetKeyDown(middleManualFreezeKey))
            {
                ToggleFingerFreezeManual(9);
            }
        }

        if (inspectorToggleThumbFreeze)
        {
            ToggleFingerFreezeManual(1);
            inspectorToggleThumbFreeze = false;
        }
        if (inspectorToggleIndexFreeze)
        {
            ToggleFingerFreezeManual(5);
            inspectorToggleIndexFreeze = false;
        }
        if (inspectorToggleMiddleFreeze)
        {
            ToggleFingerFreezeManual(9);
            inspectorToggleMiddleFreeze = false;
        }
    }

    private void ToggleFingerFreezeManual(int fingerMinMotor)
    {
        if (fingerMinMotor == 1)
        {
            thumbFreezeEnabled = !thumbFreezeEnabled;
            thumbInFreezeZone = false;
            _thumbFreezePending = false;
            _thumbFreezeCanTrigger = true;
            _suppressFromThumbFreeze = false;
            _thumbFreezeGateUnlocked = false;
        }
        else if (fingerMinMotor == 5)
        {
            indexFreezeEnabled = !indexFreezeEnabled;
            indexInFreezeZone = false;
            _indexFreezePending = false;
            _indexFreezeCanTrigger = true;
            _suppressFromIndexFreeze = false;
            _indexFreezeGateUnlocked = false;
        }
        else if (fingerMinMotor == 9)
        {
            middleFreezeEnabled = !middleFreezeEnabled;
            middleInFreezeZone = false;
            _middleFreezePending = false;
            _middleFreezeCanTrigger = true;
            _suppressFromMiddleFreeze = false;
            _middleFreezeGateUnlocked = false;
        }
    }

    private bool IsManipulateModeActive()
    {
        // modeSwitching may be unassigned in this component; _visualsForceHidden is set by ModeSwitching
        // when entering manipulate mode and provides a safe fallback signal.
        return (modeSwitching != null && modeSwitching.modeManipulate) || _visualsForceHidden;
    }

    private void ClearSelectionStateForManipulate()
    {
        activeTouchedMotorID = 0;
        activeTouchPosition = Vector3.zero;

        thumbProjectionMotorID = 0;
        indexProjectionMotorID = 0;
        middleProjectionMotorID = 0;

        thumbSegmentIndex = -1;
        indexSegmentIndex = -1;
        middleSegmentIndex = -1;

        if (thumbRightFingerProjectionSphere != null) thumbRightFingerProjectionSphere.gameObject.SetActive(false);
        if (thumbClawProjectionSphere != null) thumbClawProjectionSphere.gameObject.SetActive(false);
        if (indexRightFingerProjectionSphere != null) indexRightFingerProjectionSphere.gameObject.SetActive(false);
        if (indexClawProjectionSphere != null) indexClawProjectionSphere.gameObject.SetActive(false);
        if (middleRightFingerProjectionSphere != null) middleRightFingerProjectionSphere.gameObject.SetActive(false);
        if (middleClawProjectionSphere != null) middleClawProjectionSphere.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Hides all debug spheres
    /// </summary>
    private void HideAllDebugSpheres()
    {
        if (thumbRightFingerProjectionSphere != null)
            thumbRightFingerProjectionSphere.gameObject.SetActive(false);
        if (thumbClawProjectionSphere != null)
            thumbClawProjectionSphere.gameObject.SetActive(false);
        if (indexRightFingerProjectionSphere != null)
            indexRightFingerProjectionSphere.gameObject.SetActive(false);
        if (indexClawProjectionSphere != null)
            indexClawProjectionSphere.gameObject.SetActive(false);
        if (middleRightFingerProjectionSphere != null)
            middleRightFingerProjectionSphere.gameObject.SetActive(false);
        if (middleClawProjectionSphere != null)
            middleClawProjectionSphere.gameObject.SetActive(false);
        
        // Also hide frozen endpoint spheres
        if (frozenThumbTipSphere != null) frozenThumbTipSphere.gameObject.SetActive(false);
        if (frozenThumbBaseSphere != null) frozenThumbBaseSphere.gameObject.SetActive(false);
        if (frozenIndexTipSphere != null) frozenIndexTipSphere.gameObject.SetActive(false);
        if (frozenIndexBaseSphere != null) frozenIndexBaseSphere.gameObject.SetActive(false);
        if (frozenMiddleTipSphere != null) frozenMiddleTipSphere.gameObject.SetActive(false);
        if (frozenMiddleBaseSphere != null) frozenMiddleBaseSphere.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Hides all debug LineRenderers (frozen lines + FingerRendererManager lines)
    /// </summary>
    private void HideAllLineRenderers()
    {
        // Hide frozen line renderers
        if (frozenThumbLineRenderer != null) frozenThumbLineRenderer.enabled = false;
        if (frozenIndexLineRenderer != null) frozenIndexLineRenderer.enabled = false;
        if (frozenMiddleLineRenderer != null) frozenMiddleLineRenderer.enabled = false;
        
        // Hide FingerRendererManager lines (5-point + 2-point)
        if (fingerRendererManager != null)
        {
            fingerRendererManager.SetLineRenderersVisible(false);
        }
    }
    
    /// <summary>
    /// Hide ALL debug visuals (spheres + LineRenderers).
    /// Called when entering modeManipulate.
    /// </summary>
    public void HideAllDebugVisuals()
    {
        _visualsForceHidden = true;
        HideAllDebugSpheres();
        HideAllLineRenderers();
    }
    
    /// <summary>
    /// Restore debug visuals based on current showDebugSpheres / showDebugLines flags.
    /// Called when leaving modeManipulate.
    /// </summary>
    public void RestoreDebugVisuals()
    {
        _visualsForceHidden = false;
        
        // Restore FingerRendererManager line visibility based on current toggle
        if (fingerRendererManager != null)
        {
            fingerRendererManager.SetLineRenderersVisible(showDebugLines);
        }
        // Spheres and frozen lines will be restored naturally by the projection Update methods
        // based on the showDebugSpheres / showDebugLines flags
    }
    
    /// <summary>
    /// Update the selectable motor range text
    /// </summary>
    private void UpdateSelectableMotorRangeText()
    {
        if (!useFingertipFirst)
        {
            selectableMotorRange = "All (1-12)";
        }
        else if (!isFingertipConfirmed)
        {
            selectableMotorRange = "Fingertips only (4, 8, 12)";
        }
        else
        {
            switch (confirmedFingertipMotorID)
            {
                case 4:
                    selectableMotorRange = "Thumb (1-4, 13)";
                    break;
                case 8:
                    selectableMotorRange = "Index (5-8, 14)";
                    break;
                case 12:
                    selectableMotorRange = "Middle (9-12, 15)";
                    break;
                default:
                    selectableMotorRange = "Fingertips only (4, 8, 12)";
                    break;
            }
        }
    }
    
    /// <summary>
    /// Check whether a motor can be selected by touch (fingertip-first mode)
    /// </summary>
    /// <param name="motorID">Motor ID (1-12)</param>
    /// <returns>True if selectable</returns>
    public bool IsMotorSelectable(int motorID)
    {
        // If fingertip-first is not enabled, all motors are selectable
        if (!useFingertipFirst)
        {
            return true;
        }
        
        // Fingertip motors are always selectable
        if (motorID == 4 || motorID == 8 || motorID == 12)
        {
            return true;
        }
        
        // If a fingertip hasn't been confirmed yet, other motors are not selectable
        if (!isFingertipConfirmed)
        {
            return false;
        }
        
        // Determine selectable motors based on the confirmed fingertip
        switch (confirmedFingertipMotorID)
        {
            case 4: // Thumb confirmed → motors 1-4 selectable
                return (motorID >= 1 && motorID <= 4) || motorID == ThumbPaxiniMotorID;
            case 8: // Index confirmed → motors 5-8 selectable
                return (motorID >= 5 && motorID <= 8) || motorID == IndexPaxiniMotorID;
            case 12: // Middle confirmed → motors 9-12 selectable
                return (motorID >= 9 && motorID <= 12) || motorID == MiddlePaxiniMotorID;
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Called when a fingertip motor is confirmed (invoked by ModeSwitching)
    /// </summary>
    /// <param name="fingertipMotorID">Fingertip motor ID (4, 8, or 12)</param>
    public void OnFingertipConfirmed(int fingertipMotorID)
    {
        if (!useFingertipFirst) return;
        
        if (fingertipMotorID == 4 || fingertipMotorID == 8 || fingertipMotorID == 12)
        {
            confirmedFingertipMotorID = fingertipMotorID;
            isFingertipConfirmed = true;
            UpdateSelectableMotorRangeText();
            // Debug.Log($"[SelectMotorCollider] Fingertip motor {fingertipMotorID} confirmed! Selectable range: {selectableMotorRange}");
        }
    }
    
    /// <summary>
    /// Reset fingertip confirmation state (call when leaving selection mode or after operation)
    /// </summary>
    public void ResetFingertipConfirmation()
    {
        if (!useFingertipFirst) return;
        
        confirmedFingertipMotorID = 0;
        isFingertipConfirmed = false;
        UpdateSelectableMotorRangeText();
        // Debug.Log($"[SelectMotorCollider] Fingertip confirmation state reset");
    }
    
    /// <summary>
    /// Updates thumb motor selection based on projection (motors 1-4).
    /// Uses multi-segment projection like index/middle fingers.
    /// </summary>
    private void UpdateThumbProjection()
    {
        bool isThumbTouched = triggerRightThumbTip != null && triggerRightThumbTip.isRightThumbTipTouched;
        // Priority gate: if index or middle priority is active, suppress thumb projection
        if (_fingerPriority == 2 || _fingerPriority == 3) isThumbTouched = false;
        
        if (!isThumbTouched || leftHandPoint == null || rightThumbPath == null)
        {
            if (thumbProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == thumbProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                thumbProjectionMotorID = 0;
                thumbSegmentIndex = -1;
            }
            thumbInFreezeZone = false;
            _thumbFreezeCanTrigger = true;
            _suppressFromThumbFreeze = false;
            _thumbFreezeGateUnlocked = false;

            if (thumbRightFingerProjectionSphere != null)
                thumbRightFingerProjectionSphere.gameObject.SetActive(false);
            if (thumbClawProjectionSphere != null)
                thumbClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        int segmentIndex;
        float segmentT;
        Vector3 closestPointOnRightFinger;
        
        FingerMath.ClosestPointOnFinger(
            leftHandPoint.position,
            rightThumbPath,
            out segmentIndex,
            out segmentT,
            out closestPointOnRightFinger
        );
        
        segmentT = Mathf.Clamp01(segmentT);

        // Handle freeze zone (0-20% = toggle freeze, 20-100% = motor selection)
        if (enableFreezeMotorFeature)
        {
            float overallPct = (segmentIndex + segmentT) / 4f * 100f;
            float remappedPct;
            bool inFreeze = HandleFreezeZone(overallPct,
                _thumbFreezeGateUnlocked,
                ref thumbFreezeEnabled, ref _thumbFreezeCanTrigger, ref _suppressFromThumbFreeze,
                ref thumbInFreezeZone,
                ref _thumbFreezeLerpStart, ref _thumbFreezeLerpTarget, ref _thumbFreezeColorLerpT,
                triggerRightThumbTip != null ? triggerRightThumbTip.originalColor : Color.white,
                ref _thumbFreezePending,
                out remappedPct);
            if (inFreeze)
            {
                if (thumbProjectionMotorID != 0 && activeTouchedMotorID == thumbProjectionMotorID)
                { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
                thumbProjectionMotorID = 0;
                if (thumbRightFingerProjectionSphere != null) thumbRightFingerProjectionSphere.gameObject.SetActive(false);
                if (thumbClawProjectionSphere != null) thumbClawProjectionSphere.gameObject.SetActive(false);
                return;
            }
            segmentIndex = Mathf.Clamp((int)(remappedPct / 25f), 0, 3);
            segmentT = Mathf.Clamp01((remappedPct - segmentIndex * 25f) / 25f);
        }
        
        Vector3 clawPos = Vector3.zero;
        if (clawThumbPath != null)
        {
            int clawJointCount = clawThumbPath.GetJointCount();
            int clampedSeg = Mathf.Clamp(segmentIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawThumbPath.GetJoint(clampedSeg),
                clawThumbPath.GetJoint(clampedSeg + 1),
                segmentT
            );
        }
        
        // Convert segment index to motor ID (1-4)
        // segment 0 → motor 4, segment 1 → motor 3, segment 2 → motor 2, segment 3 → motor 1
        int motorID = 4 - segmentIndex;
        motorID = Mathf.Clamp(motorID, 1, 4);
        
        if (!IsMotorSelectable(motorID))
        {
            if (thumbProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == thumbProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                thumbProjectionMotorID = 0;
            }
            
            if (thumbRightFingerProjectionSphere != null)
                thumbRightFingerProjectionSphere.gameObject.SetActive(false);
            if (thumbClawProjectionSphere != null)
                thumbClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        thumbSegmentIndex = segmentIndex;
        thumbProjectionPosition = closestPointOnRightFinger;
        thumbClawPosition = clawPos;
        thumbClawProjectionPosition = clawPos;
        
        if (showDebugSpheres)
        {
            if (thumbRightFingerProjectionSphere != null)
            {
                thumbRightFingerProjectionSphere.gameObject.SetActive(true);
                thumbRightFingerProjectionSphere.position = closestPointOnRightFinger;
            }
            if (thumbClawProjectionSphere != null && clawThumbPath != null)
            {
                thumbClawProjectionSphere.gameObject.SetActive(true);
                thumbClawProjectionSphere.position = clawPos;
            }
        }
        else
        {
            if (thumbRightFingerProjectionSphere != null)
                thumbRightFingerProjectionSphere.gameObject.SetActive(false);
            if (thumbClawProjectionSphere != null)
                thumbClawProjectionSphere.gameObject.SetActive(false);
        }
        
        if (motorID != thumbProjectionMotorID)
        {
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
            {
                return;
            }
            
            // Clear index projection if it was active
            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8)
            {
                indexProjectionMotorID = 0;
            }
            
            // Clear middle projection if it was active
            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12)
            {
                middleProjectionMotorID = 0;
            }
            
            thumbProjectionMotorID = motorID;
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPointOnRightFinger;
            lastSwitchTime = Time.time;
        }
        else
        {
            activeTouchPosition = closestPointOnRightFinger;
        }
    }
    
    /// <summary>
    /// [Two-Point] Updates thumb motor selection using single-segment percentage mapping.
    /// Hand thumb: tip (joint 0) + base (last joint) = 1 segment.
    /// Percentage 0-100 maps smoothly to 4 claw segments (motors 1-4).
    /// </summary>
    private void UpdateThumbProjection_TwoPoint()
    {
        bool isThumbTouched = triggerRightThumbTip != null && triggerRightThumbTip.isRightThumbTipTouched;
        // Priority gate: if index or middle priority is active, suppress thumb projection
        if (_fingerPriority == 2 || _fingerPriority == 3) isThumbTouched = false;
        
        if (!isThumbTouched || leftHandPoint == null || rightThumbPath == null)
        {
            if (thumbProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == thumbProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                thumbProjectionMotorID = 0;
                thumbSegmentIndex = -1;
            }
            thumbProjectionPercent = 0f;
            thumbInFreezeZone = false;
            _thumbFreezeCanTrigger = true;
            _suppressFromThumbFreeze = false;
            _thumbFreezeGateUnlocked = false;
            
            if (thumbRightFingerProjectionSphere != null)
                thumbRightFingerProjectionSphere.gameObject.SetActive(false);
            if (thumbClawProjectionSphere != null)
                thumbClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        int jointCount = rightThumbPath.GetJointCount();
        Vector3 tipPoint = rightThumbPath.GetJoint(0);
        Vector3 basePoint = rightThumbPath.GetJoint(jointCount - 1);
        
        float t;
        FingerMath.DistancePointToSegment(leftHandPoint.position, tipPoint, basePoint, out t);
        t = Mathf.Clamp01(t);
        
        float percentage = t * 100f;
        thumbProjectionPercent = percentage;
        bool skipMotorSelection = false;

        // Handle freeze zone (0-20% = toggle freeze, 20-100% = motor selection)
        if (enableFreezeMotorFeature)
        {
            float remappedPct;
            bool inFreeze = HandleFreezeZone(percentage,
                _thumbFreezeGateUnlocked,
                ref thumbFreezeEnabled, ref _thumbFreezeCanTrigger, ref _suppressFromThumbFreeze,
                ref thumbInFreezeZone,
                ref _thumbFreezeLerpStart, ref _thumbFreezeLerpTarget, ref _thumbFreezeColorLerpT,
                triggerRightThumbTip != null ? triggerRightThumbTip.originalColor : Color.white,
                ref _thumbFreezePending,
                out remappedPct);
            if (inFreeze)
            {
                if (thumbProjectionMotorID != 0 && activeTouchedMotorID == thumbProjectionMotorID)
                { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
                thumbProjectionMotorID = 0;
                skipMotorSelection = true;
            }
        }
        
        int clawSegIndex;
        float localT;
        int clawPathSegIndex;
        if (enableFreezeMotorFeature)
        {
            if (percentage >= 100f)
            {
                clawSegIndex = 4;
                localT = 1f;
            }
            else
            {
                clawSegIndex = Mathf.Clamp((int)(percentage / 20f), 0, 4);
                localT = (percentage - clawSegIndex * 20f) / 20f;
            }
            clawPathSegIndex = clawSegIndex;
        }
        else
        {
            if (percentage >= 100f)
            {
                clawSegIndex = 3;
                localT = 1f;
            }
            else
            {
                clawSegIndex = Mathf.Clamp((int)(percentage / 25f), 0, 3);
                localT = (percentage - clawSegIndex * 25f) / 25f;
            }
            clawPathSegIndex = clawSegIndex + 1;
        }
        localT = Mathf.Clamp01(localT);
        
        // Freeze mode uses 6 claw points: 0-20 is the freeze zone, 20-100 maps to motors 4-1.
        int motorID = enableFreezeMotorFeature ? 5 - clawSegIndex : 4 - clawSegIndex;
        motorID = Mathf.Clamp(motorID, 1, 4);
        
        Vector3 closestPointOnRightFinger = Vector3.Lerp(tipPoint, basePoint, t);
        
        Vector3 clawPos = Vector3.zero;
        if (clawThumbPath != null)
        {
            int clawJointCount = clawThumbPath.GetJointCount();
            int clampedSeg = Mathf.Clamp(clawPathSegIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawThumbPath.GetJoint(clampedSeg),
                clawThumbPath.GetJoint(clampedSeg + 1),
                localT
            );
        }

        if (skipMotorSelection)
        {
            thumbSegmentIndex = clawPathSegIndex;
            thumbProjectionPosition = closestPointOnRightFinger;
            thumbClawPosition = clawPos;
            thumbClawProjectionPosition = clawPos;

            if (showDebugSpheres)
            {
                if (thumbRightFingerProjectionSphere != null)
                {
                    thumbRightFingerProjectionSphere.gameObject.SetActive(true);
                    thumbRightFingerProjectionSphere.position = closestPointOnRightFinger;
                }
                if (thumbClawProjectionSphere != null && clawThumbPath != null)
                {
                    thumbClawProjectionSphere.gameObject.SetActive(true);
                    thumbClawProjectionSphere.position = clawPos;
                }
            }
            else
            {
                if (thumbRightFingerProjectionSphere != null)
                    thumbRightFingerProjectionSphere.gameObject.SetActive(false);
                if (thumbClawProjectionSphere != null)
                    thumbClawProjectionSphere.gameObject.SetActive(false);
            }

            return;
        }
        
        if (!IsMotorSelectable(motorID))
        {
            if (thumbProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == thumbProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                thumbProjectionMotorID = 0;
            }
            
            if (thumbRightFingerProjectionSphere != null)
                thumbRightFingerProjectionSphere.gameObject.SetActive(false);
            if (thumbClawProjectionSphere != null)
                thumbClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        thumbSegmentIndex = clawPathSegIndex;
        thumbProjectionPosition = closestPointOnRightFinger;
        thumbClawPosition = clawPos;
        thumbClawProjectionPosition = clawPos;
        
        if (showDebugSpheres)
        {
            if (thumbRightFingerProjectionSphere != null)
            {
                thumbRightFingerProjectionSphere.gameObject.SetActive(true);
                thumbRightFingerProjectionSphere.position = closestPointOnRightFinger;
            }
            if (thumbClawProjectionSphere != null && clawThumbPath != null)
            {
                thumbClawProjectionSphere.gameObject.SetActive(true);
                thumbClawProjectionSphere.position = clawPos;
            }
        }
        else
        {
            if (thumbRightFingerProjectionSphere != null)
                thumbRightFingerProjectionSphere.gameObject.SetActive(false);
            if (thumbClawProjectionSphere != null)
                thumbClawProjectionSphere.gameObject.SetActive(false);
        }
        
        if (motorID != thumbProjectionMotorID)
        {
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
            {
                return;
            }
            
            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8)
            {
                indexProjectionMotorID = 0;
            }
            
            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12)
            {
                middleProjectionMotorID = 0;
            }
            
            thumbProjectionMotorID = motorID;
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPointOnRightFinger;
            lastSwitchTime = Time.time;
        }
        else
        {
            activeTouchPosition = closestPointOnRightFinger;
        }
    }
    
    /// <summary>
    /// Updates index finger motor selection based on projection
    /// </summary>
    private void UpdateIndexFingerProjection()
    {
        // Check if index finger is being touched
        bool isIndexTouched = triggerRightIndexTip != null && triggerRightIndexTip.isRightIndexTipTouched;
        // Priority gate: if thumb or middle priority is active, suppress index projection
        if (_fingerPriority == 1 || _fingerPriority == 3) isIndexTouched = false;
        
        if (!isIndexTouched || leftHandPoint == null || rightIndexPath == null)
        {
            // Index finger not touched - clear index projection state
            if (indexProjectionMotorID != 0)
            {
                // If index projection was active, release it
                if (activeTouchedMotorID == indexProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                indexProjectionMotorID = 0;
                indexSegmentIndex = -1;
            }
            indexInFreezeZone = false;
            _indexFreezeCanTrigger = true;
            _suppressFromIndexFreeze = false;
            _indexFreezeGateUnlocked = false;

            // Hide debug spheres
            if (indexRightFingerProjectionSphere != null)
                indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null)
                indexClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Calculate projection on right hand index finger
        int segmentIndex;
        float segmentT;
        Vector3 closestPointOnRightFinger;
        
        FingerMath.ClosestPointOnFinger(
            leftHandPoint.position,
            rightIndexPath,
            out segmentIndex,
            out segmentT,
            out closestPointOnRightFinger
        );
        
        // Clamp values
        segmentT = Mathf.Clamp01(segmentT);

        // Handle freeze zone (0-20% = toggle freeze, 20-100% = motor selection)
        if (enableFreezeMotorFeature)
        {
            float overallPct = (segmentIndex + segmentT) / 4f * 100f;
            float remappedPct;
            bool inFreeze = HandleFreezeZone(overallPct,
                _indexFreezeGateUnlocked,
                ref indexFreezeEnabled, ref _indexFreezeCanTrigger, ref _suppressFromIndexFreeze,
                ref indexInFreezeZone,
                ref _indexFreezeLerpStart, ref _indexFreezeLerpTarget, ref _indexFreezeColorLerpT,
                triggerRightIndexTip != null ? triggerRightIndexTip.originalColor : Color.white,
                ref _indexFreezePending,
                out remappedPct);
            if (inFreeze)
            {
                if (indexProjectionMotorID != 0 && activeTouchedMotorID == indexProjectionMotorID)
                { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
                indexProjectionMotorID = 0;
                if (indexRightFingerProjectionSphere != null) indexRightFingerProjectionSphere.gameObject.SetActive(false);
                if (indexClawProjectionSphere != null) indexClawProjectionSphere.gameObject.SetActive(false);
                return;
            }
            segmentIndex = Mathf.Clamp((int)(remappedPct / 25f), 0, 3);
            segmentT = Mathf.Clamp01((remappedPct - segmentIndex * 25f) / 25f);
        }
        
        // Calculate corresponding point on claw finger if available
        Vector3 clawPos = Vector3.zero;
        if (clawIndexPath != null)
        {
            int clawJointCount = clawIndexPath.GetJointCount();
            int clampedSeg = Mathf.Clamp(segmentIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawIndexPath.GetJoint(clampedSeg),
                clawIndexPath.GetJoint(clampedSeg + 1),
                segmentT
            );
        }
        
        // Convert segment index to motor ID (5-8)
        // segment 0 → motor 8, segment 1 → motor 7, segment 2 → motor 6, segment 3 → motor 5
        int motorID = 8 - segmentIndex;
        
        // Clamp to valid range (5-8)
        motorID = Mathf.Clamp(motorID, 5, 8);
        
        // New feature: check motor selectability (fingertip-first mode)
        if (!IsMotorSelectable(motorID))
        {
            // Motor not selectable — clear state
            if (indexProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == indexProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                indexProjectionMotorID = 0;
            }
            
            // Hide debug spheres
            if (indexRightFingerProjectionSphere != null)
                indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null)
                indexClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Update index projection state
        indexSegmentIndex = segmentIndex;
        indexProjectionPosition = closestPointOnRightFinger;
        indexClawPosition = clawPos;
        indexClawProjectionPosition = clawPos;
        
        // Update debug spheres (only if enabled)
        if (showDebugSpheres)
        {
            if (indexRightFingerProjectionSphere != null)
            {
                indexRightFingerProjectionSphere.gameObject.SetActive(true);
                indexRightFingerProjectionSphere.position = closestPointOnRightFinger;
            }
            if (indexClawProjectionSphere != null && clawIndexPath != null)
            {
                indexClawProjectionSphere.gameObject.SetActive(true);
                indexClawProjectionSphere.position = clawPos;
            }
        }
        else
        {
            // Hide spheres when debug is disabled
            if (indexRightFingerProjectionSphere != null)
                indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null)
                indexClawProjectionSphere.gameObject.SetActive(false);
        }
        
        // Check if motor changed
        if (motorID != indexProjectionMotorID)
        {
            // Check switch cooldown (if switching from another motor)
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
            {
                return; // Ignore during cooldown
            }
            
            // If currently active motor is a collider-based motor (1-4), force release it
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                // Clear thumb projection if using projection mode
                thumbProjectionMotorID = 0;
                
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            // Clear middle projection if it was active
            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12)
            {
                middleProjectionMotorID = 0;
            }
            
            indexProjectionMotorID = motorID;
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPointOnRightFinger;
            lastSwitchTime = Time.time;
        }
        else
        {
            // Same motor - just update position
            activeTouchPosition = closestPointOnRightFinger;
        }
    }
    
    /// <summary>
    /// Updates middle finger motor selection based on projection
    /// </summary>
    private void UpdateMiddleFingerProjection()
    {
        // Check if middle finger is being touched
        bool isMiddleTouched = triggerRightMiddleTip != null && triggerRightMiddleTip.isRightMiddleTipTouched;
        // Priority gate: if thumb or index priority is active, suppress middle projection
        if (_fingerPriority == 1 || _fingerPriority == 2) isMiddleTouched = false;
        
        if (!isMiddleTouched || leftHandPoint == null || rightMiddlePath == null)
        {
            // Middle finger not touched - clear middle projection state
            if (middleProjectionMotorID != 0)
            {
                // If middle projection was active, release it
                if (activeTouchedMotorID == middleProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                middleProjectionMotorID = 0;
                middleSegmentIndex = -1;
            }
            middleInFreezeZone = false;
            _middleFreezeCanTrigger = true;
            _suppressFromMiddleFreeze = false;
            _middleFreezeGateUnlocked = false;

            // Hide debug spheres
            if (middleRightFingerProjectionSphere != null)
                middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null)
                middleClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Calculate projection on right hand middle finger
        int segmentIndex;
        float segmentT;
        Vector3 closestPointOnRightFinger;
        
        FingerMath.ClosestPointOnFinger(
            leftHandPoint.position,
            rightMiddlePath,
            out segmentIndex,
            out segmentT,
            out closestPointOnRightFinger
        );
        
        // Clamp values
        segmentT = Mathf.Clamp01(segmentT);

        // Handle freeze zone (0-20% = toggle freeze, 20-100% = motor selection)
        if (enableFreezeMotorFeature)
        {
            float overallPct = (segmentIndex + segmentT) / 4f * 100f;
            float remappedPct;
            bool inFreeze = HandleFreezeZone(overallPct,
                _middleFreezeGateUnlocked,
                ref middleFreezeEnabled, ref _middleFreezeCanTrigger, ref _suppressFromMiddleFreeze,
                ref middleInFreezeZone,
                ref _middleFreezeLerpStart, ref _middleFreezeLerpTarget, ref _middleFreezeLerpColorT,
                triggerRightMiddleTip != null ? triggerRightMiddleTip.originalColor : Color.white,
                ref _middleFreezePending,
                out remappedPct);
            if (inFreeze)
            {
                if (middleProjectionMotorID != 0 && activeTouchedMotorID == middleProjectionMotorID)
                { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
                middleProjectionMotorID = 0;
                if (middleRightFingerProjectionSphere != null) middleRightFingerProjectionSphere.gameObject.SetActive(false);
                if (middleClawProjectionSphere != null) middleClawProjectionSphere.gameObject.SetActive(false);
                return;
            }
            segmentIndex = Mathf.Clamp((int)(remappedPct / 25f), 0, 3);
            segmentT = Mathf.Clamp01((remappedPct - segmentIndex * 25f) / 25f);
        }
        
        // Calculate corresponding point on claw finger if available
        Vector3 clawPos = Vector3.zero;
        if (clawMiddlePath != null)
        {
            int clawJointCount = clawMiddlePath.GetJointCount();
            int clampedSeg = Mathf.Clamp(segmentIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawMiddlePath.GetJoint(clampedSeg),
                clawMiddlePath.GetJoint(clampedSeg + 1),
                segmentT
            );
        }
        
        // Convert segment index to motor ID (9-12)
        // segment 0 → motor 12, segment 1 → motor 11, segment 2 → motor 10, segment 3 → motor 9
        int motorID = 12 - segmentIndex;
        
        // Clamp to valid range (9-12)
        motorID = Mathf.Clamp(motorID, 9, 12);
        
        // New feature: check motor selectability (fingertip-first mode)
        if (!IsMotorSelectable(motorID))
        {
            // Motor not selectable — clear state
            if (middleProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == middleProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                middleProjectionMotorID = 0;
            }
            
            // Hide debug spheres
            if (middleRightFingerProjectionSphere != null)
                middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null)
                middleClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Update middle projection state
        middleSegmentIndex = segmentIndex;
        middleProjectionPosition = closestPointOnRightFinger;
        middleClawPosition = clawPos;
        middleClawProjectionPosition = clawPos;
        
        // Update debug spheres (only if enabled)
        if (showDebugSpheres)
        {
            if (middleRightFingerProjectionSphere != null)
            {
                middleRightFingerProjectionSphere.gameObject.SetActive(true);
                middleRightFingerProjectionSphere.position = closestPointOnRightFinger;
            }
            if (middleClawProjectionSphere != null && clawMiddlePath != null)
            {
                middleClawProjectionSphere.gameObject.SetActive(true);
                middleClawProjectionSphere.position = clawPos;
            }
        }
        else
        {
            // Hide spheres when debug is disabled
            if (middleRightFingerProjectionSphere != null)
                middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null)
                middleClawProjectionSphere.gameObject.SetActive(false);
        }
        
        // Check if motor changed
        if (motorID != middleProjectionMotorID)
        {
            // Check switch cooldown (if switching from another motor)
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
            {
                return; // Ignore during cooldown
            }
            
            // If currently active motor is a collider-based motor (1-4), force release it
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                // Clear thumb projection if using projection mode
                thumbProjectionMotorID = 0;
                
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            // Clear index projection if it was active
            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8)
            {
                indexProjectionMotorID = 0;
            }
            
            middleProjectionMotorID = motorID;
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPointOnRightFinger;
            lastSwitchTime = Time.time;
        }
        else
        {
            // Same motor - just update position
            activeTouchPosition = closestPointOnRightFinger;
        }
    }

    /// <summary>
    /// [Two-Point] Updates index finger motor selection using single-segment percentage mapping.
    /// Hand finger: tip (joint 0) + base (last joint) = 1 segment.
    /// Percentage 0-100 maps smoothly to 4 claw segments (motors 5-8).
    /// </summary>
    private void UpdateIndexFingerProjection_TwoPoint()
    {
        bool isIndexTouched = triggerRightIndexTip != null && triggerRightIndexTip.isRightIndexTipTouched;
        // Priority gate: if thumb or middle priority is active, suppress index projection
        if (_fingerPriority == 1 || _fingerPriority == 3) isIndexTouched = false;
        
        if (!isIndexTouched || leftHandPoint == null || rightIndexPath == null)
        {
            if (indexProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == indexProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                indexProjectionMotorID = 0;
                indexSegmentIndex = -1;
            }
            indexProjectionPercent = 0f;
            indexInFreezeZone = false;
            _indexFreezeCanTrigger = true;
            _suppressFromIndexFreeze = false;
            _indexFreezeGateUnlocked = false;
            
            if (indexRightFingerProjectionSphere != null)
                indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null)
                indexClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Two-point mode: use only tip (joint 0) and base (last joint)
        int jointCount = rightIndexPath.GetJointCount();
        Vector3 tipPoint = rightIndexPath.GetJoint(0);
        Vector3 basePoint = rightIndexPath.GetJoint(jointCount - 1);
        
        // Project left hand point onto this single segment
        float t;
        FingerMath.DistancePointToSegment(leftHandPoint.position, tipPoint, basePoint, out t);
        t = Mathf.Clamp01(t);
        
        // t: 0 = tip, 1 = base → percentage 0-100
        float percentage = t * 100f;
        indexProjectionPercent = percentage;
        bool skipMotorSelection = false;

        // Handle freeze zone (0-20% = toggle freeze, 20-100% = motor selection)
        if (enableFreezeMotorFeature)
        {
            float remappedPct;
            bool inFreeze = HandleFreezeZone(percentage,
                _indexFreezeGateUnlocked,
                ref indexFreezeEnabled, ref _indexFreezeCanTrigger, ref _suppressFromIndexFreeze,
                ref indexInFreezeZone,
                ref _indexFreezeLerpStart, ref _indexFreezeLerpTarget, ref _indexFreezeColorLerpT,
                triggerRightIndexTip != null ? triggerRightIndexTip.originalColor : Color.white,
                ref _indexFreezePending,
                out remappedPct);
            if (inFreeze)
            {
                if (indexProjectionMotorID != 0 && activeTouchedMotorID == indexProjectionMotorID)
                { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
                indexProjectionMotorID = 0;
                skipMotorSelection = true;
            }
        }
        
        int clawSegIndex;
        float localT;
        int clawPathSegIndex;
        if (enableFreezeMotorFeature)
        {
            if (percentage >= 100f)
            {
                clawSegIndex = 4;
                localT = 1f;
            }
            else
            {
                clawSegIndex = Mathf.Clamp((int)(percentage / 20f), 0, 4);
                localT = (percentage - clawSegIndex * 20f) / 20f;
            }
            clawPathSegIndex = clawSegIndex;
        }
        else
        {
            if (percentage >= 100f)
            {
                clawSegIndex = 3;
                localT = 1f;
            }
            else
            {
                clawSegIndex = Mathf.Clamp((int)(percentage / 25f), 0, 3);
                localT = (percentage - clawSegIndex * 25f) / 25f;
            }
            clawPathSegIndex = clawSegIndex + 1;
        }
        localT = Mathf.Clamp01(localT);
        
        int motorID = enableFreezeMotorFeature ? 9 - clawSegIndex : 8 - clawSegIndex;
        motorID = Mathf.Clamp(motorID, 5, 8);
        
        // Closest point on the hand finger line (for debug sphere)
        Vector3 closestPointOnRightFinger = Vector3.Lerp(tipPoint, basePoint, t);
        
        // Calculate claw position using claw's actual segments
        Vector3 clawPos = Vector3.zero;
        if (clawIndexPath != null)
        {
            int clawJointCount = clawIndexPath.GetJointCount();
            int clampedSeg = Mathf.Clamp(clawPathSegIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawIndexPath.GetJoint(clampedSeg),
                clawIndexPath.GetJoint(clampedSeg + 1),
                localT
            );
        }

        if (skipMotorSelection)
        {
            indexSegmentIndex = clawPathSegIndex;
            indexProjectionPosition = closestPointOnRightFinger;
            indexClawPosition = clawPos;
            indexClawProjectionPosition = clawPos;

            if (showDebugSpheres)
            {
                if (indexRightFingerProjectionSphere != null)
                {
                    indexRightFingerProjectionSphere.gameObject.SetActive(true);
                    indexRightFingerProjectionSphere.position = closestPointOnRightFinger;
                }
                if (indexClawProjectionSphere != null && clawIndexPath != null)
                {
                    indexClawProjectionSphere.gameObject.SetActive(true);
                    indexClawProjectionSphere.position = clawPos;
                }
            }
            else
            {
                if (indexRightFingerProjectionSphere != null)
                    indexRightFingerProjectionSphere.gameObject.SetActive(false);
                if (indexClawProjectionSphere != null)
                    indexClawProjectionSphere.gameObject.SetActive(false);
            }

            return;
        }
        
        // Check motor selectability (fingertip-first mode)
        if (!IsMotorSelectable(motorID))
        {
            if (indexProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == indexProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                indexProjectionMotorID = 0;
            }
            
            if (indexRightFingerProjectionSphere != null)
                indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null)
                indexClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Update index projection state
        indexSegmentIndex = clawPathSegIndex;
        indexProjectionPosition = closestPointOnRightFinger;
        indexClawPosition = clawPos;
        indexClawProjectionPosition = clawPos;
        
        // Update debug spheres
        if (showDebugSpheres)
        {
            if (indexRightFingerProjectionSphere != null)
            {
                indexRightFingerProjectionSphere.gameObject.SetActive(true);
                indexRightFingerProjectionSphere.position = closestPointOnRightFinger;
            }
            if (indexClawProjectionSphere != null && clawIndexPath != null)
            {
                indexClawProjectionSphere.gameObject.SetActive(true);
                indexClawProjectionSphere.position = clawPos;
            }
        }
        else
        {
            if (indexRightFingerProjectionSphere != null)
                indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null)
                indexClawProjectionSphere.gameObject.SetActive(false);
        }
        
        // Motor switch logic
        if (motorID != indexProjectionMotorID)
        {
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
            {
                return;
            }
            
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                thumbProjectionMotorID = 0;
                
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12)
            {
                middleProjectionMotorID = 0;
            }
            
            indexProjectionMotorID = motorID;
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPointOnRightFinger;
            lastSwitchTime = Time.time;
        }
        else
        {
            activeTouchPosition = closestPointOnRightFinger;
        }
    }
    
    /// <summary>
    /// [Two-Point] Updates middle finger motor selection using single-segment percentage mapping.
    /// Hand finger: tip (joint 0) + base (last joint) = 1 segment.
    /// Percentage 0-100 maps smoothly to 4 claw segments (motors 9-12).
    /// </summary>
    private void UpdateMiddleFingerProjection_TwoPoint()
    {
        bool isMiddleTouched = triggerRightMiddleTip != null && triggerRightMiddleTip.isRightMiddleTipTouched;
        // Priority gate: if thumb or index priority is active, suppress middle projection
        if (_fingerPriority == 1 || _fingerPriority == 2) isMiddleTouched = false;
        
        if (!isMiddleTouched || leftHandPoint == null || rightMiddlePath == null)
        {
            if (middleProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == middleProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                middleProjectionMotorID = 0;
                middleSegmentIndex = -1;
            }
            middleProjectionPercent = 0f;
            middleInFreezeZone = false;
            _middleFreezeCanTrigger = true;
            _suppressFromMiddleFreeze = false;
            _middleFreezeGateUnlocked = false;
            
            if (middleRightFingerProjectionSphere != null)
                middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null)
                middleClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Two-point mode: use only tip (joint 0) and base (last joint)
        int jointCount = rightMiddlePath.GetJointCount();
        Vector3 tipPoint = rightMiddlePath.GetJoint(0);
        Vector3 basePoint = rightMiddlePath.GetJoint(jointCount - 1);
        
        // Project left hand point onto this single segment
        float t;
        FingerMath.DistancePointToSegment(leftHandPoint.position, tipPoint, basePoint, out t);
        t = Mathf.Clamp01(t);
        
        // t: 0 = tip, 1 = base → percentage 0-100
        float percentage = t * 100f;
        middleProjectionPercent = percentage;
        bool skipMotorSelection = false;

        // Handle freeze zone (0-20% = toggle freeze, 20-100% = motor selection)
        if (enableFreezeMotorFeature)
        {
            float remappedPct;
            bool inFreeze = HandleFreezeZone(percentage,
                _middleFreezeGateUnlocked,
                ref middleFreezeEnabled, ref _middleFreezeCanTrigger, ref _suppressFromMiddleFreeze,
                ref middleInFreezeZone,
                ref _middleFreezeLerpStart, ref _middleFreezeLerpTarget, ref _middleFreezeLerpColorT,
                triggerRightMiddleTip != null ? triggerRightMiddleTip.originalColor : Color.white,
                ref _middleFreezePending,
                out remappedPct);
            if (inFreeze)
            {
                if (middleProjectionMotorID != 0 && activeTouchedMotorID == middleProjectionMotorID)
                { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
                middleProjectionMotorID = 0;
                skipMotorSelection = true;
            }
        }
        
        int clawSegIndex;
        float localT;
        int clawPathSegIndex;
        if (enableFreezeMotorFeature)
        {
            if (percentage >= 100f)
            {
                clawSegIndex = 4;
                localT = 1f;
            }
            else
            {
                clawSegIndex = Mathf.Clamp((int)(percentage / 20f), 0, 4);
                localT = (percentage - clawSegIndex * 20f) / 20f;
            }
            clawPathSegIndex = clawSegIndex;
        }
        else
        {
            if (percentage >= 100f)
            {
                clawSegIndex = 3;
                localT = 1f;
            }
            else
            {
                clawSegIndex = Mathf.Clamp((int)(percentage / 25f), 0, 3);
                localT = (percentage - clawSegIndex * 25f) / 25f;
            }
            clawPathSegIndex = clawSegIndex + 1;
        }
        localT = Mathf.Clamp01(localT);
        
        int motorID = enableFreezeMotorFeature ? 13 - clawSegIndex : 12 - clawSegIndex;
        motorID = Mathf.Clamp(motorID, 9, 12);
        
        // Closest point on the hand finger line (for debug sphere)
        Vector3 closestPointOnRightFinger = Vector3.Lerp(tipPoint, basePoint, t);
        
        // Calculate claw position using claw's actual segments
        Vector3 clawPos = Vector3.zero;
        if (clawMiddlePath != null)
        {
            int clawJointCount = clawMiddlePath.GetJointCount();
            int clampedSeg = Mathf.Clamp(clawPathSegIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawMiddlePath.GetJoint(clampedSeg),
                clawMiddlePath.GetJoint(clampedSeg + 1),
                localT
            );
        }

        if (skipMotorSelection)
        {
            middleSegmentIndex = clawPathSegIndex;
            middleProjectionPosition = closestPointOnRightFinger;
            middleClawPosition = clawPos;
            middleClawProjectionPosition = clawPos;

            if (showDebugSpheres)
            {
                if (middleRightFingerProjectionSphere != null)
                {
                    middleRightFingerProjectionSphere.gameObject.SetActive(true);
                    middleRightFingerProjectionSphere.position = closestPointOnRightFinger;
                }
                if (middleClawProjectionSphere != null && clawMiddlePath != null)
                {
                    middleClawProjectionSphere.gameObject.SetActive(true);
                    middleClawProjectionSphere.position = clawPos;
                }
            }
            else
            {
                if (middleRightFingerProjectionSphere != null)
                    middleRightFingerProjectionSphere.gameObject.SetActive(false);
                if (middleClawProjectionSphere != null)
                    middleClawProjectionSphere.gameObject.SetActive(false);
            }

            return;
        }
        
        // Check motor selectability (fingertip-first mode)
        if (!IsMotorSelectable(motorID))
        {
            if (middleProjectionMotorID != 0)
            {
                if (activeTouchedMotorID == middleProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                middleProjectionMotorID = 0;
            }
            
            if (middleRightFingerProjectionSphere != null)
                middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null)
                middleClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Update middle projection state
        middleSegmentIndex = clawPathSegIndex;
        middleProjectionPosition = closestPointOnRightFinger;
        middleClawPosition = clawPos;
        middleClawProjectionPosition = clawPos;
        
        // Update debug spheres
        if (showDebugSpheres)
        {
            if (middleRightFingerProjectionSphere != null)
            {
                middleRightFingerProjectionSphere.gameObject.SetActive(true);
                middleRightFingerProjectionSphere.position = closestPointOnRightFinger;
            }
            if (middleClawProjectionSphere != null && clawMiddlePath != null)
            {
                middleClawProjectionSphere.gameObject.SetActive(true);
                middleClawProjectionSphere.position = clawPos;
            }
        }
        else
        {
            if (middleRightFingerProjectionSphere != null)
                middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null)
                middleClawProjectionSphere.gameObject.SetActive(false);
        }
        
        // Motor switch logic
        if (motorID != middleProjectionMotorID)
        {
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
            {
                return;
            }
            
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                thumbProjectionMotorID = 0;
                
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8)
            {
                indexProjectionMotorID = 0;
            }
            
            middleProjectionMotorID = motorID;
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPointOnRightFinger;
            lastSwitchTime = Time.time;
        }
        else
        {
            activeTouchPosition = closestPointOnRightFinger;
        }
    }

    // Called by MotorTriggerDetector when a motor is touched (only for motors 1-4 Thumb)
    internal void OnMotorTouched(int motorID, Vector3 position)
    {
        if (IsManipulateModeActive())
            return;

        // Only handle motors 1-4 via collider (Thumb only)
        if (motorID < 1 || motorID > 4)
            return;
        
        // If thumb projection mode is enabled, ignore collider-based thumb touches
        if (useThumbProjection)
            return;
        
        // New feature: check motor selectability (fingertip-first mode)
        if (!IsMotorSelectable(motorID))
        {
            // Debug.Log($"[SelectMotorCollider] Motor {motorID} currently not selectable (need to confirm fingertip motor 4 first)");
            return;
        }
            
        // Only allow one motor at a time
        if (activeTouchedMotorID != motorID)
        {
            // Check switch cooldown
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && timeSinceLastSwitch < switchCooldown)
            {
                return; // Ignore this touch during cooldown
            }
            
            // Force release the previous motor's detector (if it's a collider-based motor 1-4)
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            // Clear index projection if it was active
            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8)
            {
                indexProjectionMotorID = 0;
            }
            
            // Clear middle projection if it was active
            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12)
            {
                middleProjectionMotorID = 0;
            }
            
            activeTouchedMotorID = motorID;
            activeTouchPosition = position;
            lastSwitchTime = Time.time;
        }
        else
        {
            // Update position for the currently active motor
            activeTouchPosition = position;
        }
    }

    // Called by MotorTriggerDetector when a motor is released (only for motors 1-4 Thumb)
    internal void OnMotorReleased(int motorID)
    {
        if (activeTouchedMotorID == motorID && motorID >= 1 && motorID <= 4)
        {
            activeTouchedMotorID = 0;
            activeTouchPosition = Vector3.zero;
        }
    }

    internal void OnPseudoMotorTouched(int motorID, Vector3 position)
    {
        if (IsManipulateModeActive())
            return;

        if (motorID < ThumbPaxiniMotorID || motorID > MiddlePaxiniMotorID)
            return;

        if (useFingertipFirst && isFingertipConfirmed && !IsPseudoMotorOwnedByConfirmedFingertip(motorID))
            return;

        if (!IsMotorSelectable(motorID))
            return;

        if (activeTouchedMotorID != motorID)
        {
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && timeSinceLastSwitch < switchCooldown)
            {
                return;
            }

            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                thumbProjectionMotorID = 0;

                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }

            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8)
            {
                indexProjectionMotorID = 0;
            }

            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12)
            {
                middleProjectionMotorID = 0;
            }

            activeTouchedMotorID = motorID;
            activeTouchPosition = position;
            lastSwitchTime = Time.time;
        }
        else
        {
            activeTouchPosition = position;
        }
    }

    internal void OnPseudoMotorReleased(int motorID)
    {
        if (activeTouchedMotorID == motorID && motorID >= ThumbPaxiniMotorID && motorID <= MiddlePaxiniMotorID)
        {
            activeTouchedMotorID = 0;
            activeTouchPosition = Vector3.zero;
        }
    }

    private bool IsPseudoMotorOwnedByConfirmedFingertip(int motorID)
    {
        switch (confirmedFingertipMotorID)
        {
            case 4:
                return motorID == ThumbPaxiniMotorID;
            case 8:
                return motorID == IndexPaxiniMotorID;
            case 12:
                return motorID == MiddlePaxiniMotorID;
            default:
                return false;
        }
    }

    // Public methods to query motor state
    public int GetTouchedMotorID()
    {
        return activeTouchedMotorID;
    }

    public bool IsTouched()
    {
        return activeTouchedMotorID != 0;
    }

    public bool IsMotorTouched(int motorID)
    {
        return activeTouchedMotorID == motorID;
    }

    public bool TryGetTouchPosition(out Vector3 position)
    {
        position = activeTouchPosition;
        return activeTouchedMotorID != 0;
    }

    public Vector3 GetTouchPosition()
    {
        return activeTouchPosition;
    }

    public GameObject GetTouchedMotorGameObject()
    {
        if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= motorColliders.Length)
        {
            return motorColliders[activeTouchedMotorID - 1];
        }
        return null;
    }
    
    public int GetIndexSegmentIndex()
    {
        return indexSegmentIndex;
    }
    
    public Vector3 GetIndexClawProjectionPosition()
    {
        return indexClawPosition;
    }
    
    public int GetMiddleSegmentIndex()
    {
        return middleSegmentIndex;
    }
    
    public Vector3 GetMiddleClawProjectionPosition()
    {
        return middleClawPosition;
    }
    
    public int GetThumbSegmentIndex()
    {
        return thumbSegmentIndex;
    }
    
    public Vector3 GetThumbClawProjectionPosition()
    {
        return thumbClawPosition;
    }
    
    // ========== Frozen Two-Point Projection Methods ==========
    
    /// <summary>
    /// Capture the frozen two-point line when a fingertip is touched.
    /// Called by ModeSwitching when entering SelectingMotor phase.
    /// </summary>
    /// <param name="fingertipMotorID">4=Thumb, 8=Index, 12=Middle</param>
    public void CaptureFrozenLine(int fingertipMotorID)
    {
        if (!useFrozenTwoPoint || rWristTransform == null) return;
        
        // Block frozen capture for thumb if thumb projection is disabled
        if (fingertipMotorID == 4 && !useThumbProjection) return;
        
        // Clean up any existing frozen line before capturing a new one
        HideFrozenVisuals();
        frozenCaptured = false;
        
        FingerPath fingerPath = null;
        switch (fingertipMotorID)
        {
            case 4: fingerPath = rightThumbPath; break;
            case 8: fingerPath = rightIndexPath; break;
            case 12: fingerPath = rightMiddlePath; break;
            default: return;
        }
        
        if (fingerPath == null || fingerPath.GetJointCount() < 2) return;
        
        // Get world positions of tip and base
        Vector3 tipWorld = fingerPath.GetJoint(0);
        Vector3 baseWorld = fingerPath.GetJoint(fingerPath.GetJointCount() - 1);
        
        // Convert to local positions relative to R_Wrist
        frozenTipLocalPos = rWristTransform.InverseTransformPoint(tipWorld);
        frozenBaseLocalPos = rWristTransform.InverseTransformPoint(baseWorld);
        
        frozenCaptured = true;
        frozenFingertipID = fingertipMotorID;
        frozenFingerID = fingertipMotorID;
        isFrozenLineActive = true;
        
        // Debug.Log($"[SelectMotorCollider] Frozen line captured for finger {fingertipMotorID}: tip={tipWorld}, base={baseWorld}");
    }
    
    /// <summary>
    /// Release the frozen two-point line.
    /// Called by ModeSwitching when leaving SelectingMotor or resetting.
    /// </summary>
    public void ReleaseFrozenLine()
    {
        frozenCaptured = false;
        frozenFingertipID = 0;
        frozenFingerID = 0;
        isFrozenLineActive = false;
        HideFrozenVisuals();
        
        // Debug.Log($"[SelectMotorCollider] Frozen line released");
    }
    
    /// <summary>
    /// Get current frozen world positions (reconstructed from wrist)
    /// </summary>
    public void GetFrozenWorldPositions(out Vector3 tipWorld, out Vector3 baseWorld)
    {
        if (frozenCaptured && rWristTransform != null)
        {
            tipWorld = rWristTransform.TransformPoint(frozenTipLocalPos);
            baseWorld = rWristTransform.TransformPoint(frozenBaseLocalPos);
        }
        else
        {
            tipWorld = Vector3.zero;
            baseWorld = Vector3.zero;
        }
    }
    
    /// <summary>
    /// Generic frozen two-point projection update for any finger.
    /// Projects leftHandPoint onto the frozen line, maps to 4 claw segments.
    /// </summary>
    private void UpdateFrozenProjection(int motorMin, int motorMax, FingerPath clawPath)
    {
        if (!frozenCaptured || rWristTransform == null || leftHandPoint == null)
            return;
        
        // Use the same fingertip touch gate as 5-point / 2-point projections
        bool isTouched = false;
        if (motorMin == 1)
            isTouched = triggerRightThumbTip != null && triggerRightThumbTip.isRightThumbTipTouched;
        else if (motorMin == 5)
            isTouched = triggerRightIndexTip != null && triggerRightIndexTip.isRightIndexTipTouched;
        else if (motorMin == 9)
            isTouched = triggerRightMiddleTip != null && triggerRightMiddleTip.isRightMiddleTipTouched;
        
        if (!isTouched)
        {
            int clearMotorID = (motorMin == 1) ? thumbProjectionMotorID : (motorMin == 5) ? indexProjectionMotorID : middleProjectionMotorID;
            if (clearMotorID != 0 && activeTouchedMotorID == clearMotorID)
            {
                activeTouchedMotorID = 0;
                activeTouchPosition = Vector3.zero;
            }
            if (motorMin == 1) { thumbProjectionMotorID = 0; thumbSegmentIndex = -1; thumbInFreezeZone = false; _thumbFreezeCanTrigger = true; _suppressFromThumbFreeze = false; _thumbFreezeGateUnlocked = false; }
            else if (motorMin == 5) { indexProjectionMotorID = 0; indexSegmentIndex = -1; indexInFreezeZone = false; _indexFreezeCanTrigger = true; _suppressFromIndexFreeze = false; _indexFreezeGateUnlocked = false; }
            else if (motorMin == 9) { middleProjectionMotorID = 0; middleSegmentIndex = -1; middleInFreezeZone = false; _middleFreezeCanTrigger = true; _suppressFromMiddleFreeze = false; _middleFreezeGateUnlocked = false; }
            return;
        }
        
        // Reconstruct world positions from wrist-local offsets
        Vector3 tipWorld = rWristTransform.TransformPoint(frozenTipLocalPos);
        Vector3 baseWorld = rWristTransform.TransformPoint(frozenBaseLocalPos);
        
        // Project leftHandPoint onto the frozen line
        float t;
        FingerMath.DistancePointToSegment(leftHandPoint.position, tipWorld, baseWorld, out t);
        t = Mathf.Clamp01(t);
        
        float percentage = t * 100f;
        
        // Update the correct percentage debug field
        if (motorMin == 1) thumbProjectionPercent = percentage;
        else if (motorMin == 5) indexProjectionPercent = percentage;
        else if (motorMin == 9) middleProjectionPercent = percentage;

        Vector3 closestPoint = Vector3.Lerp(tipWorld, baseWorld, t);

        // Calculate claw position before freeze handling so debug spheres keep moving
        // even while the touch is inside the 0-20% freeze zone.
        int clawSegIndex;
        float localT;
        int clawPathSegIndex;
        int motorID;

        if (enableFreezeMotorFeature)
        {
            // 5 zones: 0-20(freeze zone), 20-40, 40-60, 60-80, 80-100
            if (percentage >= 100f)
            {
                clawSegIndex = 4;
                localT = 1f;
            }
            else
            {
                clawSegIndex = Mathf.Clamp((int)(percentage / 20f), 0, 4);
                localT = (percentage - clawSegIndex * 20f) / 20f;
            }
            clawPathSegIndex = clawSegIndex;

            // Reserve seg0 as freeze zone; motor selection is clamped to [motorMin, motorMax].
            motorID = (motorMax + 1) - clawSegIndex;
        }
        else
        {
            if (percentage >= 100f)
            {
                clawSegIndex = 3;
                localT = 1f;
            }
            else
            {
                clawSegIndex = Mathf.Clamp((int)(percentage / 25f), 0, 3);
                localT = (percentage - clawSegIndex * 25f) / 25f;
            }

            // Keep non-freeze behavior aligned with TwoPoint projection path indexing.
            clawPathSegIndex = clawSegIndex + 1;
            motorID = motorMax - clawSegIndex;
        }

        localT = Mathf.Clamp01(localT);
        motorID = Mathf.Clamp(motorID, motorMin, motorMax);

        Vector3 clawPos = Vector3.zero;
        if (clawPath != null)
        {
            int clawJointCount = clawPath.GetJointCount();
            int clampedSeg = Mathf.Clamp(clawPathSegIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawPath.GetJoint(clampedSeg),
                clawPath.GetJoint(clampedSeg + 1),
                localT
            );
        }

        Transform rightSphere = null, clawSphere = null;
        if (motorMin == 1) { rightSphere = thumbRightFingerProjectionSphere; clawSphere = thumbClawProjectionSphere; }
        else if (motorMin == 5) { rightSphere = indexRightFingerProjectionSphere; clawSphere = indexClawProjectionSphere; }
        else if (motorMin == 9) { rightSphere = middleRightFingerProjectionSphere; clawSphere = middleClawProjectionSphere; }

        if (showDebugSpheres)
        {
            if (rightSphere != null) { rightSphere.gameObject.SetActive(true); rightSphere.position = closestPoint; }
            if (clawSphere != null && clawPath != null) { clawSphere.gameObject.SetActive(true); clawSphere.position = clawPos; }
        }
        else
        {
            if (rightSphere != null) rightSphere.gameObject.SetActive(false);
            if (clawSphere != null) clawSphere.gameObject.SetActive(false);
        }

        // Handle freeze zone (0-20% = toggle freeze, 20-100% = motor selection)
        if (enableFreezeMotorFeature)
        {
            float remappedPct;
            bool inFreeze;
            if (motorMin == 1)
                inFreeze = HandleFreezeZone(percentage, _thumbFreezeGateUnlocked, ref thumbFreezeEnabled, ref _thumbFreezeCanTrigger, ref _suppressFromThumbFreeze, ref thumbInFreezeZone, ref _thumbFreezeLerpStart, ref _thumbFreezeLerpTarget, ref _thumbFreezeColorLerpT, triggerRightThumbTip != null ? triggerRightThumbTip.originalColor : Color.white, ref _thumbFreezePending, out remappedPct);
            else if (motorMin == 5)
                inFreeze = HandleFreezeZone(percentage, _indexFreezeGateUnlocked, ref indexFreezeEnabled, ref _indexFreezeCanTrigger, ref _suppressFromIndexFreeze, ref indexInFreezeZone, ref _indexFreezeLerpStart, ref _indexFreezeLerpTarget, ref _indexFreezeColorLerpT, triggerRightIndexTip != null ? triggerRightIndexTip.originalColor : Color.white, ref _indexFreezePending, out remappedPct);
            else
                inFreeze = HandleFreezeZone(percentage, _middleFreezeGateUnlocked, ref middleFreezeEnabled, ref _middleFreezeCanTrigger, ref _suppressFromMiddleFreeze, ref middleInFreezeZone, ref _middleFreezeLerpStart, ref _middleFreezeLerpTarget, ref _middleFreezeLerpColorT, triggerRightMiddleTip != null ? triggerRightMiddleTip.originalColor : Color.white, ref _middleFreezePending, out remappedPct);

            if (inFreeze)
            {
                int clearID = (motorMin == 1) ? thumbProjectionMotorID : (motorMin == 5) ? indexProjectionMotorID : middleProjectionMotorID;
                if (clearID != 0 && activeTouchedMotorID == clearID)
                { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
                if (motorMin == 1) thumbProjectionMotorID = 0;
                else if (motorMin == 5) indexProjectionMotorID = 0;
                else if (motorMin == 9) middleProjectionMotorID = 0;
                return;
            }
            percentage = remappedPct;
        }
        
        // Check motor selectability
        if (!IsMotorSelectable(motorID))
            return;
        
        // Update state based on which finger
        if (motorMin == 1)
        {
            thumbSegmentIndex = clawSegIndex;
            thumbProjectionPosition = closestPoint;
            thumbClawPosition = clawPos;
            thumbClawProjectionPosition = clawPos;
        }
        else if (motorMin == 5)
        {
            indexSegmentIndex = clawSegIndex;
            indexProjectionPosition = closestPoint;
            indexClawPosition = clawPos;
            indexClawProjectionPosition = clawPos;
        }
        else if (motorMin == 9)
        {
            middleSegmentIndex = clawSegIndex;
            middleProjectionPosition = closestPoint;
            middleClawPosition = clawPos;
            middleClawProjectionPosition = clawPos;
        }
        
        // Motor switch logic
        int prevMotorID = (motorMin == 1) ? thumbProjectionMotorID : (motorMin == 5) ? indexProjectionMotorID : middleProjectionMotorID;
        
        if (motorID != prevMotorID)
        {
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
                return;
            
            // Clear other projections
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4 && motorMin != 1)
                thumbProjectionMotorID = 0;
            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8 && motorMin != 5)
                indexProjectionMotorID = 0;
            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12 && motorMin != 9)
                middleProjectionMotorID = 0;
            
            if (motorMin == 1) thumbProjectionMotorID = motorID;
            else if (motorMin == 5) indexProjectionMotorID = motorID;
            else if (motorMin == 9) middleProjectionMotorID = motorID;
            
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPoint;
            lastSwitchTime = Time.time;
        }
        else
        {
            activeTouchPosition = closestPoint;
        }
    }
    
    /// <summary>
    /// Auto-create frozen LineRenderers and endpoint spheres at runtime
    /// </summary>
    private void CreateFrozenVisuals()
    {
        // Create frozen LineRenderers
        frozenThumbLineRenderer = CreateFrozenLineRenderer("FrozenThumbLine");
        frozenIndexLineRenderer = CreateFrozenLineRenderer("FrozenIndexLine");
        frozenMiddleLineRenderer = CreateFrozenLineRenderer("FrozenMiddleLine");
        
        // Create frozen endpoint spheres
        frozenThumbTipSphere = CreateFrozenSphere("FrozenThumbTipSphere");
        frozenThumbBaseSphere = CreateFrozenSphere("FrozenThumbBaseSphere");
        frozenIndexTipSphere = CreateFrozenSphere("FrozenIndexTipSphere");
        frozenIndexBaseSphere = CreateFrozenSphere("FrozenIndexBaseSphere");
        frozenMiddleTipSphere = CreateFrozenSphere("FrozenMiddleTipSphere");
        frozenMiddleBaseSphere = CreateFrozenSphere("FrozenMiddleBaseSphere");
        
        // Debug.Log("[SelectMotorCollider] Frozen visuals auto-created (3 LineRenderers + 6 Spheres)");
    }
    
    private LineRenderer CreateFrozenLineRenderer(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(this.transform);
        
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = frozenLineWidth;
        lr.endWidth = frozenLineWidth;
        lr.useWorldSpace = true;
        lr.enabled = false;
        
        // Use Unlit color material
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = frozenLineColor;
        lr.endColor = frozenLineColor;
        
        return lr;
    }
    
    private Transform CreateFrozenSphere(string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(this.transform);
        go.transform.localScale = Vector3.one * frozenSphereRadius * 2f;
        
        // Remove collider so it doesn't interfere with physics
        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
        
        // Set color
        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(Shader.Find("Sprites/Default"));
            rend.material.color = frozenSphereColor;
        }
        
        go.SetActive(false);
        return go.transform;
    }
    
    /// <summary>
    /// Update the frozen line and endpoint sphere visuals
    /// </summary>
    private void UpdateFrozenVisuals()
    {
        if (!frozenCaptured || rWristTransform == null) return;
        
        Vector3 tipWorld = rWristTransform.TransformPoint(frozenTipLocalPos);
        Vector3 baseWorld = rWristTransform.TransformPoint(frozenBaseLocalPos);
        
        // Get the correct LineRenderer and spheres based on frozen finger
        LineRenderer lr = null;
        Transform tipSphere = null, baseSphere = null;
        
        switch (frozenFingertipID)
        {
            case 4:
                lr = frozenThumbLineRenderer;
                tipSphere = frozenThumbTipSphere;
                baseSphere = frozenThumbBaseSphere;
                break;
            case 8:
                lr = frozenIndexLineRenderer;
                tipSphere = frozenIndexTipSphere;
                baseSphere = frozenIndexBaseSphere;
                break;
            case 12:
                lr = frozenMiddleLineRenderer;
                tipSphere = frozenMiddleTipSphere;
                baseSphere = frozenMiddleBaseSphere;
                break;
        }
        
        // Update LineRenderer (respect showDebugLines toggle)
        if (lr != null)
        {
            if (showDebugLines)
            {
                lr.enabled = true;
                lr.positionCount = 2;
                lr.SetPosition(0, tipWorld);
                lr.SetPosition(1, baseWorld);
            }
            else
            {
                lr.enabled = false;
            }
        }
        
        // Update endpoint spheres
        if (showDebugSpheres)
        {
            if (tipSphere != null) { tipSphere.gameObject.SetActive(true); tipSphere.position = tipWorld; }
            if (baseSphere != null) { baseSphere.gameObject.SetActive(true); baseSphere.position = baseWorld; }
        }
        else
        {
            if (tipSphere != null) tipSphere.gameObject.SetActive(false);
            if (baseSphere != null) baseSphere.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Hide all frozen visuals (lines, spheres)
    /// </summary>
    private void HideFrozenVisuals()
    {
        if (frozenThumbLineRenderer != null) frozenThumbLineRenderer.enabled = false;
        if (frozenIndexLineRenderer != null) frozenIndexLineRenderer.enabled = false;
        if (frozenMiddleLineRenderer != null) frozenMiddleLineRenderer.enabled = false;
        
        if (frozenThumbTipSphere != null) frozenThumbTipSphere.gameObject.SetActive(false);
        if (frozenThumbBaseSphere != null) frozenThumbBaseSphere.gameObject.SetActive(false);
        if (frozenIndexTipSphere != null) frozenIndexTipSphere.gameObject.SetActive(false);
        if (frozenIndexBaseSphere != null) frozenIndexBaseSphere.gameObject.SetActive(false);
        if (frozenMiddleTipSphere != null) frozenMiddleTipSphere.gameObject.SetActive(false);
        if (frozenMiddleBaseSphere != null) frozenMiddleBaseSphere.gameObject.SetActive(false);
    }

    // ─── Freeze Motor Feature Methods ─────────────────────────────────────────

    /// <summary>
    /// Evaluates the freeze zone for a finger. Returns true if the touch is in the 0-20% freeze zone
    /// and motor selection should be skipped. Handles the edge-trigger toggle and starts the color lerp.
    /// If in the motor zone (>=20%), remaps the percentage so 20-100% maps to 0-100%.
    /// </summary>
    /// <summary>
    /// Returns true if a *different* finger currently owns a confirmed motor in ModeSwitching
    /// AND that finger's tip collider is still physically being touched.
    /// When true, this finger's projection should be skipped to enforce single-finger exclusivity
    /// (Problem 3 fix).
    /// </summary>
    private bool IsBlockedByOtherFinger(int thisMinMotor, int thisMaxMotor)
    {
        if (!enableFreezeMotorFeature || modeSwitching == null)
        {
            return false;  
        } 
        int cm = modeSwitching.confirmedMotorID;
        if (cm == 0) return false;
        if (cm >= thisMinMotor && cm <= thisMaxMotor) return false; // confirmed motor belongs to THIS finger
        // Another finger owns the confirmed motor — always block regardless of physical touch.
        // The block releases only when confirmedMotorID is cleared (freeze toggled off, or ClearConfirmedMotorForFinger called).
        return true;
    }

    /// <summary>
    /// Clears projection state for the finger identified by minMotor (1=thumb,5=index,9=middle).
    /// Called when mutual exclusion blocks the finger from processing.
    /// </summary>
    private void ClearFingerProjectionState(int minMotor)
    {
        if (minMotor == 1) // Thumb
        {
            if (thumbProjectionMotorID != 0 && activeTouchedMotorID == thumbProjectionMotorID)
            { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
            thumbProjectionMotorID = 0;
            thumbSegmentIndex = -1;
            thumbInFreezeZone = false;
            _thumbFreezeGateUnlocked = false;
            if (thumbRightFingerProjectionSphere != null) thumbRightFingerProjectionSphere.gameObject.SetActive(false);
            if (thumbClawProjectionSphere != null) thumbClawProjectionSphere.gameObject.SetActive(false);
        }
        else if (minMotor == 5) // Index
        {
            if (indexProjectionMotorID != 0 && activeTouchedMotorID == indexProjectionMotorID)
            { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
            indexProjectionMotorID = 0;
            indexSegmentIndex = -1;
            indexInFreezeZone = false;
            _indexFreezeGateUnlocked = false;
            if (indexRightFingerProjectionSphere != null) indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null) indexClawProjectionSphere.gameObject.SetActive(false);
        }
        else if (minMotor == 9) // Middle
        {
            if (middleProjectionMotorID != 0 && activeTouchedMotorID == middleProjectionMotorID)
            { activeTouchedMotorID = 0; activeTouchPosition = Vector3.zero; }
            middleProjectionMotorID = 0;
            middleSegmentIndex = -1;
            middleInFreezeZone = false;
            _middleFreezeGateUnlocked = false;
            if (middleRightFingerProjectionSphere != null) middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null) middleClawProjectionSphere.gameObject.SetActive(false);
        }
    }

    private bool HandleFreezeZone(
        float percentage,
        bool gateUnlocked,        // Problem 1: gate must be true (fingertip was confirmed) before toggle can fire
        ref bool freezeEnabled,
        ref bool freezeCanTrigger,
        ref bool suppressFlag,
        ref bool inFreezeZoneDebug,
        ref Color lerpStart,
        ref Color lerpTarget,
        ref float lerpT,
        Color offColor,
        ref bool pendingFlag,     // Hold-to-trigger: toggle commits only when lerp completes
        out float remappedPercentage)
    {
        remappedPercentage = percentage;

        bool inFreezeZone = percentage < 20f;
        inFreezeZoneDebug = inFreezeZone;

        if (inFreezeZone)
        {
            // Rising edge: begin lerp toward target but do NOT commit toggle yet
            if (gateUnlocked && freezeCanTrigger)
            {
                pendingFlag      = true;
                freezeCanTrigger = false;

                if (!freezeEnabled)
                {
                    lerpStart  = offColor;     // OFF → ON: original color toward yellow
                    lerpTarget = Color.yellow;
                }
                else
                {
                    lerpStart  = Color.yellow; // ON → OFF: yellow toward original color
                    lerpTarget = offColor;
                }
                lerpT = 0f;
            }

            // Commit toggle only when lerp finishes (finger held long enough)
            if (pendingFlag && lerpT >= 1f)
            {
                freezeEnabled = !freezeEnabled;
                pendingFlag   = false;
                suppressFlag  = true;
            }

            // In freeze zone — do not select any motor
            return true;
        }
        else
        {
            // Left freeze zone: if lerp not yet complete, cancel and reverse back
            if (pendingFlag)
            {
                Color current  = Color.Lerp(lerpStart, lerpTarget, lerpT);
                lerpStart  = current;
                lerpTarget = freezeEnabled ? Color.yellow : offColor; // revert toward where we came from
                lerpT      = 0f;
                pendingFlag  = false;
            }
            freezeCanTrigger = true;
            suppressFlag     = false;
            // Remap 20-100% → 0-100% so existing motor selection logic is unchanged
            remappedPercentage = Mathf.Clamp((percentage - 20f) / 80f * 100f, 0f, 100f);
            return false;
        }
    }

    /// <summary>
    /// Advances freeze color lerp animations and pushes colors to TriggerRight*Tip renderers.
    /// Also updates the suppressManipulateTransition flag.
    /// </summary>
    private void UpdateFreezeColors()
    {
        if (!enableFreezeMotorFeature)
        {
            if (triggerRightThumbTip  != null) triggerRightThumbTip.showFreezeColor  = false;
            if (triggerRightIndexTip  != null) triggerRightIndexTip.showFreezeColor  = false;
            if (triggerRightMiddleTip != null) triggerRightMiddleTip.showFreezeColor = false;
            suppressManipulateTransition = false;
            return;
        }

        const float lerpSpeed = 4f; // ~0.25 s to fully transition

        // When useFingertipFirst is active and a fingertip is confirmed, only the confirmed finger
        // may unlock its freeze gate, enter freeze zone, or show the freeze color animation.
        // Other fingers are fully excluded — their Paxini must stay at original color.
        // If a finger's freeze is already ON, always allow it — cross-finger confirmation must not clear it.
        bool thumbFreezeAllowed  = !useFingertipFirst || !isFingertipConfirmed || confirmedFingertipMotorID == 4  || thumbFreezeEnabled;
        bool indexFreezeAllowed  = !useFingertipFirst || !isFingertipConfirmed || confirmedFingertipMotorID == 8  || indexFreezeEnabled;
        bool middleFreezeAllowed = !useFingertipFirst || !isFingertipConfirmed || confirmedFingertipMotorID == 12 || middleFreezeEnabled;

        // Single-ON rule per finger (5 motors): if another motor in the same finger group is active,
        // Paxini (13/14/15) must be OFF and return to default color.
        bool thumbPaxiniBlockedByOtherMotor  = IsPaxiniBlockedByOtherMotor(1, 4, ThumbPaxiniMotorID);
        bool indexPaxiniBlockedByOtherMotor  = IsPaxiniBlockedByOtherMotor(5, 8, IndexPaxiniMotorID);
        bool middlePaxiniBlockedByOtherMotor = IsPaxiniBlockedByOtherMotor(9, 12, MiddlePaxiniMotorID);

        // Problem 1: Set freeze gate when fingertip motors (4/8/12) are actively selected
        if (thumbFreezeAllowed  && thumbProjectionMotorID == 4)   _thumbFreezeGateUnlocked  = true;
        if (indexFreezeAllowed  && indexProjectionMotorID == 8)   _indexFreezeGateUnlocked  = true;
        if (middleFreezeAllowed && middleProjectionMotorID == 12) _middleFreezeGateUnlocked = true;

        // Reset gate for non-allowed fingers so stale state doesn't persist
        if (!thumbFreezeAllowed)  _thumbFreezeGateUnlocked  = false;
        if (!indexFreezeAllowed)  _indexFreezeGateUnlocked  = false;
        if (!middleFreezeAllowed) _middleFreezeGateUnlocked = false;

        // Problem 2: While in the freeze zone with gate unlocked, clear the confirmed motor
        // so the dark-red color disappears (mirrors the behaviour of switching between other zones)
        if (modeSwitching != null)
        {
            if (thumbInFreezeZone  && _thumbFreezeGateUnlocked)  modeSwitching.ClearConfirmedMotorForFinger(1, 4);
            if (indexInFreezeZone  && _indexFreezeGateUnlocked)  modeSwitching.ClearConfirmedMotorForFinger(5, 8);
            if (middleInFreezeZone && _middleFreezeGateUnlocked) modeSwitching.ClearConfirmedMotorForFinger(9, 12);
        }

        // Cancel any pending transition if the finger lifted off entirely while in the freeze zone
        // (HandleFreezeZone's else branch handles normal exit; this covers the abrupt-lift case)
        if (!thumbInFreezeZone && _thumbFreezePending)
        {
            Color cur = Color.Lerp(_thumbFreezeLerpStart, _thumbFreezeLerpTarget, _thumbFreezeColorLerpT);
            _thumbFreezeLerpStart  = cur;
            _thumbFreezeLerpTarget = thumbFreezeEnabled ? Color.yellow : (triggerRightThumbTip != null ? triggerRightThumbTip.originalColor : Color.white);
            _thumbFreezeColorLerpT = 0f;
            _thumbFreezePending    = false;
            _thumbFreezeCanTrigger = true;
        }
        if (!indexInFreezeZone && _indexFreezePending)
        {
            Color cur = Color.Lerp(_indexFreezeLerpStart, _indexFreezeLerpTarget, _indexFreezeColorLerpT);
            _indexFreezeLerpStart  = cur;
            _indexFreezeLerpTarget = indexFreezeEnabled ? Color.yellow : (triggerRightIndexTip != null ? triggerRightIndexTip.originalColor : Color.white);
            _indexFreezeColorLerpT = 0f;
            _indexFreezePending    = false;
            _indexFreezeCanTrigger = true;
        }
        if (!middleInFreezeZone && _middleFreezePending)
        {
            Color cur = Color.Lerp(_middleFreezeLerpStart, _middleFreezeLerpTarget, _middleFreezeLerpColorT);
            _middleFreezeLerpStart  = cur;
            _middleFreezeLerpTarget = middleFreezeEnabled ? Color.yellow : (triggerRightMiddleTip != null ? triggerRightMiddleTip.originalColor : Color.white);
            _middleFreezeLerpColorT = 0f;
            _middleFreezePending    = false;
            _middleFreezeCanTrigger = true;
        }

        if (triggerRightThumbTip != null)
        {
            if (thumbFreezeAllowed && !thumbPaxiniBlockedByOtherMotor)
            {
                _thumbFreezeColorLerpT = Mathf.MoveTowards(_thumbFreezeColorLerpT, 1f, lerpSpeed * Time.deltaTime);
                triggerRightThumbTip.showFreezeColor = true;
                if (_thumbFreezeColorLerpT >= 1f && !thumbFreezeEnabled)
                    triggerRightThumbTip.freezeDisplayColor = triggerRightThumbTip.originalColor;
                else
                    triggerRightThumbTip.freezeDisplayColor = Color.Lerp(_thumbFreezeLerpStart, _thumbFreezeLerpTarget, _thumbFreezeColorLerpT);
            }
            else
            {
                triggerRightThumbTip.showFreezeColor = false;
                thumbFreezeEnabled = false;
                _thumbFreezePending = false;
                _thumbFreezeCanTrigger = true;
                _thumbFreezeLerpStart = triggerRightThumbTip.originalColor;
                _thumbFreezeLerpTarget = triggerRightThumbTip.originalColor;
                _thumbFreezeColorLerpT = 1f;
            }
        }

        if (triggerRightIndexTip != null)
        {
            if (indexFreezeAllowed && !indexPaxiniBlockedByOtherMotor)
            {
                _indexFreezeColorLerpT = Mathf.MoveTowards(_indexFreezeColorLerpT, 1f, lerpSpeed * Time.deltaTime);
                triggerRightIndexTip.showFreezeColor = true;
                if (_indexFreezeColorLerpT >= 1f && !indexFreezeEnabled)
                    triggerRightIndexTip.freezeDisplayColor = triggerRightIndexTip.originalColor;
                else
                    triggerRightIndexTip.freezeDisplayColor = Color.Lerp(_indexFreezeLerpStart, _indexFreezeLerpTarget, _indexFreezeColorLerpT);
            }
            else
            {
                triggerRightIndexTip.showFreezeColor = false;
                indexFreezeEnabled = false;
                _indexFreezePending = false;
                _indexFreezeCanTrigger = true;
                _indexFreezeLerpStart = triggerRightIndexTip.originalColor;
                _indexFreezeLerpTarget = triggerRightIndexTip.originalColor;
                _indexFreezeColorLerpT = 1f;
            }
        }

        if (triggerRightMiddleTip != null)
        {
            if (middleFreezeAllowed && !middlePaxiniBlockedByOtherMotor)
            {
                _middleFreezeLerpColorT = Mathf.MoveTowards(_middleFreezeLerpColorT, 1f, lerpSpeed * Time.deltaTime);
                triggerRightMiddleTip.showFreezeColor = true;
                if (_middleFreezeLerpColorT >= 1f && !middleFreezeEnabled)
                    triggerRightMiddleTip.freezeDisplayColor = triggerRightMiddleTip.originalColor;
                else
                    triggerRightMiddleTip.freezeDisplayColor = Color.Lerp(_middleFreezeLerpStart, _middleFreezeLerpTarget, _middleFreezeLerpColorT);
            }
            else
            {
                triggerRightMiddleTip.showFreezeColor = false;
                middleFreezeEnabled = false;
                _middleFreezePending = false;
                _middleFreezeCanTrigger = true;
                _middleFreezeLerpStart = triggerRightMiddleTip.originalColor;
                _middleFreezeLerpTarget = triggerRightMiddleTip.originalColor;
                _middleFreezeLerpColorT = 1f;
            }
        }

        suppressManipulateTransition = _suppressFromThumbFreeze || _suppressFromIndexFreeze || _suppressFromMiddleFreeze;
    }

    public void ForcePaxiniOffForMotor(int motorID)
    {
        if (motorID >= 1 && motorID <= 4)
        {
            _suppressThumbBulkUnfreeze = true;
            thumbFreezeEnabled = false;
            _thumbFreezePending = false;
            _thumbFreezeCanTrigger = true;
            _thumbFreezeLerpStart = triggerRightThumbTip != null ? triggerRightThumbTip.originalColor : Color.white;
            _thumbFreezeLerpTarget = _thumbFreezeLerpStart;
            _thumbFreezeColorLerpT = 1f;
            if (triggerRightThumbTip != null)
            {
                triggerRightThumbTip.showFreezeColor = false;
                triggerRightThumbTip.freezeDisplayColor = triggerRightThumbTip.originalColor;
                if (triggerRightThumbTip.thumbPaxiniRenderer != null)
                    triggerRightThumbTip.thumbPaxiniRenderer.material.color = triggerRightThumbTip.originalColor;
            }
        }
        else if (motorID >= 5 && motorID <= 8)
        {
            _suppressIndexBulkUnfreeze = true;
            indexFreezeEnabled = false;
            _indexFreezePending = false;
            _indexFreezeCanTrigger = true;
            _indexFreezeLerpStart = triggerRightIndexTip != null ? triggerRightIndexTip.originalColor : Color.white;
            _indexFreezeLerpTarget = _indexFreezeLerpStart;
            _indexFreezeColorLerpT = 1f;
            if (triggerRightIndexTip != null)
            {
                triggerRightIndexTip.showFreezeColor = false;
                triggerRightIndexTip.freezeDisplayColor = triggerRightIndexTip.originalColor;
                if (triggerRightIndexTip.indexPaxiniRenderer != null)
                    triggerRightIndexTip.indexPaxiniRenderer.material.color = triggerRightIndexTip.originalColor;
            }
        }
        else if (motorID >= 9 && motorID <= 12)
        {
            _suppressMiddleBulkUnfreeze = true;
            middleFreezeEnabled = false;
            _middleFreezePending = false;
            _middleFreezeCanTrigger = true;
            _middleFreezeLerpStart = triggerRightMiddleTip != null ? triggerRightMiddleTip.originalColor : Color.white;
            _middleFreezeLerpTarget = _middleFreezeLerpStart;
            _middleFreezeLerpColorT = 1f;
            if (triggerRightMiddleTip != null)
            {
                triggerRightMiddleTip.showFreezeColor = false;
                triggerRightMiddleTip.freezeDisplayColor = triggerRightMiddleTip.originalColor;
                if (triggerRightMiddleTip.middlePaxiniRenderer != null)
                    triggerRightMiddleTip.middlePaxiniRenderer.material.color = triggerRightMiddleTip.originalColor;
            }
        }
    }

    public void ForcePaxiniOnForMotor(int motorID)
    {
        if (motorID >= 1 && motorID <= 4)
        {
            _suppressThumbBulkFreeze = true;
            thumbFreezeEnabled = true;
            _thumbFreezePending = false;
            _thumbFreezeCanTrigger = true;
            _thumbFreezeLerpStart = Color.yellow;
            _thumbFreezeLerpTarget = Color.yellow;
            _thumbFreezeColorLerpT = 1f;
            if (triggerRightThumbTip != null)
            {
                triggerRightThumbTip.showFreezeColor = true;
                triggerRightThumbTip.freezeDisplayColor = Color.yellow;
                if (triggerRightThumbTip.thumbPaxiniRenderer != null)
                    triggerRightThumbTip.thumbPaxiniRenderer.material.color = Color.yellow;
            }
        }
        else if (motorID >= 5 && motorID <= 8)
        {
            _suppressIndexBulkFreeze = true;
            indexFreezeEnabled = true;
            _indexFreezePending = false;
            _indexFreezeCanTrigger = true;
            _indexFreezeLerpStart = Color.yellow;
            _indexFreezeLerpTarget = Color.yellow;
            _indexFreezeColorLerpT = 1f;
            if (triggerRightIndexTip != null)
            {
                triggerRightIndexTip.showFreezeColor = true;
                triggerRightIndexTip.freezeDisplayColor = Color.yellow;
                if (triggerRightIndexTip.indexPaxiniRenderer != null)
                    triggerRightIndexTip.indexPaxiniRenderer.material.color = Color.yellow;
            }
        }
        else if (motorID >= 9 && motorID <= 12)
        {
            _suppressMiddleBulkFreeze = true;
            middleFreezeEnabled = true;
            _middleFreezePending = false;
            _middleFreezeCanTrigger = true;
            _middleFreezeLerpStart = Color.yellow;
            _middleFreezeLerpTarget = Color.yellow;
            _middleFreezeLerpColorT = 1f;
            if (triggerRightMiddleTip != null)
            {
                triggerRightMiddleTip.showFreezeColor = true;
                triggerRightMiddleTip.freezeDisplayColor = Color.yellow;
                if (triggerRightMiddleTip.middlePaxiniRenderer != null)
                    triggerRightMiddleTip.middlePaxiniRenderer.material.color = Color.yellow;
            }
        }
    }

    private bool IsPaxiniBlockedByOtherMotor(int fingerMinMotor, int fingerMaxMotor, int paxiniMotorID)
    {
        // New behavior: do not auto-block Paxini from same-group motor confirmations.
        // Paxini state should only change when freeze itself is toggled.
        return false;
    }

    // Called by PriorityColliderDetector when L_IndexTipSmall enters a priority collider
    internal void OnPriorityEntered(int priorityType)
    {
        _fingerPriority    = priorityType;
        debugFingerPriority = priorityType;
    }

    // Called by PriorityColliderDetector when L_IndexTipSmall exits a priority collider
    // Priority is intentionally NOT reset on exit — it persists until the other collider is entered.
    internal void OnPriorityExited(int priorityType)
    {
        // Do nothing: priority stays until the opposing collider is entered.
    }
}

// Attached to thumb/index/middle priority colliders to detect L_IndexTipSmall contact
internal class PriorityColliderDetector : MonoBehaviour
{
    private int priorityType; // 1 = thumb priority, 2 = index priority, 3 = middle priority
    private string targetTag;
    private SelectMotorCollider manager;

    public void Initialize(int type, string tag, SelectMotorCollider managerRef)
    {
        priorityType = type;
        targetTag    = tag;
        manager      = managerRef;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag) && manager != null)
            manager.OnPriorityEntered(priorityType);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag) && manager != null)
            manager.OnPriorityExited(priorityType);
    }
}

// Helper component attached to each motor collider
internal class MotorTriggerDetector : MonoBehaviour
{
    private int motorID;
    private string targetTag;
    private SelectMotorCollider manager;
    private int touchCount = 0;
    private bool isActiveMotor = false;

    public void Initialize(int id, string tag, SelectMotorCollider managerRef)
    {
        motorID = id;
        targetTag = tag;
        manager = managerRef;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            touchCount++;
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ▶ TriggerEnter - touchCount: {touchCount}, isActive: {isActiveMotor}, Time: {Time.time:F3}, Object: {other.name}");
            if (touchCount == 1 && manager != null)
            {
                isActiveMotor = true;
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) 📞 Calling OnMotorTouched (setting isActive=true)");
                manager.OnMotorTouched(motorID, other.transform.position);
            }
            else if (touchCount > 1)
            {
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ⚠ TouchCount > 1, not calling OnMotorTouched");
            }
        }
        else
        {
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) TriggerEnter IGNORED - Wrong tag: {other.tag} (need: {targetTag}), Object: {other.name}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(targetTag) && touchCount > 0 && isActiveMotor && manager != null)
        {
            // Only log for motors 9-12 to reduce spam
            if (motorID >= 9)
            {
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) TriggerStay - updating position");
            }
            manager.OnMotorTouched(motorID, other.transform.position);
        }
        else if (other.CompareTag(targetTag))
        {
            // Log why we're not updating
            if (motorID >= 9)
            {
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) TriggerStay BLOCKED - touchCount: {touchCount}, isActive: {isActiveMotor}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            int prevCount = touchCount;
            touchCount = Mathf.Max(0, touchCount - 1);
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ◀ TriggerExit - touchCount: {prevCount} -> {touchCount}, isActive: {isActiveMotor}, Time: {Time.time:F3}, Object: {other.name}");
            if (touchCount == 0 && isActiveMotor && manager != null)
            {
                isActiveMotor = false;
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) 📞 Calling OnMotorReleased");
                manager.OnMotorReleased(motorID);
            }
            else if (touchCount == 0 && !isActiveMotor)
            {
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ⚠ TouchCount=0 but NOT active (was force released?)");
            }
        }
        else
        {
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) TriggerExit IGNORED - Wrong tag: {other.tag}, Object: {other.name}");
        }
    }

    // Force this detector to stop being active (called when another motor takes over)
    public void ForceRelease()
    {
        // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ForceRelease called - isActive: {isActiveMotor}, touchCount: {touchCount}");
        if (isActiveMotor)
        {
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ❌ FORCE RELEASED (touchCount still: {touchCount})");
            isActiveMotor = false;
            // Don't reset touchCount - the collider might still be physically touching
            // But we stop reporting to the manager
        }
        else
        {
            // Debug.LogWarning($"[Detector] Motor {motorID} (element[{motorID-1}]) ⚠ ForceRelease called but was NOT active! (touchCount: {touchCount})");
        }
    }
}
