using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// i want the same funtionality when the right middle finger is touched (triggerRightThumbTip), just as the same way you do with right index finger

public class JointAngle : MonoBehaviour
{
    [System.Serializable]
    public class RotationPointPair
    {
        public Transform point0;
        public Transform point1;
    }

    [Header("Target Rotation Tag")]
    public string targetRotationTag = "L_IndexTipSmall"; //TODO: left index tip small

    public float leftIndexTipToRightThumbTipAngle; // angle between L_IndexTipSmall local red axis and R_thumb_b local red axis
    public float leftIndexTipToRightIndexTipAngle; // angle between L_IndexTipSmall local red axis and R_index_c local red axis
    public float leftIndexTipToRightMiddleTipAngle; // angle between L_IndexTipSmall local red axis and R_middle_c local red axis
    private Transform leftIndexTipReference;
    
    public GameObject thumbRotationCollider;
    public GameObject indexRotationCollider;
    public GameObject middleRotationCollider;
    public bool thumbRotationColliderMode = false;
    public bool indexRotationColliderMode = false;
    public bool middleRotationColliderMode = false;

    //TODO: thumb legacy mode and new mode, index legacy mode and new mode, middle legacy mode and new mode

    // when thumb rotation collider is touched, enter the old rotation mode (legacy mode) for thumb
    // if leftIndexTipToRightThumbTipAngle is negtive the moment thumbRotationColliderMode beomes true
    // once the thumbRotationColliderMode bocomes false, reset enterRightThumbWithOldRotation to false
    public bool enterRightThumbWithOldRotation = false;

    // when thumb rotation collider is touched, enter the new rotation mode for thumb
    // if leftIndexTipToRightThumbTipAngle is positive the moment thumbRotationColliderMode beomes true
    // once the thumbRotationColliderMode bocomes false, reset enterRightThumbWithNewRotation to false
    public bool enterRightThumbWithNewRotation = false;

    // when index rotation collider is touched, enter the old rotation mode (legacy mode) for index finger
    // if leftIndexTipToRightIndexTipAngle is negtive the moment indexRotationColliderMode beomes true
    // once the indexRotationColliderMode bocomes false, reset enterRightIndexWithOldRotation to false
    public bool enterRightIndexWithOldRotation = false; 

    // when index rotation collider is touched, enter the new rotation mode for index finger
    // if leftIndexTipToRightIndexTipAngle is positive the moment indexRotationColliderMode beomes true
    // once the indexRotationColliderMode bocomes false, reset enterRightIndexWithNewRotation to false
    public bool enterRightIndexWithNewRotation = false;

    // when middle rotation collider is touched, enter the old rotation mode (legacy mode) for middle finger
    // if leftIndexTipToRightMiddleTipAngle is negtive the moment middleRotationColliderMode beomes true
    // once the middleRotationColliderMode bocomes false, reset enterRightMiddleWithOldRotation to false
    public bool enterRightMiddleWithOldRotation = false;
    
    // when middle rotation collider is touched, enter the new rotation mode for middle finger
    // if leftIndexTipToRightMiddleTipAngle is positive the moment middleRotationColliderMode beomes true
    // once the middleRotationColliderMode bocomes false, reset enterRightMiddleWithNewRotation to false
    public bool enterRightMiddleWithNewRotation = false;

    public string thumbRotationDebug = "";
    public string indexRotationDebug = "";
    public string middleRotationDebug = "";
    public string touchRoutingDebug = "";
    
    public Dictionary<string, Transform> joints = new Dictionary<string, Transform>();
    public float thumbAngle0, thumbAngle1, thumbLRAngle;
    public float indexAngle0, indexAngle1, indexAngle2, indexLRAngle;
    public float middleAngle0, middleAngle1, middleAngle2, middleLRAngle;

    private Vector3 palmNormal;
    private Vector3 thumbPlaneNormal;
    private Vector3 initialWristRight;  // Store initial wrist direction
    private Vector3 initialThumbRight;  // Store initial thumb direction
    private Vector3 initialRotationAxis; // Store initial rotation axis to determine direction

    public float indexMiddleDistance;
    public float indexMiddleAngleOnPalm;
    public float indexToBaselineAngleOnPalm;
    public float middleToBaselineAngleOnPalm;
    [Header("Baseline Angle Mode")]
    public bool useConstrainedBaselineAngleRange = true;
    [Tooltip("Shift baseline parallel toward index finger in centimeters.")]
    public float baselineOffsetTowardIndexCm = 0.5f;

    public float thumbPalmAngle;
    public float wristThumbAngle;

    // Public properties to expose plane state
    public bool isPlaneActive { get; private set; } = false;
    public string activeFinger { get; private set; } = "None"; // "Index", "Middle", "Thumb", or "None"
    public float isClockWise;
    public bool indexNewTouch { get; private set; }
    public bool middleNewTouch { get; private set; }
    public bool thumbNewTouch { get; private set; }

    // Reference for twisting
    public TriggerRightIndexTip triggerRightIndexTip;
    public TriggerRightMiddleTip triggerRightMiddleTip;
    public TriggerRightThumbTip triggerRightThumbTip;
    public ModeSwitching modeSwitching;

    [Header("New Rotation Mode")] //FIXME: new rotation mode
    public bool thumbNewRotationMode = false;
    public bool indexNewRotationMode = false;
    public bool middleNewRotationMode = false;

    public Transform L_index_c;

    public Vector3 rotationPoint0Position;
    public Vector3 rotationPoint1Position;
    public Vector3 projectedIndexTip;
    public Vector3 projectedThumbTip;

    [Header("Index Rotation Points")]
    public RotationPointPair indexLegacyPoints = new RotationPointPair();
    public RotationPointPair indexNewPoints = new RotationPointPair();

    [Header("Middle Rotation Points")]
    public RotationPointPair middleLegacyPoints = new RotationPointPair();
    public RotationPointPair middleNewPoints = new RotationPointPair();

    [Header("Thumb Rotation Points")]
    public RotationPointPair thumbLegacyPoints = new RotationPointPair();
    public RotationPointPair thumbNewPoints = new RotationPointPair();

    private const string RotationPoint0Key = "Rotation_Point_0";
    private const string RotationPoint1Key = "Rotation_Point_1";

    [Header("Debug Visuals")]
    public bool showDebugVisuals = false; // Set to false to hide the red line and green plane
    public bool showIndexMiddleBaseline = true;
    public float baselineLineLength = 0.1f;
    public Color baselineLineColor = Color.cyan;
    [Header("Angle Calculation Visuals")]
    public bool showIndexAngleVisualization = false;
    public bool showMiddleAngleVisualization = false;
    public Color indexProjectionLineColor = new Color(1f, 0.6f, 0f, 1f);
    public Color indexAngleVectorLineColor = new Color(1f, 0.2f, 0.2f, 1f);
    public Color middleProjectionLineColor = new Color(0.2f, 0.7f, 1f, 1f);
    public Color middleAngleVectorLineColor = new Color(0.2f, 1f, 0.4f, 1f);

    private LineRenderer lineRenderer;
    private LineRenderer baselineLineRenderer;
    private LineRenderer indexProjectionLineRenderer;
    private LineRenderer indexAngleVectorLineRenderer;
    private LineRenderer middleProjectionLineRenderer;
    private LineRenderer middleAngleVectorLineRenderer;
    private GameObject debugPlane;

    private Vector3 previousIndexTip;
    private Vector3 previousThumbTip;
    private bool hasPreviousFrame = false;
    private float lastRotationDirection = 1f; // Default to clockwise
    private float noRotationTimer = 0f; // Timer for no rotation detection

    private Queue<float> rotationHistory = new Queue<float>();
    private const int ROTATION_HISTORY_SIZE = 10; // Look at last 10 frames
    public float rotationChangeTimer = 0f;
    private const float ROTATION_CHANGE_COOLDOWN = 0.3f; // Don't change direction more than once per 0.3 seconds
    public float cumulativeRotation = 0f; // Track total rotation
    private const float MIN_ROTATION_THRESHOLD = 0.02f; // Minimum rotation to consider
    public float publiAaverageRotation = 0f;
    private float nextDiagLogTime = 0f;
    private const float DIAG_LOG_INTERVAL = 0.5f;
    private bool _lastModeManipulate = false;
    private bool _lastIndexRotationColliderMode = false;
    private bool _lastMiddleRotationColliderMode = false;
    private bool _lastThumbRotationColliderMode = false;

    // ─── Angle Smoothing ─────────────────────────────────────────────────────
    private struct AngleFilterState { public bool init; public float val; }

    [Header("Angle Smoothing")]
    [Tooltip("ON: apply delta-clamp outlier reject + EMA to all raw joint angles before they are read by ClawModuleController.")]
    public bool enableAngleSmoothing = true;
    [Range(0.05f, 1f)]
    [Tooltip("EMA alpha for bone-flexion angles (thumbAngle1, indexAngle1/2, middleAngle1/2). Lower = smoother but more lag.")]
    public float flexionSmoothAlpha = 0.25f;
    [Range(0.05f, 1f)]
    [Tooltip("EMA alpha for palm-plane angles (thumbPalmAngle, wristThumbAngle, indexMiddleAngleOnPalm, baselines).")]
    public float palmAngleSmoothAlpha = 0.15f;
    [Tooltip("Max degrees a bone-flexion angle may jump in one frame before being clamped.")]
    public float flexionMaxDeltaDeg = 12f;
    [Tooltip("Max degrees a palm-plane angle may jump in one frame before being clamped.")]
    public float palmMaxDeltaDeg = 8f;

    private AngleFilterState _fThumbA0, _fThumbA1;
    private AngleFilterState _fIndexA0, _fIndexA1, _fIndexA2;
    private AngleFilterState _fMiddleA0, _fMiddleA1, _fMiddleA2;
    private AngleFilterState _fThumbPalm, _fWristThumb;
    private AngleFilterState _fIdxMidAngle, _fIdxBaseline, _fMidBaseline;

    void AssignJointIfFound(string key, string objectName)
    {
        GameObject obj = GameObject.Find(objectName);
        if (obj != null)
        {
            joints[key] = obj.transform;
        }
        else
        {
            Debug.LogWarning("[JointAngle][Init] Missing GameObject: " + objectName + " for joint key: " + key, this);
        }
    }

    bool TryGetRotationPointPair(RotationPointPair pair, out Vector3 point0, out Vector3 point1)
    {
        point0 = Vector3.zero;
        point1 = Vector3.zero;

        if (pair == null || pair.point0 == null || pair.point1 == null)
            return false;

        point0 = pair.point0.position;
        point1 = pair.point1.position;
        return true;
    }

    string GetFingerRouteFromMotorID(int motorID)
    {
        if (motorID >= 1 && motorID <= 4)
            return "Thumb";
        if (motorID >= 5 && motorID <= 8)
            return "Index";
        if (motorID >= 9 && motorID <= 12)
            return "Middle";
        return "None";
    }

    string GetPairDebugName(RotationPointPair pair)
    {
        string point0Name = pair != null && pair.point0 != null ? pair.point0.name : "null";
        string point1Name = pair != null && pair.point1 != null ? pair.point1.name : "null";
        return point0Name + " | " + point1Name;
    }

    bool ValidateCriticalReferences(string stage)
    {
        List<string> issues = new List<string>();

        if (lineRenderer == null)
            issues.Add("lineRenderer is null");
        if (debugPlane == null)
            issues.Add("debugPlane is null");
        if (!joints.ContainsKey("Thumb0") || joints["Thumb0"] == null)
            issues.Add("joint Thumb0 missing/null");
        if (!joints.ContainsKey("Thumb1") || joints["Thumb1"] == null)
            issues.Add("joint Thumb1 missing/null");
        if (!joints.ContainsKey("Index0") || joints["Index0"] == null)
            issues.Add("joint Index0 missing/null");
        if (!joints.ContainsKey("Index1") || joints["Index1"] == null)
            issues.Add("joint Index1 missing/null");
        if (!joints.ContainsKey("Index2") || joints["Index2"] == null)
            issues.Add("joint Index2 missing/null");
        if (!joints.ContainsKey("Middle0") || joints["Middle0"] == null)
            issues.Add("joint Middle0 missing/null");
        if (!joints.ContainsKey("Middle1") || joints["Middle1"] == null)
            issues.Add("joint Middle1 missing/null");
        if (!joints.ContainsKey("Middle2") || joints["Middle2"] == null)
            issues.Add("joint Middle2 missing/null");
        if (!joints.ContainsKey("Wrist") || joints["Wrist"] == null)
            issues.Add("joint Wrist missing/null");
        if (!joints.ContainsKey("PalmIndex") || joints["PalmIndex"] == null)
            issues.Add("joint PalmIndex missing/null");
        if (!joints.ContainsKey("PalmRing") || joints["PalmRing"] == null)
            issues.Add("joint PalmRing missing/null");

        if (issues.Count > 0)
        {
            if (Time.time >= nextDiagLogTime)
            {
                Debug.LogError("[JointAngle][Diag][" + stage + "] " + string.Join(" | ", issues), this);
                nextDiagLogTime = Time.time + DIAG_LOG_INTERVAL;
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// Outlier-reject then EMA-smooth a single angle value.
    /// Call with the raw computed value; the filter state is updated in place.
    /// When enableAngleSmoothing is false the raw value passes through unchanged.
    /// </summary>
    private float ApplyAngleFilter(float raw, ref AngleFilterState f, float maxDelta, float alpha)
    {
        if (!enableAngleSmoothing) return raw;
        if (!f.init) { f.init = true; f.val = raw; return raw; }
        float delta = raw - f.val;
        if (Mathf.Abs(delta) > maxDelta)
            raw = f.val + Mathf.Sign(delta) * maxDelta;
        f.val = Mathf.Lerp(f.val, raw, alpha);
        return f.val;
    }

    void Start()
    {
        // Thumb joints
        AssignJointIfFound("Thumb0", "R_thumb_a");
        AssignJointIfFound("Thumb1", "R_thumb_b"); //TODO: thumb tip
        // joints["ThumbM"] = GameObject.Find("R_thumb_Proximal").transform;

        // Index joints
        AssignJointIfFound("Index0", "R_index_Proximal");
        AssignJointIfFound("Index1", "R_index_b");
        AssignJointIfFound("Index2", "R_index_c"); //TODO: index tip
        // joints["IndexM"] = GameObject.Find("R_index_meta").transform;

        // Middle joints
        AssignJointIfFound("Middle0", "R_middle_Proximal");
        AssignJointIfFound("Middle1", "R_middle_b");
        AssignJointIfFound("Middle2", "R_middle_c"); //TODO: middle tip
        // joints["MiddleM"] = GameObject.Find("R_middle_meta").transform;

        // Points needed for forming the basic plane of the palm
        AssignJointIfFound("Wrist", "R_Wrist");
        AssignJointIfFound("Elbow", "Elbow");
        AssignJointIfFound("PalmIndex", "R_index_Proximal");
        AssignJointIfFound("PalmRing", "R_ring_Proximal");

        AssignJointIfFound("L_index0", "L_index_Proximal");
        AssignJointIfFound("L_Thumb_Tip", "L_Thumb_Tip");

        ResolveLeftIndexTipReference();

        thumbAngle0 = 0f;
        thumbAngle1 = 0f;
        indexAngle0 = 0f;
        indexAngle1 = 0f;
        indexAngle2 = 0f;
        middleAngle0 = 0f;
        middleAngle1 = 0f;
        middleAngle2 = 0f;
        thumbLRAngle = 0f;
        indexLRAngle = 0f;
        middleLRAngle = 0f;

        leftIndexTipToRightThumbTipAngle = 0f;
        leftIndexTipToRightIndexTipAngle = 0f;
        leftIndexTipToRightMiddleTipAngle = 0f;

        indexMiddleDistance = 0f;
        indexToBaselineAngleOnPalm = 0f;
        middleToBaselineAngleOnPalm = 0f;

        // Initialize wrist-thumb reference vectors
        if (joints.ContainsKey("Wrist") && joints.ContainsKey("Thumb0"))
        {
            initialWristRight = joints["Wrist"].right;
            initialThumbRight = joints["Thumb0"].right;
            // Calculate initial rotation axis to determine closing direction
            initialRotationAxis = Vector3.Cross(initialWristRight, initialThumbRight).normalized;
        }

        // Optionally auto-find if not assigned in Inspector
        if (triggerRightIndexTip == null)
            triggerRightIndexTip = FindObjectOfType<TriggerRightIndexTip>();

        if (triggerRightMiddleTip == null)
            triggerRightMiddleTip = FindObjectOfType<TriggerRightMiddleTip>();

        if (triggerRightThumbTip == null)
            triggerRightThumbTip = FindObjectOfType<TriggerRightThumbTip>();

        if (modeSwitching == null)
            modeSwitching = FindObjectOfType<ModeSwitching>();

        SetupRotationColliderDetectors();
        ResetRotationColliderModes();
        _lastModeManipulate = modeSwitching != null && modeSwitching.modeManipulate;

        // Create LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.positionCount = 2;

        // Create baseline LineRenderer used for index-middle independent angle reference
        baselineLineRenderer = new GameObject("IndexMiddleBaselineLine").AddComponent<LineRenderer>();
        baselineLineRenderer.transform.SetParent(transform, false);
        baselineLineRenderer.startWidth = 0.003f;
        baselineLineRenderer.endWidth = 0.003f;
        baselineLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        baselineLineRenderer.startColor = baselineLineColor;
        baselineLineRenderer.endColor = baselineLineColor;
        baselineLineRenderer.positionCount = 2;
        baselineLineRenderer.enabled = false;

        indexProjectionLineRenderer = CreateDebugLineRenderer(
            "IndexProjectionLine",
            indexProjectionLineColor,
            0.0025f
        );
        indexAngleVectorLineRenderer = CreateDebugLineRenderer(
            "IndexAngleVectorLine",
            indexAngleVectorLineColor,
            0.003f
        );
        middleProjectionLineRenderer = CreateDebugLineRenderer(
            "MiddleProjectionLine",
            middleProjectionLineColor,
            0.0025f
        );
        middleAngleVectorLineRenderer = CreateDebugLineRenderer(
            "MiddleAngleVectorLine",
            middleAngleVectorLineColor,
            0.003f
        );

        // Create debug plane
        debugPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        debugPlane.transform.localScale = new Vector3(0.05f, 1f, 0.05f);
        debugPlane.name = "Index1_DebugPlane";

        // Make it semi-transparent and double-sided
        Renderer planeRenderer = debugPlane.GetComponent<Renderer>();
        Material planeMaterial = new Material(Shader.Find("Standard"));
        planeMaterial.color = new Color(0f, 1f, 0f, 0.3f);
        planeMaterial.SetFloat("_Mode", 3); // Transparent mode
        planeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        planeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        planeMaterial.SetInt("_ZWrite", 0);
        planeMaterial.SetInt("_Cull", 0); // Disable backface culling (0 = Off, shows both sides)
        planeMaterial.DisableKeyword("_ALPHATEST_ON");
        planeMaterial.EnableKeyword("_ALPHABLEND_ON");
        planeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        planeMaterial.renderQueue = 3000;
        planeRenderer.material = planeMaterial;
    }

    void Update()
    {
        bool isValidForUpdate = ValidateCriticalReferences("UpdateStart");
        if (!isValidForUpdate)
        {
            touchRoutingDebug = "Update aborted: ValidateCriticalReferences(UpdateStart) returned false";
            thumbRotationDebug = "Update aborted before thumb debug. Check Console for [JointAngle][Diag][UpdateStart].";
            indexRotationDebug = "Update aborted before index debug. Check Console for [JointAngle][Diag][UpdateStart].";
            middleRotationDebug = "Update aborted before middle debug. Check Console for [JointAngle][Diag][UpdateStart].";
            return;
        }

        UpdatePalmNormal();

        UpdateThumbPlane(); // Uncomment this line

        thumbPalmAngle    = ApplyAngleFilter(UpdateThumbPalmAngle(),     ref _fThumbPalm,   palmMaxDeltaDeg,    palmAngleSmoothAlpha);
        wristThumbAngle   = ApplyAngleFilter(GetWristThumbAngle(),        ref _fWristThumb,  palmMaxDeltaDeg,    palmAngleSmoothAlpha);

        // thumbAngle0 = GetThumbAngle("Thumb0");
        thumbAngle0 = ApplyAngleFilter(
            joints["Thumb0"].localEulerAngles.z < 100 ? 0 : 360 - joints["Thumb0"].localEulerAngles.z,
            ref _fThumbA0, flexionMaxDeltaDeg, flexionSmoothAlpha);
        thumbAngle1 = ApplyAngleFilter(GetJointAngle("Thumb1", "Thumb0"), ref _fThumbA1,  flexionMaxDeltaDeg, flexionSmoothAlpha);
        // thumbLRAngle = GetRotateAngle("ThumbM", "Thumb0", "Thumb1");

        indexAngle0 = ApplyAngleFilter(GetJointPalmAngle("Index0"),        ref _fIndexA0, flexionMaxDeltaDeg, flexionSmoothAlpha);
        indexAngle1 = ApplyAngleFilter(GetJointAngle("Index1", "Index0"),  ref _fIndexA1, flexionMaxDeltaDeg, flexionSmoothAlpha);
        indexAngle2 = ApplyAngleFilter(GetJointAngle("Index2", "Index1"),  ref _fIndexA2, flexionMaxDeltaDeg, flexionSmoothAlpha);
        // indexLRAngle = GetRotateAngle("IndexM", "Index0", "Index1");

        middleAngle0 = ApplyAngleFilter(GetJointPalmAngle("Middle0"),         ref _fMiddleA0, flexionMaxDeltaDeg, flexionSmoothAlpha);
        middleAngle1 = ApplyAngleFilter(GetJointAngle("Middle1", "Middle0"),  ref _fMiddleA1, flexionMaxDeltaDeg, flexionSmoothAlpha);
        middleAngle2 = ApplyAngleFilter(GetJointAngle("Middle2", "Middle1"),  ref _fMiddleA2, flexionMaxDeltaDeg, flexionSmoothAlpha);
        // middleLRAngle = GetRotateAngle("MiddleM", "Middle0", "Middle1");

        UpdateLeftIndexToRightTipAngles();
        UpdateIndexEntryModeFromAngle();
        UpdateMiddleEntryModeFromAngle();
        UpdateThumbEntryModeFromAngle();

        indexMiddleDistance = GetProjectedDistanceOnPalm("Index1", "Middle1") * 100f;
        indexMiddleAngleOnPalm = GetIndexMiddleAngleOnPalm();
        UpdateIndexMiddleIndependentAnglesAndBaseline();
        // Filter palm-plane angles after their computation functions have written them
        indexMiddleAngleOnPalm       = ApplyAngleFilter(indexMiddleAngleOnPalm,       ref _fIdxMidAngle,  palmMaxDeltaDeg, palmAngleSmoothAlpha);
        indexToBaselineAngleOnPalm   = ApplyAngleFilter(indexToBaselineAngleOnPalm,   ref _fIdxBaseline,  palmMaxDeltaDeg, palmAngleSmoothAlpha);
        middleToBaselineAngleOnPalm  = ApplyAngleFilter(middleToBaselineAngleOnPalm,  ref _fMidBaseline,  palmMaxDeltaDeg, palmAngleSmoothAlpha);

        // Determine which finger to use based on touch detection.
        // Each finger can independently choose legacy mode (2 source points)
        // or new mode (Rotation_Point_0 source + L_index0 anchor).
        bool useIndexFinger = false;
        bool useMiddleFinger = false;
        bool useThumbFinger = false;
        string activeJoint = "Index1";
        Dictionary<string, Vector3> touchedPoints = null;

        Dictionary<string, Vector3> indexTouchPoints = null;
        Dictionary<string, Vector3> middleTouchPoints = null;
        Dictionary<string, Vector3> thumbTouchPoints = null;

        if (triggerRightIndexTip != null)
        {
            indexTouchPoints = triggerRightIndexTip.GetAllTouchedPoints();
            if (indexTouchPoints == null)
            {
                if (Time.time >= nextDiagLogTime)
                {
                    Debug.LogWarning("[JointAngle][TouchDiag] triggerRightIndexTip.GetAllTouchedPoints() returned null", this);
                    nextDiagLogTime = Time.time + DIAG_LOG_INTERVAL;
                }
            }
        }

        if (triggerRightMiddleTip != null)
        {
            middleTouchPoints = triggerRightMiddleTip.GetAllTouchedPoints();
            if (middleTouchPoints == null)
            {
                if (Time.time >= nextDiagLogTime)
                {
                    Debug.LogWarning("[JointAngle][TouchDiag] triggerRightMiddleTip.GetAllTouchedPoints() returned null", this);
                    nextDiagLogTime = Time.time + DIAG_LOG_INTERVAL;
                }
            }
        }

        if (triggerRightThumbTip != null)
        {
            thumbTouchPoints = triggerRightThumbTip.GetAllTouchedPoints();
            if (thumbTouchPoints == null)
            {
                if (Time.time >= nextDiagLogTime)
                {
                    Debug.LogWarning("[JointAngle][TouchDiag] triggerRightThumbTip.GetAllTouchedPoints() returned null", this);
                    nextDiagLogTime = Time.time + DIAG_LOG_INTERVAL;
                }
            }
        }

        bool indexColliderTouched = triggerRightIndexTip != null && triggerRightIndexTip.isRightIndexTipTouched;
        bool middleColliderTouched = triggerRightMiddleTip != null && triggerRightMiddleTip.isRightMiddleTipTouched;
        bool thumbColliderTouched = triggerRightThumbTip != null && triggerRightThumbTip.isRightThumbTipTouched;
        bool hasIndexTouchPoints = indexTouchPoints != null && indexTouchPoints.Count > 0;
        bool hasMiddleTouchPoints = middleTouchPoints != null && middleTouchPoints.Count > 0;
        bool hasThumbTouchPoints = thumbTouchPoints != null && thumbTouchPoints.Count > 0;

        bool hasIndexLegacyPoints = TryGetRotationPointPair(indexLegacyPoints, out Vector3 indexLegacyPoint0, out Vector3 indexLegacyPoint1);
        bool hasIndexNewPoints = TryGetRotationPointPair(indexNewPoints, out Vector3 indexNewPoint0, out Vector3 indexNewPoint1);
        bool hasMiddleLegacyPoints = TryGetRotationPointPair(middleLegacyPoints, out Vector3 middleLegacyPoint0, out Vector3 middleLegacyPoint1);
        bool hasMiddleNewPoints = TryGetRotationPointPair(middleNewPoints, out Vector3 middleNewPoint0, out Vector3 middleNewPoint1);
        bool hasThumbLegacyPoints = TryGetRotationPointPair(thumbLegacyPoints, out Vector3 thumbLegacyPoint0, out Vector3 thumbLegacyPoint1);
        bool hasThumbNewPoints = TryGetRotationPointPair(thumbNewPoints, out Vector3 thumbNewPoint0, out Vector3 thumbNewPoint1);

        int confirmedMotorID = modeSwitching != null ? modeSwitching.confirmedMotorID : 0;
        string routedFinger = GetFingerRouteFromMotorID(confirmedMotorID);
        bool isModeSelect = modeSwitching != null && modeSwitching.modeSelect;
        bool isModeManipulate = modeSwitching != null && modeSwitching.modeManipulate;
        UpdateRotationColliderLatchByMode(isModeManipulate);
        bool allowDirectionUpdate = IsRotationColliderGateOpenForMotorRange(confirmedMotorID, isModeManipulate);

        if (!allowDirectionUpdate)
        {
            // Keep clockwise/counterclockwise neutral until manipulate mode starts.
            isClockWise = 0f;
            hasPreviousFrame = false;
            lastRotationDirection = 1f;
            noRotationTimer = 0f;
            cumulativeRotation = 0f;
            rotationHistory.Clear();
            rotationChangeTimer = 0f;
        }

        bool indexLegacyTouch = routedFinger == "Index" && enterRightIndexWithOldRotation && indexColliderTouched && hasIndexTouchPoints &&
            hasIndexLegacyPoints;
        bool middleLegacyTouch = routedFinger == "Middle" && enterRightMiddleWithOldRotation && middleColliderTouched && hasMiddleTouchPoints &&
            hasMiddleLegacyPoints;
        bool thumbLegacyTouch = routedFinger == "Thumb" && enterRightThumbWithOldRotation && thumbColliderTouched && hasThumbTouchPoints &&
            hasThumbLegacyPoints;

        // Index new/legacy mode is now decided at index rotation collider entry by signed angle.
        indexNewTouch = routedFinger == "Index" && enterRightIndexWithNewRotation && indexColliderTouched && hasIndexTouchPoints && hasIndexNewPoints;

        indexRotationDebug = " " + indexNewTouch + 
        "\nenterRightIndexWithOldRotation: " + enterRightIndexWithOldRotation +
        "\nenterRightIndexWithNewRotation: " + enterRightIndexWithNewRotation +
        "\nleftIndexTipToRightIndexTipAngle: " + leftIndexTipToRightIndexTipAngle +
        "\nindexNewRotationMode(inspector): " + indexNewRotationMode +
        "\nindexColliderTouched: " + indexColliderTouched +
        "\nindexTouchPoints count: " + (indexTouchPoints != null ? indexTouchPoints.Count : 0) + 
        "\nindexLegacyPoints assigned: " + hasIndexLegacyPoints +
        "\nindexNewPoints assigned: " + hasIndexNewPoints;

        middleNewTouch = routedFinger == "Middle" && enterRightMiddleWithNewRotation && middleColliderTouched && hasMiddleTouchPoints && hasMiddleNewPoints;

        middleRotationDebug = " " + middleNewTouch +
        "\nenterRightMiddleWithOldRotation: " + enterRightMiddleWithOldRotation +
        "\nenterRightMiddleWithNewRotation: " + enterRightMiddleWithNewRotation +
        "\nleftIndexTipToRightMiddleTipAngle: " + leftIndexTipToRightMiddleTipAngle +
        "\nmiddleNewRotationMode(inspector): " + middleNewRotationMode +
        "\nmiddleColliderTouched: " + middleColliderTouched +
        "\nmiddleTouchPoints count: " + (middleTouchPoints != null ? middleTouchPoints.Count : 0) + 
        "\nmiddleLegacyPoints assigned: " + hasMiddleLegacyPoints +
        "\nmiddleNewPoints assigned: " + hasMiddleNewPoints;

        thumbNewTouch = routedFinger == "Thumb" && enterRightThumbWithNewRotation && thumbColliderTouched && hasThumbTouchPoints && hasThumbNewPoints;

        thumbRotationDebug = " " + thumbNewTouch +
        "\nenterRightThumbWithOldRotation: " + enterRightThumbWithOldRotation +
        "\nenterRightThumbWithNewRotation: " + enterRightThumbWithNewRotation +
        "\nleftIndexTipToRightThumbTipAngle: " + leftIndexTipToRightThumbTipAngle +
        "\nthumbNewRotationMode: " + thumbNewRotationMode +
        "\ntriggerRightThumbTip == null: " + (triggerRightThumbTip == null) +
        "\nthumbColliderTouched: " + thumbColliderTouched +
        "\nthumbTouchPoints count: " + (thumbTouchPoints != null ? thumbTouchPoints.Count : 0) + 
        "\nthumbLegacyPoints assigned: " + hasThumbLegacyPoints +
        "\nthumbNewPoints assigned: " + hasThumbNewPoints;

        string selectedMode = "None";
        string selectedPair = "None";

        if (indexNewTouch || indexLegacyTouch)
        {
            useIndexFinger = true;
            activeJoint = "Index1";
            selectedMode = indexNewTouch ? "Index-New" : "Index-Legacy";
            selectedPair = indexNewTouch
                ? GetPairDebugName(indexNewPoints)
                : GetPairDebugName(indexLegacyPoints);
            touchedPoints = indexNewTouch
                ? new Dictionary<string, Vector3>
                {
                    [RotationPoint1Key] = indexNewPoint1,
                    [RotationPoint0Key] = indexNewPoint0
                }
                : new Dictionary<string, Vector3>
                {
                    [RotationPoint1Key] = indexLegacyPoint1,
                    [RotationPoint0Key] = indexLegacyPoint0
                };
        }
        else if (middleNewTouch || middleLegacyTouch)
        {
            useMiddleFinger = true;
            activeJoint = "Middle1";
            selectedMode = middleNewTouch ? "Middle-New" : "Middle-Legacy";
            selectedPair = middleNewTouch
                ? GetPairDebugName(middleNewPoints)
                : GetPairDebugName(middleLegacyPoints);
            touchedPoints = middleNewTouch
                ? new Dictionary<string, Vector3>
                {
                    [RotationPoint1Key] = middleNewPoint1,
                    [RotationPoint0Key] = middleNewPoint0
                }
                : new Dictionary<string, Vector3>
                {
                    [RotationPoint1Key] = middleLegacyPoint1,
                    [RotationPoint0Key] = middleLegacyPoint0
                };
        }
        else if (thumbNewTouch || thumbLegacyTouch)
        {
            useThumbFinger = true;
            activeJoint = "Thumb1";
            selectedMode = thumbNewTouch ? "Thumb-New" : "Thumb-Legacy";
            selectedPair = thumbNewTouch
                ? GetPairDebugName(thumbNewPoints)
                : GetPairDebugName(thumbLegacyPoints);
            touchedPoints = thumbNewTouch
                ? new Dictionary<string, Vector3>
                {
                    [RotationPoint1Key] = thumbNewPoint1,
                    [RotationPoint0Key] = thumbNewPoint0
                }
                : new Dictionary<string, Vector3>
                {
                    [RotationPoint1Key] = thumbLegacyPoint1,
                    [RotationPoint0Key] = thumbLegacyPoint0
                };
        }

        touchRoutingDebug =
            "confirmedMotorID: " + confirmedMotorID +
            "\nroutedFinger: " + routedFinger +
            "\nmodeSelect: " + isModeSelect +
            "\nmodeManipulate: " + isModeManipulate +
            "\nallowDirectionUpdate: " + allowDirectionUpdate +
            "\nthumbRotationColliderMode: " + thumbRotationColliderMode +
            "\nindexRotationColliderMode: " + indexRotationColliderMode +
            "\nmiddleRotationColliderMode: " + middleRotationColliderMode +
            "\nindexColliderTouched: " + indexColliderTouched +
            "\nindexTouchPoints count: " + (indexTouchPoints != null ? indexTouchPoints.Count : 0) +
            "\nmiddleColliderTouched: " + middleColliderTouched +
            "\nmiddleTouchPoints count: " + (middleTouchPoints != null ? middleTouchPoints.Count : 0) +
            "\nthumbColliderTouched: " + thumbColliderTouched +
            "\nthumbTouchPoints count: " + (thumbTouchPoints != null ? thumbTouchPoints.Count : 0) +
            "\nindexLegacyTouch: " + indexLegacyTouch +
            "\nindexNewTouch: " + indexNewTouch +
            "\nmiddleLegacyTouch: " + middleLegacyTouch +
            "\nmiddleNewTouch: " + middleNewTouch +
            "\nthumbLegacyTouch: " + thumbLegacyTouch +
            "\nthumbNewTouch: " + thumbNewTouch +
            "\nselectedMode: " + selectedMode +
            "\nselectedPair: " + selectedPair +
            "\nactiveJoint: " + activeJoint;

        // Process touched points and update visualization
        if (allowDirectionUpdate && (useIndexFinger || useMiddleFinger || useThumbFinger) && touchedPoints != null)
        {
            if (touchedPoints.ContainsKey(RotationPoint1Key) && touchedPoints.ContainsKey(RotationPoint0Key))
            {
                rotationPoint0Position = touchedPoints[RotationPoint0Key];
                rotationPoint1Position = touchedPoints[RotationPoint1Key];

                // Project positions onto the debug plane
                if (debugPlane != null && joints.ContainsKey(activeJoint))
                {
                    Transform activeFingerJoint = joints[activeJoint];
                    Vector3 planeNormal = activeFingerJoint.right;

                    // UPDATE PLANE POSITION FIRST, BEFORE PROJECTION
                    debugPlane.transform.position = activeFingerJoint.position + activeFingerJoint.right * -0.22f;
                    debugPlane.transform.rotation = Quaternion.LookRotation(activeFingerJoint.up, activeFingerJoint.right);

                    // NOW get the updated plane position for projection
                    Vector3 planePoint = debugPlane.transform.position;

                    projectedIndexTip = ProjectPointOnPlane(rotationPoint1Position, planePoint, planeNormal);
                    projectedThumbTip = ProjectPointOnPlane(rotationPoint0Position, planePoint, planeNormal);

                    // Draw red line using raw (unprojected) positions
                    lineRenderer.SetPosition(0, rotationPoint1Position);
                    lineRenderer.SetPosition(1, rotationPoint0Position);

                    // Debug.Log($"Active Joint: {activeJoint}, ProjectedIndex: {projectedIndexTip}, ProjectedThumb: {projectedThumbTip}");

                    // Calculate rotation direction compared to previous frame
                    if (!allowDirectionUpdate)
                    {
                        isClockWise = 0f;
                        hasPreviousFrame = false;
                    }
                    else if (hasPreviousFrame)
                    {
                        float newRotation = GetRotationDirection(
                            previousIndexTip, previousThumbTip,
                            projectedIndexTip, projectedThumbTip,
                            activeJoint
                        );

                        // Add to rotation history
                        rotationHistory.Enqueue(newRotation);
                        if (rotationHistory.Count > ROTATION_HISTORY_SIZE)
                            rotationHistory.Dequeue();

                        // Calculate weighted average (recent frames have more weight)
                        float weightedSum = 0f;
                        float weightTotal = 0f;
                        int index = 0;
                        foreach (float rot in rotationHistory)
                        {
                            float weight = (index + 1) / (float)rotationHistory.Count; // More recent = higher weight
                            weightedSum += rot * weight;
                            weightTotal += weight;
                            index++;
                        }

                        float averageRotation = weightTotal > 0 ? weightedSum / weightTotal : 0f;

                        // Track cumulative rotation magnitude
                        if (newRotation != 0f)
                        {
                            cumulativeRotation += Mathf.Abs(Vector3.Angle(
                                (previousThumbTip - previousIndexTip).normalized,
                                (projectedThumbTip - projectedIndexTip).normalized
                            ));
                        }

                        // Only update if we have clear consensus AND enough rotation AND cooldown expired
                        rotationChangeTimer += Time.deltaTime;
                        publiAaverageRotation = Mathf.Abs(averageRotation); // For debugging

                        if (Mathf.Abs(averageRotation) > 0.7f && // Clear direction (> 50% consensus)
                            cumulativeRotation > 0.04f && // Minimum rotation threshold MIN_ROTATION_THRESHOLD
                            rotationChangeTimer >= ROTATION_CHANGE_COOLDOWN) // Cooldown expired
                        {
                            float newDirection = averageRotation > 0 ? 1f : -1f;
                            // Only change if different from current
                            if (newDirection != isClockWise)
                            {
                                isClockWise = newDirection;
                                lastRotationDirection = newDirection;
                                rotationChangeTimer = 0f; // Reset cooldown
                                cumulativeRotation = 0f; // Reset cumulative rotation
                            }

                            noRotationTimer = 0f;
                        }
                        // else if (cumulativeRotation < MIN_ROTATION_THRESHOLD)
                        // {
                        //     // No meaningful rotation detected
                        //     noRotationTimer += Time.deltaTime;

                        //     if (noRotationTimer >= 1f)
                        //     {
                        //         isClockWise = 0f;
                        //         cumulativeRotation = 0f;
                        //         Debug.Log("1111111111111111111111111111111111");
                        //     }
                        //     else
                        //     {
                        //         // Only use lastRotationDirection if we've had enough rotation history
                        //         // This prevents premature direction assignment on first touch
                        //         if (rotationHistory.Count >= ROTATION_HISTORY_SIZE / 2)
                        //         {
                        //             isClockWise = lastRotationDirection;
                        //             Debug.Log("2222222222222222222222222222222222");
                        //         }
                        //         else
                        //         {
                        //             // Not enough rotation data yet - stay at 0
                        //             isClockWise = 0f;
                        //             Debug.Log("3333333333333333333333333333333333 - Waiting for clear rotation");
                        //         }
                        //     }
                        // }
                    }
                    else
                    {
                        isClockWise = 0f;  // First frame - no rotation yet
                        noRotationTimer = 0f;
                        cumulativeRotation = 0f;
                        rotationHistory.Clear();
                    }

                    // Store current frame for next comparison
                    previousIndexTip = projectedIndexTip;
                    previousThumbTip = projectedThumbTip;
                    hasPreviousFrame = true;

                    // Show the plane
                    debugPlane.SetActive(false); // TODO: turn off
                    isPlaneActive = true;
                    activeFinger = useIndexFinger ? "Index" : (useMiddleFinger ? "Middle" : "Thumb");
                }
                else
                {
                    if (lineRenderer == null)
                    {
                        if (Time.time >= nextDiagLogTime)
                        {
                            Debug.LogError("[JointAngle][Diag] lineRenderer became null before drawing fallback line", this);
                            nextDiagLogTime = Time.time + DIAG_LOG_INTERVAL;
                        }
                        return;
                    }

                    lineRenderer.SetPosition(0, rotationPoint1Position);
                    lineRenderer.SetPosition(1, rotationPoint0Position);
                }

                lineRenderer.enabled = showDebugVisuals; // Replaced lineRenderer.enabled = true;

                if (hasPreviousFrame && isClockWise != 0)
                {
                    // Debug.Log($"Rotation on {activeJoint}: {(isClockWise > 0 ? "CLOCKWISE" : "COUNTERCLOCKWISE")} ({isClockWise:F3})");
                }
            }
        }
        else
        {
            // No touch detected - hide everything
            lineRenderer.enabled = false;
            debugPlane.SetActive(false); // HIDE THE PLANE
            isPlaneActive = false;
            activeFinger = "None";
            hasPreviousFrame = false;
            lastRotationDirection = 1f;
            noRotationTimer = 0f;
            cumulativeRotation = 0f;
            rotationHistory.Clear();
            rotationChangeTimer = 0f;
        }

        // Debug.Log("rotationPoint1Position: " + rotationPoint1Position.ToString("F4") + ", rotationPoint0Position: " + rotationPoint0Position.ToString("F4"));
    }

    private void ResolveLeftIndexTipReference()
    {
        // Prefer explicit inspector assignment when available.
        if (L_index_c != null)
        {
            leftIndexTipReference = L_index_c;
            return;
        }

        GameObject leftTipByName = GameObject.Find("L_IndexTipSmall");
        if (leftTipByName != null)
        {
            leftIndexTipReference = leftTipByName.transform;
            return;
        }

        // Fallback: if targetRotationTag is a real tag, try resolving by tag.
        if (!string.IsNullOrEmpty(targetRotationTag))
        {
            try
            {
                GameObject leftTipByTag = GameObject.FindGameObjectWithTag(targetRotationTag);
                if (leftTipByTag != null)
                    leftIndexTipReference = leftTipByTag.transform;
            }
            catch
            {
                // Ignore invalid tag definitions and keep trying other fallbacks.
            }
        }
    }

    private float GetProjectedRedAxisSignedAngleOnTargetRedBluePlane(Transform source, Transform target)
    {
        if (source == null || target == null)
            return 0f;

        // Target red-blue plane is spanned by target.right (red) and target.forward (blue).
        // Its normal is target.up (green).
        Vector3 sourceRed = source.right;
        Vector3 projectedSourceRed = Vector3.ProjectOnPlane(sourceRed, target.up);

        if (projectedSourceRed.sqrMagnitude < 1e-10f)
            return 0f;

        return Vector3.SignedAngle(target.right, projectedSourceRed.normalized, target.up);
    }

    private void UpdateLeftIndexToRightTipAngles()
    {
        if (leftIndexTipReference == null)
            ResolveLeftIndexTipReference();

        Transform rightThumbTip = joints.ContainsKey("Thumb1") ? joints["Thumb1"] : null;
        Transform rightIndexTip = joints.ContainsKey("Index2") ? joints["Index2"] : null;
        Transform rightMiddleTip = joints.ContainsKey("Middle2") ? joints["Middle2"] : null;

        leftIndexTipToRightThumbTipAngle = GetProjectedRedAxisSignedAngleOnTargetRedBluePlane(leftIndexTipReference, rightThumbTip);
        leftIndexTipToRightIndexTipAngle = GetProjectedRedAxisSignedAngleOnTargetRedBluePlane(leftIndexTipReference, rightIndexTip);
        leftIndexTipToRightMiddleTipAngle = GetProjectedRedAxisSignedAngleOnTargetRedBluePlane(leftIndexTipReference, rightMiddleTip);
    }

    private void UpdateIndexEntryModeFromAngle()
    {
        // Latch old/new mode exactly when index rotation collider mode turns on.
        if (indexRotationColliderMode && !_lastIndexRotationColliderMode)
        {
            if (leftIndexTipToRightIndexTipAngle < 0f)
            {
                enterRightIndexWithOldRotation = true;
                enterRightIndexWithNewRotation = false;
            }
            else
            {
                enterRightIndexWithOldRotation = false;
                enterRightIndexWithNewRotation = true;
            }
        }

        // When latch is off, clear both mode decisions.
        if (!indexRotationColliderMode)
        {
            enterRightIndexWithOldRotation = false;
            enterRightIndexWithNewRotation = false;
        }

        _lastIndexRotationColliderMode = indexRotationColliderMode;
    }

    private void UpdateMiddleEntryModeFromAngle()
    {
        // Latch old/new mode exactly when middle rotation collider mode turns on.
        if (middleRotationColliderMode && !_lastMiddleRotationColliderMode)
        {
            if (leftIndexTipToRightMiddleTipAngle < 0f)
            {
                enterRightMiddleWithOldRotation = true;
                enterRightMiddleWithNewRotation = false;
            }
            else
            {
                enterRightMiddleWithOldRotation = false;
                enterRightMiddleWithNewRotation = true;
            }
        }

        // When latch is off, clear both mode decisions.
        if (!middleRotationColliderMode)
        {
            enterRightMiddleWithOldRotation = false;
            enterRightMiddleWithNewRotation = false;
        }

        _lastMiddleRotationColliderMode = middleRotationColliderMode;
    }

    private void UpdateThumbEntryModeFromAngle()
    {
        // Latch old/new mode exactly when thumb rotation collider mode turns on.
        if (thumbRotationColliderMode && !_lastThumbRotationColliderMode)
        {
            if (leftIndexTipToRightThumbTipAngle < 0f)
            {
                enterRightThumbWithOldRotation = true;
                enterRightThumbWithNewRotation = false;
            }
            else
            {
                enterRightThumbWithOldRotation = false;
                enterRightThumbWithNewRotation = true;
            }
        }

        // When latch is off, clear both mode decisions.
        if (!thumbRotationColliderMode)
        {
            enterRightThumbWithOldRotation = false;
            enterRightThumbWithNewRotation = false;
        }

        _lastThumbRotationColliderMode = thumbRotationColliderMode;
    }

    void UpdateIndexMiddleIndependentAnglesAndBaseline()
    {
        if (!joints.ContainsKey("Wrist") || !joints.ContainsKey("Index0") || !joints.ContainsKey("Middle0") ||
            !joints.ContainsKey("Index1") || !joints.ContainsKey("Middle1"))
        {
            indexToBaselineAngleOnPalm = 0f;
            middleToBaselineAngleOnPalm = 0f;
            if (baselineLineRenderer != null)
                baselineLineRenderer.enabled = false;
            SetAngleVisualizationEnabled(false, false);
            return;
        }

        Vector3 wrist = joints["Wrist"].position;
        Vector3 indexMcp = joints["Index0"].position;
        Vector3 middleMcp = joints["Middle0"].position;
        Vector3 midpoint = (indexMcp + middleMcp) * 0.5f;

        Vector3 planeNormal = Vector3.Cross(indexMcp - wrist, middleMcp - wrist);
        if (planeNormal.sqrMagnitude < 1e-10f)
        {
            indexToBaselineAngleOnPalm = 0f;
            middleToBaselineAngleOnPalm = 0f;
            if (baselineLineRenderer != null)
                baselineLineRenderer.enabled = false;
            SetAngleVisualizationEnabled(false, false);
            return;
        }
        planeNormal.Normalize();

        Vector3 mcpLine = middleMcp - indexMcp;
        if (mcpLine.sqrMagnitude < 1e-10f)
        {
            indexToBaselineAngleOnPalm = 0f;
            middleToBaselineAngleOnPalm = 0f;
            if (baselineLineRenderer != null)
                baselineLineRenderer.enabled = false;
            SetAngleVisualizationEnabled(false, false);
            return;
        }

        // Baseline direction: in palm plane and perpendicular to MCP line.
        Vector3 baselineDirection = Vector3.Cross(planeNormal, mcpLine).normalized;

        // Keep baseline direction stable by aligning it with Wrist->Midpoint direction projected on the same plane.
        Vector3 wristToMidpoint = Vector3.ProjectOnPlane(midpoint - wrist, planeNormal).normalized;
        if (wristToMidpoint.sqrMagnitude > 1e-10f && Vector3.Dot(baselineDirection, wristToMidpoint) < 0f)
            baselineDirection = -baselineDirection;

        Vector3 towardIndexDirection = (indexMcp - middleMcp).normalized;
        float baselineOffsetMeters = baselineOffsetTowardIndexCm * 0.01f;
        Vector3 baselineOrigin = midpoint + towardIndexDirection * baselineOffsetMeters;

        Vector3 indexPip = joints["Index1"].position;
        Vector3 middlePip = joints["Middle1"].position;
        Vector3 indexPipProjected = ProjectPointOnPlane(indexPip, baselineOrigin, planeNormal);
        Vector3 middlePipProjected = ProjectPointOnPlane(middlePip, baselineOrigin, planeNormal);

        Vector3 indexVector = indexPipProjected - baselineOrigin;
        Vector3 middleVector = middlePipProjected - baselineOrigin;

        float indexSignedAngle = indexVector.sqrMagnitude > 1e-10f
            ? Vector3.SignedAngle(baselineDirection, indexVector.normalized, planeNormal)
            : 0f;

        float middleSignedAngle = middleVector.sqrMagnitude > 1e-10f
            ? Vector3.SignedAngle(baselineDirection, middleVector.normalized, planeNormal)
            : 0f;

        if (useConstrainedBaselineAngleRange)
        {
            // Index constrained mode: keep original negative side, map to non-negative output.
            indexToBaselineAngleOnPalm = Mathf.Max(0f, -indexSignedAngle);
            // Middle constrained mode: keep magnitude only (no negative values).
            middleToBaselineAngleOnPalm = Mathf.Abs(middleSignedAngle);
        }
        else
        {
            // Free mode: preserve original signed angles.
            indexToBaselineAngleOnPalm = indexSignedAngle;
            middleToBaselineAngleOnPalm = middleSignedAngle;
        }

        if (baselineLineRenderer == null)
            return;

        baselineLineRenderer.startColor = baselineLineColor;
        baselineLineRenderer.endColor = baselineLineColor;
        baselineLineRenderer.enabled = showDebugVisuals && showIndexMiddleBaseline;

        if (baselineLineRenderer.enabled)
        {
            float halfLength = Mathf.Max(0.005f, baselineLineLength * 0.5f);
            baselineLineRenderer.SetPosition(0, baselineOrigin - baselineDirection * halfLength);
            baselineLineRenderer.SetPosition(1, baselineOrigin + baselineDirection * halfLength);
        }

        bool drawIndex = showIndexAngleVisualization;
        bool drawMiddle = showMiddleAngleVisualization;
        SetAngleVisualizationEnabled(drawIndex, drawMiddle);

        if (drawIndex)
        {
            indexProjectionLineRenderer.startColor = indexProjectionLineColor;
            indexProjectionLineRenderer.endColor = indexProjectionLineColor;
            indexProjectionLineRenderer.SetPosition(0, indexPip);
            indexProjectionLineRenderer.SetPosition(1, indexPipProjected);

            indexAngleVectorLineRenderer.startColor = indexAngleVectorLineColor;
            indexAngleVectorLineRenderer.endColor = indexAngleVectorLineColor;
            indexAngleVectorLineRenderer.SetPosition(0, baselineOrigin);
            indexAngleVectorLineRenderer.SetPosition(1, indexPipProjected);
        }

        if (drawMiddle)
        {
            middleProjectionLineRenderer.startColor = middleProjectionLineColor;
            middleProjectionLineRenderer.endColor = middleProjectionLineColor;
            middleProjectionLineRenderer.SetPosition(0, middlePip);
            middleProjectionLineRenderer.SetPosition(1, middlePipProjected);

            middleAngleVectorLineRenderer.startColor = middleAngleVectorLineColor;
            middleAngleVectorLineRenderer.endColor = middleAngleVectorLineColor;
            middleAngleVectorLineRenderer.SetPosition(0, baselineOrigin);
            middleAngleVectorLineRenderer.SetPosition(1, middlePipProjected);
        }
    }

    LineRenderer CreateDebugLineRenderer(string name, Color color, float width)
    {
        LineRenderer lr = new GameObject(name).AddComponent<LineRenderer>();
        lr.transform.SetParent(transform, false);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.positionCount = 2;
        lr.enabled = false;
        return lr;
    }

    void SetAngleVisualizationEnabled(bool indexEnabled, bool middleEnabled)
    {
        if (indexProjectionLineRenderer != null)
            indexProjectionLineRenderer.enabled = indexEnabled;
        if (indexAngleVectorLineRenderer != null)
            indexAngleVectorLineRenderer.enabled = indexEnabled;
        if (middleProjectionLineRenderer != null)
            middleProjectionLineRenderer.enabled = middleEnabled;
        if (middleAngleVectorLineRenderer != null)
            middleAngleVectorLineRenderer.enabled = middleEnabled;
    }

    public float GetIndexMiddleAngleOnPalm()
    {
        if (!joints.ContainsKey("Index1") || !joints.ContainsKey("Middle1") || !joints.ContainsKey("PalmIndex"))
            return 0f;
        Vector3 basePoint = joints["PalmIndex"].position;
        Vector3 a = ProjectPointOnPlane(joints["Index1"].position, basePoint, palmNormal) - basePoint;
        Vector3 b = ProjectPointOnPlane(joints["Middle1"].position, basePoint, palmNormal) - basePoint;
        float signedAngle = Vector3.SignedAngle(a, b, palmNormal);
        return Mathf.Abs(signedAngle);
    }

    // calculate the angles between thumbPlaneNormal and palmNormal
    // Returns positive angle when thumb is open, 0 when thumb is closed
    float UpdateThumbPalmAngle()
    {
        if (!joints.ContainsKey("Wrist"))
            return 0f;

        // Calculate the angle between the two plane normals
        float angle = Vector3.Angle(thumbPlaneNormal, palmNormal);

        // Use a consistent reference: the wrist's forward direction (up vector)
        // This provides a stable reference regardless of when the hand enters the scene
        Vector3 wristUp = joints["Wrist"].forward;

        // Calculate the cross product to track the directional relationship between planes
        Vector3 currentCrossProduct = Vector3.Cross(thumbPlaneNormal, palmNormal);

        // Compare cross product with wrist up direction to determine sign
        // Positive dot product = thumb is open (away from palm)
        // Negative dot product = thumb is closed (toward palm)
        float signDot = Vector3.Dot(currentCrossProduct, wristUp);

        // Return positive angle or 0
        if (signDot > 0)
        {
            return angle;  // Thumb open
        }
        else
        {
            return 0f; // Thumb closed - return 0 instead of negative
        }
    }

    // Calculate the angle between R_Wrist's red vector and R_thumb_a's red vector
    float GetWristThumbAngle()
    {
        if (!joints.ContainsKey("Wrist") || !joints.ContainsKey("Thumb0"))
            return 0f;

        Vector3 wristRight = joints["Wrist"].right;
        Vector3 thumbRight = joints["Thumb0"].right;

        // Vector3.Angle always returns the smaller angle (0-180 degrees)
        float angle = Vector3.Angle(wristRight, thumbRight);
        return angle;
    }

    // compute thumb plane normal from (Wrist, R_thumb_a, R_index_Proximal) points.
    void UpdateThumbPlane()
    {
        Vector3 p0 = joints["Wrist"].position;
        Vector3 p1 = joints["Thumb0"].position;
        Vector3 p2 = joints["Index0"].position;

        Vector3 v1 = (p1 - p0).normalized;
        Vector3 v2 = (p2 - p0).normalized;

        thumbPlaneNormal = Vector3.Cross(v1, v2).normalized;
    }

    // Compute the palm’s normal from (Wrist, PalmIndex, PalmRing) points.
    void UpdatePalmNormal()
    {
        Vector3 p0 = joints["Wrist"].position;
        Vector3 p1 = joints["PalmIndex"].position;
        Vector3 p2 = joints["PalmRing"].position;

        Vector3 v1 = (p1 - p0).normalized;
        Vector3 v2 = (p2 - p0).normalized;

        palmNormal = Vector3.Cross(v1, v2).normalized;
    }

    float GetJointPalmAngle(string targetJoint)
    {
        if (!joints.ContainsKey(targetJoint))
            return 0f;

        Vector3 jointForwardVector = joints[targetJoint].right;

        Vector3 projectedForward = Vector3.ProjectOnPlane(jointForwardVector, palmNormal).normalized;

        float angle = Vector3.Angle(jointForwardVector, projectedForward);
        return angle;
    }

    float GetJointAngle(string targetJoint, string parentJoint)
    {
        // Calculate the angle between the target joint and its parent joint
        if (!joints.ContainsKey(targetJoint) || !joints.ContainsKey(parentJoint))
            return 0f;

        Vector3 targetDirection = joints[targetJoint].right;
        Vector3 parentDirection = joints[parentJoint].right;

        float angle = Vector3.Angle(targetDirection, parentDirection);

        // Ensure the angle is always positive
        return Mathf.Abs(angle);
    }

    float GetRotateAngle(string basicPoint, string middlePoint, string targetPoint)
    {
        Vector3 basicVector = joints[middlePoint].position - joints[basicPoint].position;
        Vector3 targetVector = joints[targetPoint].position - joints[middlePoint].position;

        if (basicVector == Vector3.zero || targetVector == Vector3.zero)
            return 0f;

        // project onto the palm normal plane and calculate the angle
        Vector3 projectedBasic = Vector3.ProjectOnPlane(basicVector, palmNormal).normalized;
        Vector3 projectedTarget = Vector3.ProjectOnPlane(targetVector, palmNormal).normalized;
        float angle = Vector3.Angle(projectedBasic, projectedTarget);

        return angle;
    }

    // when calculating thumb angles, we need to use thumb plane
    float GetThumbAngle(string targetJoint)
    {
        if (!joints.ContainsKey(targetJoint))
            return 0f;

        Vector3 jointForwardVector = joints[targetJoint].right;

        Vector3 projectedForward = Vector3.ProjectOnPlane(jointForwardVector, thumbPlaneNormal).normalized;

        float angle = Vector3.Angle(jointForwardVector, projectedForward);
        return angle;
    }

    float GetProjectedDistanceOnPalm(string jointA, string jointB)
    {
        if (!joints.ContainsKey(jointA) || !joints.ContainsKey(jointB))
            return 0f;

        Vector3 pointA = joints[jointA].position;
        Vector3 pointB = joints[jointB].position;
        Vector3 palmOrigin = joints["Wrist"].position;

        Vector3 projectedA = ProjectPointOnPlane(pointA, palmOrigin, palmNormal);
        Vector3 projectedB = ProjectPointOnPlane(pointB, palmOrigin, palmNormal);

        return Vector3.Distance(projectedA, projectedB);
    }

    Vector3 ProjectPointOnPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
    {
        Vector3 toPoint = point - planePoint;
        float distance = Vector3.Dot(toPoint, planeNormal);
        return point - (distance * planeNormal);
    }

    // Calculate rotation direction from previous frame to current frame
    // Returns 1 for clockwise, -1 for counterclockwise, 0 for no clear rotation
    float GetRotationDirection(Vector3 prevPoint1, Vector3 prevPoint2, Vector3 currPoint1, Vector3 currPoint2, string jointName = "Index1")
    {
        if (!joints.ContainsKey(jointName))
            return 0f;

        Transform joint = joints[jointName];
        Vector3 planeNormal = joint.right; // The plane normal (red axis)

        // Get the line vectors for previous and current frames
        Vector3 prevLine = (prevPoint2 - prevPoint1).normalized;
        Vector3 currLine = (currPoint2 - currPoint1).normalized;

        // Check if lines are too similar (no meaningful rotation)
        float similarity = Vector3.Dot(prevLine, currLine);
        if (similarity > 0.9999f) // Almost identical
            return 0f;

        // Calculate the cross product to determine rotation direction
        // Cross product of (previous → current) gives rotation axis
        Vector3 rotationAxis = Vector3.Cross(prevLine, currLine);

        // Project rotation axis onto plane normal to get signed rotation
        float rotationSign = Vector3.Dot(rotationAxis, planeNormal);
        // Debug.Log("Rotation Sign: " + rotationSign);

        // Return only 1 or -1 based on sign
        if (rotationSign > 0.001f)
            return 1f;  // Clockwise
        else if (rotationSign < -0.001f)
            return -1f; // Counterclockwise
        else
            return 0f;  // No clear rotation
    }

    // Public method to safely get joint transform
    public Transform GetJoint(string jointName)
    {
        if (joints != null && joints.ContainsKey(jointName))
            return joints[jointName];
        return null;
    }

    // Calculate distance between L_index_c and Index2 joint
    public float GetLIndexToIndex2Distance()
    {
        if (L_index_c == null)
        {
            // Debug.LogWarning("L_index_c is not assigned!");
            return 0f;
        }

        if (!joints.ContainsKey("Index2"))
        {
            // Debug.LogWarning("Index2 joint not found!");
            return 0f;
        }

        return Vector3.Distance(L_index_c.position, joints["Index2"].position);
    }

    private void SetupRotationColliderDetectors()
    {
        AttachRotationColliderDetector(thumbRotationCollider, 1);
        AttachRotationColliderDetector(indexRotationCollider, 2);
        AttachRotationColliderDetector(middleRotationCollider, 3);
    }

    private void AttachRotationColliderDetector(GameObject colliderObject, int colliderType)
    {
        if (colliderObject == null)
            return;

        RotationColliderDetector detector = colliderObject.GetComponent<RotationColliderDetector>();
        if (detector == null)
            detector = colliderObject.AddComponent<RotationColliderDetector>();

        detector.Initialize(colliderType, targetRotationTag, this);
    }

    private void UpdateRotationColliderLatchByMode(bool isModeManipulate)
    {
        if (isModeManipulate != _lastModeManipulate)
        {
            ResetRotationColliderModes();
            _lastModeManipulate = isModeManipulate;
        }
    }

    private void ResetRotationColliderModes()
    {
        thumbRotationColliderMode = false;
        indexRotationColliderMode = false;
        middleRotationColliderMode = false;
    }

    private bool IsRotationColliderGateOpenForMotorRange(int confirmedMotorID, bool isModeManipulate)
    {
        if (!isModeManipulate)
            return false;

        if (confirmedMotorID >= 1 && confirmedMotorID <= 4)
            return thumbRotationColliderMode;
        if (confirmedMotorID >= 5 && confirmedMotorID <= 8)
            return indexRotationColliderMode;
        if (confirmedMotorID >= 9 && confirmedMotorID <= 12)
            return middleRotationColliderMode;

        return false;
    }

    internal void OnRotationColliderEntered(int colliderType)
    {
        if (modeSwitching == null || !modeSwitching.modeManipulate)
            return;

        if (colliderType == 1)
            thumbRotationColliderMode = true;
        else if (colliderType == 2)
            indexRotationColliderMode = true;
        else if (colliderType == 3)
            middleRotationColliderMode = true;
    }
}

// Attached to thumbRotationCollider / indexRotationCollider / middleRotationCollider
// to detect targetRotationTag contact in modeManipulate and latch the corresponding flag.
internal class RotationColliderDetector : MonoBehaviour
{
    private int colliderType; // 1 = thumb, 2 = index, 3 = middle
    private string targetTag;
    private JointAngle manager;

    public void Initialize(int type, string tag, JointAngle managerRef)
    {
        colliderType = type;
        targetTag = tag;
        manager = managerRef;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag) && manager != null)
            manager.OnRotationColliderEntered(colliderType);
    }
}