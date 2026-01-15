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
    private float rotationSpeed = 13f; // degrees per second
    public bool isMapping = true;
    public float tt = 0f;

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

    public Vector3 thumbFingerJoint1MaxRotationVector;
    public Vector3 thumbFingerJoint2MaxRotationVector;

    public float currentThumbRotationY = 0f;
    public float currentThumbRotationZ = 0f;

    public float maxThumbYAxisAngle;
    public float maxThumbZAxisAngle;

    public float currentThumbTipRotationZ = 0f;

    public float currentThumbInnerExtensionRotationZ = 0f;


    // ==============================
    // 🔹 Index Finger State
    // ==============================
    private Quaternion IndexAngle1CenterInitialRotation;
    private Quaternion IndexAngle2CenterInitialRotation;

    public Vector3 indexFingerJoint1MaxRotationVector;
    public Vector3 indexFingerJoint2MaxRotationVector;

    public float currentIndexRotationY = 0f;
    public float currentIndexRotationZ = 0f;

    public float maxIndexYAxisAngle;
    public float maxIndexZAxisAngle;

    public float currentIndexTipRotationZ = 0f;

    public float currentIndexInnerExtensionRotationZ = 0f;

    // ==============================
    // 🔹 Middle Finger State
    // ==============================
    private Quaternion MiddleAngle1CenterInitialRotation;
    private Quaternion MiddleAngle2CenterInitialRotation;

    public Vector3 middleFingerJoint1MaxRotationVector;
    public Vector3 middleFingerJoint2MaxRotationVector;

    public float currentMiddleRotationY = 0f;
    public float currentMiddleRotationZ = 0f;

    public float maxMiddleYAxisAngle;
    public float maxMiddleZAxisAngle;

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
    private Dictionary<string, float> accumulatedJointChanges = new Dictionary<string, float>();
    private Dictionary<string, float> accumulatedMidJointChanges = new Dictionary<string, float>();
    private Dictionary<string, float> accumulatedBaseJointChanges = new Dictionary<string, float>();
    private Dictionary<string, float> previousAdditionalAngles = new Dictionary<string, float>();
    private Dictionary<string, float> accumulatedAdditionalChanges = new Dictionary<string, float>();
    
    public bool isFingerTipTriggered = false;
    public float jointAngleValueDebug = 0f;
    public float currentTipRotationDebug = 0f;
    public float totalAngleChange = 0f;

    void Start()
    {
        originalColor = thumbJoint1Renderer.material.color;

        // --- Initialize Thumb ---
        ThumbAngle1CenterInitialRotation = ThumbAngle1Center.localRotation;
        ThumbAngle2CenterInitialRotation = ThumbAngle2Center.localRotation;
        thumbFingerJoint1MaxRotationVector = ThumbAngle1Center.localRotation.eulerAngles;
        thumbFingerJoint2MaxRotationVector = ThumbAngle2Center.localRotation.eulerAngles;
        maxThumbYAxisAngle = ThumbAngle1CenterInitialRotation.eulerAngles.y;
        maxThumbZAxisAngle = ThumbAngle2CenterInitialRotation.eulerAngles.z;

        // --- Initialize Index ---
        IndexAngle1CenterInitialRotation = IndexAngle1Center.localRotation;
        IndexAngle2CenterInitialRotation = IndexAngle2Center.localRotation;
        indexFingerJoint1MaxRotationVector = IndexAngle1Center.localRotation.eulerAngles;
        indexFingerJoint2MaxRotationVector = IndexAngle2Center.localRotation.eulerAngles;
        maxIndexYAxisAngle = IndexAngle1CenterInitialRotation.eulerAngles.y;
        maxIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;

        // --- Initialize Middle ---
        MiddleAngle1CenterInitialRotation = MiddleAngle1Center.localRotation;
        MiddleAngle2CenterInitialRotation = MiddleAngle2Center.localRotation;
        middleFingerJoint1MaxRotationVector = MiddleAngle1Center.localRotation.eulerAngles;
        middleFingerJoint2MaxRotationVector = MiddleAngle2Center.localRotation.eulerAngles;
        maxMiddleYAxisAngle = MiddleAngle1CenterInitialRotation.eulerAngles.y;
        maxMiddleZAxisAngle = MiddleAngle2CenterInitialRotation.eulerAngles.z;
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

        HandleInput();

        // ==============================
        // 🔹 Thumb
        // ==============================

        // if (ThumbAngle3Center != null)
        //     ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1*1.7f, 0f, 0f);
        
        // UpdateInnerExtension(
        //     triggerThumbInnerExtension.isThumbInnerExtensionTouched,
        //     triggerRightThumbTip.isRightThumbTipTouched,
        //     jointAngle.thumbAngle1,
        //     "Thumb3",
        //     "Thumb0",
        //     ref currentThumbInnerExtensionRotationZ,
        //     rotationSpeed,
        //     thumbJoint3Renderer,
        //     purpleColor,
        //     originalColor,
        //     ThumbAngle3Center,
        //     ref isThumb3Triggered,
        //     () => jointAngle.thumbAngle0 == 0f
        //     // () => jointAngle.thumbPalmAngle < 10f
        //     // jointAngle.thumbAngle0 == 0f
        // );

        // UpdateFingertipExtension(
        //     triggerRightThumbTip.isRightThumbTipTouched,
        //     jointAngle.thumbAngle1,
        //     302f,
        //     "Thumb1",
        //     "Thumb0",
        //     ref currentThumbTipRotationZ,
        //     rotationSpeed,
        //     thumbJoint4Renderer,
        //     purpleColor,
        //     originalColor,
        //     ThumbAngle4Center,
        //     ref isThumb4Triggered,
        //     () => jointAngle.thumbAngle0 == 0f
        // );

        UpdateFingertipExtensionV2(
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
            modeSwitching.modeManipulate,
            modeSwitching.currentRedMotorID,
            4,  // Expected motor ID for thumb
            20.0f,  // Thumb requires 20 degree change
            jointAngle.thumbPalmAngle  // Track thumbPalmAngle changes
        );

        // UpdateThumbAbduction();

        // UpdateThumbFingerTwist();

        // ==============================
        // 🔹 Index Finger
        // ==============================
        
        // if (IndexAngle3Center != null)
        //     IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1 + jointAngle.indexAngle0, 0f, 0f);
        
        // UpdateIndexFingerAbduction();
        
        // UpdateIndexFingerTwist();

        // UpdateInnerExtension(
        //     triggerIndexInnerExtension.isIndexInnerExtensionTouched,
        //     triggerRightIndexTip.isRightIndexTipTouched,
        //     jointAngle.indexAngle1,
        //     "Index3",
        //     "Index0",
        //     ref currentIndexInnerExtensionRotationZ,
        //     rotationSpeed,
        //     indexJoint3Renderer,
        //     purpleColor,
        //     originalColor,
        //     IndexAngle3Center,
        //     ref isIndex3Triggered
        // );

        // UpdateFingertipExtension(
        //     triggerRightIndexTip.isRightIndexTipTouched,
        //     jointAngle.indexAngle2,
        //     302f,
        //     "Index2",
        //     "Index0",
        //     ref currentIndexTipRotationZ,
        //     rotationSpeed,
        //     indexJoint4Renderer,
        //     purpleColor,
        //     originalColor,
        //     IndexAngle4Center,
        //     ref isIndex4Triggered
        // );

        UpdateFingertipExtensionV2(
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
            modeSwitching.modeManipulate,
            modeSwitching.currentRedMotorID,
            8  // Expected motor ID for thumb
        );

        // ==============================
        // 🔹 Middle Finger State
        // ==============================

        // UpdateMiddleFingerAbduction();
        
        // UpdateMiddleFingerTwist();

        // if (MiddleAngle3Center != null)
        //     MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1 + jointAngle.middleAngle0, 0f, 0f);

        // UpdateInnerExtension(
        //     triggerMiddleInnerExtension.isMiddleInnerExtensionTouched,
        //     triggerRightMiddleTip.isRightMiddleTipTouched,
        //     jointAngle.middleAngle1,
        //     "Middle3",
        //     "Middle0",
        //     ref currentMiddleInnerExtensionRotationZ,
        //     rotationSpeed,
        //     middleJoint3Renderer,
        //     purpleColor,
        //     originalColor,
        //     MiddleAngle3Center,
        //     ref isMiddle3Triggered
        // );
        
        // UpdateFingertipExtension(
        //     triggerRightMiddleTip.isRightMiddleTipTouched,
        //     jointAngle.middleAngle2,
        //     302f,
        //     "Middle2",
        //     "Middle0",
        //     ref currentMiddleTipRotationZ,
        //     rotationSpeed,
        //     middleJoint4Renderer,
        //     purpleColor,
        //     originalColor,
        //     MiddleAngle4Center,
        //     ref isMiddle4Triggered
        // );

        UpdateFingertipExtensionV2(
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
            modeSwitching.modeManipulate,
            modeSwitching.currentRedMotorID,
            12
        );
    }

    // ==============================
    // 🔹 Input Handling
    // ==============================
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            isMapping = !isMapping;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetFingerRotations();
        }
    }

    void UpdateThumbAbduction()
    {
        Quaternion targetRotation = ThumbAngle1CenterInitialRotation;
        maxThumbYAxisAngle = NormalizeAngle(thumbFingerJoint1MaxRotationVector.y);

        // Initialize touch duration for thumb abduction
        if (!fingerTipTouchDurations.ContainsKey("ThumbAbduction"))
        {
            fingerTipTouchDurations["ThumbAbduction"] = 0f;
        }

        // triggerRightThumbAbduction.isRightThumbAbductionTouched 
        if (!isFingerTipTriggered && triggerRightThumbAbduction.isRightThumbAbductionTouched && jointAngle.thumbPalmAngle > 55f && !isAnyMotor4Triggered && !isThumb2Triggered && canControlThumb1)
        {
            fingerTipTouchDurations["ThumbAbduction"] += Time.deltaTime;
            thumbJoint1Renderer.material.color = Color.Lerp(originalColor, yellowColor, Mathf.Min(fingerTipTouchDurations["ThumbAbduction"], 1f));
            isThumb1Triggered = true;

            // Only apply rotation after 1 second
            if (fingerTipTouchDurations["ThumbAbduction"] > 1.0f)
            {
                currentThumbRotationY -= rotationSpeed * Time.deltaTime;
                currentThumbRotationY = Mathf.Clamp(currentThumbRotationY, -60f, 0f);

                thumbFingerJoint1MaxRotationVector =
                    (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, currentThumbRotationY, 0f)).eulerAngles;

                thumbJoint1Renderer.material.color = yellowColor;
            }
        }
        else
        {
            fingerTipTouchDurations["ThumbAbduction"] = 0f;
            thumbJoint1Renderer.material.color = originalColor;
            isThumb1Triggered = false;
        }

        // Base angle from thumb-palm angle
        float baseAngle = 45f - jointAngle.thumbPalmAngle;
        
        targetRotation *= Quaternion.Euler(0f, baseAngle + currentThumbRotationY, 0f);

        // mapping using thumb palm angle
        float thumbPalmAngleDiff = 45f - jointAngle.thumbPalmAngle;
        if (isMapping && Mathf.Abs(thumbPalmAngleDiff) > 0.1f)
        {
            float delta = maxThumbYAxisAngle;
            float targetY = baseAngle + thumbFingerJoint1MaxRotationVector.y - delta * (thumbPalmAngleDiff / 45f);

            Vector3 euler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
        }

        if (ThumbAngle1Center != null)
            ThumbAngle1Center.localRotation = targetRotation;
    }


    // ==============================
    // 🔹 Index Finger Abduction (Y-axis)
    // ==============================

    private void UpdateIndexFingerAbduction()
    {
        maxIndexYAxisAngle = NormalizeAngle(indexFingerJoint1MaxRotationVector.y);
        Quaternion targetRotation = IndexAngle1CenterInitialRotation;

        // Initialize touch duration for index abduction
        if (!fingerTipTouchDurations.ContainsKey("IndexAbduction"))
        {
            fingerTipTouchDurations["IndexAbduction"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.9f && !isAnyMotor4Triggered && canControlIndex1)
        {
            fingerTipTouchDurations["IndexAbduction"] += Time.deltaTime;
            indexJoint1Renderer.material.color = Color.Lerp(originalColor, yellowColor, Mathf.Min(fingerTipTouchDurations["IndexAbduction"], 1f));
            isIndex1Triggered = true;

            // Only apply rotation after 1 second
            if (fingerTipTouchDurations["IndexAbduction"] > 1.0f)
            {
                currentIndexRotationY -= rotationSpeed * Time.deltaTime;
                currentIndexRotationY = Mathf.Max(currentIndexRotationY, -60f);

                indexFingerJoint1MaxRotationVector =
                    (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationY, 0f)).eulerAngles;

                indexJoint1Renderer.material.color = yellowColor;
            }
        }
        else
        {
            fingerTipTouchDurations["IndexAbduction"] = 0f;
            indexJoint1Renderer.material.color = originalColor;
            isIndex1Triggered = false;
        }

        targetRotation *= Quaternion.Euler(0f, currentIndexRotationY, 0f);

        if (jointAngle.indexMiddleDistance < 3.5f && IndexAngle1Center != null)
        {
            float delta = maxIndexYAxisAngle;
            float targetY = isMapping
                ? maxIndexYAxisAngle + (30 - delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
                : indexFingerJoint1MaxRotationVector.y + 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

            Vector3 euler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
        }

        if (IndexAngle1Center != null)
            IndexAngle1Center.localRotation = targetRotation;
    }

    // ==============================
    // 🔹 Middle Finger Abduction (Y-axis)
    // ==============================

    private void UpdateMiddleFingerAbduction()
    {
        maxMiddleYAxisAngle = NormalizeAngle(middleFingerJoint1MaxRotationVector.y);
        Quaternion targetRotation = MiddleAngle1CenterInitialRotation;

        // Initialize touch duration for middle abduction
        if (!fingerTipTouchDurations.ContainsKey("MiddleAbduction"))
        {
            fingerTipTouchDurations["MiddleAbduction"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.indexMiddleDistance > 3.9f && !isAnyMotor4Triggered && canControlMiddle1)
        {
            fingerTipTouchDurations["MiddleAbduction"] += Time.deltaTime;
            middleJoint1Renderer.material.color = Color.Lerp(originalColor, yellowColor, Mathf.Min(fingerTipTouchDurations["MiddleAbduction"], 1f));
            isMiddle1Triggered = true;

            // Only apply rotation after 1 second
            if (fingerTipTouchDurations["MiddleAbduction"] > 1.0f)
            {
                currentMiddleRotationY += rotationSpeed * Time.deltaTime;
                currentMiddleRotationY = Mathf.Min(currentMiddleRotationY, 60f);

                middleFingerJoint1MaxRotationVector =
                    (MiddleAngle1CenterInitialRotation * Quaternion.Euler(0f, currentMiddleRotationY, 0f)).eulerAngles;

                middleJoint1Renderer.material.color = yellowColor;
            }
        }
        else
        {
            fingerTipTouchDurations["MiddleAbduction"] = 0f;
            middleJoint1Renderer.material.color = originalColor;
            isMiddle1Triggered = false;
        }

        targetRotation *= Quaternion.Euler(0f, currentMiddleRotationY, 0f);

        if (jointAngle.indexMiddleDistance < 3.5f && MiddleAngle1Center != null)
        {
            float delta = maxMiddleYAxisAngle;
            float targetY = isMapping
                ? maxMiddleYAxisAngle - (30 + delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
                : middleFingerJoint1MaxRotationVector.y - 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

            Vector3 euler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
        }

        if (MiddleAngle1Center != null)
            MiddleAngle1Center.localRotation = targetRotation;
    }

    
    // ==============================
    // 🔹 Twist
    // ==============================

    private void UpdateThumbFingerTwist()
    {
        Quaternion targetRotation = ThumbAngle2CenterInitialRotation;
        maxThumbZAxisAngle = NormalizeAngle(thumbFingerJoint2MaxRotationVector.z);

        // Initialize touch duration for thumb twist
        if (!fingerTipTouchDurations.ContainsKey("ThumbTwist"))
        {
            fingerTipTouchDurations["ThumbTwist"] = 0f;
        }

        if (!isFingerTipTriggered && triggerRightThumbTip.isRightThumbTipTouched && jointAngle.isPlaneActive && !isAnyMotor4Triggered && !isThumb1Triggered && canControlThumb2) //  && jointAngle.thumbPalmAngle > 10f && jointAngle.thumbPalmAngle < 55f
        {
            fingerTipTouchDurations["ThumbTwist"] += Time.deltaTime;
            thumbJoint2Renderer.material.color = Color.Lerp(originalColor, greenColor, Mathf.Min(fingerTipTouchDurations["ThumbTwist"] / 0.7f, 1f));
            isThumb2Triggered = true;

            // Only apply rotation after 1 second
            if (fingerTipTouchDurations["ThumbTwist"] > 0.7f)
            {
                currentThumbRotationZ -= (-jointAngle.isClockWise) * rotationSpeed * Time.deltaTime;
                currentThumbRotationZ = Mathf.Clamp(currentThumbRotationZ, -60f, 0f);

                thumbFingerJoint2MaxRotationVector =
                    (ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentThumbRotationZ)).eulerAngles;

                thumbJoint2Renderer.material.color = greenColor;
            }
        }
        else
        {
            fingerTipTouchDurations["ThumbTwist"] = 0f;
            thumbJoint2Renderer.material.color = originalColor;
            isThumb2Triggered = false;
        }

        // Base angle from wrist-thumb angle - always apply this
        float baseAngle = 45f - jointAngle.wristThumbAngle;
        targetRotation = ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, baseAngle + currentThumbRotationZ);

        // mapping using wrist thumb angle
        float wristThumbAngleDiff = 45f - jointAngle.wristThumbAngle;
        if (isMapping && Mathf.Abs(currentThumbRotationZ) > 0.1f && Mathf.Abs(wristThumbAngleDiff) > 0.1f)
        {
            float delta = maxThumbZAxisAngle;
            float targetZ = baseAngle + thumbFingerJoint2MaxRotationVector.z - delta * (wristThumbAngleDiff / 45f);

            Vector3 euler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
        }

        if (ThumbAngle2Center != null)
            ThumbAngle2Center.localRotation = targetRotation;
    }

    private void UpdateIndexFingerTwist()
    {
        Quaternion targetRotation = IndexAngle2CenterInitialRotation;
        maxIndexZAxisAngle = NormalizeAngle(indexFingerJoint2MaxRotationVector.z);

        // Initialize touch duration for index twist
        if (!fingerTipTouchDurations.ContainsKey("IndexTwist"))
        {
            fingerTipTouchDurations["IndexTwist"] = 0f;
        }

        //   triggerIndexInnerExtension.isIndexInnerExtensionTouched // && jointAngle.joints["Index0"].localRotation.eulerAngles.z > 100.0f 
        if (!isFingerTipTriggered && jointAngle.joints["Index0"].localRotation.eulerAngles.z > 100.0f  && triggerRightIndexTip.isRightIndexTipTouched && jointAngle.isPlaneActive && !isAnyMotor4Triggered && jointAngle.indexMiddleDistance < 3.5f && canControlIndex2 && !isMiddle2Triggered)
        {
            fingerTipTouchDurations["IndexTwist"] += Time.deltaTime;
            indexJoint2Renderer.material.color = Color.Lerp(originalColor, greenColor, Mathf.Min(fingerTipTouchDurations["IndexTwist"] / 0.7f, 1f));
            isIndex2Triggered = true;

            // Only apply rotation after 1 second
            if (fingerTipTouchDurations["IndexTwist"] > 0.7f)
            {
                // Only rotate if there's actual rotation happening (isClockWise != 0)
                if(currentIndexRotationZ >= -58f && currentIndexRotationZ <= 0 && Mathf.Abs(jointAngle.isClockWise) > 0.1f)
                {   
                    currentIndexRotationZ -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
                }

                currentIndexRotationZ = Mathf.Clamp(currentIndexRotationZ, -58f, 0f);

                indexFingerJoint2MaxRotationVector =
                    (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZ)).eulerAngles;

                indexJoint2Renderer.material.color = greenColor;
            }
        }
        else
        {
            fingerTipTouchDurations["IndexTwist"] = 0f;
            indexJoint2Renderer.material.color = originalColor;
            isIndex2Triggered = false;
        }

        targetRotation *= Quaternion.Euler(0f, 0f, currentIndexRotationZ);

        // mapping using index and middle finger distance
        if (jointAngle.indexMiddleDistance < 3.5f && MiddleAngle2Center != null)
        {
            float delta = maxIndexZAxisAngle;
            // float targetZ = isMapping
            //     ? maxIndexZAxisAngle + (30 + delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
            //     : indexFingerJoint1MaxRotationVector.y + 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

            float targetZ = indexFingerJoint2MaxRotationVector.z - delta * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

            Vector3 euler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
        }

        if (IndexAngle2Center != null)
            IndexAngle2Center.localRotation = targetRotation;
    }

    private void UpdateMiddleFingerTwist()
    {
        Quaternion targetRotation = MiddleAngle2CenterInitialRotation;
        maxMiddleZAxisAngle = NormalizeAngle(middleFingerJoint2MaxRotationVector.z);

        // Initialize touch duration for middle twist
        if (!fingerTipTouchDurations.ContainsKey("MiddleTwist"))
        {
            fingerTipTouchDurations["MiddleTwist"] = 0f;
        }

        if (!isFingerTipTriggered && jointAngle.joints["Middle0"].localRotation.eulerAngles.z > 100.0f && triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.isPlaneActive && !isAnyMotor4Triggered && jointAngle.indexMiddleDistance < 3.5f && canControlMiddle2 && !isIndex2Triggered)
        {
            fingerTipTouchDurations["MiddleTwist"] += Time.deltaTime;
            middleJoint2Renderer.material.color = Color.Lerp(originalColor, greenColor, Mathf.Min(fingerTipTouchDurations["MiddleTwist"] / 0.7f, 1f));
            isMiddle2Triggered = true;

            // Only apply rotation after 1 second
            if (fingerTipTouchDurations["MiddleTwist"] > 0.7f)
            {
                if(currentMiddleRotationZ <= 58f && currentMiddleRotationZ >= 0)
                {
                    currentMiddleRotationZ -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
                }

                // if(currentMiddleRotationZ <= 0) currentMiddleRotationZ = 0; 
                // if (currentMiddleRotationZ > 58) currentMiddleRotationZ = 58;

                currentMiddleRotationZ = Mathf.Clamp(currentMiddleRotationZ, 0f, 58f);
                
                middleFingerJoint2MaxRotationVector =
                    (MiddleAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentMiddleRotationZ)).eulerAngles;

                middleJoint2Renderer.material.color = greenColor;
            }
        }
        else
        {
            fingerTipTouchDurations["MiddleTwist"] = 0f;
            middleJoint2Renderer.material.color = originalColor;
            isMiddle2Triggered = false;
        }

        targetRotation *= Quaternion.Euler(0f, 0f, currentMiddleRotationZ);

        // mapping using index and middle finger distance
        if (jointAngle.indexMiddleDistance < 3.5f && MiddleAngle2Center != null)
        {
            float delta = maxMiddleZAxisAngle;
            // float targetZ = isMapping
            //     ? maxMiddleZAxisAngle - (30 + delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
            //     : middleFingerJoint1MaxRotationVector.y - 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

            float targetZ = middleFingerJoint2MaxRotationVector.z - delta * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

            Vector3 euler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
        }

        if (MiddleAngle2Center != null)
            MiddleAngle2Center.localRotation = targetRotation;
    }

    private float NormalizeAngle(float angle)
    {
        return angle >= 300 ? angle - 360 : angle;
    }

    // private void UpdateInnerExtension(
    //     bool isInnerExtensionTouched,
    //     bool isTipTouched,
    //     float jointAngleValue,
    //     string touchDurationKey,
    //     string baseJointName,
    //     ref float currentRotation,
    //     float rotationSpeed,
    //     Renderer jointRenderer,
    //     Color activeColor,
    //     Color inactiveColor,
    //     Transform jointTransform,
    //     ref bool relatedMotorTriggered,
    //     System.Func<bool> additionalCondition = null)
    // {
    //     // Initialize rotation based on jointAngleValue for the base angle
    //     Quaternion targetRotation = Quaternion.Euler(jointAngleValue + currentRotation, 0f, 0f);

    //     // Initialize touch duration if not already present
    //     if (!fingerTipTouchDurations.ContainsKey(touchDurationKey))
    //     {
    //         fingerTipTouchDurations[touchDurationKey] = 0f;
    //     }

    //     // Determine the condition to use
    //     bool conditionMet;
    //     if (additionalCondition != null)
    //     {
    //         conditionMet = isInnerExtensionTouched && !isTipTouched && additionalCondition();
    //     }
    //     else
    //     {
    //         conditionMet = isInnerExtensionTouched && 
    //             jointAngle.joints[baseJointName].localRotation.eulerAngles.z > 10.0 &&
    //             jointAngle.joints[baseJointName].localRotation.eulerAngles.z < 30.0 &&
    //             !isTipTouched;
    //     }

    //     // Check if inner extension is touched (separate from tip touch)
    //     if (conditionMet)
    //     {
    //         fingerTipTouchDurations[touchDurationKey] += Time.deltaTime;
    //         jointRenderer.material.color = Color.Lerp(inactiveColor, activeColor, Mathf.Min(fingerTipTouchDurations[touchDurationKey], 1f));
    //         relatedMotorTriggered = true;

    //         // Only apply rotation after 1 second
    //         if (fingerTipTouchDurations[touchDurationKey] > 1.0f && conditionMet)
    //         {
    //             currentRotation -= rotationSpeed * Time.deltaTime;
    //             currentRotation = Mathf.Clamp(currentRotation, -80f, 0f);
    //             jointRenderer.material.color = activeColor;
    //         }
    //     }
    //     else
    //     {
    //         fingerTipTouchDurations[touchDurationKey] = 0f;
    //         jointRenderer.material.color = inactiveColor;
    //         relatedMotorTriggered = false;
    //     }

    //     if (isMapping)
    //     {
    //         float normalized = Mathf.InverseLerp(0f, 25f, jointAngleValue);
    //         float tipEffect = Mathf.Lerp(currentRotation, 0f, normalized); // impact decreases as jointAngleValue approaches 25
    //         float finalAngle = jointAngleValue + tipEffect;
    //         targetRotation = Quaternion.Euler(finalAngle, 0f, 0f);
    //     }
    //     else
    //     {
    //         targetRotation = Quaternion.Euler(jointAngleValue + currentRotation, 0f, 0f);
    //     }

    //     if (jointTransform != null)
    //         jointTransform.localRotation = targetRotation;
    // }

    // private void UpdateFingertipExtension(
    //     bool isTipTouched,
    //     float jointAngleValue,
    //     float requiredAngleThreshold,
    //     string jointName,
    //     string baseJointName,
    //     ref float currentTipRotation,
    //     float rotationSpeed,
    //     Renderer jointRenderer,
    //     Color activeColor,
    //     Color inactiveColor,
    //     Transform jointTransform,
    //     ref bool relatedMotorTriggered,
    //     System.Func<bool> additionalCondition = null)
    // {
    //     // Initialize rotation based on jointAngleValue for the base angle
    //     Quaternion targetRotation = Quaternion.Euler(jointAngleValue + currentTipRotation, 0f, 0f);

    //     // Initialize touch duration if not already present
    //     if (!fingerTipTouchDurations.ContainsKey(jointName))
    //     {
    //         fingerTipTouchDurations[jointName] = 0f;
    //     }
        
    //     // Initialize activation state if not already present
    //     if (!fingerTipActivated.ContainsKey(jointName))
    //     {
    //         fingerTipActivated[jointName] = false;
    //     }

    //     // Determine the initial condition to use (for activation)
    //     bool initialConditionMet;
    //     if (additionalCondition != null)
    //     {
    //         initialConditionMet = isTipTouched && additionalCondition();
    //     }
    //     else
    //     {
    //         initialConditionMet = isTipTouched && jointAngle.joints[baseJointName].localRotation.eulerAngles.z > 12.0 && jointAngle.joints[baseJointName].localRotation.eulerAngles.z < 30.0;
    //     }
        
    //     // Once activated, only check if still touched
    //     bool conditionMet = fingerTipActivated[jointName] ? isTipTouched : initialConditionMet;

    //     // Update the touch duration
    //     if (conditionMet)
    //     {
    //         fingerTipTouchDurations[jointName] += Time.deltaTime;
    //         // Change color to show it's being touched
    //         jointRenderer.material.color = Color.Lerp(inactiveColor, activeColor, Mathf.Min(fingerTipTouchDurations[jointName], 1f));
    //         relatedMotorTriggered = true;
            
    //         // Activate once the duration exceeds 1 second and initial condition is met
    //         if (fingerTipTouchDurations[jointName] > 1.0f && initialConditionMet)
    //         {
    //             fingerTipActivated[jointName] = true;
    //         }
    //     }
    //     else
    //     {
    //         // Reset the timer and activation state if no longer touched
    //         fingerTipTouchDurations[jointName] = 0f;
    //         fingerTipActivated[jointName] = false;
    //         jointRenderer.material.color = inactiveColor;
    //         relatedMotorTriggered = false;
    //     }

    //     // Only apply rotation if touched for more than 1 second and activated
    //     if (fingerTipTouchDurations[jointName] > 1.0f && fingerTipActivated[jointName])
    //     {
    //         // Smoothly increase the rotation while the tip is touched
    //         currentTipRotation -= rotationSpeed * Time.deltaTime;
    //         currentTipRotation = Mathf.Clamp(currentTipRotation, -80f, 0f);
    //         jointRenderer.material.color = activeColor;
    //         relatedMotorTriggered = true;
    //     }
    //     else
    //     {
    //         relatedMotorTriggered = false;
    //     }

    //     if (isMapping)
    //     {
    //         // float normalized = Mathf.InverseLerp(0f, 25f, jointAngleValue);
    //         // float tipEffect = Mathf.Lerp(currentTipRotation, 0f, normalized); // impact decreases as jointAngleValue approaches 25
    //         // float finalAngle = jointAngleValue + tipEffect;
    //         // targetRotation = Quaternion.Euler(finalAngle, 0f, 0f);
    //         targetRotation = Quaternion.Euler(jointAngleValue + currentTipRotation, 0f, 0f);
    //     }
    //     else
    //     {
    //         targetRotation = Quaternion.Euler(jointAngleValue + currentTipRotation, 0f, 0f);
    //     }

    //     jointAngleValueDebug = jointAngleValue;
    //     currentTipRotationDebug = currentTipRotation;


    //     if (jointTransform != null)
    //         jointTransform.localRotation = targetRotation;
    // }

    private void UpdateFingertipExtensionV2(
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
        bool isManipulatingMode = false,
        int motorID = 0,
        int expectedMotorID = 0,
        float angleThreshold = 15.0f,
        float? additionalAngle = null)
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
            
            // Initialize additional angle tracking if provided
            if (additionalAngle.HasValue)
            {
                previousAdditionalAngles[jointName] = additionalAngle.Value;
                accumulatedAdditionalChanges[jointName] = 0f;
            }
        }
        
        // Calculate accumulated angle changes - all joints use unified wrap-around handling
        totalAngleChange = 0f;
        if (isTipTouched && previousJointAngles.ContainsKey(jointName))
        {
            // Process jointName with wrap-around
            float currentJointAngle = jointAngle.joints[jointName].localRotation.eulerAngles.z;
            float deltaJoint = currentJointAngle - previousJointAngles[jointName];
            if (deltaJoint < -180f) deltaJoint += 360f;
            else if (deltaJoint > 180f) deltaJoint -= 360f;
            accumulatedJointChanges[jointName] += deltaJoint;
            previousJointAngles[jointName] = currentJointAngle;
            
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
            
            totalAngleChange = Mathf.Abs(accumulatedJointChanges[jointName]) + 
                               Mathf.Abs(accumulatedMidJointChanges[jointName]) + 
                               Mathf.Abs(accumulatedBaseJointChanges[jointName]);
            
            // Add additional angle change if available
            if (additionalAngle.HasValue && accumulatedAdditionalChanges.ContainsKey(jointName))
            {
                totalAngleChange += Mathf.Abs(accumulatedAdditionalChanges[jointName]);
            }
            
            // Debug output
            string additionalInfo = additionalAngle.HasValue && accumulatedAdditionalChanges.ContainsKey(jointName) 
                ? $", Additional: {accumulatedAdditionalChanges[jointName]:F2}" 
                : "";
            Debug.Log($"{jointName} - Joint: {accumulatedJointChanges[jointName]:F2}, Mid: {accumulatedMidJointChanges[jointName]:F2}, Base: {accumulatedBaseJointChanges[jointName]:F2}{additionalInfo}, Total: {totalAngleChange:F2}");
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
            // Check if motor ID matches when in manipulating mode
            // If expectedMotorID is set (not 0), must be in manipulating mode AND motor ID must match
            // If expectedMotorID is 0, no restriction (backward compatibility)
            bool motorIDMatches = (expectedMotorID == 0) || (isManipulatingMode && motorID == expectedMotorID);
            
            // Determine the initial condition to use (for activation)
            // New condition: total angle change must be greater than threshold
            // Also check that other motor 4s are not triggered (mutual exclusion)
            // And check motor ID matches in manipulating mode
            bool initialConditionMet = isTipTouched && totalAngleChange > angleThreshold && !shouldPreventActivation && motorIDMatches;
            
            // Debug: only log when condition is actually met
            if (initialConditionMet)
            {
                // Debug.Log($"{jointName} - Condition Met! totalAngleChange: {totalAngleChange}, isTipTouched: {isTipTouched}");
            }
            
            // Once activated, only check if still touched
            bool conditionMet = fingerTipActivated[jointName] ? isTipTouched : initialConditionMet;

            // Update the touch duration and activation
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
        if (fingerTipActivated[jointName])
        {
            // Smoothly increase the rotation while the tip is touched
            currentTipRotation -= rotationSpeed * Time.deltaTime;
            currentTipRotation = Mathf.Clamp(currentTipRotation, -80f, 0f);
            // jointRenderer.material.color = activeColor;
            relatedMotorTriggered = true;
        }
        else
        {
            relatedMotorTriggered = false;
        }

        if (isMapping)
        {
            targetRotation = Quaternion.Euler(jointAngleValue + currentTipRotation, 0f, 0f);
        }
        else
        {
            targetRotation = Quaternion.Euler(jointAngleValue + currentTipRotation, 0f, 0f);
        }

        jointAngleValueDebug = jointAngleValue;
        currentTipRotationDebug = currentTipRotation;

        if (jointTransform != null)
            jointTransform.localRotation = targetRotation;
    }

    // ==============================
    // 🔹 Reset Function
    // ==============================
    public void ResetFingerRotations()
    {
        isMapping = true;

        currentThumbRotationY = currentThumbRotationZ = 0f;
        currentIndexRotationY = currentIndexRotationZ = 0f;
        currentMiddleRotationY = currentMiddleRotationZ = 0f;

        currentThumbTipRotationZ = 0f;
        currentIndexTipRotationZ = 0f;
        currentMiddleTipRotationZ = 0f;

        currentThumbInnerExtensionRotationZ = 0f;
        currentIndexInnerExtensionRotationZ = 0f;
        currentMiddleInnerExtensionRotationZ = 0f;

        thumbFingerJoint1MaxRotationVector = ThumbAngle1CenterInitialRotation.eulerAngles;
        thumbFingerJoint2MaxRotationVector = ThumbAngle2CenterInitialRotation.eulerAngles;
        indexFingerJoint1MaxRotationVector = IndexAngle1CenterInitialRotation.eulerAngles;
        indexFingerJoint2MaxRotationVector = IndexAngle2CenterInitialRotation.eulerAngles;
        middleFingerJoint1MaxRotationVector = MiddleAngle1CenterInitialRotation.eulerAngles;
        middleFingerJoint2MaxRotationVector = MiddleAngle2CenterInitialRotation.eulerAngles;

        maxThumbYAxisAngle = ThumbAngle1CenterInitialRotation.eulerAngles.y;
        maxThumbZAxisAngle = ThumbAngle2CenterInitialRotation.eulerAngles.z;
        maxIndexYAxisAngle = IndexAngle1CenterInitialRotation.eulerAngles.y;
        maxIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;
        maxMiddleYAxisAngle = MiddleAngle1CenterInitialRotation.eulerAngles.y;
        maxMiddleZAxisAngle = MiddleAngle2CenterInitialRotation.eulerAngles.z;

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

        ApplyResetRotations();
        tt = 0f;
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
}