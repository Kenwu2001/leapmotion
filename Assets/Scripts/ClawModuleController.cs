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
    public TriggerRightIndexTwist triggerRightIndexTwist;

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

    // ----------------------------------------- debug
    public float jointAngleValueDebug = 0f;
    public float currentTipRotationDebug = 0f;

    // Dictionary to track how long each fingertip has been touched
    private Dictionary<string, float> fingerTipTouchDurations = new Dictionary<string, float>();

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

        UpdateThumbAbduction();

        UpdateThumbFingerTwist();

        if (ThumbAngle3Center != null)
            ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f); //FIXME: to let it bend for more deeper
        
        UpdateFingertipExtension(
            triggerRightThumbTip.isRightThumbTipTouched,
            jointAngle.thumbAngle1,
            302f,
            "Thumb1",
            "Thumb0",
            ref currentThumbTipRotationZ,
            rotationSpeed,
            thumbJoint4Renderer,
            purpleColor,
            originalColor,
            ThumbAngle4Center,
            ref isThumb4Triggered
        );
        
        // if (ThumbAngle4Center != null)
        //     ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f);

        // ==============================
        // 🔹 Index Finger
        // ==============================
        
        UpdateIndexFingerAbduction();  //good
        
        UpdateIndexFingerTwist();

        // Quaternion targetIndexJoint4Rotation = Quaternion.Euler(jointAngle.indexAngle2 + currentIndexTipRotationZ, 0f, 0f);

        // targetIndexJoint4Rotation = Quaternion.Euler(jointAngle.indexAngle2 + currentIndexTipRotationZ, 0f, 0f);

        if (IndexAngle3Center != null)
            IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);

        UpdateFingertipExtension(
            triggerRightIndexTip.isRightIndexTipTouched,
            jointAngle.indexAngle2,
            302f,
            "Index2",
            "Index0",
            ref currentIndexTipRotationZ,
            rotationSpeed,
            indexJoint4Renderer,
            purpleColor,
            originalColor,
            IndexAngle4Center,
            ref isIndex4Triggered
        );

        // if (IndexAngle4Center != null)
        //     IndexAngle4Center.localRotation = Quaternion.Euler(jointAngle.indexAngle2, 0f, 0f);

        // ==============================
        // 🔹 Middle Finger State
        // ==============================

        UpdateMiddleFingerAbduction();   // good
        
        UpdateMiddleFingerTwist();

        if (MiddleAngle3Center != null)
            MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);

        UpdateFingertipExtension(
            triggerRightMiddleTip.isRightMiddleTipTouched,
            jointAngle.middleAngle2,
            302f,
            "Middle2",
            "Middle0",
            ref currentMiddleTipRotationZ,
            rotationSpeed,
            middleJoint4Renderer,
            purpleColor,
            originalColor,
            MiddleAngle4Center,
            ref isMiddle4Triggered
        );

        // if (MiddleAngle4Center != null)
        //     MiddleAngle4Center.localRotation = Quaternion.Euler(jointAngle.middleAngle2, 0f, 0f);
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

        if (triggerRightThumbTip.isRightThumbTipTouched && jointAngle.thumbPalmAngle > 58f && !isAnyMotor4Triggered && !isThumb2Triggered && canControlThumb1)
        {
            currentThumbRotationY -= rotationSpeed * Time.deltaTime;
            currentThumbRotationY = Mathf.Clamp(currentThumbRotationY, -60f, 0f);

            thumbFingerJoint1MaxRotationVector =
                (ThumbAngle1CenterInitialRotation * Quaternion.Euler(0f, currentThumbRotationY, 0f)).eulerAngles;

            thumbJoint1Renderer.material.color = greenColor;
            isThumb1Triggered = true;
        }
        else
        {
            thumbJoint1Renderer.material.color = originalColor;
            isThumb1Triggered = false;
        }

        // Base angle from thumb-palm angle
        float baseAngle = 45f - jointAngle.thumbPalmAngle;
        
        targetRotation *= Quaternion.Euler(0f, baseAngle + currentThumbRotationY, 0f);

        // mapping using thumb palm angle
        float thumbPalmAngleDiff = 45f - jointAngle.thumbPalmAngle;
        if (isMapping && Mathf.Abs(currentThumbRotationY) > 0.1f && Mathf.Abs(thumbPalmAngleDiff) > 0.1f)
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

        if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.9f && !isAnyMotor4Triggered && canControlIndex1)
        {
            currentIndexRotationY -= rotationSpeed * Time.deltaTime;
            currentIndexRotationY = Mathf.Max(currentIndexRotationY, -60f);

            indexFingerJoint1MaxRotationVector =
                (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationY, 0f)).eulerAngles;

            indexJoint1Renderer.material.color = yellowColor;
            isIndex1Triggered = true;
        }
        else
        {
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

        if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.indexMiddleDistance > 3.9f && !isAnyMotor4Triggered && canControlMiddle1)
        {
            currentMiddleRotationY += rotationSpeed * Time.deltaTime;
            currentMiddleRotationY = Mathf.Min(currentMiddleRotationY, 60f);

            middleFingerJoint1MaxRotationVector =
                (MiddleAngle1CenterInitialRotation * Quaternion.Euler(0f, currentMiddleRotationY, 0f)).eulerAngles;

            middleJoint1Renderer.material.color = yellowColor;
            isMiddle1Triggered = true;
        }
        else
        {
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

        if (triggerRightThumbTip.isRightThumbTipTouched && jointAngle.isPlaneActive && !isAnyMotor4Triggered && !isThumb1Triggered && canControlThumb2 && jointAngle.thumbPalmAngle < 58f)
        {
            currentThumbRotationZ -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
            currentThumbRotationZ = Mathf.Clamp(currentThumbRotationZ, -60f, 0f);

            thumbFingerJoint2MaxRotationVector =
                (ThumbAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentThumbRotationZ)).eulerAngles;

            thumbJoint2Renderer.material.color = greenColor;
            isThumb2Triggered = true;
        }
        else
        {
            thumbJoint2Renderer.material.color = originalColor;
            isThumb2Triggered = false;
        }

        // Base angle from wrist-thumb angle
        float baseAngle = 45f - jointAngle.wristThumbAngle;
        
        targetRotation *= Quaternion.Euler(0f, 0f, baseAngle + currentThumbRotationZ);

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

    // private void UpdateIndexFingerTwist() //bottom motor
    // {
    //     Quaternion targetRotation = IndexAngle1CenterInitialRotation;

    //     if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.isPlaneActive)
    //     {
    //         currentIndexRotationY -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
    //         currentIndexRotationY = Mathf.Max(currentIndexRotationY, -60f);

    //         indexJoint1Renderer.material.color = yellowColor;
    //     }
    //     else
    //     {
    //         indexJoint1Renderer.material.color = originalColor;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, currentIndexRotationY, 0f);

    //     if (IndexAngle1Center != null)
    //         IndexAngle1Center.localRotation = targetRotation;
    // }

    private void UpdateIndexFingerTwist() // second motor
    {
        Quaternion targetRotation = IndexAngle2CenterInitialRotation;
        maxIndexZAxisAngle = NormalizeAngle(indexFingerJoint2MaxRotationVector.z);

        //   triggerRightIndexTwist.isRightIndexTwistTouched
        if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.isPlaneActive && !isAnyMotor4Triggered && jointAngle.indexMiddleDistance < 3.5f && canControlIndex2)
        {
            if(currentIndexRotationZ >= -58f && currentIndexRotationZ <= 0)
            {   
                currentIndexRotationZ -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
            }

            currentIndexRotationZ = Mathf.Max(currentIndexRotationZ, -58);

            indexFingerJoint2MaxRotationVector =
                (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZ)).eulerAngles;

            indexJoint2Renderer.material.color = greenColor;
            isIndex2Triggered = true;
        }
        else
        {
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

    // private void UpdateMiddleFingerTwist()  // bottom motor
    // {
    //     Quaternion targetRotation = MiddleAngle1CenterInitialRotation;

    //     if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.isPlaneActive)
    //     {
    //         currentMiddleRotationY -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
    //         currentMiddleRotationY = Mathf.Max(currentMiddleRotationY, -60f);

    //         middleJoint1Renderer.material.color = yellowColor;
    //     }
    //     else
    //     {
    //         middleJoint1Renderer.material.color = originalColor;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, currentMiddleRotationY, 0f);

    //     if (MiddleAngle1Center != null)
    //         MiddleAngle1Center.localRotation = targetRotation;
    // }

    private void UpdateMiddleFingerTwist()  // second motor
    {
        Quaternion targetRotation = MiddleAngle2CenterInitialRotation;
        maxMiddleZAxisAngle = NormalizeAngle(middleFingerJoint2MaxRotationVector.z);

        if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.isPlaneActive && !isAnyMotor4Triggered && jointAngle.indexMiddleDistance < 3.5f && canControlMiddle2)
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
            isMiddle2Triggered = true;
        }
        else
        {
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

    // ==============================
    // 🔹 Index Finger Abduction (Z-axis)
    // ==============================
    // private void UpdateIndexFingerAbductionByZ()
    // {
    //     maxIndexZAxisAngle = NormalizeAngle(indexFingerJoint2MaxRotationVector.z);
    //     Quaternion targetRotation = IndexAngle2CenterInitialRotation;

    //     if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.5f)
    //     {
    //         currentIndexRotationZ -= rotationSpeed * Time.deltaTime;
    //         // currentIndexRotationZ = Mathf.Max(currentIndexRotationZ, -60f);

    //         indexFingerJoint2MaxRotationVector =
    //             (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZ)).eulerAngles;

    //         indexJoint2Renderer.material.color = yellowColor;
    //     }
    //     else
    //     {
    //         indexJoint2Renderer.material.color = originalColor;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, 0f, currentIndexRotationZ);

    //     if (jointAngle.indexMiddleDistance < 3.5f && IndexAngle2Center != null)
    //     {
    //         float delta = maxIndexZAxisAngle;
    //         float targetZ = isMapping
    //             ? maxIndexZAxisAngle + (30 - delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
    //             : indexFingerJoint2MaxRotationVector.z + 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

    //         Vector3 euler = targetRotation.eulerAngles;
    //         targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //     }

    //     if (IndexAngle2Center != null)
    //         IndexAngle2Center.localRotation = targetRotation;
    // }

    // ==============================
    // 🔹 Middle Finger Abduction (Z-axis)
    // ==============================
    // private void UpdateMiddleFingerAbductionByZ()
    // {
    //     maxMiddleZAxisAngle = NormalizeAngle(middleFingerJoint2MaxRotationVector.z);
    //     Quaternion targetRotation = MiddleAngle2CenterInitialRotation;

    //     if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.indexMiddleDistance > 3.5f)
    //     {
    //         currentMiddleRotationZ += rotationSpeed * Time.deltaTime;
    //         currentMiddleRotationZ = Mathf.Min(currentMiddleRotationZ, 60f);

    //         middleFingerJoint2MaxRotationVector =
    //             (MiddleAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentMiddleRotationZ)).eulerAngles;

    //         middleJoint2Renderer.material.color = yellowColor;
    //     }
    //     else
    //     {
    //         middleJoint2Renderer.material.color = originalColor;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, 0f, currentMiddleRotationZ);

    //     if (jointAngle.indexMiddleDistance < 3.5f && MiddleAngle2Center != null)
    //     {
    //         float delta = maxMiddleZAxisAngle;
    //         float targetZ = isMapping
    //             ? maxMiddleZAxisAngle - (30 + delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
    //             : middleFingerJoint2MaxRotationVector.z - 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

    //         Vector3 euler = targetRotation.eulerAngles;
    //         targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
    //     }

    //     if (MiddleAngle2Center != null)
    //         MiddleAngle2Center.localRotation = targetRotation;
    // }

    //TODO: update thumb abduction

    // ==============================
    // 🔹 Utility
    // ==============================
    private float NormalizeAngle(float angle)
    {
        return angle >= 300 ? angle - 360 : angle;
    }

    private void UpdateFingertipExtension(
        bool isTipTouched,
        float jointAngleValue,
        float requiredAngleThreshold,
        string jointName,
        string baseJointName,
        ref float currentTipRotation,
        float rotationSpeed,
        Renderer jointRenderer,
        Color activeColor,
        Color inactiveColor,
        Transform jointTransform,
        ref bool relatedMotorTriggered)
    {
        // Initialize rotation based on jointAngleValue for the base angle
        Quaternion targetRotation = Quaternion.Euler(jointAngleValue + currentTipRotation, 0f, 0f);

        // Initialize touch duration if not already present
        if (!fingerTipTouchDurations.ContainsKey(jointName))
        {
            fingerTipTouchDurations[jointName] = 0f;
        }

        // Update the touch duration
        // if (isTipTouched && jointAngle.joints.ContainsKey(jointName) &&
        //     jointAngle.joints[jointName].localRotation.eulerAngles.x > requiredAngleThreshold && jointAngle.joints[baseJointName].localRotation.eulerAngles.z > 10.0 && jointAngle.joints[baseJointName].localRotation.eulerAngles.z < 30.0)
        if (isTipTouched && jointAngle.joints[baseJointName].localRotation.eulerAngles.z > 10.0 && jointAngle.joints[baseJointName].localRotation.eulerAngles.z < 30.0)
        {
            fingerTipTouchDurations[jointName] += Time.deltaTime;
            // Change color to show it's being touched
            jointRenderer.material.color = Color.Lerp(inactiveColor, activeColor, Mathf.Min(fingerTipTouchDurations[jointName], 1f));
            relatedMotorTriggered = true;
        }
        else
        {
            // Reset the timer if no longer touched
            fingerTipTouchDurations[jointName] = 0f;
            jointRenderer.material.color = inactiveColor;
            relatedMotorTriggered = false;
        }

        // Only apply rotation if touched for more than 1 second
        // if (fingerTipTouchDurations[jointName] > 1.0f &&
        //     jointAngle.joints.ContainsKey(jointName) &&
        //     jointAngle.joints[jointName].localRotation.eulerAngles.x > requiredAngleThreshold)
        if (fingerTipTouchDurations[jointName] > 1.0f)
        {
            // Smoothly increase the rotation while the tip is touched
            currentTipRotation -= rotationSpeed * Time.deltaTime;
            jointRenderer.material.color = activeColor;
            relatedMotorTriggered = true;
        }
        else
        {
            relatedMotorTriggered = false;
        }

        if (isMapping)
        {
            float normalized = Mathf.InverseLerp(0f, 25f, jointAngleValue);
            float tipEffect = Mathf.Lerp(currentTipRotation, 0f, normalized); // impact decreases as jointAngleValue approaches 25
            float finalAngle = jointAngleValue + tipEffect;
            targetRotation = Quaternion.Euler(finalAngle, 0f, 0f);
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

        // Clear touch durations
        fingerTipTouchDurations.Clear();

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