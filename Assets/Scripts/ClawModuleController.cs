using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClawModuleController : MonoBehaviour
{
    // ==============================
    // 🔹 External References
    // ==============================
    public JointAngle jointAngle;
    public TriggerRightIndexTip triggerRightIndexTip;
    public TriggerRightMiddleTip triggerRightMiddleTip;
    public TriggerRightThumbTip triggerRightThumbTip;

    public TriggerRightThumbAbduction triggerRightThumbAbduction;

    public TriggerThumbInnerExtension triggerThumbInnerExtension;
    public TriggerIndexInnerExtension triggerIndexInnerExtension;

    public TriggerMiddleInnerExtension triggerMiddleInnerExtension;

    public ModeSwitching modeSwitching;
    public TcpSender tcpSender;

    public PaxiniValue paxiniValue;

    // public FingerSnapManager fingerSnapManager;

    // ==============================
    // 🔹 Thumb Transforms
    // ==============================
    public Transform ThumbAngle1Center;
    public Transform ThumbAngle2Center;
    public Transform ThumbAngle3Center;
    public Transform ThumbAngle4Center;

    // ==============================
    // 🔹 Thumb Renderer
    // ==============================
    public Renderer thumbJoint1Renderer;
    public Renderer thumbJoint2Renderer;

    public Renderer thumbJoint3Renderer;
    public Renderer thumbJoint4Renderer;

    // ==============================
    // 🔹 Index Finger Transforms
    // ==============================
    public Transform IndexAngle1Center;
    public Transform IndexAngle2Center;
    public Transform IndexAngle3Center;
    public Transform IndexAngle4Center;

    // ==============================
    // 🔹 Index Renderer
    // ==============================
    public Renderer indexJoint1Renderer;
    public Renderer indexJoint2Renderer;

    public Renderer indexJoint3Renderer;
    public Renderer indexJoint4Renderer;

    // ==============================
    // 🔹 Middle Finger Transforms
    // ==============================
    public Transform MiddleAngle1Center;
    public Transform MiddleAngle2Center;
    public Transform MiddleAngle3Center;
    public Transform MiddleAngle4Center;


    // ==============================
    // 🔹 Middle Renderer
    // ==============================
    public Renderer middleJoint1Renderer;
    public Renderer middleJoint2Renderer;

    public Renderer middleJoint3Renderer;
    public Renderer middleJoint4Renderer;

    // ==============================
    // 🔹 Configuration
    // ==============================
    private float rotationSpeed = 15f; // degrees per second
    public float twistRotationSpeed = 20f; // degrees per second (for twist operations)
    public bool isFullRangeMapping = true;

    // ==============================
    // 🔹 Colors
    // ==============================
    private Color originalColor;
    public Color redColor = Color.red;
    public Color yellowColor = new Color(1f, 0.9647f, 0f); // #FFF600
    public Color purpleColor = new Color(0.5f, 0f, 0.5f);
    // green color
    public Color greenColor = Color.green;

    // ==============================
    // 🔹 Thumb Finger State
    // ==============================
    private Quaternion ThumbAngle1CenterInitialRotation;
    private Quaternion ThumbAngle2CenterInitialRotation;

    public Vector3 thumbGripperJoint1MaxRotationVector;
    public Vector3 thumbGripperJoint1MinRotationVector;
    public Vector3 thumbGripperJoint2MaxRotationVector;
    public Vector3 thumbGripperJoint2MinRotationVector;

    public float currentThumbRotationY = 0f;
    public float currentThumbRotationYMax = 0f;
    public float currentThumbRotationYMin = 0f;
    public float currentThumbRotationZ = 0f;
    public float currentThumbRotationZMax = 0f;
    public float currentThumbRotationZMin = 0f;
    public bool hasThumbAbductionAdjustment = false;

    public float maxThumbYAxisAngle;
    public float minThumbYAxisAngle;
    public float maxThumbZAxisAngle;
    public float minThumbZAxisAngle;

    public float currentThumbTipRotationZ = 0f;

    public float currentThumbInnerExtensionRotationZ = 0f;


    // ==============================
    // 🔹 Index Finger State
    // ==============================
    private Quaternion IndexAngle1CenterInitialRotation;
    private Quaternion IndexAngle2CenterInitialRotation;

    public Vector3 indexGripperJoint1MaxRotationVector;
    public Vector3 indexGripperJoint1MinRotationVector;
    public Vector3 indexGripperJoint2MaxRotationVector;
    public Vector3 indexGripperJoint2MinRotationVector;

    public float currentIndexRotationYMax = 0f;
    public float currentIndexRotationYMin = 0f;
    public float currentIndexRotationZMax = 0f;
    public float currentIndexRotationZMin = 0f;

    public float maxIndexYAxisAngle;
    public float minIndexYAxisAngle;
    public float maxIndexZAxisAngle;
    public float minIndexZAxisAngle;

    public float currentIndexTipRotationZ = 0f;

    public float currentIndexInnerExtensionRotationZ = 0f;

    // ==============================
    // 🔹 Middle Finger State
    // ==============================
    private Quaternion MiddleAngle1CenterInitialRotation;
    private Quaternion MiddleAngle2CenterInitialRotation;

    public Vector3 middleGripperJoint1MaxRotationVector;
    public Vector3 middleGripperJoint1MinRotationVector;
    public Vector3 middleGripperJoint2MaxRotationVector;
    public Vector3 middleGripperJoint2MinRotationVector;

    public float currentMiddleRotationYMax = -60f;
    public float currentMiddleRotationYMin = 0f;
    public float currentMiddleRotationZ = 0f;
    public float currentMiddleRotationZMax = 0f;
    public float currentMiddleRotationZMin = 0f;

    public float currentMiddleRotationY
    {
        get
        {
            return currentMiddleRotationYMin > 0f
                ? currentMiddleRotationYMin
                : currentMiddleRotationYMax;
        }
        set
        {
            if (value > 0f)
            {
                currentMiddleRotationYMin = Mathf.Clamp(value, 0f, 90f);
                currentMiddleRotationYMax = -60f;
                middleGripperJoint1MinRotationVector = GetMiddleJoint1MinRotationVector();
            }
            else
            {
                currentMiddleRotationYMax = Mathf.Clamp(value, -90f, 0f);
                currentMiddleRotationYMin = 0f;
                middleGripperJoint1MaxRotationVector = GetMiddleJoint1MaxRotationVector();
            }

            maxMiddleYAxisAngle = NormalizeMiddleJoint1MaxAngle(middleGripperJoint1MaxRotationVector.y);
            minMiddleYAxisAngle = NormalizeAngle(middleGripperJoint1MinRotationVector.y);
            RefreshMiddleJoint1YDebug(value > 0f ? "currentMiddleRotationY:set:min" : "currentMiddleRotationY:set:max");
        }
    }

    public float maxMiddleYAxisAngle;
    public float minMiddleYAxisAngle;
    public float maxMiddleZAxisAngle;
    public float minMiddleZAxisAngle;

    public float currentMiddleTipRotationZ = 0f;

    public float currentMiddleInnerExtensionRotationZ = 0f;


    // ==============================
    // 🔹 which motor is being triggered now
    // ==============================

    public bool isThumb1Triggered = false;
    public bool isThumb2Triggered = false;
    public bool isThumb3Triggered = false;
    public bool isThumb4Triggered = false;

    public bool isIndex1Triggered = false;
    public bool isIndex2Triggered = false;
    public bool isIndex3Triggered = false;
    public bool isIndex4Triggered = false;

    public bool isMiddle1Triggered = false;
    public bool isMiddle2Triggered = false;
    public bool isMiddle3Triggered = false;
    public bool isMiddle4Triggered = false;

    public bool isAnyMotorTriggered = false;
    public bool isAnyMotor4Triggered = false;

    public bool canControlThumb1 = false;
    public bool canControlThumb2 = false;
    public bool canControlIndex1 = false;
    public bool canControlIndex2 = false;
    public bool canControlMiddle1 = false;
    public bool canControlMiddle2 = false;


    // Dictionary to track how long each fingertip has been touched
    private Dictionary<string, float> fingerTipTouchDurations = new Dictionary<string, float>();

    // Dictionary to track if a fingertip action has been activated (locked in)
    private Dictionary<string, bool> fingerTipActivated = new Dictionary<string, bool>();

    // Dictionaries to track joint angle changes (unified approach for all joints)
    private Dictionary<string, float> previousJointAngles = new Dictionary<string, float>();
    private Dictionary<string, float> previousMidJointAngles = new Dictionary<string, float>();
    private Dictionary<string, float> previousBaseJointAngles = new Dictionary<string, float>();
    public Dictionary<string, float> accumulatedJointChanges = new Dictionary<string, float>();
    private Dictionary<string, float> accumulatedMidJointChanges = new Dictionary<string, float>();
    private Dictionary<string, float> accumulatedBaseJointChanges = new Dictionary<string, float>();
    private Dictionary<string, float> previousAdditionalAngles = new Dictionary<string, float>();
    private Dictionary<string, float> accumulatedAdditionalChanges = new Dictionary<string, float>();

    public bool isFingerTipTriggered = false;
    public float totalAngleChange = 0f;
    public float jointAngleValueDebug = 0f;
    public float currentTipRotationDebug = 0f;
    public float mappedJointAngleValueDebug = 0f;
    public float finalAngleDebug = 0f;
    public Vector3 targetRotationEulerDebug = Vector3.zero;
    public float indexTargetYDebug = float.NaN;
    public float indexAbductionDeltaDebug = float.NaN;
    public float indexAbductionTargetZDebug = float.NaN;
    public string thumbAbductionDeltaNegativeDebug = "N/A";
    public string thumbAbductionDeltaPositiveDebug = "N/A";
    public string thumbPronation360ZoneDebug = "N/A";
    public string thumbPronationNon360ZoneDebug = "N/A";
    public string middleJoint1YDebugSource = "N/A";
    public float middleJoint1MaxRawYDebug = float.NaN;
    public float middleJoint1MinRawYDebug = float.NaN;
    public float middleJoint1MaxNormalizedYDebug = float.NaN;
    public float middleJoint1MinNormalizedYDebug = float.NaN;
    public float middleJoint1LocalEulerYDebug = float.NaN;

    public bool IsResetState
    {
        get
        {
            const float epsilon = 0.0001f;

            bool allAnglesAtReset = Mathf.Abs(currentThumbRotationY) <= epsilon &&
                Mathf.Abs(currentThumbRotationYMax) <= epsilon &&
                Mathf.Abs(currentThumbRotationYMin) <= epsilon &&
                Mathf.Abs(currentThumbRotationZ) <= epsilon &&
                Mathf.Abs(currentThumbRotationZMax) <= epsilon &&
                Mathf.Abs(currentThumbRotationZMin) <= epsilon &&
                Mathf.Abs(currentIndexRotationYMax) <= epsilon &&
                Mathf.Abs(currentIndexRotationYMin) <= epsilon &&
                Mathf.Abs(currentIndexRotationZMax) <= epsilon &&
                Mathf.Abs(currentIndexRotationZMin) <= epsilon &&
                Mathf.Abs(currentMiddleRotationYMax + 60f) <= epsilon &&
                Mathf.Abs(currentMiddleRotationYMin) <= epsilon &&
                Mathf.Abs(currentMiddleRotationZ) <= epsilon &&
                Mathf.Abs(currentMiddleRotationZMax) <= epsilon &&
                Mathf.Abs(currentMiddleRotationZMin) <= epsilon &&
                Mathf.Abs(currentThumbTipRotationZ) <= epsilon &&
                Mathf.Abs(currentIndexTipRotationZ) <= epsilon &&
                Mathf.Abs(currentMiddleTipRotationZ) <= epsilon &&
                Mathf.Abs(currentThumbInnerExtensionRotationZ) <= epsilon &&
                Mathf.Abs(currentIndexInnerExtensionRotationZ) <= epsilon &&
                Mathf.Abs(currentMiddleInnerExtensionRotationZ) <= epsilon &&
                !hasThumbAbductionAdjustment;

            if (!allAnglesAtReset)
            {
                return false;
            }

            if (modeSwitching != null && modeSwitching.modeManipulate)
            {
                return false;
            }

            if (IsAnyPaxiniYellowActive())
            {
                return false;
            }

            return true;
        }
    }

    private bool IsAnyPaxiniYellowActive()
    {
        SelectMotorCollider smc = (modeSwitching != null) ? modeSwitching.SelectMotorCollider : null;
        if (smc != null && (smc.thumbFreezeEnabled || smc.indexFreezeEnabled || smc.middleFreezeEnabled))
        {
            return true;
        }

        return IsTipShowingYellow(triggerRightThumbTip) ||
               IsTipShowingYellow(triggerRightIndexTip) ||
               IsTipShowingYellow(triggerRightMiddleTip);
    }

    private static bool IsTipShowingYellow(TriggerRightThumbTip tip)
    {
        return tip != null && tip.showFreezeColor && IsColorNearYellow(tip.freezeDisplayColor);
    }

    private static bool IsTipShowingYellow(TriggerRightIndexTip tip)
    {
        return tip != null && tip.showFreezeColor && IsColorNearYellow(tip.freezeDisplayColor);
    }

    private static bool IsTipShowingYellow(TriggerRightMiddleTip tip)
    {
        return tip != null && tip.showFreezeColor && IsColorNearYellow(tip.freezeDisplayColor);
    }

    private static bool IsColorNearYellow(Color color)
    {
        return color.r >= 0.95f && color.g >= 0.85f && color.b <= 0.2f;
    }

    // Track thumbPalmAngle changes for direction switching
    private float previousThumbPalmAngle = 0f;
    private float thumbPalmAngleChangeTimer = 0f;
    public bool isThumbRotatingNegative = true; // true = negative (original), false = positive
    private float thumbPalmAngleAtDirectionStart = 0f;

    // Sliding window detection for thumbPalmAngle changes
    private Queue<(float time, float angle)> thumbAngleHistory = new Queue<(float, float)>();
    private const float DETECTION_WINDOW = 0.2f;
    private const float DIRECTION_THRESHOLD = 5f;

    // Sliding window detection for indexMiddleAngleOnPalm changes
    private Queue<(float time, float angle)> indexAngleHistory = new Queue<(float, float)>();
    public bool isIndexRotatingNegative = true; // true = outward (negative), false = inward (positive)
    private bool hasIndexAbductionDirection = false;
    private bool hasIndexAbductionFirstDirection = false;
    private bool canRotateIndexAbductionThisTouch = false;
    private bool isIndexAbductionUsingMaxRangeThisTouch = true;
    private bool hasIndexPronationFirstDirection = false;
    private bool canRotateIndexPronationThisTouch = false;
    private bool isIndexPronationUsingMaxRangeThisTouch = true;
    private bool hasIndexMinInitialized = false;
    private bool hasMiddlePronationFirstDirection = false;
    private bool canRotateMiddlePronationThisTouch = false;
    private bool isMiddlePronationUsingMaxRangeThisTouch = true;
    private bool hasMiddleAbductionDirection = false;
    private bool hasMiddleAbductionFirstDirection = false;
    private bool canRotateMiddleAbductionThisTouch = false;
    private bool isMiddleAbductionUsingMaxRangeThisTouch = true;

    private bool hasThumbAbductionDirection = false;
    private bool hasThumbAbductionFirstDirection = false;
    private bool canRotateThumbAbductionThisTouch = false;
    private bool isThumbAbductionUsingMaxRangeThisTouch = true;

    private bool hasThumbPronationFirstDirection = false;
    private bool canRotateThumbPronationThisTouch = false;
    private bool isThumbPronationUsingMaxRangeThisTouch = true;
    private bool hasThumbMinInitialized = false;

    // Sliding window detection for middle finger indexMiddleAngleOnPalm changes
    private Queue<(float time, float angle)> middleAngleHistory = new Queue<(float, float)>();
    public bool isMiddleRotatingPositive = true; // true = positive (outward), false = negative (inward)

    // For snapping
    public bool thumbIndexInThumbRange = false; // 320-340 330
    public bool thumbIndexInIndexRange = false; // 20-40 30
    public bool thumbMiddleInThumbRange = false; // 20-40 30
    public bool thumbMiddleInMiddleRange = false; //0-30
    public bool indexMiddleInIndexRange = false;
    public bool indexMiddleInMiddleRange = false;

    // Snap state tracking
    private bool _thumbMotor1Locked = false;
    private Quaternion _thumbMotor1LockedRot;

    private bool _thumbMotor2Locked = false;
    private Quaternion _thumbMotor2LockedRot;

    private bool _thumbMotor3Locked = false;
    private Quaternion _thumbMotor3LockedRot;

    private bool _thumbMotor4Locked = false;
    private Quaternion _thumbMotor4LockedRot;

    private bool _indexMotor1Locked = false;
    private Quaternion _indexMotor1LockedRot;

    private bool _indexMotor2Locked = false;
    private Quaternion _indexMotor2LockedRot;

    private bool _indexMotor3Locked = false;
    private Quaternion _indexMotor3LockedRot;

    private bool _indexMotor4Locked = false;
    private Quaternion _indexMotor4LockedRot;

    private bool _middleMotor1Locked = false;
    private Quaternion _middleMotor1LockedRot;

    private bool _middleMotor2Locked = false;
    private Quaternion _middleMotor2LockedRot;

    private bool _middleMotor3Locked = false;
    private Quaternion _middleMotor3LockedRot;

    private bool _middleMotor4Locked = false;
    private Quaternion _middleMotor4LockedRot;

    // ==============================
    // 🔹 Manipulation Freeze State
    // ==============================
    // Track if we are actively manipulating (fingertip touched and green)
    public bool isActivelyManipulating = false;
    private bool _manipulationFreezeInitialized = false;

    // Locked rotations for ALL motors during manipulation
    private Quaternion _freezeThumbMotor1Rot;
    private Quaternion _freezeThumbMotor2Rot;
    private Quaternion _freezeThumbMotor3Rot;
    private Quaternion _freezeThumbMotor4Rot;
    private Quaternion _freezeIndexMotor1Rot;
    private Quaternion _freezeIndexMotor2Rot;
    private Quaternion _freezeIndexMotor3Rot;
    private Quaternion _freezeIndexMotor4Rot;
    private Quaternion _freezeMiddleMotor1Rot;
    private Quaternion _freezeMiddleMotor2Rot;
    private Quaternion _freezeMiddleMotor3Rot;
    private Quaternion _freezeMiddleMotor4Rot;

    // ==============================
    // 🔹 Keyboard Control
    // ==============================
    [Header("=== Keyboard Control ===")]
    [Tooltip("ON: Enable WASD+QE keyboard control for motor offsets")]
    public bool useKeyboardControl = false;

    [Header("=== Extension Clamp Range ===")]
    [Tooltip("Off: clamp X extension/tip to (-80, 50). On: clamp to (-90, 90).")]
    public bool useFullExtensionClampRange = false;
    [Header("=== Extension Full Range Mapping Mode ===")]
    [Tooltip("ON: inner-part extension (row 3) uses full range mapping formula")]
    public bool innerExtensionFullRangeMapping = true;
    [Tooltip("ON: tip-part extension (row 4) uses full range mapping formula")]
    public bool tipExtensionFullRangeMapping = false;
    [Header("=== Extension Offset ===")]
    [Tooltip("Positive value pushes extension joints further inward on X axis.")]
    public float extensionInwardOffsetDeg = 20f;
    [Header("=== Index & Middle Individual Mode ===")]
    [Tooltip("ON: Use indexToBaselineAngleOnPalm / middleToBaselineAngleOnPalm for index/middle abduction+pronation mapping.")]
    public bool useIndexMiddleIndividualMode = false;
    public float tt = 0f;

    private int kbCurrentRow = 3;
    private int kbCurrentCol = 1;
    private const int KB_ROWS = 4;
    private const int KB_COLS = 3;
    private Transform[,] kbMotorArray;
    private Renderer[,] kbRendererArray;
    private Renderer kbCurrentSelectedRenderer;
    private float kbRotationSpeed = 18f;
    private bool _prevUseKeyboardControl = false;

    // ==============================
    // 🔹 SMC Freeze Motor Feature State
    // ==============================
    private bool _smcThumbFreezeWasEnabled  = false;
    private bool _smcIndexFreezeWasEnabled  = false;
    private bool _smcMiddleFreezeWasEnabled = false;

    private Quaternion _smcFrozenThumbM1,  _smcFrozenThumbM2,  _smcFrozenThumbM3,  _smcFrozenThumbM4;
    private Quaternion _smcFrozenIndexM1,  _smcFrozenIndexM2,  _smcFrozenIndexM3,  _smcFrozenIndexM4;
    private Quaternion _smcFrozenMiddleM1, _smcFrozenMiddleM2, _smcFrozenMiddleM3, _smcFrozenMiddleM4;

    /// <summary>
    /// Awake runs before ALL Start() calls.
    /// Save originalColor here (before any Start() can change material colors).
    /// If keyboard mode is on, disable ModeSwitching so its Start()
    /// never runs and never sets joints to gray.
    /// </summary>
    void Awake()
    {
        if (!this.enabled) return;

        // Capture the TRUE original color before any Start() modifies materials
        if (thumbJoint1Renderer != null)
            originalColor = thumbJoint1Renderer.material.color;

        useIndexMiddleIndividualMode = false;

        if (useKeyboardControl && modeSwitching != null)
        {
            modeSwitching.enabled = false;
        }
    }

    void Start()
    {
        // --- Initialize Thumb ---
        ThumbAngle1CenterInitialRotation = ThumbAngle1Center.localRotation;
        ThumbAngle2CenterInitialRotation = ThumbAngle2Center.localRotation;
        thumbGripperJoint1MaxRotationVector = GetThumbJoint1MaxRotationVector();
        thumbGripperJoint1MinRotationVector =
            (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, 60f, 0f)).eulerAngles;
        thumbGripperJoint2MaxRotationVector = ThumbAngle2Center.localRotation.eulerAngles;
        if (thumbGripperJoint2MaxRotationVector.z < 1f) thumbGripperJoint2MaxRotationVector.z = 360f;
        thumbGripperJoint2MinRotationVector = ThumbAngle2Center.localRotation.eulerAngles;
        maxThumbYAxisAngle = ThumbAngle1CenterInitialRotation.eulerAngles.y;
        minThumbYAxisAngle = NormalizeAngle(thumbGripperJoint1MinRotationVector.y);
        maxThumbZAxisAngle = thumbGripperJoint2MaxRotationVector.z;

        // --- Initialize Index ---
        IndexAngle1CenterInitialRotation = IndexAngle1Center.localRotation;
        IndexAngle2CenterInitialRotation = IndexAngle2Center.localRotation;
        indexGripperJoint1MaxRotationVector = GetIndexJoint1MaxRotationVector();
        indexGripperJoint1MinRotationVector =
            (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, 60f, 0f)).eulerAngles;
        indexGripperJoint2MaxRotationVector = IndexAngle2Center.localRotation.eulerAngles;
        indexGripperJoint2MinRotationVector = IndexAngle2Center.localRotation.eulerAngles;
        maxIndexYAxisAngle = IndexAngle1CenterInitialRotation.eulerAngles.y;
        minIndexYAxisAngle = NormalizeAngle(indexGripperJoint1MinRotationVector.y);
        maxIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;
        minIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;

        // --- Initialize Middle ---
        MiddleAngle1CenterInitialRotation = MiddleAngle1Center.localRotation;
        MiddleAngle2CenterInitialRotation = MiddleAngle2Center.localRotation;
        currentMiddleRotationYMax = -60f;
        currentMiddleRotationYMin = 0f;
        currentMiddleRotationZMax = 0f;
        currentMiddleRotationZMin = 0f;
        middleGripperJoint1MaxRotationVector = GetMiddleJoint1MaxRotationVector();
        middleGripperJoint1MinRotationVector = GetMiddleJoint1MinRotationVector();
        middleGripperJoint2MaxRotationVector = GetMiddleJoint2MaxRotationVector();
        middleGripperJoint2MinRotationVector = MiddleAngle2Center.localRotation.eulerAngles;
        maxMiddleYAxisAngle = NormalizeMiddleJoint1MaxAngle(middleGripperJoint1MaxRotationVector.y);
        minMiddleYAxisAngle = NormalizeAngle(middleGripperJoint1MinRotationVector.y);
        maxMiddleZAxisAngle = MiddleAngle2CenterInitialRotation.eulerAngles.z;
        RefreshMiddleJoint1YDebug("Start");

        // --- Initialize Keyboard Control arrays (always, so runtime toggle works) ---
        kbMotorArray = new Transform[KB_ROWS, KB_COLS] {
            {ThumbAngle1Center, IndexAngle1Center, MiddleAngle1Center},
            {ThumbAngle2Center, IndexAngle2Center, MiddleAngle2Center},
            {ThumbAngle3Center, IndexAngle3Center, MiddleAngle3Center},
            {ThumbAngle4Center, IndexAngle4Center, MiddleAngle4Center}
        };
        kbRendererArray = new Renderer[KB_ROWS, KB_COLS] {
            {thumbJoint1Renderer, indexJoint1Renderer, middleJoint1Renderer},
            {thumbJoint2Renderer, indexJoint2Renderer, middleJoint2Renderer},
            {thumbJoint3Renderer, indexJoint3Renderer, middleJoint3Renderer},
            {thumbJoint4Renderer, indexJoint4Renderer, middleJoint4Renderer}
        };

        // If keyboard mode is already ON at start, enter it
        _prevUseKeyboardControl = useKeyboardControl;
        if (useKeyboardControl)
        {
            EnterKeyboardMode();
        }
    }

    void Update()
    {
        // Check if any fingertip extension is being touched (highest priority)
        // This checks if any fingertip is currently touched and meets initial conditions (lerping)
        isFingerTipTriggered = false;

        // Check thumb tip
        // if (triggerRightThumbTip.isRightThumbTipTouched && jointAngle.thumbAngle0 == 0f)
        // {
        //     isFingerTipTriggered = true;
        // }
        // // Check index tip
        // if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.joints.ContainsKey("Index0") && 
        //     jointAngle.joints["Index0"].localRotation.eulerAngles.z > 12.0 && jointAngle.joints["Index0"].localRotation.eulerAngles.z < 30.0)
        // {
        //     isFingerTipTriggered = true;
        // }
        // // Check middle tip
        // if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.joints.ContainsKey("Middle0") && 
        //     jointAngle.joints["Middle0"].localRotation.eulerAngles.z > 12.0 && jointAngle.joints["Middle0"].localRotation.eulerAngles.z < 30.0)
        // {
        //     isFingerTipTriggered = true;
        // }

        isAnyMotorTriggered = isThumb1Triggered || isThumb2Triggered || isThumb3Triggered || isThumb4Triggered ||
                             isIndex1Triggered || isIndex2Triggered || isIndex3Triggered || isIndex4Triggered ||
                             isMiddle1Triggered || isMiddle2Triggered || isMiddle3Triggered || isMiddle4Triggered;

        isAnyMotor4Triggered = isThumb4Triggered || isIndex4Triggered || isMiddle4Triggered;

        canControlThumb1 = !isThumb2Triggered && !isThumb3Triggered &&
                            !isIndex1Triggered && !isIndex2Triggered && !isIndex3Triggered &&
                           !isMiddle1Triggered && !isMiddle2Triggered && !isMiddle3Triggered;

        canControlThumb2 = !isThumb1Triggered && !isThumb3Triggered &&
                            !isIndex1Triggered && !isIndex2Triggered && !isIndex3Triggered &&
                           !isMiddle1Triggered && !isMiddle2Triggered && !isMiddle3Triggered;

        canControlIndex1 = !isThumb1Triggered && !isThumb2Triggered && !isThumb3Triggered &&
                            !isIndex2Triggered && !isIndex3Triggered &&
                           !isMiddle1Triggered && !isMiddle2Triggered && !isMiddle3Triggered;

        canControlIndex2 = !isThumb1Triggered && !isThumb2Triggered && !isThumb3Triggered &&
                            !isIndex1Triggered && !isIndex3Triggered && !isMiddle1Triggered && !isMiddle3Triggered;

        canControlMiddle1 = !isThumb1Triggered && !isThumb2Triggered && !isThumb3Triggered &&
                            !isIndex1Triggered && !isIndex2Triggered && !isIndex3Triggered &&
                           !isMiddle2Triggered && !isMiddle3Triggered;

        canControlMiddle2 = !isThumb1Triggered && !isThumb2Triggered && !isThumb3Triggered &&
                            !isIndex1Triggered && !isIndex3Triggered && !isMiddle1Triggered && !isMiddle3Triggered;

        // ==============================
        // 🔹 Manipulation Freeze Logic
        // ==============================
        // Detect if any fingertip is being touched and we're in manipulation mode
        bool anyFingertipTouched = triggerRightThumbTip.isRightThumbTipTouched ||
                                   triggerRightIndexTip.isRightIndexTipTouched ||
                                   triggerRightMiddleTip.isRightMiddleTipTouched;

        // Check if manipulation is active (in modeManipulate + fingertip touched)
        // This matches the "green" condition: fingertip turns green when touched in modeManipulate
        bool wasActivelyManipulating = isActivelyManipulating;
        isActivelyManipulating = modeSwitching.modeManipulate && anyFingertipTouched;

        // Initialize freeze rotations when manipulation starts
        if (isActivelyManipulating && !_manipulationFreezeInitialized)
        {
            _manipulationFreezeInitialized = true;

            // Store current rotations of all motors
            if (ThumbAngle1Center != null) _freezeThumbMotor1Rot = ThumbAngle1Center.localRotation;
            if (ThumbAngle2Center != null) _freezeThumbMotor2Rot = ThumbAngle2Center.localRotation;
            if (ThumbAngle3Center != null) _freezeThumbMotor3Rot = ThumbAngle3Center.localRotation;
            if (ThumbAngle4Center != null) _freezeThumbMotor4Rot = ThumbAngle4Center.localRotation;
            if (IndexAngle1Center != null) _freezeIndexMotor1Rot = IndexAngle1Center.localRotation;
            if (IndexAngle2Center != null) _freezeIndexMotor2Rot = IndexAngle2Center.localRotation;
            if (IndexAngle3Center != null) _freezeIndexMotor3Rot = IndexAngle3Center.localRotation;
            if (IndexAngle4Center != null) _freezeIndexMotor4Rot = IndexAngle4Center.localRotation;
            if (MiddleAngle1Center != null) _freezeMiddleMotor1Rot = MiddleAngle1Center.localRotation;
            if (MiddleAngle2Center != null) _freezeMiddleMotor2Rot = MiddleAngle2Center.localRotation;
            if (MiddleAngle3Center != null) _freezeMiddleMotor3Rot = MiddleAngle3Center.localRotation;
            if (MiddleAngle4Center != null) _freezeMiddleMotor4Rot = MiddleAngle4Center.localRotation;
        }

        // Reset freeze state when manipulation ends
        if (!isActivelyManipulating)
        {
            _manipulationFreezeInitialized = false;
        }

        HandleInput();

        // Detect keyboard toggle change at runtime
        if (useKeyboardControl != _prevUseKeyboardControl)
        {
            _prevUseKeyboardControl = useKeyboardControl;
            if (useKeyboardControl)
                EnterKeyboardMode();
            else
                ExitKeyboardMode();
        }

        // Keyboard control handling
        if (useKeyboardControl)
        {
            HandleKeyboardControl();
        }

        #region UpdateFingers
        // ==============================
        // 🔹 Thumb
        // ==============================

        UpdateFingertipExtension( // inner part
            triggerRightThumbTip.isRightThumbTipTouched,
            jointAngle.thumbAngle1,
            "Thumb1",
            "",
            "Thumb0",
            ref currentThumbInnerExtensionRotationZ,
            rotationSpeed,
            thumbJoint3Renderer,
            purpleColor,
            originalColor,
            ThumbAngle3Center,
            ref isThumb3Triggered,
            isIndex3Triggered || isMiddle3Triggered,
            ref _thumbMotor3Locked,
            ref _thumbMotor3LockedRot,
            modeSwitching.modeManipulate,
            modeSwitching.confirmedMotorID,
            3,  // Expected motor ID for thumb
            5.0f,  // Thumb requires 20 degree change
            jointAngle.thumbPalmAngle,  // Track thumbPalmAngle changes
            isFullRangeMapping, //innerExtensionFullRangeMapping,
            paxiniValue.isThumbTouchSnapped
        );

        UpdateFingertipExtension(
            triggerRightThumbTip.isRightThumbTipTouched,
            jointAngle.thumbAngle1,
            "Thumb1",
            "",
            "Thumb0",
            ref currentThumbTipRotationZ,
            rotationSpeed,
            thumbJoint4Renderer,
            purpleColor,
            originalColor,
            ThumbAngle4Center,
            ref isThumb4Triggered,
            isIndex4Triggered || isMiddle4Triggered,
            ref _thumbMotor4Locked,
            ref _thumbMotor4LockedRot,
            modeSwitching.modeManipulate,
            modeSwitching.confirmedMotorID,
            4,  // Expected motor ID for thumb
            5.0f,  // Thumb requires 20 degree change
            jointAngle.thumbPalmAngle,  // Track thumbPalmAngle changes
            isFullRangeMapping, //tipExtensionFullRangeMapping,
            paxiniValue.isThumbTouchSnapped
        );

        // UpdateThumbPronation();
        UpdateThumbPronationMaxMinMode();

        // UpdateThumAbduction();
        UpdateThumbFingerAbductionMaxMinMode();

        // ==============================
        // 🔹 Index Finger
        // ==============================

        // if (IndexAngle3Center != null)
        //     IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1 + jointAngle.indexAngle0, 0f, 0f);

        // UpdateIndexFingerAbductionByAngleByZ();
        UpdateIndexFingerAbductionMaxMinMode();
        // UpdateIndexFingerPronationByAngleByY();
        UpdateIndexFingerPronationMaxMinMode();

        UpdateFingertipExtension( // inner part
            triggerRightIndexTip.isRightIndexTipTouched,
            jointAngle.indexAngle1,
            "Index2",
            "Index1",
            "Index0",
            ref currentIndexInnerExtensionRotationZ,
            rotationSpeed,
            indexJoint3Renderer,
            purpleColor,
            originalColor,
            IndexAngle3Center,
            ref isIndex3Triggered,
            isThumb3Triggered || isMiddle3Triggered,
            ref _indexMotor3Locked,
            ref _indexMotor3LockedRot,
            modeSwitching.modeManipulate,
            modeSwitching.confirmedMotorID,
            7,
            5.0f,
            null,
            isFullRangeMapping, //innerExtensionFullRangeMapping,
            paxiniValue.isIndexTouchSnapped
        );

        UpdateFingertipExtension(
            triggerRightIndexTip.isRightIndexTipTouched,
            jointAngle.indexAngle2,
            "Index2",
            "Index1",
            "Index0",
            ref currentIndexTipRotationZ,
            rotationSpeed,
            indexJoint4Renderer,
            purpleColor,
            originalColor,
            IndexAngle4Center,
            ref isIndex4Triggered,
            isThumb4Triggered || isMiddle4Triggered,
            ref _indexMotor4Locked,
            ref _indexMotor4LockedRot,
            modeSwitching.modeManipulate,
            modeSwitching.confirmedMotorID,
            8,
            5.0f,
            null,
            isFullRangeMapping, //tipExtensionFullRangeMapping,
            paxiniValue.isIndexTouchSnapped
        );

        // ==============================
        // 🔹 Middle Finger State
        // ==============================

        // UpdateMiddleFingerAbductionByAngleByZ();
        UpdateMiddleFingerAbductionMaxMinMode();
        // UpdateMiddleFingerPronationByAngleByY();
        UpdateMiddleFingerPronationMaxMinMode();

        UpdateFingertipExtension(
            triggerRightMiddleTip.isRightMiddleTipTouched,
            jointAngle.middleAngle1,
            "Middle2",
            "Middle1",
            "Middle0",
            ref currentMiddleInnerExtensionRotationZ,
            rotationSpeed,
            middleJoint3Renderer,
            purpleColor,
            originalColor,
            MiddleAngle3Center,
            ref isMiddle3Triggered,
            isThumb3Triggered || isIndex3Triggered,
            ref _middleMotor3Locked,
            ref _middleMotor3LockedRot,
            modeSwitching.modeManipulate,
            modeSwitching.confirmedMotorID,
            11,
            5.0f,
            null,
            isFullRangeMapping, //innerExtensionFullRangeMapping,
            paxiniValue.isMiddleTouchSnapped
        );

        UpdateFingertipExtension(
            triggerRightMiddleTip.isRightMiddleTipTouched,
            jointAngle.middleAngle2,
            "Middle2",
            "Middle1",
            "Middle0",
            ref currentMiddleTipRotationZ,
            rotationSpeed,
            middleJoint4Renderer,
            purpleColor,
            originalColor,
            MiddleAngle4Center,
            ref isMiddle4Triggered,
            isThumb4Triggered || isIndex4Triggered,
            ref _middleMotor4Locked,
            ref _middleMotor4LockedRot,
            modeSwitching.modeManipulate,
            modeSwitching.confirmedMotorID,
            12,
            5.0f,
            null,
            isFullRangeMapping, //tipExtensionFullRangeMapping,
            paxiniValue.isMiddleTouchSnapped
        );
        #endregion

        // ==============================
        // 🔹 Apply Freeze to Non-Target Motors During Manipulation
        // ==============================
        if (isActivelyManipulating && _manipulationFreezeInitialized)
        {
            int targetMotorID = modeSwitching.confirmedMotorID;

            // Freeze all motors except the one being controlled
            // Motor IDs: 1=Thumb1, 2=Thumb2, 3=Thumb3, 4=Thumb4
            //            5=Index1, 6=Index2, 7=Index3, 8=Index4
            //            9=Middle1, 10=Middle2, 11=Middle3, 12=Middle4

            if (targetMotorID != 1 && ThumbAngle1Center != null)
                ThumbAngle1Center.localRotation = _freezeThumbMotor1Rot;
            if (targetMotorID != 2 && ThumbAngle2Center != null)
                ThumbAngle2Center.localRotation = _freezeThumbMotor2Rot;
            if (targetMotorID != 3 && ThumbAngle3Center != null)
                ThumbAngle3Center.localRotation = _freezeThumbMotor3Rot;
            if (targetMotorID != 4 && ThumbAngle4Center != null)
                ThumbAngle4Center.localRotation = _freezeThumbMotor4Rot;

            if (targetMotorID != 5 && IndexAngle1Center != null)
                IndexAngle1Center.localRotation = _freezeIndexMotor1Rot;
            if (targetMotorID != 6 && IndexAngle2Center != null)
                IndexAngle2Center.localRotation = _freezeIndexMotor2Rot;
            if (targetMotorID != 7 && IndexAngle3Center != null)
                IndexAngle3Center.localRotation = _freezeIndexMotor3Rot;
            if (targetMotorID != 8 && IndexAngle4Center != null)
                IndexAngle4Center.localRotation = _freezeIndexMotor4Rot;

            if (targetMotorID != 9 && MiddleAngle1Center != null)
                MiddleAngle1Center.localRotation = _freezeMiddleMotor1Rot;
            if (targetMotorID != 10 && MiddleAngle2Center != null)
                MiddleAngle2Center.localRotation = _freezeMiddleMotor2Rot;
            if (targetMotorID != 11 && MiddleAngle3Center != null)
                MiddleAngle3Center.localRotation = _freezeMiddleMotor3Rot;
            if (targetMotorID != 12 && MiddleAngle4Center != null)
                MiddleAngle4Center.localRotation = _freezeMiddleMotor4Rot;
        }

        // ==============================
        // 🔹 Apply SMC Finger Freeze
        // ==============================
        ApplySMCFreezeMotors();
    }

    // ==============================
    // 🔹 Input Handling
    // ==============================
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            isFullRangeMapping = !isFullRangeMapping;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetFingerRotations();
        }
    }

    // ==============================
    // 🔹 SMC Freeze Motor
    // ==============================
    /// <summary>
    /// Locks all 4 motors of a finger whose freeze flag is enabled in SelectMotorCollider.
    /// Captures the rotation on the rising edge and overrides it every frame while frozen.
    /// </summary>
    private void ApplySMCFreezeMotors()
    {
        if (modeSwitching == null || modeSwitching.SelectMotorCollider == null) return;
        SelectMotorCollider smc = modeSwitching.SelectMotorCollider;
        if (!smc.enableFreezeMotorFeature)
        {
            _smcThumbFreezeWasEnabled  = false;
            _smcIndexFreezeWasEnabled  = false;
            _smcMiddleFreezeWasEnabled = false;
            return;
        }

        // ── Thumb ──────────────────────────────────────────────────────────────────
        bool thumbFreeze = smc.thumbFreezeEnabled;
        if (thumbFreeze && !_smcThumbFreezeWasEnabled)
        {
            if (ThumbAngle1Center != null) _smcFrozenThumbM1 = ThumbAngle1Center.localRotation;
            if (ThumbAngle2Center != null) _smcFrozenThumbM2 = ThumbAngle2Center.localRotation;
            if (ThumbAngle3Center != null) _smcFrozenThumbM3 = ThumbAngle3Center.localRotation;
            if (ThumbAngle4Center != null) _smcFrozenThumbM4 = ThumbAngle4Center.localRotation;
        }
        _smcThumbFreezeWasEnabled = thumbFreeze;
        if (thumbFreeze)
        {
            if (ThumbAngle1Center != null) ThumbAngle1Center.localRotation = _smcFrozenThumbM1;
            if (ThumbAngle2Center != null) ThumbAngle2Center.localRotation = _smcFrozenThumbM2;
            if (ThumbAngle3Center != null) ThumbAngle3Center.localRotation = _smcFrozenThumbM3;
            if (ThumbAngle4Center != null) ThumbAngle4Center.localRotation = _smcFrozenThumbM4;
        }

        // ── Index ──────────────────────────────────────────────────────────────────
        bool indexFreeze = smc.indexFreezeEnabled;
        if (indexFreeze && !_smcIndexFreezeWasEnabled)
        {
            if (IndexAngle1Center != null) _smcFrozenIndexM1 = IndexAngle1Center.localRotation;
            if (IndexAngle2Center != null) _smcFrozenIndexM2 = IndexAngle2Center.localRotation;
            if (IndexAngle3Center != null) _smcFrozenIndexM3 = IndexAngle3Center.localRotation;
            if (IndexAngle4Center != null) _smcFrozenIndexM4 = IndexAngle4Center.localRotation;
        }
        _smcIndexFreezeWasEnabled = indexFreeze;
        if (indexFreeze)
        {
            if (IndexAngle1Center != null) IndexAngle1Center.localRotation = _smcFrozenIndexM1;
            if (IndexAngle2Center != null) IndexAngle2Center.localRotation = _smcFrozenIndexM2;
            if (IndexAngle3Center != null) IndexAngle3Center.localRotation = _smcFrozenIndexM3;
            if (IndexAngle4Center != null) IndexAngle4Center.localRotation = _smcFrozenIndexM4;
        }

        // ── Middle ─────────────────────────────────────────────────────────────────
        bool middleFreeze = smc.middleFreezeEnabled;
        if (middleFreeze && !_smcMiddleFreezeWasEnabled)
        {
            if (MiddleAngle1Center != null) _smcFrozenMiddleM1 = MiddleAngle1Center.localRotation;
            if (MiddleAngle2Center != null) _smcFrozenMiddleM2 = MiddleAngle2Center.localRotation;
            if (MiddleAngle3Center != null) _smcFrozenMiddleM3 = MiddleAngle3Center.localRotation;
            if (MiddleAngle4Center != null) _smcFrozenMiddleM4 = MiddleAngle4Center.localRotation;
        }
        _smcMiddleFreezeWasEnabled = middleFreeze;
        if (middleFreeze)
        {
            if (MiddleAngle1Center != null) MiddleAngle1Center.localRotation = _smcFrozenMiddleM1;
            if (MiddleAngle2Center != null) MiddleAngle2Center.localRotation = _smcFrozenMiddleM2;
            if (MiddleAngle3Center != null) MiddleAngle3Center.localRotation = _smcFrozenMiddleM3;
            if (MiddleAngle4Center != null) MiddleAngle4Center.localRotation = _smcFrozenMiddleM4;
        }
    }

    // #region @ThumbAbduction
    // void UpdateThumAbduction()
    // {
    //     Quaternion targetRotation = ThumbAngle2CenterInitialRotation;
    //     maxThumbZAxisAngle = NormalizeAngle(thumbGripperJoint2MaxRotationVector.z);

    //     // Initialize touch duration for thumb twist
    //     if (!fingerTipTouchDurations.ContainsKey("ThumbTwist"))
    //     {
    //         fingerTipTouchDurations["ThumbTwist"] = 0f;
    //     }

    //     if (!isFingerTipTriggered && triggerRightThumbTip.isRightThumbTipTouched && jointAngle.isPlaneActive
    //         && !isAnyMotor4Triggered && !isThumb1Triggered && canControlThumb2 && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 2) //  && jointAngle.thumbPalmAngle > 10f && jointAngle.thumbPalmAngle < 55f
    //     {
    //         fingerTipTouchDurations["ThumbTwist"] += Time.deltaTime;
    //         isThumb2Triggered = true;

    //         // Only apply rotation after 0.3 seconds
    //         if (fingerTipTouchDurations["ThumbTwist"] > 0.2f)
    //         {
    //             // Initialize tracking on first frame after 0.2 seconds
    //             if (fingerTipTouchDurations["ThumbTwist"] <= 0.2f + Time.deltaTime)
    //             {
    //                 thumbAngleHistory.Clear();
    //                 isThumbRotatingNegative = true;
    //             }

    //             float currentTime = Time.time;
    //             thumbAngleHistory.Enqueue((currentTime, jointAngle.thumbPalmAngle));

    //             if (thumbAngleHistory.Count > 0)
    //             {
    //                 float oldestTime = thumbAngleHistory.Peek().time;
    //                 float timeDiff = currentTime - oldestTime;

    //                 if (timeDiff > DETECTION_WINDOW + 0.1f)
    //                 {
    //                     while (thumbAngleHistory.Count > 1 &&
    //                            currentTime - thumbAngleHistory.Peek().time > DETECTION_WINDOW)
    //                     {
    //                         thumbAngleHistory.Dequeue();
    //                     }
    //                 }


    //                 if (timeDiff >= DETECTION_WINDOW)
    //                 {
    //                     float oldestAngle = thumbAngleHistory.Peek().angle;
    //                     float currentAngle = jointAngle.thumbPalmAngle;
    //                     float angleChange = currentAngle - oldestAngle;

    //                     bool previousDirection = isThumbRotatingNegative;

    //                     if (isThumbRotatingNegative && angleChange >= DIRECTION_THRESHOLD)
    //                     {
    //                         isThumbRotatingNegative = false;
    //                     }
    //                     else if (!isThumbRotatingNegative && angleChange <= -DIRECTION_THRESHOLD)
    //                     {
    //                         isThumbRotatingNegative = true;
    //                     }
    //                     else
    //                     {
    //                         // Debug.Log("aaaaaaaaaaaaaaaaaaa");
    //                     }
    //                 }
    //                 else
    //                 {
    //                     // Debug.Log("nnnnnnnnnnnnnnnnnnnnn");
    //                 }
    //             }

    //             if (isThumbRotatingNegative)
    //             {
    //                 currentThumbRotationZ += twistRotationSpeed * Time.deltaTime;
    //                 currentThumbRotationZ = Mathf.Clamp(currentThumbRotationZ, -90f, 90f);
    //             }
    //             else
    //             {
    //                 currentThumbRotationZ -= twistRotationSpeed * Time.deltaTime;
    //                 currentThumbRotationZ = Mathf.Clamp(currentThumbRotationZ, -90f, 90f);
    //             }

    //             hasThumbAbductionAdjustment = true;

    //             thumbGripperJoint2MaxRotationVector =
    //                 (ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentThumbRotationZ)).eulerAngles;
    //         }
    //     }
    //     else
    //     {
    //         fingerTipTouchDurations["ThumbTwist"] = 0f;
    //         isThumb2Triggered = false;
    //         thumbAngleHistory.Clear();
    //         isThumbRotatingNegative = true;
    //     }

    //     // Base angle from wrist-thumb angle - always apply this
    //     float baseAngle = 45f - jointAngle.wristThumbAngle;                  // float baseAngle = 30f - jointAngle.wristThumbAngle;

    //     if (hasThumbAbductionAdjustment)
    //     {
    //         targetRotation = ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, baseAngle + currentThumbRotationZ);
    //     }

    //     // mapping using wrist thumb angle
    //     float wristThumbAngleDiff = 45f - jointAngle.wristThumbAngle;           // float wristThumbAngleDiff = 30f - jointAngle.wristThumbAngle;

    //     if (currentThumbRotationZ >= 60)
    //     {
    //         float clampedwristThumbAngleDiff = Mathf.Clamp(wristThumbAngleDiff, 0f, 15f);
    //         float targetZ = Remap(0f, 15f, 60f, currentThumbRotationZ, clampedwristThumbAngleDiff);
    //         Debug.Log($"currentThumbRotationY: {currentThumbRotationZ:F4}, targetZ: {targetZ:F4}, clampedwristThumbAngleDiff: {clampedwristThumbAngleDiff:F4}");
    //         Vector3 euler = targetRotation.eulerAngles;
    //         targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //     }
    //     else if (hasThumbAbductionAdjustment && Mathf.Abs(wristThumbAngleDiff) > 0.1f)
    //     {
    //         // if(isFullRangeMapping)
    //         // {
    //         //     float delta = maxThumbZAxisAngle;
    //         //     if (delta <= 0)
    //         //     {
    //         //         float targetZ = baseAngle + thumbGripperJoint2MaxRotationVector.z - delta * (wristThumbAngleDiff / 45f);
    //         //         thumbAbductionDeltaNegativeDebug = $"delta<=0 targetZ: {targetZ:F4}, baseAngle: {baseAngle:F4}, thumbGripperJoint2MaxRotationVector.z: {thumbGripperJoint2MaxRotationVector.z:F4}, delta: {delta:F4}, wristThumbAngleDiff: {wristThumbAngleDiff:F4}";
    //         //         if (targetZ <= 300f && targetZ >= 200f) targetZ = 300f;
    //         //         if (targetZ >= 60f && targetZ <= 150f) targetZ = 60f;
    //         //         Vector3 euler = targetRotation.eulerAngles;
    //         //         targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //         //     }
    //         //     else
    //         //     {
    //         //         float targetZ = baseAngle + thumbGripperJoint2MaxRotationVector.z + delta * (wristThumbAngleDiff / 45f);
    //         //         thumbAbductionDeltaPositiveDebug = $"delta>0 targetZ: {targetZ:F4}, baseAngle: {baseAngle:F4}, thumbGripperJoint2MaxRotationVector.z: {thumbGripperJoint2MaxRotationVector.z:F4}, delta: {delta:F4}, wristThumbAngleDiff: {wristThumbAngleDiff:F4}";
    //         //         if (targetZ <= 300f && targetZ >= 200f) targetZ = 300f;
    //         //         if (targetZ >= 60f && targetZ <= 150f) targetZ = 60f;
    //         //         Vector3 euler = targetRotation.eulerAngles;
    //         //         targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //         //     }
    //         // }

    //         if (isFullRangeMapping)
    //         {
    //             float clampedwristThumbAngleDiff = Mathf.Clamp(wristThumbAngleDiff, 0f, 15f);
    //             // bool towardIndexFinger = thumbGripperJoint1MaxRotationVector.y < 100f && thumbGripperJoint1MaxRotationVector.y > 0f;
    //             // float startYUnwrapped = towardIndexFinger
    //             //     ? thumbGripperJoint1MaxRotationVector.y + 360f
    //             //     : thumbGripperJoint1MaxRotationVector.y;
    //             float delta = maxThumbZAxisAngle;

    //             if (currentThumbRotationZ <= 0)
    //             {
    //                 float targetZUnwrapped = Remap(0f, 15f, currentThumbRotationZ, 60f, clampedwristThumbAngleDiff);
    //                 float targetZ = Mathf.Repeat(targetZUnwrapped, 360f);

    //                 // thumbAbductionDeltaNegativeDebug = $"delta<=0 targetZ: {targetZ:F4}, baseAngle: {baseAngle:F4}, thumbGripperJoint2MaxRotationVector.z: {thumbGripperJoint2MaxRotationVector.z:F4}, delta: {delta:F4}, wristThumbAngleDiff: {wristThumbAngleDiff:F4}";
    //                 Vector3 euler = targetRotation.eulerAngles;
    //                 targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //             }
    //             else // toward index finger direction
    //             {
    //                 float targetZUnwrapped = Remap(0f, 15f, currentThumbRotationZ, 60f, clampedwristThumbAngleDiff);
    //                 float targetZ = Mathf.Repeat(targetZUnwrapped, 360f);
    //                 // thumbAbductionDeltaPositiveDebug = $"delta>0 targetZ: {targetZ:F4}, baseAngle: {baseAngle:F4}, thumbGripperJoint2MaxRotationVector.z: {thumbGripperJoint2MaxRotationVector.z:F4}, delta: {delta:F4}, wristThumbAngleDiff: {wristThumbAngleDiff:F4}";
    //                 Vector3 euler = targetRotation.eulerAngles;
    //                 targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //             }
    //         }
    //         else
    //         {
    //             float clampedwristThumbAngleDiff = Mathf.Clamp(wristThumbAngleDiff, 0f, 15f);
    //             float delta = maxThumbZAxisAngle;

    //             if (currentThumbRotationZ <= 0)
    //             {
    //                 float targetZUnwrapped = Remap(0f, 15f, currentThumbRotationZ, 60f + currentThumbRotationZ, clampedwristThumbAngleDiff);
    //                 float targetZ = Mathf.Repeat(targetZUnwrapped, 360f);

    //                 // thumbAbductionDeltaNegativeDebug = $"delta<=0 targetZ: {targetZ:F4}, baseAngle: {baseAngle:F4}, thumbGripperJoint2MaxRotationVector.z: {thumbGripperJoint2MaxRotationVector.z:F4}, delta: {delta:F4}, wristThumbAngleDiff: {wristThumbAngleDiff:F4}";
    //                 Vector3 euler = targetRotation.eulerAngles;
    //                 targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //             }
    //             else // toward index finger direction
    //             {
    //                 float targetZUnwrapped = Remap(0f, 15f, currentThumbRotationZ, 60f, clampedwristThumbAngleDiff);
    //                 float targetZ = Mathf.Repeat(targetZUnwrapped, 360f);
    //                 // thumbAbductionDeltaPositiveDebug = $"delta>0 targetZ: {targetZ:F4}, baseAngle: {baseAngle:F4}, thumbGripperJoint2MaxRotationVector.z: {thumbGripperJoint2MaxRotationVector.z:F4}, delta: {delta:F4}, wristThumbAngleDiff: {wristThumbAngleDiff:F4}";
    //                 Vector3 euler = targetRotation.eulerAngles;
    //                 targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //             }
    //         }
    //     }


    //     // snapping
    //     if (modeSwitching.modeSelect && paxiniValue.isThumbTouchSnapped)
    //     {
    //         if (!_thumbMotor2Locked && ThumbAngle2Center != null)
    //         {
    //             _thumbMotor2Locked = true;
    //             _thumbMotor2LockedRot = ThumbAngle2Center.localRotation;
    //         }

    //         if (ThumbAngle2Center != null)
    //             ThumbAngle2Center.localRotation = _thumbMotor2LockedRot;
    //     }
    //     else
    //     {
    //         _thumbMotor2Locked = false;

    //         if (ThumbAngle2Center != null)
    //             ThumbAngle2Center.localRotation = targetRotation;

    //         // Debug.Log("ThumbAngle2Center.localRotation.eulerAngles.z : " + ThumbAngle2Center.localRotation.eulerAngles.z);
    //     }
    // }
    // #endregion

    #region @ThumbAbdMaxMin
    void UpdateThumbFingerAbductionMaxMinMode()
    {
        Quaternion targetRotation = ThumbAngle2CenterInitialRotation;

        if (!fingerTipTouchDurations.ContainsKey("ThumbTwist"))
        {
            fingerTipTouchDurations["ThumbTwist"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightThumbTip.isRightThumbTipTouched && jointAngle.isPlaneActive
            && !isAnyMotor4Triggered && !isThumb1Triggered && canControlThumb2 && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 2)
        {
            fingerTipTouchDurations["ThumbTwist"] += Time.deltaTime;
            isThumb2Triggered = true;

            if (fingerTipTouchDurations["ThumbTwist"] > 0.2f)
            {
                if (fingerTipTouchDurations["ThumbTwist"] <= 0.2f + Time.deltaTime)
                {
                    thumbAngleHistory.Clear();
                    isThumbRotatingNegative = true;
                    hasThumbAbductionDirection = false;
                    hasThumbAbductionFirstDirection = false;
                    canRotateThumbAbductionThisTouch = false;
                    isThumbAbductionUsingMaxRangeThisTouch = true;
                }

                float currentTime = Time.time;
                thumbAngleHistory.Enqueue((currentTime, jointAngle.thumbPalmAngle));

                if (thumbAngleHistory.Count > 0)
                {
                    float oldestTime = thumbAngleHistory.Peek().time;
                    float timeDiff = currentTime - oldestTime;

                    if (timeDiff > DETECTION_WINDOW + 0.1f)
                    {
                        while (thumbAngleHistory.Count > 1 &&
                               currentTime - thumbAngleHistory.Peek().time > DETECTION_WINDOW)
                        {
                            thumbAngleHistory.Dequeue();
                        }
                    }

                    if (timeDiff >= DETECTION_WINDOW)
                    {
                        float oldestAngle = thumbAngleHistory.Peek().angle;
                        float currentAngle = jointAngle.thumbPalmAngle;
                        float angleChange = currentAngle - oldestAngle;

                        if (angleChange >= DIRECTION_THRESHOLD)
                        {
                            isThumbRotatingNegative = false;

                            if (!hasThumbAbductionFirstDirection)
                            {
                                hasThumbAbductionFirstDirection = true;
                                canRotateThumbAbductionThisTouch = true;
                                isThumbAbductionUsingMaxRangeThisTouch = true;
                                currentThumbRotationZMax = Mathf.Clamp(currentThumbRotationZMax, -90f, 0f);
                            }

                            hasThumbAbductionDirection = true;
                        }
                        else if (angleChange <= -DIRECTION_THRESHOLD)
                        {
                            isThumbRotatingNegative = true;

                            if (!hasThumbAbductionFirstDirection)
                            {
                                hasThumbAbductionFirstDirection = true;
                                canRotateThumbAbductionThisTouch = true;
                                isThumbAbductionUsingMaxRangeThisTouch = false;
                                currentThumbRotationZMin = Mathf.Clamp(currentThumbRotationZMin, 0f, 90f);
                            }

                            hasThumbAbductionDirection = true;
                        }
                    }
                }

                if (canRotateThumbAbductionThisTouch && hasThumbAbductionDirection)
                {
                    if (isThumbAbductionUsingMaxRangeThisTouch)
                    {
                        if (!isThumbRotatingNegative)
                            currentThumbRotationZMax -= twistRotationSpeed * Time.deltaTime;
                        else
                            currentThumbRotationZMax += twistRotationSpeed * Time.deltaTime;
                        currentThumbRotationZMax = Mathf.Clamp(currentThumbRotationZMax, -90f, 0f);
                        thumbGripperJoint2MaxRotationVector =
                            (ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentThumbRotationZMax)).eulerAngles;
                        if (thumbGripperJoint2MaxRotationVector.z < 1f) thumbGripperJoint2MaxRotationVector.z = 360f;
                        maxThumbZAxisAngle = thumbGripperJoint2MaxRotationVector.z;
                    }
                    else
                    {
                        if (isThumbRotatingNegative)
                            currentThumbRotationZMin += twistRotationSpeed * Time.deltaTime;
                        else
                            currentThumbRotationZMin -= twistRotationSpeed * Time.deltaTime;
                        currentThumbRotationZMin = Mathf.Clamp(currentThumbRotationZMin, 0f, 90f);
                        thumbGripperJoint2MinRotationVector =
                            (ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentThumbRotationZMin)).eulerAngles;
                        minThumbZAxisAngle = thumbGripperJoint2MinRotationVector.z;
                    }

                    hasThumbAbductionAdjustment = true;
                }
            }
        }
        else
        {
            fingerTipTouchDurations["ThumbTwist"] = 0f;
            isThumb2Triggered = false;
            thumbAngleHistory.Clear();
            isThumbRotatingNegative = true;
            hasThumbAbductionDirection = false;
            hasThumbAbductionFirstDirection = false;
            canRotateThumbAbductionThisTouch = false;
        }

        float currentThumbRotationZForTarget = hasThumbAbductionAdjustment
            ? (isThumbAbductionUsingMaxRangeThisTouch ? currentThumbRotationZMax : currentThumbRotationZMin)
            : 0f;

        targetRotation *= Quaternion.Euler(0f, 0f, currentThumbRotationZForTarget);

        // Base angle from wrist-thumb angle - always apply this
        float baseAngle = 45f - jointAngle.wristThumbAngle;

        if (hasThumbAbductionAdjustment)
        {
            targetRotation = ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, baseAngle + currentThumbRotationZForTarget);
        }

        // mapping using wrist thumb angle
        float wristThumbAngleDiff = 45f - jointAngle.wristThumbAngle;

        if (hasThumbAbductionAdjustment)
        {
            if (isFullRangeMapping)
            {
                float clampedwristThumbAngleDiff = Mathf.Clamp(wristThumbAngleDiff, 0f, 15f);
                float delta = maxThumbZAxisAngle;
                float targetZ;
                targetZ = Remap(0f, 15f, thumbGripperJoint2MaxRotationVector.z, 360 + thumbGripperJoint2MinRotationVector.z, clampedwristThumbAngleDiff);
                targetZ = Mathf.Repeat(targetZ, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
            }
            else
            {
                float clampedwristThumbAngleDiff = Mathf.Clamp(wristThumbAngleDiff, 0f, 15f);
                float delta = maxThumbZAxisAngle;
                float targetZ;
                float rightestPos = thumbGripperJoint2MaxRotationVector.z + 60f >= 360 + thumbGripperJoint2MinRotationVector.z ? 360 + thumbGripperJoint2MinRotationVector.z : thumbGripperJoint2MaxRotationVector.z + 60f;
                targetZ = Remap(0f, 15f, thumbGripperJoint2MaxRotationVector.z, rightestPos, clampedwristThumbAngleDiff);
                targetZ = Mathf.Repeat(targetZ, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
            }
        }

        // snapping
        if (modeSwitching.modeSelect && paxiniValue.isThumbTouchSnapped)
        {
            if (!_thumbMotor2Locked && ThumbAngle2Center != null)
            {
                _thumbMotor2Locked = true;
                _thumbMotor2LockedRot = ThumbAngle2Center.localRotation;
            }

            if (ThumbAngle2Center != null)
                ThumbAngle2Center.localRotation = _thumbMotor2LockedRot;
        }
        else
        {
            _thumbMotor2Locked = false;

            if (ThumbAngle2Center != null)
                ThumbAngle2Center.localRotation = targetRotation;
        }
    }
    #endregion

    // #region @IndexAbdByZ
    // /// <summary>
    // /// Controls Index finger Z-axis abduction (swapped from Y-axis), motorID == 6
    // /// </summary>
    // private void UpdateIndexFingerAbductionByAngleByZ()
    // {
    //     maxIndexZAxisAngle = NormalizeAngle(indexGripperJoint2MaxRotationVector.z);
    //     Quaternion targetRotation = IndexAngle2CenterInitialRotation;

    //     // Initialize touch duration for index abduction Z
    //     if (!fingerTipTouchDurations.ContainsKey("IndexAbductionZ"))
    //     {
    //         fingerTipTouchDurations["IndexAbductionZ"] = 0f;
    //     }

    //     if (!isFingerTipTriggered && triggerRightIndexTip.isRightIndexTipTouched
    //          && !isAnyMotor4Triggered && canControlIndex2 && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 6)
    //     {
    //         fingerTipTouchDurations["IndexAbductionZ"] += Time.deltaTime;
    //         isIndex2Triggered = true;

    //         // Only apply rotation after 0.2 second
    //         if (fingerTipTouchDurations["IndexAbductionZ"] > 0.2f)
    //         {
    //             // Initialize tracking on first frame after 0.2 second
    //             if (fingerTipTouchDurations["IndexAbductionZ"] <= 0.2f + Time.deltaTime)
    //             {
    //                 indexAngleHistory.Clear();
    //                 isIndexRotatingNegative = true;
    //             }

    //             float currentTime = Time.time;
    //             indexAngleHistory.Enqueue((currentTime, jointAngle.indexMiddleAngleOnPalm));

    //             // Clean up old entries while keeping at least one reference point
    //             if (indexAngleHistory.Count > 0)
    //             {
    //                 float oldestTime = indexAngleHistory.Peek().time;
    //                 float timeDiff = currentTime - oldestTime;

    //                 if (timeDiff > DETECTION_WINDOW + 0.1f)
    //                 {
    //                     while (indexAngleHistory.Count > 1 &&
    //                            currentTime - indexAngleHistory.Peek().time > DETECTION_WINDOW)
    //                     {
    //                         indexAngleHistory.Dequeue();
    //                     }
    //                 }

    //                 // Check if we have enough history to detect direction change
    //                 if (timeDiff >= DETECTION_WINDOW)
    //                 {
    //                     float oldestAngle = indexAngleHistory.Peek().angle;
    //                     float currentAngle = jointAngle.indexMiddleAngleOnPalm;
    //                     float angleChange = currentAngle - oldestAngle;

    //                     // If angle increased by >= 5 degrees, switch to negative direction (outward)
    //                     if (angleChange >= DIRECTION_THRESHOLD)
    //                     {
    //                         isIndexRotatingNegative = true;
    //                     }
    //                     // If angle decreased by >= 5 degrees, switch to positive direction (inward)
    //                     else if (angleChange <= -DIRECTION_THRESHOLD)
    //                     {
    //                         isIndexRotatingNegative = false;
    //                     }
    //                     // Otherwise, keep current direction
    //                 }
    //             }

    //             // Apply rotation based on current direction (Z-axis)
    //             if (isIndexRotatingNegative)
    //             {
    //                 currentIndexRotationZMax -= rotationSpeed * Time.deltaTime;
    //                 // currentIndexRotationZMax = Mathf.Clamp(currentIndexRotationZMax, -58f, 0f);
    //                 currentIndexRotationZMax = Mathf.Clamp(currentIndexRotationZMax, -58f, 58f);
    //             }
    //             else
    //             {
    //                 currentIndexRotationZMax += rotationSpeed * Time.deltaTime;
    //                 // currentIndexRotationZMax = Mathf.Clamp(currentIndexRotationZMax, -58f, 0f);
    //                 currentIndexRotationZMax = Mathf.Clamp(currentIndexRotationZMax, -58f, 58f);
    //             }

    //             indexGripperJoint2MaxRotationVector =
    //                 (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZMax)).eulerAngles;
    //         }
    //     }
    //     else
    //     {
    //         fingerTipTouchDurations["IndexAbductionZ"] = 0f;
    //         isIndex2Triggered = false;
    //         indexAngleHistory.Clear();
    //         isIndexRotatingNegative = true;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, 0f, currentIndexRotationZMax);
    //     indexAbductionDeltaDebug = float.NaN;
    //     indexAbductionTargetZDebug = float.NaN;

    //     //FIXME: abduction remapping
    //     if (IndexAngle2Center != null)
    //     {
    //         // float delta = maxIndexZAxisAngle;
    //         // float targetZ = indexGripperJoint2MaxRotationVector.z - delta * ((57f - jointAngle.indexMiddleAngleOnPalm) / 24f);
    //         // if (targetZ >= 360f) targetZ = 0.1f;

    //         // indexAbductionDeltaDebug = delta;
    //         // indexAbductionTargetZDebug = targetZ;

    //         // Vector3 euler = targetRotation.eulerAngles;
    //         // targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);

    //         if (isFullRangeMapping)
    //         {
    //             float delta = maxIndexZAxisAngle;
    //             float targetZ = 0f;

    //             if (delta <= 0) targetZ = Remap(20, 57, 360, 360 + delta, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57)); // 360 + delta
    //             else targetZ = Remap(20, 57, delta, 0, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));

    //             indexAbductionDeltaDebug = delta;
    //             indexAbductionTargetZDebug = targetZ;

    //             Vector3 euler = targetRotation.eulerAngles;
    //             targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //         }
    //         else
    //         {
    //             float delta = maxIndexZAxisAngle;
    //             float targetZ = 0f;

    //             if (delta <= 0) targetZ = Remap(20, 57, 360, 360 + delta, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57)); // 360 + delta
    //             else targetZ = Remap(20, 57, delta, 0, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));

    //             indexAbductionDeltaDebug = delta;
    //             indexAbductionTargetZDebug = targetZ;

    //             Vector3 euler = targetRotation.eulerAngles;
    //             targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //         }
    //     }

    //     // snapping
    //     if (modeSwitching.modeSelect && paxiniValue.isIndexTouchSnapped)
    //     {
    //         if (!_indexMotor2Locked && IndexAngle2Center != null)
    //         {
    //             _indexMotor2Locked = true;
    //             _indexMotor2LockedRot = IndexAngle2Center.localRotation;
    //         }

    //         if (IndexAngle2Center != null)
    //             IndexAngle2Center.localRotation = _indexMotor2LockedRot;
    //     }
    //     else
    //     {
    //         _indexMotor2Locked = false;

    //         if (IndexAngle2Center != null)
    //             IndexAngle2Center.localRotation = targetRotation;
    //     }
    // }
    // #endregion

    #region @IndexAbdMaxMin
    /// <summary>
    /// Controls Index finger Z-axis abduction (swapped from Y-axis), motorID == 6
    /// </summary>
    private void UpdateIndexFingerAbductionMaxMinMode()
    {
        // maxIndexZAxisAngle = NormalizeAngle(indexGripperJoint2MaxRotationVector.z);
        Quaternion targetRotation = IndexAngle2CenterInitialRotation;

        // Initialize touch duration for index abduction Z
        if (!fingerTipTouchDurations.ContainsKey("IndexAbductionZ"))
        {
            fingerTipTouchDurations["IndexAbductionZ"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightIndexTip.isRightIndexTipTouched
             && !isAnyMotor4Triggered && canControlIndex2 && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 6)
        {
            fingerTipTouchDurations["IndexAbductionZ"] += Time.deltaTime;
            isIndex2Triggered = true;

            // Only apply rotation after 0.2 second
            if (fingerTipTouchDurations["IndexAbductionZ"] > 0.2f)
            {
                // Initialize tracking on first frame after 0.2 second
                if (fingerTipTouchDurations["IndexAbductionZ"] <= 0.2f + Time.deltaTime)
                {
                    indexAngleHistory.Clear();
                    hasIndexAbductionDirection = false;
                    hasIndexAbductionFirstDirection = false;
                    canRotateIndexAbductionThisTouch = false;
                    isIndexAbductionUsingMaxRangeThisTouch = true;
                }

                float currentTime = Time.time;
                indexAngleHistory.Enqueue((currentTime, jointAngle.indexMiddleAngleOnPalm));

                // Clean up old entries while keeping at least one reference point
                if (indexAngleHistory.Count > 0)
                {
                    float oldestTime = indexAngleHistory.Peek().time;
                    float timeDiff = currentTime - oldestTime;

                    if (timeDiff > DETECTION_WINDOW + 0.1f)
                    {
                        while (indexAngleHistory.Count > 1 &&
                               currentTime - indexAngleHistory.Peek().time > DETECTION_WINDOW)
                        {
                            indexAngleHistory.Dequeue();
                        }
                    }

                    // Check if we have enough history to detect direction change
                    if (timeDiff >= DETECTION_WINDOW)
                    {
                        float oldestAngle = indexAngleHistory.Peek().angle;
                        float currentAngle = jointAngle.indexMiddleAngleOnPalm;
                        float angleChange = currentAngle - oldestAngle;

                        // If angle increased by >= 5 degrees, switch to negative direction (outward)
                        if (angleChange >= DIRECTION_THRESHOLD)
                        {
                            isIndexRotatingNegative = true;

                            if (!hasIndexAbductionFirstDirection)
                            {
                                hasIndexAbductionFirstDirection = true;
                                canRotateIndexAbductionThisTouch = true;
                                isIndexAbductionUsingMaxRangeThisTouch = true;
                                currentIndexRotationZMax = Mathf.Clamp(currentIndexRotationZMax, -58f, 0f);
                            }

                            hasIndexAbductionDirection = true;
                        }
                        // If angle decreased by >= 5 degrees, switch to positive direction (inward)
                        else if (angleChange <= -DIRECTION_THRESHOLD)
                        {
                            isIndexRotatingNegative = false;

                            if (!hasIndexAbductionFirstDirection)
                            {
                                hasIndexAbductionFirstDirection = true;
                                canRotateIndexAbductionThisTouch = true;
                                isIndexAbductionUsingMaxRangeThisTouch = false;
                                currentIndexRotationZMin = Mathf.Clamp(currentIndexRotationZMin, 0f, 58f);
                            }

                            hasIndexAbductionDirection = true;
                        }
                    }
                }

                // Keep rotating with the last detected direction for this touch session.
                if (canRotateIndexAbductionThisTouch && hasIndexAbductionDirection)
                {
                    if (isIndexRotatingNegative)
                    {
                        if (isIndexAbductionUsingMaxRangeThisTouch)
                            currentIndexRotationZMax -= rotationSpeed * Time.deltaTime;
                        else
                            currentIndexRotationZMin -= rotationSpeed * Time.deltaTime;
                    }
                    else
                    {
                        if (isIndexAbductionUsingMaxRangeThisTouch)
                            currentIndexRotationZMax += rotationSpeed * Time.deltaTime;
                        else
                            currentIndexRotationZMin += rotationSpeed * Time.deltaTime;
                    }

                    if (isIndexAbductionUsingMaxRangeThisTouch)
                    {
                        currentIndexRotationZMax = Mathf.Clamp(currentIndexRotationZMax, -90f, 0f);
                        indexGripperJoint2MaxRotationVector =
                            (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZMax)).eulerAngles;
                        maxIndexZAxisAngle = indexGripperJoint2MaxRotationVector.z;
                    }
                    else
                    {
                        currentIndexRotationZMin = Mathf.Clamp(currentIndexRotationZMin, 0f, 90f);
                        indexGripperJoint2MinRotationVector =
                            (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZMin)).eulerAngles;
                        minIndexZAxisAngle = indexGripperJoint2MinRotationVector.z;
                    }
                }
            }
        }
        else
        {
            fingerTipTouchDurations["IndexAbductionZ"] = 0f;
            isIndex2Triggered = false;
            indexAngleHistory.Clear();
            isIndexRotatingNegative = true;
            hasIndexAbductionDirection = false;
            hasIndexAbductionFirstDirection = false;
            canRotateIndexAbductionThisTouch = false;
            isIndexAbductionUsingMaxRangeThisTouch = true;
        }

        float currentIndexRotationZForTarget = currentIndexRotationZMin > 0f
            ? currentIndexRotationZMin
            : currentIndexRotationZMax;
        targetRotation *= Quaternion.Euler(0f, 0f, currentIndexRotationZForTarget);
        indexAbductionDeltaDebug = float.NaN;
        indexAbductionTargetZDebug = float.NaN;

        //FIXME: abduction remapping
        if (IndexAngle2Center != null)
        {
            float indexRemapMin = useIndexMiddleIndividualMode ? 15f : 20f;
            float indexRemapMax = useIndexMiddleIndividualMode ? 30f : 57f;
            float clampedIndexAngleOnPalm = useIndexMiddleIndividualMode
                ? Mathf.Clamp(jointAngle.indexToBaselineAngleOnPalm, indexRemapMin, indexRemapMax)
                : Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, indexRemapMin, indexRemapMax);

            if (isFullRangeMapping)
            {
                float targetZ = 0f;

                if (indexGripperJoint2MaxRotationVector.z > 200f)
                    targetZ = Remap(indexRemapMin, indexRemapMax, 360 + indexGripperJoint2MinRotationVector.z, indexGripperJoint2MaxRotationVector.z, clampedIndexAngleOnPalm);
                else
                    targetZ = Remap(indexRemapMin, indexRemapMax, indexGripperJoint2MinRotationVector.z, 0, clampedIndexAngleOnPalm);

                targetZ = Mathf.Repeat(targetZ, 360f);

                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
            }
            else
            {
                float targetZ = 0f;

                if (indexGripperJoint2MaxRotationVector.z > 200f)
                {
                    float rightestPos = (indexGripperJoint2MaxRotationVector.z + 60) > 360f + indexGripperJoint2MinRotationVector.z
                        ? 360f + indexGripperJoint2MinRotationVector.z
                        : indexGripperJoint2MaxRotationVector.z + 60f;
                    targetZ = Remap(indexRemapMin, indexRemapMax, rightestPos, indexGripperJoint2MaxRotationVector.z, clampedIndexAngleOnPalm);
                }
                else
                    targetZ = Remap(indexRemapMin, indexRemapMax, indexGripperJoint2MinRotationVector.z, 0, clampedIndexAngleOnPalm);

                targetZ = Mathf.Repeat(targetZ, 360f);

                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
            }
        }

        // snapping
        if (modeSwitching.modeSelect && paxiniValue.isIndexTouchSnapped)
        {
            if (!_indexMotor2Locked && IndexAngle2Center != null)
            {
                _indexMotor2Locked = true;
                _indexMotor2LockedRot = IndexAngle2Center.localRotation;
            }

            if (IndexAngle2Center != null)
                IndexAngle2Center.localRotation = _indexMotor2LockedRot;
        }
        else
        {
            _indexMotor2Locked = false;

            if (IndexAngle2Center != null)
                IndexAngle2Center.localRotation = targetRotation;
        }
    }
    #endregion

    // #region @MiddleAbdByZ
    // /// <summary>
    // /// Controls Middle finger Z-axis abduction (swapped from Y-axis), motorID == 10
    // /// </summary>
    // void UpdateMiddleFingerAbductionByAngleByZ()
    // {
    //     maxMiddleZAxisAngle = NormalizeAngle(middleGripperJoint2MaxRotationVector.z);
    //     Quaternion targetRotation = MiddleAngle2CenterInitialRotation;

    //     // Initialize touch duration for middle abduction Z
    //     if (!fingerTipTouchDurations.ContainsKey("MiddleAbductionZ"))
    //     {
    //         fingerTipTouchDurations["MiddleAbductionZ"] = 0f;
    //     }

    //     if (!isFingerTipTriggered && triggerRightMiddleTip.isRightMiddleTipTouched
    //          && !isAnyMotor4Triggered && canControlMiddle2 && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 10)
    //     {
    //         fingerTipTouchDurations["MiddleAbductionZ"] += Time.deltaTime;
    //         isMiddle2Triggered = true;

    //         // Only apply rotation after 0.2 second
    //         if (fingerTipTouchDurations["MiddleAbductionZ"] > 0.2f)
    //         {
    //             // Initialize tracking on first frame after 0.2 second
    //             if (fingerTipTouchDurations["MiddleAbductionZ"] <= 0.2f + Time.deltaTime)
    //             {
    //                 middleAngleHistory.Clear();
    //                 isMiddleRotatingPositive = true;
    //             }

    //             float currentTime = Time.time;
    //             middleAngleHistory.Enqueue((currentTime, jointAngle.indexMiddleAngleOnPalm));

    //             // Clean up old entries while keeping at least one reference point
    //             if (middleAngleHistory.Count > 0)
    //             {
    //                 float oldestTime = middleAngleHistory.Peek().time;
    //                 float timeDiff = currentTime - oldestTime;

    //                 if (timeDiff > DETECTION_WINDOW + 0.1f)
    //                 {
    //                     while (middleAngleHistory.Count > 1 &&
    //                            currentTime - middleAngleHistory.Peek().time > DETECTION_WINDOW)
    //                     {
    //                         middleAngleHistory.Dequeue();
    //                     }
    //                 }

    //                 // Check if we have enough history to detect direction change
    //                 if (timeDiff >= DETECTION_WINDOW)
    //                 {
    //                     float oldestAngle = middleAngleHistory.Peek().angle;
    //                     float currentAngle = jointAngle.indexMiddleAngleOnPalm;
    //                     float angleChange = currentAngle - oldestAngle;

    //                     // If angle increased by >= 5 degrees, switch to positive direction (+=, outward)
    //                     if (angleChange >= DIRECTION_THRESHOLD)
    //                     {
    //                         isMiddleRotatingPositive = true;
    //                     }
    //                     // If angle decreased by >= 5 degrees, switch to negative direction (-=, inward)
    //                     else if (angleChange <= -DIRECTION_THRESHOLD)
    //                     {
    //                         isMiddleRotatingPositive = false;
    //                     }
    //                     // Otherwise, keep current direction
    //                 }
    //             }

    //             // Apply rotation based on current direction (Z-axis)
    //             if (isMiddleRotatingPositive)
    //             {
    //                 currentMiddleRotationZ += rotationSpeed * Time.deltaTime;
    //                 // currentMiddleRotationZ = Mathf.Clamp(currentMiddleRotationZ, 0f, 58f);
    //                 currentMiddleRotationZ = Mathf.Clamp(currentMiddleRotationZ, -58f, 58f);
    //             }
    //             else
    //             {
    //                 currentMiddleRotationZ -= rotationSpeed * Time.deltaTime;
    //                 // currentMiddleRotationZ = Mathf.Clamp(currentMiddleRotationZ, 0f, 58f);
    //                 currentMiddleRotationZ = Mathf.Clamp(currentMiddleRotationZ, -58f, 58f);
    //             }

    //             middleGripperJoint2MaxRotationVector =
    //                 (MiddleAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentMiddleRotationZ)).eulerAngles;
    //         }
    //     }
    //     else
    //     {
    //         fingerTipTouchDurations["MiddleAbductionZ"] = 0f;
    //         isMiddle2Triggered = false;
    //         middleAngleHistory.Clear();
    //         isMiddleRotatingPositive = true;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, 0f, currentMiddleRotationZ);

    //     //FIXME: abduction remapping
    //     if (MiddleAngle2Center != null)
    //     {
    //         // float delta = maxMiddleZAxisAngle;

    //         // float targetZ = middleGripperJoint2MaxRotationVector.z - delta * ((57f - jointAngle.indexMiddleAngleOnPalm) / 24f);
    //         // if (targetZ <= 0f) targetZ = 0f;

    //         // Vector3 euler = targetRotation.eulerAngles;
    //         // targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);

    //         if (isFullRangeMapping)
    //         {
    //             float delta = maxMiddleZAxisAngle;
    //             float targetZ = 0f;

    //             if (delta >= 0) targetZ = Remap(20, 57, 360, 360 + delta, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57)); // 360 + delta
    //             else targetZ = Remap(20, 57, delta, 0, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));

    //             // indexAbductionDeltaDebug = delta;
    //             // indexAbductionTargetZDebug = targetZ;

    //             Vector3 euler = targetRotation.eulerAngles;
    //             targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //         }
    //     }

    //     // snapping
    //     if (modeSwitching.modeSelect && paxiniValue.isMiddleTouchSnapped)
    //     {
    //         if (!_middleMotor2Locked && MiddleAngle2Center != null)
    //         {
    //             _middleMotor2Locked = true;
    //             _middleMotor2LockedRot = MiddleAngle2Center.localRotation;
    //         }

    //         if (MiddleAngle2Center != null)
    //             MiddleAngle2Center.localRotation = _middleMotor2LockedRot;
    //     }
    //     else
    //     {
    //         _middleMotor2Locked = false;

    //         if (MiddleAngle2Center != null)
    //             MiddleAngle2Center.localRotation = targetRotation;
    //     }
    // }
    // #endregion

    #region @MiddleAbdMaxMin
    /// <summary>
    /// Controls Middle finger Z-axis abduction (swapped from Y-axis), motorID == 10
    /// </summary>
    void UpdateMiddleFingerAbductionMaxMinMode()
    {
        Quaternion targetRotation = MiddleAngle2CenterInitialRotation;

        // Initialize touch duration for middle abduction Z
        if (!fingerTipTouchDurations.ContainsKey("MiddleAbductionZ"))
        {
            fingerTipTouchDurations["MiddleAbductionZ"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightMiddleTip.isRightMiddleTipTouched
             && !isAnyMotor4Triggered && canControlMiddle2 && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 10)
        {
            fingerTipTouchDurations["MiddleAbductionZ"] += Time.deltaTime;
            isMiddle2Triggered = true;

            // Only apply rotation after 0.2 second
            if (fingerTipTouchDurations["MiddleAbductionZ"] > 0.2f)
            {
                // Initialize tracking on first frame after 0.2 second
                if (fingerTipTouchDurations["MiddleAbductionZ"] <= 0.2f + Time.deltaTime)
                {
                    middleAngleHistory.Clear();
                    hasMiddleAbductionDirection = false;
                    hasMiddleAbductionFirstDirection = false;
                    canRotateMiddleAbductionThisTouch = false;
                    isMiddleAbductionUsingMaxRangeThisTouch = true;
                }

                float currentTime = Time.time;
                middleAngleHistory.Enqueue((currentTime, jointAngle.indexMiddleAngleOnPalm));

                // Clean up old entries while keeping at least one reference point
                if (middleAngleHistory.Count > 0)
                {
                    float oldestTime = middleAngleHistory.Peek().time;
                    float timeDiff = currentTime - oldestTime;

                    if (timeDiff > DETECTION_WINDOW + 0.1f)
                    {
                        while (middleAngleHistory.Count > 1 &&
                               currentTime - middleAngleHistory.Peek().time > DETECTION_WINDOW)
                        {
                            middleAngleHistory.Dequeue();
                        }
                    }

                    // Check if we have enough history to detect direction change
                    if (timeDiff >= DETECTION_WINDOW)
                    {
                        float oldestAngle = middleAngleHistory.Peek().angle;
                        float currentAngle = jointAngle.indexMiddleAngleOnPalm;
                        float angleChange = currentAngle - oldestAngle;

                        // angleChange >= DIRECTION_THRESHOLD → adjust currentMiddleRotationZMin (range 0 to 90)
                        if (angleChange >= DIRECTION_THRESHOLD)
                        {
                            isMiddleRotatingPositive = true;

                            if (!hasMiddleAbductionFirstDirection)
                            {
                                hasMiddleAbductionFirstDirection = true;
                                canRotateMiddleAbductionThisTouch = true;
                                isMiddleAbductionUsingMaxRangeThisTouch = false;
                                currentMiddleRotationZMin = Mathf.Clamp(currentMiddleRotationZMin, 0f, 90f);
                            }

                            hasMiddleAbductionDirection = true;
                        }
                        // angleChange <= -DIRECTION_THRESHOLD → adjust currentMiddleRotationZMax (range -90 to 0)
                        else if (angleChange <= -DIRECTION_THRESHOLD)
                        {
                            isMiddleRotatingPositive = false;

                            if (!hasMiddleAbductionFirstDirection)
                            {
                                hasMiddleAbductionFirstDirection = true;
                                canRotateMiddleAbductionThisTouch = true;
                                isMiddleAbductionUsingMaxRangeThisTouch = true;
                                currentMiddleRotationZMax = Mathf.Clamp(currentMiddleRotationZMax, -90f, 0f);
                            }

                            hasMiddleAbductionDirection = true;
                        }
                    }
                }

                // Keep rotating with the last detected direction for this touch session.
                if (canRotateMiddleAbductionThisTouch && hasMiddleAbductionDirection)
                {
                    if (isMiddleRotatingPositive)
                    {
                        if (isMiddleAbductionUsingMaxRangeThisTouch)
                            currentMiddleRotationZMax += rotationSpeed * Time.deltaTime;
                        else
                            currentMiddleRotationZMin += rotationSpeed * Time.deltaTime;
                    }
                    else
                    {
                        if (isMiddleAbductionUsingMaxRangeThisTouch)
                            currentMiddleRotationZMax -= rotationSpeed * Time.deltaTime;
                        else
                            currentMiddleRotationZMin -= rotationSpeed * Time.deltaTime;
                    }

                    if (isMiddleAbductionUsingMaxRangeThisTouch)
                    {
                        currentMiddleRotationZMax = Mathf.Clamp(currentMiddleRotationZMax, -90f, 0f);
                        middleGripperJoint2MaxRotationVector = GetMiddleJoint2MaxRotationVector();
                        maxMiddleZAxisAngle = middleGripperJoint2MaxRotationVector.z;
                    }
                    else
                    {
                        currentMiddleRotationZMin = Mathf.Clamp(currentMiddleRotationZMin, 0f, 90f);
                        middleGripperJoint2MinRotationVector =
                            (MiddleAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentMiddleRotationZMin)).eulerAngles;
                        minMiddleZAxisAngle = middleGripperJoint2MinRotationVector.z;
                    }
                }
            }
        }
        else
        {
            fingerTipTouchDurations["MiddleAbductionZ"] = 0f;
            isMiddle2Triggered = false;
            middleAngleHistory.Clear();
            isMiddleRotatingPositive = true;
            hasMiddleAbductionDirection = false;
            hasMiddleAbductionFirstDirection = false;
            canRotateMiddleAbductionThisTouch = false;
            isMiddleAbductionUsingMaxRangeThisTouch = true;
        }

        float currentMiddleRotationZForTarget = currentMiddleRotationZMin > 0f
            ? currentMiddleRotationZMin
            : currentMiddleRotationZMax;
        targetRotation *= Quaternion.Euler(0f, 0f, currentMiddleRotationZForTarget);

        if (MiddleAngle2Center != null)
        {
            float middleRemapMin = useIndexMiddleIndividualMode ? 20f : 20f;
            float middleRemapMax = useIndexMiddleIndividualMode ? 35f : 57f;
            float clampedMiddleAngleOnPalm = useIndexMiddleIndividualMode
                ? Mathf.Clamp(jointAngle.middleToBaselineAngleOnPalm, middleRemapMin, middleRemapMax)
                : Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, middleRemapMin, middleRemapMax);

            if (isFullRangeMapping)
            {
                // At angleOnPalm==57 → middleGripperJoint2MaxRotationVector.z
                // At angleOnPalm==20 → 360 + middleGripperJoint2MinRotationVector.z
                float targetZ = Remap(middleRemapMin, middleRemapMax, middleGripperJoint2MaxRotationVector.z, 360 + middleGripperJoint2MinRotationVector.z, clampedMiddleAngleOnPalm);
                targetZ = Mathf.Repeat(targetZ, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
            }
            else
            {
                float leftestPos = 360 + middleGripperJoint2MinRotationVector.z - 60f < middleGripperJoint2MaxRotationVector.z
                    ? middleGripperJoint2MaxRotationVector.z
                    : 360 + middleGripperJoint2MinRotationVector.z - 60f;
                float targetZ = Remap(middleRemapMin, middleRemapMax, leftestPos, 360 + middleGripperJoint2MinRotationVector.z, clampedMiddleAngleOnPalm);
                targetZ = Mathf.Repeat(targetZ, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
            }
        }

        // snapping
        if (modeSwitching.modeSelect && paxiniValue.isMiddleTouchSnapped)
        {
            if (!_middleMotor2Locked && MiddleAngle2Center != null)
            {
                _middleMotor2Locked = true;
                _middleMotor2LockedRot = MiddleAngle2Center.localRotation;
            }

            if (MiddleAngle2Center != null)
                MiddleAngle2Center.localRotation = _middleMotor2LockedRot;
        }
        else
        {
            _middleMotor2Locked = false;

            if (MiddleAngle2Center != null)
                MiddleAngle2Center.localRotation = targetRotation;
        }
    }
    #endregion

    // #region @ThumbPronation
    // void UpdateThumbPronation()
    // {
    //     Quaternion targetRotation = ThumbAngle1CenterInitialRotation;
    //     maxThumbYAxisAngle = NormalizeAngle(thumbGripperJoint1MaxRotationVector.y);

    //     // Initialize touch duration for thumb abduction
    //     if (!fingerTipTouchDurations.ContainsKey("ThumbAbduction"))
    //     {
    //         fingerTipTouchDurations["ThumbAbduction"] = 0f;
    //     }

    //     if (!isFingerTipTriggered && triggerRightThumbTip.isRightThumbTipTouched
    //          && !isAnyMotor4Triggered && !isThumb2Triggered && canControlThumb1
    //          && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 1)
    //     {
    //         fingerTipTouchDurations["ThumbAbduction"] += Time.deltaTime;
    //         // thumbJoint1Renderer.material.color = Color.Lerp(originalColor, greenColor, Mathf.Min(fingerTipTouchDurations["ThumbAbduction"] / 0.7f, 1f));
    //         isThumb1Triggered = true;

    //         if (fingerTipTouchDurations["ThumbAbduction"] > 0.2f)
    //         {
    //             currentThumbRotationY -= (-jointAngle.isClockWise) * rotationSpeed * Time.deltaTime;
    //             currentThumbRotationY = Mathf.Clamp(currentThumbRotationY, -90f, 90f);

    //             thumbGripperJoint1MaxRotationVector =
    //                 (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, currentThumbRotationY, 0f)).eulerAngles;

    //             // thumbJoint1Renderer.material.color = greenColor;
    //         }
    //     }
    //     else
    //     {
    //         fingerTipTouchDurations["ThumbAbduction"] = 0f;
    //         // thumbJoint1Renderer.material.color = originalColor;
    //         isThumb1Triggered = false;
    //     }

    //     // Base angle from thumb-palm angle
    //     float baseAngle = 45f - jointAngle.thumbPalmAngle;

    //     targetRotation *= Quaternion.Euler(0f, baseAngle + currentThumbRotationY, 0f);

    //     // mapping using thumb palm angle
    //     float thumbPalmAngleDiff = 45f - jointAngle.thumbPalmAngle;
    //     bool towardIndexFinger = thumbGripperJoint1MaxRotationVector.y < 100f && thumbGripperJoint1MaxRotationVector.y > 0f;
    //     thumbPronation360ZoneDebug = $"mapping skipped, towardIndex pending, isFullRangeMapping: {isFullRangeMapping}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}";
    //     thumbPronationNon360ZoneDebug = $"mapping skipped, towardIndex pending, isFullRangeMapping: {isFullRangeMapping}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}";
    //     // Always apply remap (no outer gate) when currentThumbRotationY >= 60f to prevent twitch at diff≈0
    //     if (towardIndexFinger && currentThumbRotationY >= 60f)
    //     {
    //         float clampedThumbPalmAngleDiff = Mathf.Clamp(thumbPalmAngleDiff, 0f, 15f);
    //         float targetY = Remap(0f, 15f, 60f, currentThumbRotationY, clampedThumbPalmAngleDiff);
    //         Debug.Log($"currentThumbRotationY: {currentThumbRotationY:F4}, targetY: {targetY:F4}, clampedThumbPalmAngleDiff: {clampedThumbPalmAngleDiff:F4}");
    //         Vector3 euler = targetRotation.eulerAngles;
    //         targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //     }
    //     else if (Mathf.Abs(thumbPalmAngleDiff) > 0.1f)
    //     {
    //         // float delta = maxThumbYAxisAngle;
    //         // float clampedThumbPalmAngleDiff = Mathf.Clamp(thumbPalmAngleDiff, -5f, 15f);

    //         // if (thumbGripperJoint1MaxRotationVector.y < 100f && thumbGripperJoint1MaxRotationVector.y > 0f) // toward index finger direction
    //         // {
    //         //     // Old formula:
    //         //     // float targetY = baseAngle + thumbGripperJoint1MaxRotationVector.y + 360f + delta * (thumbPalmAngleDiff / 45f);

    //         //     float targetYAtMinDiff = -5f + thumbGripperJoint1MaxRotationVector.y + 360f + delta * (-5f / 45f);
    //         //     float targetYAtMaxDiff = 15f + thumbGripperJoint1MaxRotationVector.y + 360f + delta * (15f / 45f);
    //         //     float targetY = Remap(-5f, 15f, targetYAtMinDiff, targetYAtMaxDiff, clampedThumbPalmAngleDiff);
    //         //     thumbPronation360ZoneDebug = $"/////360 zone targetY: {targetY:F4}, minY: {targetYAtMinDiff:F4}, maxY: {targetYAtMaxDiff:F4}, baseAngle: {baseAngle:F4}, thumbGripperJoint1MaxRotationVector.y: {thumbGripperJoint1MaxRotationVector.y:F4}, delta: {delta:F4}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}, clampedDiff: {clampedThumbPalmAngleDiff:F4}";
    //         //     if (targetY >= 420f) targetY = 420f;
    //         //     Vector3 euler = targetRotation.eulerAngles;
    //         //     targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //         // }
    //         // else
    //         // {
    //         //     // Old formula:
    //         //     // float targetY = baseAngle + thumbGripperJoint1MaxRotationVector.y - delta * (thumbPalmAngleDiff / 45f);

    //         //     float targetYAtMinDiff = -5f + thumbGripperJoint1MaxRotationVector.y - delta * (-5f / 45f);
    //         //     float targetYAtMaxDiff = 15f + thumbGripperJoint1MaxRotationVector.y - delta * (15f / 45f);
    //         //     float targetY = Remap(-5f, 15f, targetYAtMinDiff, targetYAtMaxDiff, clampedThumbPalmAngleDiff);
    //         //     thumbPronationNon360ZoneDebug = $"/////non-360 zone targetY: {targetY:F4}, minY: {targetYAtMinDiff:F4}, maxY: {targetYAtMaxDiff:F4}, baseAngle: {baseAngle:F4}, thumbGripperJoint1MaxRotationVector.y: {thumbGripperJoint1MaxRotationVector.y:F4}, delta: {delta:F4}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}, clampedDiff: {clampedThumbPalmAngleDiff:F4}";
    //         //     if (targetY <= 300f) targetY = 300f;
    //         //     Vector3 euler = targetRotation.eulerAngles;
    //         //     targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //         // }

    //         if (isFullRangeMapping)
    //         {
    //             float clampedThumbPalmAngleDiff = Mathf.Clamp(thumbPalmAngleDiff, 0f, 15f);
    //             float startYUnwrapped = towardIndexFinger
    //                 ? thumbGripperJoint1MaxRotationVector.y + 360f
    //                 : thumbGripperJoint1MaxRotationVector.y;

    //             if (towardIndexFinger) // toward index finger direction (currentThumbRotationY < 60f here)
    //             {
    //                 float targetYUnwrapped = Remap(0f, 15f, startYUnwrapped, 420f, clampedThumbPalmAngleDiff);
    //                 float targetY = Mathf.Repeat(targetYUnwrapped, 360f);
    //                 // thumbPronation360ZoneDebug = $"!!!!!360 zone rawY: {targetYUnwrapped:F4}, wrappedY: {targetY:F4}, startYUnwrapped: {startYUnwrapped:F4}, thumbGripperJoint1MaxRotationVector.y: {thumbGripperJoint1MaxRotationVector.y:F4}, currentThumbRotationY: {currentThumbRotationY:F4}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}, clampedDiff: {clampedThumbPalmAngleDiff:F4}";
    //                 Vector3 euler = targetRotation.eulerAngles;
    //                 targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //             }
    //             else
    //             {
    //                 float targetYUnwrapped = Remap(0f, 15f, startYUnwrapped, 420f, clampedThumbPalmAngleDiff);
    //                 float targetY = Mathf.Repeat(targetYUnwrapped, 360f);
    //                 // thumbPronationNon360ZoneDebug = $"!!!!!non-360 zone rawY: {targetYUnwrapped:F4}, wrappedY: {targetY:F4}, startYUnwrapped: {startYUnwrapped:F4}, thumbGripperJoint1MaxRotationVector.y: {thumbGripperJoint1MaxRotationVector.y:F4}, currentThumbRotationY: {currentThumbRotationY:F4}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}, clampedDiff: {clampedThumbPalmAngleDiff:F4}";
    //                 Vector3 euler = targetRotation.eulerAngles;
    //                 targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //             }
    //         }
    //         else
    //         {
    //             float clampedThumbPalmAngleDiff = Mathf.Clamp(thumbPalmAngleDiff, 0f, 15f);
    //             float startYUnwrapped = towardIndexFinger
    //                 ? thumbGripperJoint1MaxRotationVector.y + 360f
    //                 : thumbGripperJoint1MaxRotationVector.y;

    //             if (towardIndexFinger) // toward index finger direction
    //             {
    //                 float targetYUnwrapped = Remap(0f, 15f, startYUnwrapped, 420f, clampedThumbPalmAngleDiff);
    //                 float targetY = Mathf.Repeat(targetYUnwrapped, 360f);
    //                 // thumbPronation360ZoneDebug = $"*****360 zone rawY: {targetYUnwrapped:F4}, wrappedY: {targetY:F4}, startYUnwrapped: {startYUnwrapped:F4}, thumbGripperJoint1MaxRotationVector.y: {thumbGripperJoint1MaxRotationVector.y:F4}, currentThumbRotationY: {currentThumbRotationY:F4}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}, clampedDiff: {clampedThumbPalmAngleDiff:F4}";
    //                 Vector3 euler = targetRotation.eulerAngles;
    //                 targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //             }
    //             else
    //             {
    //                 float targetYUnwrapped = Remap(0f, 15f, startYUnwrapped, startYUnwrapped + 60f, clampedThumbPalmAngleDiff);
    //                 float targetY = Mathf.Repeat(targetYUnwrapped, 360f);
    //                 // thumbPronationNon360ZoneDebug = $"*****non-360 zone rawY: {targetYUnwrapped:F4}, wrappedY: {targetY:F4}, startYUnwrapped: {startYUnwrapped:F4}, thumbGripperJoint1MaxRotationVector.y: {thumbGripperJoint1MaxRotationVector.y:F4}, currentThumbRotationY: {currentThumbRotationY:F4}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}, clampedDiff: {clampedThumbPalmAngleDiff:F4}";
    //                 Vector3 euler = targetRotation.eulerAngles;
    //                 targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //             }
    //         }
    //     }

    //     // thumb snapping logic

    //     thumbIndexInThumbRange = IsAngleInRange(targetRotation.eulerAngles.y, 320f, 340f);
    //     thumbMiddleInThumbRange = IsAngleInRange(targetRotation.eulerAngles.y, 10f, 30f);

    //     // snapping
    //     if (modeSwitching.modeSelect && paxiniValue.isThumbTouchSnapped)
    //     {
    //         // Touch snapping is active - lock the current rotation
    //         if (!_thumbMotor1Locked && ThumbAngle1Center != null)
    //         {
    //             _thumbMotor1Locked = true;
    //             _thumbMotor1LockedRot = ThumbAngle1Center.localRotation;
    //         }

    //         if (ThumbAngle1Center != null)
    //             ThumbAngle1Center.localRotation = _thumbMotor1LockedRot;
    //     }
    //     else
    //     {
    //         // No touch snapping - apply angle-based snapping or normal rotation
    //         _thumbMotor1Locked = false;

    //         if (ThumbAngle1Center != null)
    //         {
    //             ThumbAngle1Center.localRotation = targetRotation;
    //         }
    //     }
    // }
    // #endregion

    #region @ThumbProMaxMin
    void UpdateThumbPronationMaxMinMode()
    {
        Quaternion targetRotation = ThumbAngle1CenterInitialRotation;
        maxThumbYAxisAngle = NormalizeAngle(thumbGripperJoint1MaxRotationVector.y);
        minThumbYAxisAngle = NormalizeAngle(thumbGripperJoint1MinRotationVector.y);

        if (!fingerTipTouchDurations.ContainsKey("ThumbAbduction"))
        {
            fingerTipTouchDurations["ThumbAbduction"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightThumbTip.isRightThumbTipTouched
             && !isAnyMotor4Triggered && !isThumb2Triggered && canControlThumb1
             && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 1)
        {
            fingerTipTouchDurations["ThumbAbduction"] += Time.deltaTime;
            isThumb1Triggered = true;

            if (fingerTipTouchDurations["ThumbAbduction"] > 0.2f)
            {
                if (fingerTipTouchDurations["ThumbAbduction"] <= 0.2f + Time.deltaTime)
                {
                    hasThumbPronationFirstDirection = false;
                    canRotateThumbPronationThisTouch = false;
                    isThumbPronationUsingMaxRangeThisTouch = true;
                }

                if (Mathf.Abs(jointAngle.isClockWise) > 0.1f)
                {
                    // clockwise → rotationDelta < 0 → Max range (thumbGripperJoint1MaxRotationVector.y decreases from 360)
                    // counterclockwise → rotationDelta > 0 → Min range (thumbGripperJoint1MinRotationVector.y increases from 60)
                    float rotationDelta = -jointAngle.isClockWise * twistRotationSpeed * Time.deltaTime;

                    if (!hasThumbPronationFirstDirection)
                    {
                        hasThumbPronationFirstDirection = true;
                        canRotateThumbPronationThisTouch = true;
                        isThumbPronationUsingMaxRangeThisTouch = rotationDelta < 0f;

                        if (isThumbPronationUsingMaxRangeThisTouch)
                        {
                            currentThumbRotationYMax = Mathf.Clamp(currentThumbRotationYMax, -90f, 0f);
                        }
                        else
                        {
                            if (!hasThumbMinInitialized)
                            {
                                currentThumbRotationYMin = 60f;
                                hasThumbMinInitialized = true;
                            }
                            else
                            {
                                currentThumbRotationYMin = Mathf.Clamp(currentThumbRotationYMin, 0f, 90f);
                            }
                            thumbGripperJoint1MinRotationVector =
                                (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, currentThumbRotationYMin, 0f)).eulerAngles;
                            minThumbYAxisAngle = NormalizeAngle(thumbGripperJoint1MinRotationVector.y);
                        }
                    }

                    if (canRotateThumbPronationThisTouch && isThumbPronationUsingMaxRangeThisTouch)
                    {
                        currentThumbRotationYMax += rotationDelta;
                        currentThumbRotationYMax = Mathf.Clamp(currentThumbRotationYMax, -90f, 0f);

                        thumbGripperJoint1MaxRotationVector = GetThumbJoint1MaxRotationVector();
                        maxThumbYAxisAngle = NormalizeAngle(thumbGripperJoint1MaxRotationVector.y);
                    }
                    else if (canRotateThumbPronationThisTouch)
                    {
                        currentThumbRotationYMin += rotationDelta;
                        currentThumbRotationYMin = Mathf.Clamp(currentThumbRotationYMin, 0f, 90f);

                        thumbGripperJoint1MinRotationVector =
                            (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, currentThumbRotationYMin, 0f)).eulerAngles;
                        minThumbYAxisAngle = NormalizeAngle(thumbGripperJoint1MinRotationVector.y);
                    }
                }
            }
        }
        else
        {
            fingerTipTouchDurations["ThumbAbduction"] = 0f;
            isThumb1Triggered = false;
            hasThumbPronationFirstDirection = false;
            canRotateThumbPronationThisTouch = false;
            isThumbPronationUsingMaxRangeThisTouch = true;
        }

        float currentThumbRotationYForTarget = currentThumbRotationYMin > 0f
            ? currentThumbRotationYMin
            : currentThumbRotationYMax;

        // Base angle from thumb-palm angle
        float baseAngle = 45f - jointAngle.thumbPalmAngle;
        targetRotation *= Quaternion.Euler(0f, baseAngle + currentThumbRotationYForTarget, 0f);

        // mapping using thumb palm angle
        float thumbPalmAngleDiff = 45f - jointAngle.thumbPalmAngle;
        // bool towardIndexFinger = thumbGripperJoint1MaxRotationVector.y < 100f && thumbGripperJoint1MaxRotationVector.y > 0f;
        thumbPronation360ZoneDebug = $"mapping skipped, towardIndex pending, isFullRangeMapping: {isFullRangeMapping}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}";
        thumbPronationNon360ZoneDebug = $"mapping skipped, towardIndex pending, isFullRangeMapping: {isFullRangeMapping}, thumbPalmAngleDiff: {thumbPalmAngleDiff:F4}";

        if (ThumbAngle1Center != null)
        {
            if (isFullRangeMapping)
            {
                float clampedThumbPalmAngleDiff = Mathf.Clamp(thumbPalmAngleDiff, 0f, 15f);

                float targetYUnwrapped = Remap(0f, 15f, thumbGripperJoint1MaxRotationVector.y, 360 + thumbGripperJoint1MinRotationVector.y, clampedThumbPalmAngleDiff);
                float targetY = Mathf.Repeat(targetYUnwrapped, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
            }
            else
            {
                float clampedThumbPalmAngleDiff = Mathf.Clamp(thumbPalmAngleDiff, 0f, 15f);
                float rightestPos = thumbGripperJoint1MaxRotationVector.y + 60f > 360 + thumbGripperJoint1MinRotationVector.y
                    ? 360 + thumbGripperJoint1MinRotationVector.y
                    : thumbGripperJoint1MaxRotationVector.y + 60f;

                float targetYUnwrapped = Remap(0f, 15f, thumbGripperJoint1MaxRotationVector.y, rightestPos, clampedThumbPalmAngleDiff);
                float targetY = Mathf.Repeat(targetYUnwrapped, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
            }
        }

        thumbIndexInThumbRange = IsAngleInRange(targetRotation.eulerAngles.y, 320f, 340f);
        thumbMiddleInThumbRange = IsAngleInRange(targetRotation.eulerAngles.y, 10f, 30f);

        // snapping
        if (modeSwitching.modeSelect && paxiniValue.isThumbTouchSnapped)
        {
            if (!_thumbMotor1Locked && ThumbAngle1Center != null)
            {
                _thumbMotor1Locked = true;
                _thumbMotor1LockedRot = ThumbAngle1Center.localRotation;
            }

            if (ThumbAngle1Center != null)
                ThumbAngle1Center.localRotation = _thumbMotor1LockedRot;
        }
        else
        {
            _thumbMotor1Locked = false;

            if (ThumbAngle1Center != null)
                ThumbAngle1Center.localRotation = targetRotation;
        }
    }
    #endregion

    // #region @IndexPronation
    // /// <summary>
    // /// Controls Index finger Y-axis twist (swapped from Z-axis), motorID == 5
    // /// </summary>
    // private void UpdateIndexFingerPronationByAngleByY()
    // {
    //     Quaternion targetRotation = IndexAngle1CenterInitialRotation;
    //     maxIndexYAxisAngle = NormalizeAngle(indexGripperJoint1MaxRotationVector.y);
    //     indexTargetYDebug = float.NaN;

    //     if (!fingerTipTouchDurations.ContainsKey("IndexTwistY"))
    //     {
    //         fingerTipTouchDurations["IndexTwistY"] = 0f;
    //     }

    //     if (!isFingerTipTriggered && triggerRightIndexTip.isRightIndexTipTouched
    //             && jointAngle.isPlaneActive && !isAnyMotor4Triggered && canControlIndex1 && !isMiddle1Triggered
    //             && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 5)
    //     {
    //         fingerTipTouchDurations["IndexTwistY"] += Time.deltaTime;
    //         isIndex1Triggered = true;

    //         if (fingerTipTouchDurations["IndexTwistY"] > 0.2f)
    //         {
    //             if (currentIndexRotationY >= -90f && currentIndexRotationY <= 90f && Mathf.Abs(jointAngle.isClockWise) > 0.1f)
    //             {
    //                 currentIndexRotationY -= jointAngle.isClockWise * twistRotationSpeed * Time.deltaTime;
    //             }

    //             // currentIndexRotationY = Mathf.Clamp(currentIndexRotationY, -90f, 0f);
    //             currentIndexRotationY = Mathf.Clamp(currentIndexRotationY, -90f, 90f);

    //             indexGripperJoint1MaxRotationVector =
    //                 (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationY, 0f)).eulerAngles;
    //         }
    //     }
    //     else
    //     {
    //         fingerTipTouchDurations["IndexTwistY"] = 0f;
    //         isIndex1Triggered = false;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, currentIndexRotationY, 0f);



    //     if (IndexAngle1Center != null)
    //     {
    //         // float delta = maxIndexYAxisAngle;
    //         // float targetY = isFullRangeMapping
    //         //     ? maxIndexYAxisAngle + (30 - delta) * ((57f - jointAngle.indexMiddleAngleOnPalm) / 24f)
    //         //     : indexGripperJoint1MaxRotationVector.y + 30 * ((57f - jointAngle.indexMiddleAngleOnPalm) / 24f);

    //         // if (targetY >= 70) targetY = 70f;
    //         // indexTargetYDebug = targetY;

    //         // Vector3 euler = targetRotation.eulerAngles;
    //         // targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);


    //         // 往左轉 maxIndexYAxisAngle 最多會是 0 往 -60, targetY 往右是 0 往 60
    //         if (isFullRangeMapping)
    //         {
    //             float targetY;
    //             if (currentIndexRotationY > 60f) targetY = Remap(20, 57, currentIndexRotationY, 60, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             else targetY = Remap(20, 57, 60, currentIndexRotationY, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             Vector3 euler = targetRotation.eulerAngles;
    //             targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //         }
    //         else
    //         {
    //             float targetY;
    //             if (currentIndexRotationY <= 0) targetY = Remap(20, 57, 60 + currentIndexRotationY, currentIndexRotationY, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             else if (currentIndexRotationY > 0 && currentIndexRotationY <= 60f) targetY = Remap(20, 57, 60, currentIndexRotationY, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             else targetY = Remap(20, 57, currentIndexRotationY, 60, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             Vector3 euler = targetRotation.eulerAngles;
    //             targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //         }
    //     }

    //     // Snap range check for Y-axis
    //     indexMiddleInIndexRange = IsAngleInRange(targetRotation.eulerAngles.y, 295f, 335f);
    //     thumbIndexInIndexRange = IsAngleInRange(targetRotation.eulerAngles.y, 20f, 40f);

    //     // snapping
    //     if (modeSwitching.modeSelect && paxiniValue.isIndexTouchSnapped)
    //     {
    //         if (!_indexMotor1Locked && IndexAngle1Center != null)
    //         {
    //             _indexMotor1Locked = true;
    //             _indexMotor1LockedRot = IndexAngle1Center.localRotation;
    //         }

    //         if (IndexAngle1Center != null)
    //             IndexAngle1Center.localRotation = _indexMotor1LockedRot;
    //     }
    //     else
    //     {
    //         _indexMotor1Locked = false;

    //         if (IndexAngle1Center != null)
    //         {
    //             if (modeSwitching.modeSelect && indexMiddleInIndexRange && indexMiddleInMiddleRange)
    //             {
    //                 Vector3 snapEuler = targetRotation.eulerAngles;
    //                 snapEuler.y = 330f; // adjust snap angle if needed
    //                 IndexAngle1Center.localRotation = Quaternion.Euler(snapEuler.x, snapEuler.y, snapEuler.z);
    //             }
    //             else
    //             {
    //                 IndexAngle1Center.localRotation = targetRotation;
    //             }
    //         }
    //     }
    // }
    // #endregion

    #region @IndexPronationMaxMin
    /// <summary>
    /// Controls Index finger Y-axis twist (swapped from Z-axis), motorID == 5
    /// </summary>
    private void UpdateIndexFingerPronationMaxMinMode()
    {
        Quaternion targetRotation = IndexAngle1CenterInitialRotation;
        maxIndexYAxisAngle = NormalizeAngle(indexGripperJoint1MaxRotationVector.y);
        minIndexYAxisAngle = NormalizeAngle(indexGripperJoint1MinRotationVector.y);
        indexTargetYDebug = float.NaN;

        if (!fingerTipTouchDurations.ContainsKey("IndexTwistY"))
        {
            fingerTipTouchDurations["IndexTwistY"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightIndexTip.isRightIndexTipTouched
                && jointAngle.isPlaneActive && !isAnyMotor4Triggered && canControlIndex1 && !isMiddle1Triggered
                && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 5)
        {
            fingerTipTouchDurations["IndexTwistY"] += Time.deltaTime;
            isIndex1Triggered = true;

            if (fingerTipTouchDurations["IndexTwistY"] > 0.2f)
            {
                if (fingerTipTouchDurations["IndexTwistY"] <= 0.2f + Time.deltaTime)
                {
                    hasIndexPronationFirstDirection = false;
                    canRotateIndexPronationThisTouch = false;
                    isIndexPronationUsingMaxRangeThisTouch = true;
                }

                if (Mathf.Abs(jointAngle.isClockWise) > 0.1f)
                {
                    float rotationDelta = -jointAngle.isClockWise * twistRotationSpeed * Time.deltaTime;
                    // Debug.Log("jointAngle.isClockWise: " + jointAngle.isClockWise + ", rotationDelta: " + rotationDelta);

                    if (!hasIndexPronationFirstDirection)
                    {
                        hasIndexPronationFirstDirection = true;
                        canRotateIndexPronationThisTouch = true;
                        isIndexPronationUsingMaxRangeThisTouch = rotationDelta < 0f;

                        if (isIndexPronationUsingMaxRangeThisTouch)
                        {
                            currentIndexRotationYMax = Mathf.Clamp(currentIndexRotationYMax, -90f, 0f);
                        }
                        else
                        {
                            if (!hasIndexMinInitialized)
                            {
                                currentIndexRotationYMin = 60f;
                                hasIndexMinInitialized = true;
                            }
                            else
                            {
                                currentIndexRotationYMin = Mathf.Clamp(currentIndexRotationYMin, 0f, 90f);
                            }
                            indexGripperJoint1MinRotationVector =
                                (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationYMin, 0f)).eulerAngles;
                            minIndexYAxisAngle = NormalizeAngle(indexGripperJoint1MinRotationVector.y);
                        }
                    }

                    if (canRotateIndexPronationThisTouch && isIndexPronationUsingMaxRangeThisTouch)
                    {
                        currentIndexRotationYMax += rotationDelta;
                        currentIndexRotationYMax = Mathf.Clamp(currentIndexRotationYMax, -90f, 0f);

                        indexGripperJoint1MaxRotationVector = GetIndexJoint1MaxRotationVector();
                        maxIndexYAxisAngle = NormalizeAngle(indexGripperJoint1MaxRotationVector.y);
                    }
                    else if (canRotateIndexPronationThisTouch)
                    {
                        currentIndexRotationYMin += rotationDelta;
                        currentIndexRotationYMin = Mathf.Clamp(currentIndexRotationYMin, 0f, 90f);

                        indexGripperJoint1MinRotationVector =
                            (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationYMin, 0f)).eulerAngles;
                        minIndexYAxisAngle = NormalizeAngle(indexGripperJoint1MinRotationVector.y);
                    }
                }
            }
        }
        else
        {
            fingerTipTouchDurations["IndexTwistY"] = 0f;
            isIndex1Triggered = false;
            hasIndexPronationFirstDirection = false;
            canRotateIndexPronationThisTouch = false;
            isIndexPronationUsingMaxRangeThisTouch = true;
        }

        float currentIndexRotationYForTarget = currentIndexRotationYMin > 0f
            ? currentIndexRotationYMin
            : currentIndexRotationYMax;
        targetRotation *= Quaternion.Euler(0f, currentIndexRotationYForTarget, 0f);



        if (IndexAngle1Center != null)
        {
            float indexRemapMin = useIndexMiddleIndividualMode ? 15f : 20f;
            float indexRemapMax = useIndexMiddleIndividualMode ? 30f : 57f;
            float clampedIndexAngleOnPalm = useIndexMiddleIndividualMode
                ? Mathf.Clamp(jointAngle.indexToBaselineAngleOnPalm, indexRemapMin, indexRemapMax)
                : Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, indexRemapMin, indexRemapMax);

            // 往左轉 maxIndexYAxisAngle 最多會是 0 往 -60, targetY 往右是 0 往 60
            if (isFullRangeMapping)
            {
                float targetY;
                targetY = Remap(indexRemapMin, indexRemapMax, 360 + indexGripperJoint1MinRotationVector.y, indexGripperJoint1MaxRotationVector.y, clampedIndexAngleOnPalm);
                // repeat 360
                targetY = Mathf.Repeat(targetY, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
            }
            else
            {
                float targetY;
                targetY = Remap(indexRemapMin, indexRemapMax, indexGripperJoint1MaxRotationVector.y + 60f, indexGripperJoint1MaxRotationVector.y, clampedIndexAngleOnPalm);
                targetY = Mathf.Repeat(targetY, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
            }
        }

        // Snap range check for Y-axis
        indexMiddleInIndexRange = IsAngleInRange(targetRotation.eulerAngles.y, 295f, 335f);
        thumbIndexInIndexRange = IsAngleInRange(targetRotation.eulerAngles.y, 20f, 40f);

        // snapping
        if (modeSwitching.modeSelect && paxiniValue.isIndexTouchSnapped)
        {
            if (!_indexMotor1Locked && IndexAngle1Center != null)
            {
                _indexMotor1Locked = true;
                _indexMotor1LockedRot = IndexAngle1Center.localRotation;
            }

            if (IndexAngle1Center != null)
                IndexAngle1Center.localRotation = _indexMotor1LockedRot;
        }
        else
        {
            _indexMotor1Locked = false;

            if (IndexAngle1Center != null)
            {
                if (modeSwitching.modeSelect && indexMiddleInIndexRange && indexMiddleInMiddleRange)
                {
                    // Vector3 snapEuler = targetRotation.eulerAngles;
                    // snapEuler.y = 330f; // adjust snap angle if needed
                    // IndexAngle1Center.localRotation = Quaternion.Euler(snapEuler.x, snapEuler.y, snapEuler.z);
                    IndexAngle1Center.localRotation = targetRotation;
                }
                else
                {
                    IndexAngle1Center.localRotation = targetRotation;
                }
            }
        }
    }
    #endregion

    // #region @MiddlePronation
    // /// <summary>
    // /// Controls Middle finger Y-axis pronation (swapped from Z-axis), motorID == 9
    // /// </summary>
    // private void UpdateMiddleFingerPronationByAngleByY()
    // {
    //     Quaternion targetRotation = MiddleAngle1CenterInitialRotation;
    //     maxMiddleYAxisAngle = NormalizeAngle(middleGripperJoint1MaxRotationVector.y);

    //     if (!fingerTipTouchDurations.ContainsKey("MiddleTwistY"))
    //     {
    //         fingerTipTouchDurations["MiddleTwistY"] = 0f;
    //     }

    //     if (!isFingerTipTriggered && triggerRightMiddleTip.isRightMiddleTipTouched
    //          && jointAngle.isPlaneActive && !isAnyMotor4Triggered && canControlMiddle1 && !isIndex1Triggered
    //         && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 9)
    //     {
    //         fingerTipTouchDurations["MiddleTwistY"] += Time.deltaTime;
    //         isMiddle1Triggered = true;

    //         if (fingerTipTouchDurations["MiddleTwistY"] > 0.2f)
    //         {
    //             if (currentMiddleRotationY <= 90f && currentMiddleRotationY >= -90)
    //             {
    //                 currentMiddleRotationY -= jointAngle.isClockWise * twistRotationSpeed * Time.deltaTime;
    //             }

    //             currentMiddleRotationY = Mathf.Clamp(currentMiddleRotationY, -90f, 90f);

    //             middleGripperJoint1MaxRotationVector =
    //                 (MiddleAngle1CenterInitialRotation * Quaternion.Euler(0f, currentMiddleRotationY, 0f)).eulerAngles;
    //         }
    //     }
    //     else
    //     {
    //         fingerTipTouchDurations["MiddleTwistY"] = 0f;
    //         isMiddle1Triggered = false;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, currentMiddleRotationY, 0f);

    //     if (MiddleAngle1Center != null)
    //     {
    //         // float delta = maxMiddleYAxisAngle;
    //         // float targetY = isFullRangeMapping
    //         //     ? maxMiddleYAxisAngle - (30 + delta) * ((57f - jointAngle.indexMiddleAngleOnPalm) / 24f)
    //         //     : middleGripperJoint1MaxRotationVector.y - 30 * ((57f - jointAngle.indexMiddleAngleOnPalm) / 24f);

    //         // if (targetY <= -70f) targetY = -70f;

    //         // Vector3 euler = targetRotation.eulerAngles;
    //         // targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);

    //         if (isFullRangeMapping)
    //         {
    //             float targetY;
    //             if (currentMiddleRotationY < -60f) targetY = Remap(20, 57, currentMiddleRotationY, -60, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             else targetY = Remap(20, 57, -60, currentMiddleRotationY, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             Vector3 euler = targetRotation.eulerAngles;
    //             targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //         }
    //         else
    //         {
    //             float targetY;
    //             if (currentMiddleRotationY > 0) targetY = Remap(20, 57, -60 + currentMiddleRotationY, currentMiddleRotationY, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             else if (currentMiddleRotationY <= 0 && currentMiddleRotationY >= -60) targetY = Remap(20, 57, -60, currentMiddleRotationY, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             else targetY = Remap(20, 57, currentMiddleRotationY, -60, Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, 20, 57));
    //             Vector3 euler = targetRotation.eulerAngles;
    //             targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //         }
    //     }

    //     // Snap range check for Y-axis
    //     indexMiddleInMiddleRange = IsAngleInRange(targetRotation.eulerAngles.y, 30f, 65f);
    //     thumbMiddleInMiddleRange = IsAngleInRange(targetRotation.eulerAngles.y, 0f, 30f);

    //     // snapping
    //     if (modeSwitching.modeSelect && paxiniValue.isMiddleTouchSnapped)
    //     {
    //         if (!_middleMotor1Locked && MiddleAngle1Center != null)
    //         {
    //             _middleMotor1Locked = true;
    //             _middleMotor1LockedRot = MiddleAngle1Center.localRotation;
    //         }

    //         if (MiddleAngle1Center != null)
    //             MiddleAngle1Center.localRotation = _middleMotor1LockedRot;
    //     }
    //     else
    //     {
    //         _middleMotor1Locked = false;

    //         if (MiddleAngle1Center != null)
    //         {
    //             if (modeSwitching.modeSelect && indexMiddleInMiddleRange && indexMiddleInIndexRange)
    //             {
    //                 Vector3 snapEuler = targetRotation.eulerAngles;
    //                 snapEuler.y = 30f; // adjust snap angle if needed
    //                 MiddleAngle1Center.localRotation = Quaternion.Euler(snapEuler.x, snapEuler.y, snapEuler.z);
    //             }
    //             else
    //             {
    //                 MiddleAngle1Center.localRotation = targetRotation;
    //             }
    //         }
    //     }
    // }
    // #endregion

    #region @MiddlePronationMaxMin
    /// <summary>
    /// Controls Middle finger Y-axis pronation (swapped from Z-axis), motorID == 9
    /// </summary>
    private void UpdateMiddleFingerPronationMaxMinMode()
    {
        Quaternion targetRotation = MiddleAngle1CenterInitialRotation;

        if (!fingerTipTouchDurations.ContainsKey("MiddleTwistY"))
        {
            fingerTipTouchDurations["MiddleTwistY"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightMiddleTip.isRightMiddleTipTouched
             && jointAngle.isPlaneActive && !isAnyMotor4Triggered && canControlMiddle1 && !isIndex1Triggered
            && modeSwitching.modeManipulate && modeSwitching.confirmedMotorID == 9)
        {
            fingerTipTouchDurations["MiddleTwistY"] += Time.deltaTime;
            isMiddle1Triggered = true;

            if (fingerTipTouchDurations["MiddleTwistY"] > 0.2f)
            {
                if (fingerTipTouchDurations["MiddleTwistY"] <= 0.2f + Time.deltaTime)
                {
                    hasMiddlePronationFirstDirection = false;
                    canRotateMiddlePronationThisTouch = false;
                    isMiddlePronationUsingMaxRangeThisTouch = true;
                }

                if (Mathf.Abs(jointAngle.isClockWise) > 0.1f)
                {
                    float rotationDelta = -jointAngle.isClockWise * twistRotationSpeed * Time.deltaTime;

                    if (!hasMiddlePronationFirstDirection)
                    {
                        hasMiddlePronationFirstDirection = true;
                        canRotateMiddlePronationThisTouch = true;
                        isMiddlePronationUsingMaxRangeThisTouch = rotationDelta < 0f;

                        if (isMiddlePronationUsingMaxRangeThisTouch)
                        {
                            currentMiddleRotationYMax = Mathf.Clamp(currentMiddleRotationYMax, -90f, 0f);
                            middleGripperJoint1MaxRotationVector = GetMiddleJoint1MaxRotationVector();
                            maxMiddleYAxisAngle = NormalizeMiddleJoint1MaxAngle(middleGripperJoint1MaxRotationVector.y);
                            RefreshMiddleJoint1YDebug("MiddlePronation:first:max");
                        }
                        else
                        {
                            currentMiddleRotationYMin = Mathf.Clamp(currentMiddleRotationYMin, 0f, 90f);
                            middleGripperJoint1MinRotationVector = GetMiddleJoint1MinRotationVector();
                            minMiddleYAxisAngle = NormalizeAngle(middleGripperJoint1MinRotationVector.y);
                            RefreshMiddleJoint1YDebug("MiddlePronation:first:min");
                        }
                    }

                    if (canRotateMiddlePronationThisTouch && isMiddlePronationUsingMaxRangeThisTouch)
                    {
                        currentMiddleRotationYMax += rotationDelta;
                        currentMiddleRotationYMax = Mathf.Clamp(currentMiddleRotationYMax, -90f, 0f);

                        middleGripperJoint1MaxRotationVector = GetMiddleJoint1MaxRotationVector();
                        maxMiddleYAxisAngle = NormalizeMiddleJoint1MaxAngle(middleGripperJoint1MaxRotationVector.y);
                        RefreshMiddleJoint1YDebug("MiddlePronation:update:max");
                    }
                    else if (canRotateMiddlePronationThisTouch)
                    {
                        currentMiddleRotationYMin += rotationDelta;
                        currentMiddleRotationYMin = Mathf.Clamp(currentMiddleRotationYMin, 0f, 90f);

                        middleGripperJoint1MinRotationVector = GetMiddleJoint1MinRotationVector();
                        minMiddleYAxisAngle = NormalizeAngle(middleGripperJoint1MinRotationVector.y);
                        RefreshMiddleJoint1YDebug("MiddlePronation:update:min");
                    }
                }
            }
        }
        else
        {
            fingerTipTouchDurations["MiddleTwistY"] = 0f;
            isMiddle1Triggered = false;
            hasMiddlePronationFirstDirection = false;
            canRotateMiddlePronationThisTouch = false;
            isMiddlePronationUsingMaxRangeThisTouch = true;
        }

        float currentMiddleRotationYForTarget = currentMiddleRotationYMin > 0f
            ? currentMiddleRotationYMin
            : currentMiddleRotationYMax + 60f;
        targetRotation *= Quaternion.Euler(0f, currentMiddleRotationYForTarget, 0f);

        if (MiddleAngle1Center != null)
        {
            float middleRemapMin = useIndexMiddleIndividualMode ? 20f : 20f;
            float middleRemapMax = useIndexMiddleIndividualMode ? 35f : 57f;
            float clampedMiddleAngleOnPalm = useIndexMiddleIndividualMode
                ? Mathf.Clamp(jointAngle.middleToBaselineAngleOnPalm, middleRemapMin, middleRemapMax)
                : Mathf.Clamp(jointAngle.indexMiddleAngleOnPalm, middleRemapMin, middleRemapMax);

            if (isFullRangeMapping)
            {
                float targetY;
                targetY = Remap(middleRemapMin, middleRemapMax, middleGripperJoint1MaxRotationVector.y, 360 + middleGripperJoint1MinRotationVector.y, clampedMiddleAngleOnPalm);

                targetY = Mathf.Repeat(targetY, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
            }
            else
            {
                float targetY;
                targetY = Remap(middleRemapMin, middleRemapMax, 360 + middleGripperJoint1MinRotationVector.y - 60f, 360 + middleGripperJoint1MinRotationVector.y, clampedMiddleAngleOnPalm);

                targetY = Mathf.Repeat(targetY, 360f);
                Vector3 euler = targetRotation.eulerAngles;
                targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
            }
        }

        // Snap range check for Y-axis
        indexMiddleInMiddleRange = IsAngleInRange(targetRotation.eulerAngles.y, 30f, 65f);
        thumbMiddleInMiddleRange = IsAngleInRange(targetRotation.eulerAngles.y, 0f, 30f);

        // snapping
        if (modeSwitching.modeSelect && paxiniValue.isMiddleTouchSnapped)
        {
            if (!_middleMotor1Locked && MiddleAngle1Center != null)
            {
                _middleMotor1Locked = true;
                _middleMotor1LockedRot = MiddleAngle1Center.localRotation;
            }

            if (MiddleAngle1Center != null)
                MiddleAngle1Center.localRotation = _middleMotor1LockedRot;
        }
        else
        {
            _middleMotor1Locked = false;

            if (MiddleAngle1Center != null)
            {
                if (modeSwitching.modeSelect && indexMiddleInMiddleRange && indexMiddleInIndexRange)
                {
                    // Vector3 snapEuler = targetRotation.eulerAngles;
                    // snapEuler.y = 30f; // adjust snap angle if needed
                    // MiddleAngle1Center.localRotation = Quaternion.Euler(snapEuler.x, snapEuler.y, snapEuler.z);
                    MiddleAngle1Center.localRotation = targetRotation;
                }
                else
                {
                    MiddleAngle1Center.localRotation = targetRotation;
                }
            }
        }
    }
    #endregion

    private float NormalizeAngle(float angle)
    {
        return angle >= 300 ? angle - 360 : angle;
    }

    private Vector3 GetThumbJoint1MaxRotationVector()
    {
        Vector3 rotationVector =
            (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, currentThumbRotationYMax, 0f)).eulerAngles;

        if (Mathf.Abs(currentThumbRotationYMax) <= 0.0001f)
            rotationVector.y = 360f;

        return rotationVector;
    }

    private Vector3 GetIndexJoint1MaxRotationVector()
    {
        Vector3 rotationVector =
            (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationYMax, 0f)).eulerAngles;

        if (Mathf.Abs(currentIndexRotationYMax) <= 0.0001f)
            rotationVector.y = 360f;

        return rotationVector;
    }

    private Vector3 GetMiddleJoint1MaxRotationVector()
    {
        Vector3 rotationVector = MiddleAngle1CenterInitialRotation.eulerAngles;

        rotationVector.y = Mathf.Clamp(360f + currentMiddleRotationYMax, 270f, 360f);
        return rotationVector;
    }

    private Vector3 GetMiddleJoint1MinRotationVector()
    {
        Vector3 rotationVector = MiddleAngle1CenterInitialRotation.eulerAngles;
        rotationVector.y = Mathf.Clamp(currentMiddleRotationYMin, 0f, 90f);
        return rotationVector;
    }

    private Vector3 GetMiddleJoint2MaxRotationVector()
    {
        Vector3 rotationVector = MiddleAngle2CenterInitialRotation.eulerAngles;
        rotationVector.z = Mathf.Clamp(360f + currentMiddleRotationZMax, 270f, 360f);
        return rotationVector;
    }

    private float NormalizeMiddleJoint1MaxAngle(float angle)
    {
        return angle - 360f;
    }

    private bool IsAngleInRange(float angle, float min, float max)
    {
        // Handle wrap-around case (e.g., 302-310 crosses 360/0 boundary)
        if (min > max)
        {
            return angle >= min || angle <= max;
        }
        else
        {
            return angle >= min && angle <= max;
        }
    }

    private float ExtensionClampMin => useFullExtensionClampRange ? -90f : -80f;
    private float ExtensionClampMax => useFullExtensionClampRange ? 90f : 50f;

    #region @Extension
    private void UpdateFingertipExtension(
        bool isTipTouched,
        float jointAngleValue,
        string jointName,
        string midJointName,
        string baseJointName,
        ref float currentTipRotation,
        float rotationSpeed,
        Renderer jointRenderer,
        Color activeColor,
        Color inactiveColor,
        Transform jointTransform,
        ref bool relatedMotorTriggered,
        bool shouldPreventActivation,
        ref bool motorLocked,
        ref Quaternion motorLockedRotation,
        bool isManipulatingMode = false,
        int motorID = -2,
        int expectedMotorID = -3,
        float angleThreshold = 15.0f,
        float? additionalAngle = null,
        bool useFullRangeMappingForThisExtension = true,
        bool paxiniTouchSnapped = false)
    {
        // Initialize rotation based on jointAngleValue for the base angle
        Quaternion targetRotation = Quaternion.Euler(jointAngleValue + currentTipRotation, 0f, 0f);

        // Initialize touch duration if not already present
        if (!fingerTipTouchDurations.ContainsKey(jointName))
        {
            fingerTipTouchDurations[jointName] = 0f;
        }

        // Initialize activation state if not already present
        if (!fingerTipActivated.ContainsKey(jointName))
        {
            fingerTipActivated[jointName] = false;
        }

        // Track joint angles when tip is first touched (initialize once)
        if (isTipTouched && !previousJointAngles.ContainsKey(jointName))
        {
            // Initialize tracking - all joints use same approach
            previousJointAngles[jointName] = jointAngle.joints[jointName].localRotation.eulerAngles.z;
            accumulatedJointChanges[jointName] = 0f;

            previousMidJointAngles[jointName] = !string.IsNullOrEmpty(midJointName) ?
                jointAngle.joints[midJointName].localRotation.eulerAngles.z : 0f;
            accumulatedMidJointChanges[jointName] = 0f;

            previousBaseJointAngles[jointName] = jointAngle.joints[baseJointName].localRotation.eulerAngles.z;
            accumulatedBaseJointChanges[jointName] = 0f;

            // for thumb
            if (additionalAngle.HasValue)
            {
                previousAdditionalAngles[jointName] = additionalAngle.Value;
                accumulatedAdditionalChanges[jointName] = 0f;
            }
        }

        // Calculate totalAngleChange for display (without abs, keep sign)
        float localTotalAngleChange = 0f;

        // Initialize min/max tracking and previous value if not exists
        if (!accumulatedJointChanges.ContainsKey(jointName + "_min"))
        {
            accumulatedJointChanges[jointName + "_min"] = 0f;
            accumulatedJointChanges[jointName + "_max"] = 0f;
            accumulatedJointChanges[jointName + "_prev"] = 0f;
        }

        float localMinAngle = accumulatedJointChanges[jointName + "_min"];
        float localMaxAngle = accumulatedJointChanges[jointName + "_max"];
        float previousTotalAngleChange = accumulatedJointChanges[jointName + "_prev"];

        if (isTipTouched && previousJointAngles.ContainsKey(jointName))
        {
            // Process jointName with wrap-around
            float currentJointAngle = jointAngle.joints[jointName].localRotation.eulerAngles.z;
            float deltaJoint = currentJointAngle - previousJointAngles[jointName];
            if (deltaJoint < -180f) deltaJoint += 360f;
            else if (deltaJoint > 180f) deltaJoint -= 360f;
            accumulatedJointChanges[jointName] += deltaJoint;
            previousJointAngles[jointName] = currentJointAngle;
            // Debug.Log("accumulatedJointChanges[" + jointName + "] = " + accumulatedJointChanges[jointName]);

            // Process midJoint with wrap-around (same logic for all)
            float deltaMid = 0f;
            if (!string.IsNullOrEmpty(midJointName))
            {
                float currentMidAngle = jointAngle.joints[midJointName].localRotation.eulerAngles.z;
                deltaMid = currentMidAngle - previousMidJointAngles[jointName];
                if (deltaMid < -180f) deltaMid += 360f;
                else if (deltaMid > 180f) deltaMid -= 360f;
                accumulatedMidJointChanges[jointName] += deltaMid;
                previousMidJointAngles[jointName] = currentMidAngle;
            }

            // Process baseJoint with wrap-around (same logic for all)
            float currentBaseAngle = jointAngle.joints[baseJointName].localRotation.eulerAngles.z;
            float deltaBase = currentBaseAngle - previousBaseJointAngles[jointName];
            if (deltaBase < -180f) deltaBase += 360f;
            else if (deltaBase > 180f) deltaBase -= 360f;
            accumulatedBaseJointChanges[jointName] += deltaBase;
            previousBaseJointAngles[jointName] = currentBaseAngle;

            // Process additional angle if provided (reversed direction: previous - current)
            if (additionalAngle.HasValue && previousAdditionalAngles.ContainsKey(jointName))
            {
                float currentAdditional = additionalAngle.Value;
                float deltaAdditional = previousAdditionalAngles[jointName] - currentAdditional;  // Reversed: previous - current
                accumulatedAdditionalChanges[jointName] += deltaAdditional;
                previousAdditionalAngles[jointName] = currentAdditional;
            }

            // Calculate total angle change WITHOUT abs (keep sign for direction)
            localTotalAngleChange = accumulatedJointChanges[jointName] +
                               accumulatedMidJointChanges[jointName] +
                               accumulatedBaseJointChanges[jointName];

            // Add additional angle change if available
            if (additionalAngle.HasValue && accumulatedAdditionalChanges.ContainsKey(jointName))
            {
                localTotalAngleChange += accumulatedAdditionalChanges[jointName];
            }

            // Update min/max with 10 degree threshold
            bool isIncreasing = localTotalAngleChange > previousTotalAngleChange;
            bool isDecreasing = localTotalAngleChange < previousTotalAngleChange;

            if (isIncreasing)
            {
                // Only update max if moved at least 10 degrees from min
                if (localTotalAngleChange - localMinAngle >= 10f)
                {
                    localMaxAngle = localTotalAngleChange;
                    accumulatedJointChanges[jointName + "_max"] = localMaxAngle;
                }
            }
            else if (isDecreasing)
            {
                // Only update min if moved at least 10 degrees from max
                if (localMaxAngle - localTotalAngleChange >= 10f)
                {
                    localMinAngle = localTotalAngleChange;
                    accumulatedJointChanges[jointName + "_min"] = localMinAngle;
                }
            }

            // Store direction flags for rotation control
            accumulatedJointChanges[jointName + "_isIncreasing"] = isIncreasing ? 1f : 0f;
            accumulatedJointChanges[jointName + "_isDecreasing"] = isDecreasing ? 1f : 0f;

            // Store current value as previous for next frame
            accumulatedJointChanges[jointName + "_prev"] = localTotalAngleChange;
        }
        else
        {
            // If not currently tracking, show 0
            if (isTipTouched)
            {
                totalAngleChange = 0f;
            }
        }

        // Reset tracking when not touched
        if (!isTipTouched)
        {
            if (previousJointAngles.ContainsKey(jointName))
            {
                previousJointAngles.Remove(jointName);
                previousMidJointAngles.Remove(jointName);
                previousBaseJointAngles.Remove(jointName);
                accumulatedJointChanges.Remove(jointName);
                accumulatedMidJointChanges.Remove(jointName);
                accumulatedBaseJointChanges.Remove(jointName);
                accumulatedJointChanges.Remove(jointName + "_min");
                accumulatedJointChanges.Remove(jointName + "_max");
                accumulatedJointChanges.Remove(jointName + "_prev");
                accumulatedJointChanges.Remove(jointName + "_isIncreasing");
                accumulatedJointChanges.Remove(jointName + "_isDecreasing");
                previousAdditionalAngles.Remove(jointName);
                accumulatedAdditionalChanges.Remove(jointName);
            }
            fingerTipTouchDurations[jointName] = 0f;
            fingerTipActivated[jointName] = false;
            // jointRenderer.material.color = inactiveColor;
            relatedMotorTriggered = false;
        }
        else
        {
            // bool motorIDMatches = (expectedMotorID == 0) || (isManipulatingMode && motorID == expectedMotorID);
            bool motorIDMatches = isManipulatingMode && motorID == expectedMotorID;

            // Use absolute range (max - min) for activation threshold
            float angleRange = localMaxAngle - localMinAngle;
            bool initialConditionMet = isTipTouched && angleRange > angleThreshold && !shouldPreventActivation && motorIDMatches;

            bool conditionMet = fingerTipActivated[jointName] ? isTipTouched : initialConditionMet; // Once activated, only check if still touched

            if (conditionMet)
            {
                fingerTipTouchDurations[jointName] += Time.deltaTime;

                // Activate immediately when initial condition is met
                if (initialConditionMet)
                {
                    fingerTipActivated[jointName] = true;
                    // jointRenderer.material.color = activeColor;
                    relatedMotorTriggered = true;
                    isFingerTipTriggered = true;
                }
                else if (fingerTipActivated[jointName])
                {
                    // Keep triggering while activated
                    isFingerTipTriggered = true;
                }
            }
        }

        // Apply rotation immediately when activated
        if (fingerTipActivated[jointName] && motorID == expectedMotorID)
        {
            // Get latest min/max values from dictionary
            float currentMinAngle = accumulatedJointChanges.ContainsKey(jointName + "_min") ?
                                    accumulatedJointChanges[jointName + "_min"] : 0f;
            float currentMaxAngle = accumulatedJointChanges.ContainsKey(jointName + "_max") ?
                                    accumulatedJointChanges[jointName + "_max"] : 0f;
            float angleRange = currentMaxAngle - currentMinAngle;

            // Get current total angle change
            float currentTotalAngle = accumulatedJointChanges.ContainsKey(jointName + "_prev") ?
                                      accumulatedJointChanges[jointName + "_prev"] : 0f;

            // Calculate distance from min and max
            float distanceFromMin = Mathf.Abs(currentTotalAngle - currentMinAngle);
            float distanceFromMax = Mathf.Abs(currentTotalAngle - currentMaxAngle);

            // Debug.Log("angleRange: " + angleRange + ", distanceFromMin: " + distanceFromMin + ", distanceFromMax: " + distanceFromMax);

            // If closer to max (moving in positive direction), rotate negative
            if (distanceFromMax < distanceFromMin && angleRange > 15f)
            {
                currentTipRotation -= rotationSpeed * Time.deltaTime;
                // Debug.Log("Rotating NEGATIVE (closer to max)");
            }
            // If closer to min (moving in negative direction), rotate positive
            else if (distanceFromMin < distanceFromMax && angleRange > 15f)
            {
                currentTipRotation += rotationSpeed * Time.deltaTime;
                // Debug.Log("Rotating POSITIVE (closer to min)");
            }

            currentTipRotation = Mathf.Clamp(currentTipRotation, ExtensionClampMin, ExtensionClampMax); // negative: face body, positive: away from body
            // jointRenderer.material.color = activeColor;
            relatedMotorTriggered = true;
        }
        else
        {
            relatedMotorTriggered = false;
            // Debug.Log("No!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");   90 -30
        }

        float mappedJointAngleValue = 0f;
        float finalAngle;

        if (useFullRangeMappingForThisExtension)
        {
            // float finalAngle;
            // if (jointName.Contains("Thumb"))
            // {
            //     finalAngle = Mathf.Clamp(2.1f * jointAngleValue + currentTipRotation - extensionInwardOffsetDeg, ExtensionClampMin, ExtensionClampMax);
            // }
            // else if (jointName.Contains("Index"))
            // {
            //     finalAngle = Mathf.Clamp(2.0f * jointAngleValue + currentTipRotation - extensionInwardOffsetDeg, ExtensionClampMin, ExtensionClampMax);
            // }
            // else
            // {
            //     finalAngle = Mathf.Clamp(1.5f * jointAngleValue + currentTipRotation - extensionInwardOffsetDeg, ExtensionClampMin, ExtensionClampMax);
            // }
            finalAngle = 0f;

            if (jointName.Contains("Thumb"))
            {
                if (expectedMotorID == 3)
                {
                    mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 15, 35));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
                else if (expectedMotorID == 4)
                {
                    mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 15, 35));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
            }
            else if (jointName.Contains("Index"))
            {
                if (expectedMotorID == 7)
                {
                    mappedJointAngleValue = Remap(10, 55, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 10, 55));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
                else if (expectedMotorID == 8)
                {
                    mappedJointAngleValue = Remap(10, 30, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 10, 30));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
            }
            else
            {
                if (expectedMotorID == 11)
                {
                    mappedJointAngleValue = Remap(15, 60, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 15, 60));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
                else if (expectedMotorID == 12)
                {
                    mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 15, 35));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
            }

            targetRotation = Quaternion.Euler(finalAngle, 0f, 0f);
        }
        else
        {
            finalAngle = 0f;

            if (jointName.Contains("Thumb"))
            {
                if (expectedMotorID == 3)
                {
                    if (currentTipRotation >= 0) mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 15, 35));
                    else mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90 + currentTipRotation, Mathf.Clamp(jointAngleValue, 15, 35));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
                else if (expectedMotorID == 4)
                {
                    if (currentTipRotation >= 0) mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 15, 35));
                    else mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90 + currentTipRotation, Mathf.Clamp(jointAngleValue, 15, 35));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
            }
            else if (jointName.Contains("Index"))
            {
                if (expectedMotorID == 7)
                {
                    if (currentTipRotation >= 0) mappedJointAngleValue = Remap(10, 55, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 10, 55));
                    else mappedJointAngleValue = Remap(10, 55, currentTipRotation, 90 + currentTipRotation, Mathf.Clamp(jointAngleValue, 10, 55));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
                else if (expectedMotorID == 8)
                {
                    if (currentTipRotation >= 0) mappedJointAngleValue = Remap(10, 30, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 10, 30));
                    else mappedJointAngleValue = Remap(10, 30, currentTipRotation, 90 + currentTipRotation, Mathf.Clamp(jointAngleValue, 10, 30));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
            }
            else
            {
                if (expectedMotorID == 11)
                {
                    if (currentTipRotation >= 0) mappedJointAngleValue = Remap(15, 60, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 15, 60));
                    else mappedJointAngleValue = Remap(15, 60, currentTipRotation, 90 + currentTipRotation, Mathf.Clamp(jointAngleValue, 15, 60));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
                else if (expectedMotorID == 12)
                {
                    if (currentTipRotation >= 0) mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90, Mathf.Clamp(jointAngleValue, 15, 35));
                    else mappedJointAngleValue = Remap(15, 35, currentTipRotation, 90 + currentTipRotation, Mathf.Clamp(jointAngleValue, 15, 35));
                    finalAngle = Mathf.Clamp(mappedJointAngleValue, ExtensionClampMin, ExtensionClampMax);
                }
            }

            targetRotation = Quaternion.Euler(finalAngle, 0f, 0f);
        }

        // jointAngleValueDebug = jointAngleValue;
        // currentTipRotationDebug = currentTipRotation;
        // mappedJointAngleValueDebug = mappedJointAngleValue;
        // finalAngleDebug = finalAngle;
        // targetRotationEulerDebug = targetRotation.eulerAngles;

        // motor locking logic for extension
        if (modeSwitching.modeSelect && paxiniTouchSnapped)
        {
            if (!motorLocked && jointTransform != null)
            {
                motorLocked = true;
                motorLockedRotation = jointTransform.localRotation;
            }

            if (jointTransform != null)
                jointTransform.localRotation = motorLockedRotation;
        }
        else
        {
            motorLocked = false;

            if (jointTransform != null)
                jointTransform.localRotation = targetRotation;
        }
    }
    #endregion

    public static float Remap(
    float inMin,
    float inMax,
    float outMin,
    float outMax,
    float value)
    {
        float t = Mathf.InverseLerp(inMin, inMax, value);
        return Mathf.Lerp(outMin, outMax, t);
    }

    #region ResetFunction
    // ==============================
    // 🔹 Reset Function
    // ==============================
    public void ResetFingerRotations()
    {
        ForceDisengageOnReset();

        isFullRangeMapping = true;
        useIndexMiddleIndividualMode = false;

        currentThumbRotationY = currentThumbRotationZ = 0f;
        currentThumbRotationYMax = 0f;
        currentThumbRotationYMin = 0f;
        currentThumbRotationZMax = 0f;
        currentThumbRotationZMin = 0f;
        hasThumbAbductionAdjustment = false;
        currentIndexRotationYMax = currentIndexRotationYMin = 0f;
        currentIndexRotationZMax = 0f;
        currentIndexRotationZMin = 0f;
        currentMiddleRotationYMax = -60f;
        currentMiddleRotationYMin = 0f;
        currentMiddleRotationZ = 0f;
        currentMiddleRotationZMax = 0f;
        currentMiddleRotationZMin = 0f;

        currentThumbTipRotationZ = 0f;
        currentIndexTipRotationZ = 0f;
        currentMiddleTipRotationZ = 0f;

        currentThumbInnerExtensionRotationZ = 0f;
        currentIndexInnerExtensionRotationZ = 0f;
        currentMiddleInnerExtensionRotationZ = 0f;

        thumbGripperJoint1MaxRotationVector = GetThumbJoint1MaxRotationVector();
        thumbGripperJoint1MinRotationVector =
            (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, 60f, 0f)).eulerAngles;
        thumbGripperJoint2MaxRotationVector = ThumbAngle2CenterInitialRotation.eulerAngles;
        if (thumbGripperJoint2MaxRotationVector.z < 1f) thumbGripperJoint2MaxRotationVector.z = 360f;
        thumbGripperJoint2MinRotationVector = ThumbAngle2CenterInitialRotation.eulerAngles;
        indexGripperJoint1MaxRotationVector = GetIndexJoint1MaxRotationVector();
        indexGripperJoint1MinRotationVector =
            (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, 60f, 0f)).eulerAngles;
        indexGripperJoint2MaxRotationVector = IndexAngle2CenterInitialRotation.eulerAngles;
        indexGripperJoint2MinRotationVector = IndexAngle2CenterInitialRotation.eulerAngles;
        middleGripperJoint1MaxRotationVector = GetMiddleJoint1MaxRotationVector();
        middleGripperJoint1MinRotationVector = GetMiddleJoint1MinRotationVector();
        middleGripperJoint2MaxRotationVector = GetMiddleJoint2MaxRotationVector();
        middleGripperJoint2MinRotationVector = MiddleAngle2CenterInitialRotation.eulerAngles;

        maxThumbYAxisAngle = ThumbAngle1CenterInitialRotation.eulerAngles.y;
        minThumbYAxisAngle = NormalizeAngle(thumbGripperJoint1MinRotationVector.y);
        maxThumbZAxisAngle = thumbGripperJoint2MaxRotationVector.z;
        minThumbZAxisAngle = 0f;
        maxIndexYAxisAngle = IndexAngle1CenterInitialRotation.eulerAngles.y;
        minIndexYAxisAngle = NormalizeAngle(indexGripperJoint1MinRotationVector.y);
        maxIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;
        minIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;
        maxMiddleYAxisAngle = NormalizeMiddleJoint1MaxAngle(middleGripperJoint1MaxRotationVector.y);
        minMiddleYAxisAngle = NormalizeAngle(middleGripperJoint1MinRotationVector.y);
        maxMiddleZAxisAngle = MiddleAngle2CenterInitialRotation.eulerAngles.z;
        RefreshMiddleJoint1YDebug("ResetFingerRotations");

        // Clear touch durations and activation states
        fingerTipTouchDurations.Clear();
        fingerTipActivated.Clear();

        // Clear joint angle tracking
        previousJointAngles.Clear();
        previousMidJointAngles.Clear();
        previousBaseJointAngles.Clear();
        accumulatedJointChanges.Clear();
        accumulatedMidJointChanges.Clear();
        accumulatedBaseJointChanges.Clear();
        previousAdditionalAngles.Clear();
        accumulatedAdditionalChanges.Clear();
        indexAngleHistory.Clear();
        isIndexRotatingNegative = true;
        hasIndexAbductionDirection = false;
        hasIndexAbductionFirstDirection = false;
        canRotateIndexAbductionThisTouch = false;
        isIndexAbductionUsingMaxRangeThisTouch = true;
        hasIndexPronationFirstDirection = false;
        canRotateIndexPronationThisTouch = false;
        isIndexPronationUsingMaxRangeThisTouch = true;
        hasIndexMinInitialized = false;
        hasThumbPronationFirstDirection = false;
        canRotateThumbPronationThisTouch = false;
        isThumbPronationUsingMaxRangeThisTouch = true;
        hasThumbMinInitialized = false;
        hasMiddlePronationFirstDirection = false;
        canRotateMiddlePronationThisTouch = false;
        isMiddlePronationUsingMaxRangeThisTouch = true;

        ApplyResetRotations();
        ResetAllFreezeStates();
        ResetModeSwitchingState();
        SetEmbodimentInitialColors();
        ForceAllPaxiniOffAndRestoreColor();
        tt = 0f;
    }

    private void ForceDisengageOnReset()
    {
        if (tcpSender != null)
        {
            tcpSender.SetEngagement(false, "claw_reset");
        }
    }

    /// <summary>
    /// Clears all freeze-related state so reset returns every finger to an unfrozen baseline.
    /// </summary>
    private void ResetAllFreezeStates()
    {
        _manipulationFreezeInitialized = false;

        _smcThumbFreezeWasEnabled = false;
        _smcIndexFreezeWasEnabled = false;
        _smcMiddleFreezeWasEnabled = false;

        if (modeSwitching == null || modeSwitching.SelectMotorCollider == null)
        {
            return;
        }

        SelectMotorCollider smc = modeSwitching.SelectMotorCollider;
        smc.thumbFreezeEnabled = false;
        smc.indexFreezeEnabled = false;
        smc.middleFreezeEnabled = false;
    }

    private void ApplyResetRotations()
    {
        if (ThumbAngle1Center != null)
            ThumbAngle1Center.localRotation = ThumbAngle1CenterInitialRotation;
        if (ThumbAngle2Center != null)
            ThumbAngle2Center.localRotation = ThumbAngle2CenterInitialRotation;
        if (ThumbAngle3Center != null)
            ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle0, 0f, 0f);
        if (ThumbAngle4Center != null)
            ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1, 0f, 0f);

        if (IndexAngle1Center != null)
            IndexAngle1Center.localRotation = IndexAngle1CenterInitialRotation;
        if (IndexAngle2Center != null)
            IndexAngle2Center.localRotation = IndexAngle2CenterInitialRotation;
        if (IndexAngle3Center != null)
            IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);
        if (IndexAngle4Center != null)
            IndexAngle4Center.localRotation = Quaternion.Euler(jointAngle.indexAngle2, 0f, 0f);

        if (MiddleAngle1Center != null)
            MiddleAngle1Center.localRotation = MiddleAngle1CenterInitialRotation;
        if (MiddleAngle2Center != null)
            MiddleAngle2Center.localRotation = MiddleAngle2CenterInitialRotation;
        if (MiddleAngle3Center != null)
            MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);
        if (MiddleAngle4Center != null)
            MiddleAngle4Center.localRotation = Quaternion.Euler(jointAngle.middleAngle2, 0f, 0f);
    }
    #endregion

    #region KeyboardControl
    // ==============================
    // 🔹 Keyboard Control Methods
    // ==============================

    /// <summary>
    /// Called when useKeyboardControl is toggled ON.
    /// Resets all offsets/rotations, disables ModeSwitching and all colliders,
    /// sets all joints to originalColor, then highlights selected motor.
    /// </summary>
    void EnterKeyboardMode()
    {
        ResetFingerRotations();
        ResetModeSwitchingState();
        SetCollidersEnabled(false);
        if (modeSwitching != null) modeSwitching.enabled = false;
        kbCurrentRow = 3;
        kbCurrentCol = 1;
        KbSetAllColors(originalColor);
        KbUpdateSelection();
    }

    /// <summary>
    /// Called when useKeyboardControl is toggled OFF (back to embodiment).
    /// Resets all offsets/rotations, re-enables ModeSwitching and all colliders,
    /// sets embodiment initial colors (fingertips=originalColor, others=gray).
    /// </summary>
    void ExitKeyboardMode()
    {
        ResetFingerRotations();
        ResetModeSwitchingState();
        SetCollidersEnabled(true);
        if (modeSwitching != null) modeSwitching.enabled = true;
        SetEmbodimentInitialColors();
        kbCurrentSelectedRenderer = null;
    }

    /// <summary>
    /// Enable or disable all trigger colliders and SelectMotorCollider.
    /// When disabled, no hand-tracking collision detection occurs.
    /// </summary>
    void SetCollidersEnabled(bool enabled)
    {
        if (modeSwitching != null && modeSwitching.SelectMotorCollider != null)
            modeSwitching.SelectMotorCollider.enabled = enabled;

        SetTriggerColliderEnabled(triggerRightIndexTip, enabled);
        SetTriggerColliderEnabled(triggerRightMiddleTip, enabled);
        SetTriggerColliderEnabled(triggerRightThumbTip, enabled);
        SetTriggerColliderEnabled(triggerRightThumbAbduction, enabled);
        SetTriggerColliderEnabled(triggerThumbInnerExtension, enabled);
        SetTriggerColliderEnabled(triggerIndexInnerExtension, enabled);
        SetTriggerColliderEnabled(triggerMiddleInnerExtension, enabled);
    }

    void SetTriggerColliderEnabled(MonoBehaviour trigger, bool enabled)
    {
        if (trigger == null) return;
        Collider col = trigger.GetComponent<Collider>();
        if (col != null) col.enabled = enabled;
    }

    /// <summary>
    /// Resets ModeSwitching back to its initial select state.
    /// </summary>
    void ResetModeSwitchingState()
    {
        if (modeSwitching != null)
        {
            modeSwitching.modeSelect = true;
            modeSwitching.motorSelected = false;
            modeSwitching.modeManipulate = false;
            modeSwitching.confirmedMotorID = 0;
            modeSwitching.currentRedMotorID = 0;
            modeSwitching.lastTouchedMotorID = 0;

            if (modeSwitching.SelectMotorCollider != null)
            {
                modeSwitching.SelectMotorCollider.RestoreDebugVisuals();
                modeSwitching.SelectMotorCollider.ReleaseFrozenLine();
                modeSwitching.SelectMotorCollider.ResetFingertipConfirmation();
            }

            if (modeSwitching.useFingertipFirst)
            {
                modeSwitching.currentPhase = ModeSwitching.SelectionPhase.SelectingFingertip;
                modeSwitching.confirmedFingertipID = 0;
            }
        }
    }

    private void RestoreAllMotorColorsToOriginal()
    {
        if (thumbJoint1Renderer != null) thumbJoint1Renderer.material.color = originalColor;
        if (thumbJoint2Renderer != null) thumbJoint2Renderer.material.color = originalColor;
        if (thumbJoint3Renderer != null) thumbJoint3Renderer.material.color = originalColor;
        if (thumbJoint4Renderer != null) thumbJoint4Renderer.material.color = originalColor;

        if (indexJoint1Renderer != null) indexJoint1Renderer.material.color = originalColor;
        if (indexJoint2Renderer != null) indexJoint2Renderer.material.color = originalColor;
        if (indexJoint3Renderer != null) indexJoint3Renderer.material.color = originalColor;
        if (indexJoint4Renderer != null) indexJoint4Renderer.material.color = originalColor;

        if (middleJoint1Renderer != null) middleJoint1Renderer.material.color = originalColor;
        if (middleJoint2Renderer != null) middleJoint2Renderer.material.color = originalColor;
        if (middleJoint3Renderer != null) middleJoint3Renderer.material.color = originalColor;
        if (middleJoint4Renderer != null) middleJoint4Renderer.material.color = originalColor;
    }

    private void ForceAllPaxiniOffAndRestoreColor()
    {
        SelectMotorCollider smc = (modeSwitching != null) ? modeSwitching.SelectMotorCollider : null;
        if (smc != null)
        {
            smc.ForcePaxiniOffForMotor(1);
            smc.ForcePaxiniOffForMotor(5);
            smc.ForcePaxiniOffForMotor(9);
            return;
        }

        if (triggerRightThumbTip != null)
        {
            triggerRightThumbTip.showFreezeColor = false;
            triggerRightThumbTip.freezeDisplayColor = triggerRightThumbTip.originalColor;
            if (triggerRightThumbTip.thumbPaxiniRenderer != null)
                triggerRightThumbTip.thumbPaxiniRenderer.material.color = triggerRightThumbTip.originalColor;
        }

        if (triggerRightIndexTip != null)
        {
            triggerRightIndexTip.showFreezeColor = false;
            triggerRightIndexTip.freezeDisplayColor = triggerRightIndexTip.originalColor;
            if (triggerRightIndexTip.indexPaxiniRenderer != null)
                triggerRightIndexTip.indexPaxiniRenderer.material.color = triggerRightIndexTip.originalColor;
        }

        if (triggerRightMiddleTip != null)
        {
            triggerRightMiddleTip.showFreezeColor = false;
            triggerRightMiddleTip.freezeDisplayColor = triggerRightMiddleTip.originalColor;
            if (triggerRightMiddleTip.middlePaxiniRenderer != null)
                triggerRightMiddleTip.middlePaxiniRenderer.material.color = triggerRightMiddleTip.originalColor;
        }
    }

    /// <summary>
    /// Sets embodiment initial colors: fingertips (row 3 / joint4) = originalColor,
    /// other 9 joints (rows 0-2 / joint1-3) = gray.
    /// </summary>
    void SetEmbodimentInitialColors()
    {
        Color gray = modeSwitching != null ? modeSwitching.grayColor : new Color(0.5f, 0.5f, 0.5f, 1f);

        // Rows 0-2 (joint1, joint2, joint3) = gray
        thumbJoint1Renderer.material.color = gray;
        thumbJoint2Renderer.material.color = gray;
        thumbJoint3Renderer.material.color = gray;
        indexJoint1Renderer.material.color = gray;
        indexJoint2Renderer.material.color = gray;
        indexJoint3Renderer.material.color = gray;
        middleJoint1Renderer.material.color = gray;
        middleJoint2Renderer.material.color = gray;
        middleJoint3Renderer.material.color = gray;

        // Row 3 (joint4 = fingertips) = originalColor
        thumbJoint4Renderer.material.color = originalColor;
        indexJoint4Renderer.material.color = originalColor;
        middleJoint4Renderer.material.color = originalColor;
    }

    /// <summary>
    /// Sets all motor renderer colors to the given color.
    /// </summary>
    void KbSetAllColors(Color color)
    {
        for (int row = 0; row < KB_ROWS; row++)
        {
            for (int col = 0; col < KB_COLS; col++)
            {
                Renderer renderer = kbRendererArray[row, col];
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }
    }

    void KbUpdateSelection()
    {
        // Restore previous selection to white
        if (kbCurrentSelectedRenderer != null)
        {
            kbCurrentSelectedRenderer.material.color = originalColor;
        }

        kbCurrentSelectedRenderer = kbRendererArray[kbCurrentRow, kbCurrentCol];
        if (kbCurrentSelectedRenderer != null)
        {
            Transform selectedTransform = kbMotorArray[kbCurrentRow, kbCurrentCol];
            if (selectedTransform == IndexAngle1Center || selectedTransform == IndexAngle2Center ||
                selectedTransform == IndexAngle3Center || selectedTransform == IndexAngle4Center)
                kbCurrentSelectedRenderer.material.color = Color.red;
            else
                kbCurrentSelectedRenderer.material.color = Color.green;
        }
    }

    void HandleKeyboardControl()
    {
        // WASD navigation
        bool moved = false;
        if (Input.GetKeyDown(KeyCode.W)) { kbCurrentRow = (kbCurrentRow + 1) % KB_ROWS; moved = true; }
        else if (Input.GetKeyDown(KeyCode.S)) { kbCurrentRow = (kbCurrentRow - 1 + KB_ROWS) % KB_ROWS; moved = true; }
        else if (Input.GetKeyDown(KeyCode.A)) { kbCurrentCol = (kbCurrentCol - 1 + KB_COLS) % KB_COLS; moved = true; }
        else if (Input.GetKeyDown(KeyCode.D)) { kbCurrentCol = (kbCurrentCol + 1) % KB_COLS; moved = true; }
        if (moved) KbUpdateSelection();

        // Q/E rotation for selected motor
        float rotDelta = kbRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) KbApplyRotation(kbCurrentRow, kbCurrentCol, -rotDelta);
        if (Input.GetKey(KeyCode.E)) KbApplyRotation(kbCurrentRow, kbCurrentCol, rotDelta);

        // U/J - all Row 3 (Angle4) motors
        if (Input.GetKey(KeyCode.U)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(3, c, -rotDelta); }
        if (Input.GetKey(KeyCode.J)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(3, c, rotDelta); }

        // I/K - all Row 2 (Angle3) motors
        if (Input.GetKey(KeyCode.I)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(2, c, -rotDelta); }
        if (Input.GetKey(KeyCode.K)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(2, c, rotDelta); }

        // P - reset all keyboard offsets
        if (Input.GetKeyDown(KeyCode.P))
        {
            ForceDisengageOnReset();

            currentThumbRotationY = 0f;
            currentThumbRotationZ = 0f;
            hasThumbAbductionAdjustment = false;
            currentThumbInnerExtensionRotationZ = 0f;
            currentThumbTipRotationZ = 0f;
            currentIndexRotationYMax = 0f;
            currentIndexRotationYMin = 0f;
            currentIndexRotationZMax = 0f;
            currentIndexRotationZMin = 0f;
            currentIndexInnerExtensionRotationZ = 0f;
            currentIndexTipRotationZ = 0f;
            currentMiddleRotationYMax = -60f;
            currentMiddleRotationYMin = 0f;
            currentMiddleRotationZ = 0f;
            currentMiddleRotationZMax = 0f;
            currentMiddleRotationZMin = 0f;
            currentMiddleInnerExtensionRotationZ = 0f;
            currentMiddleTipRotationZ = 0f;

            thumbGripperJoint1MaxRotationVector = ThumbAngle1CenterInitialRotation.eulerAngles;
            thumbGripperJoint2MaxRotationVector = ThumbAngle2CenterInitialRotation.eulerAngles;
            indexGripperJoint1MaxRotationVector = GetIndexJoint1MaxRotationVector();
            indexGripperJoint1MinRotationVector =
                (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, 60f, 0f)).eulerAngles;
            indexGripperJoint2MaxRotationVector = IndexAngle2CenterInitialRotation.eulerAngles;
            indexGripperJoint2MinRotationVector = IndexAngle2CenterInitialRotation.eulerAngles;
            middleGripperJoint1MaxRotationVector = GetMiddleJoint1MaxRotationVector();
            middleGripperJoint1MinRotationVector = GetMiddleJoint1MinRotationVector();
            middleGripperJoint2MaxRotationVector = GetMiddleJoint2MaxRotationVector();
            middleGripperJoint2MinRotationVector = MiddleAngle2CenterInitialRotation.eulerAngles;
        }
    }

    /// <summary>
    /// Apply keyboard rotation delta to the correct offset variable for [row, col],
    /// using the same clamp limits as the manipulation functions.
    /// Also updates the MaxRotationVector so isFullRangeMapping formulas stay consistent.
    /// </summary>
    void KbApplyRotation(int row, int col, float delta)
    {
        // Row 0 = Angle1 (Y-axis), Row 1 = Angle2 (Z-axis), Row 2 = Angle3 (X inner), Row 3 = Angle4 (X tip)
        // Col 0 = Thumb, Col 1 = Index, Col 2 = Middle
        switch (row)
        {
            case 0: // Y-axis motors
                switch (col)
                {
                    case 0:
                        currentThumbRotationY += delta;
                        currentThumbRotationY = Mathf.Clamp(currentThumbRotationY, -60f, 60f);
                        thumbGripperJoint1MaxRotationVector =
                            (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, currentThumbRotationY, 0f)).eulerAngles;
                        break;
                    case 1:
                        if (delta < 0f)
                        {
                            currentIndexRotationYMax += delta;
                            currentIndexRotationYMax = Mathf.Clamp(currentIndexRotationYMax, -90f, 0f);
                            indexGripperJoint1MaxRotationVector = GetIndexJoint1MaxRotationVector();
                        }
                        else if (delta > 0f)
                        {
                            if (currentIndexRotationYMin <= 0f)
                                currentIndexRotationYMin = 60f;

                            currentIndexRotationYMin += delta;
                            currentIndexRotationYMin = Mathf.Clamp(currentIndexRotationYMin, 0f, 90f);
                            indexGripperJoint1MinRotationVector =
                                (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationYMin, 0f)).eulerAngles;
                        }
                        break;
                    case 2:
                        if (delta < 0f)
                        {
                            currentMiddleRotationYMax += delta;
                            currentMiddleRotationYMax = Mathf.Clamp(currentMiddleRotationYMax, -90f, 0f);
                            middleGripperJoint1MaxRotationVector = GetMiddleJoint1MaxRotationVector();
                            maxMiddleYAxisAngle = NormalizeMiddleJoint1MaxAngle(middleGripperJoint1MaxRotationVector.y);
                            RefreshMiddleJoint1YDebug("KbApplyRotation:max");
                        }
                        else if (delta > 0f)
                        {
                            currentMiddleRotationYMin += delta;
                            currentMiddleRotationYMin = Mathf.Clamp(currentMiddleRotationYMin, 0f, 90f);
                            middleGripperJoint1MinRotationVector = GetMiddleJoint1MinRotationVector();
                            minMiddleYAxisAngle = NormalizeAngle(middleGripperJoint1MinRotationVector.y);
                            RefreshMiddleJoint1YDebug("KbApplyRotation:min");
                        }
                        break;
                }
                break;

            case 1: // Z-axis motors
                switch (col)
                {
                    case 0:
                        currentThumbRotationZ += delta;
                        currentThumbRotationZ = Mathf.Clamp(currentThumbRotationZ, -60f, 60f);
                        hasThumbAbductionAdjustment = true;
                        thumbGripperJoint2MaxRotationVector =
                            (ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentThumbRotationZ)).eulerAngles;
                        break;
                    case 1:
                        currentIndexRotationZMax += delta;
                        currentIndexRotationZMax = Mathf.Clamp(currentIndexRotationZMax, -58f, 0f);
                        indexGripperJoint2MaxRotationVector =
                            (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZMax)).eulerAngles;
                        break;
                    case 2:
                        currentMiddleRotationZ += delta;
                        currentMiddleRotationZ = Mathf.Clamp(currentMiddleRotationZ, 0f, 58f);
                        middleGripperJoint2MaxRotationVector =
                            (MiddleAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentMiddleRotationZ)).eulerAngles;
                        break;
                }
                break;

            case 2: // X-axis inner extension motors
                switch (col)
                {
                    case 0:
                        currentThumbInnerExtensionRotationZ += delta;
                        currentThumbInnerExtensionRotationZ = Mathf.Clamp(currentThumbInnerExtensionRotationZ, ExtensionClampMin, ExtensionClampMax);
                        break;
                    case 1:
                        currentIndexInnerExtensionRotationZ += delta;
                        currentIndexInnerExtensionRotationZ = Mathf.Clamp(currentIndexInnerExtensionRotationZ, ExtensionClampMin, ExtensionClampMax);
                        break;
                    case 2:
                        currentMiddleInnerExtensionRotationZ += delta;
                        currentMiddleInnerExtensionRotationZ = Mathf.Clamp(currentMiddleInnerExtensionRotationZ, ExtensionClampMin, ExtensionClampMax);
                        break;
                }
                break;

            case 3: // X-axis tip extension motors
                switch (col)
                {
                    case 0:
                        currentThumbTipRotationZ += delta;
                        currentThumbTipRotationZ = Mathf.Clamp(currentThumbTipRotationZ, ExtensionClampMin, ExtensionClampMax);
                        break;
                    case 1:
                        currentIndexTipRotationZ += delta;
                        currentIndexTipRotationZ = Mathf.Clamp(currentIndexTipRotationZ, ExtensionClampMin, ExtensionClampMax);
                        break;
                    case 2:
                        currentMiddleTipRotationZ += delta;
                        currentMiddleTipRotationZ = Mathf.Clamp(currentMiddleTipRotationZ, ExtensionClampMin, ExtensionClampMax);
                        break;
                }
                break;
        }
    }

    private void RefreshMiddleJoint1YDebug(string source)
    {
        middleJoint1YDebugSource = source;
        middleJoint1MaxRawYDebug = middleGripperJoint1MaxRotationVector.y;
        middleJoint1MinRawYDebug = middleGripperJoint1MinRotationVector.y;
        middleJoint1MaxNormalizedYDebug = NormalizeMiddleJoint1MaxAngle(middleGripperJoint1MaxRotationVector.y);
        middleJoint1MinNormalizedYDebug = NormalizeAngle(middleGripperJoint1MinRotationVector.y);
        middleJoint1LocalEulerYDebug = MiddleAngle1Center != null ? MiddleAngle1Center.localEulerAngles.y : float.NaN;
    }
    #endregion
}