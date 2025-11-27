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
    private float rotationSpeed = 8f; // degrees per second
    public bool isMapping = true;
    public float tt = 0f;

    // ==============================
    // 🔹 Colors
    // ==============================
    private Color originalColor;
    public Color redColor = Color.red;
    public Color yellowColor = new Color(1f, 0.9647f, 0f); // #FFF600
    public Color purpleColor = new Color(0.5f, 0f, 0.5f);

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

    // ----------------------------------------- debug
    public float jointAngleValueDebug = 0f;
    public float currentTipRotationDebug = 0f;

    // Dictionary to track how long each fingertip has been touched
    private Dictionary<string, float> fingerTipTouchDurations = new Dictionary<string, float>();

    void Start()
    {
        if (jointAngle == null)
        {
            Debug.LogError("JointAngle is not assigned in the inspector for " + gameObject.name);
        }

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
        HandleInput();

        // ==============================
        // 🔹 Thumb
        // ==============================

        UpdateThumbFingerTwist();

        if (ThumbAngle3Center != null)
            ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f); //FIXME: to let it bend for more deeper
        if (ThumbAngle4Center != null)
            ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f);

        // ==============================
        // 🔹 Index Finger
        // ==============================
        // UpdateIndexFingerTwist();

        // UpdateIndexFingerAbduction();
        UpdateIndexFingerAbductionByZ();

        Quaternion targetIndexJoint4Rotation = Quaternion.Euler(jointAngle.indexAngle2 + currentIndexTipRotationZ, 0f, 0f);

        targetIndexJoint4Rotation = Quaternion.Euler(jointAngle.indexAngle2 + currentIndexTipRotationZ, 0f, 0f);

        if (IndexAngle3Center != null)
            IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);

        UpdateFingertipExtension(
            triggerRightIndexTip.isRightIndexTipTouched,
            jointAngle.indexAngle2,
            302f,
            "Index2",
            ref currentIndexTipRotationZ,
            rotationSpeed,
            indexJoint4Renderer,
            purpleColor,
            originalColor,
            IndexAngle4Center
        );

        // ==============================
        // 🔹 Middle Finger State
        // ==============================
        UpdateMiddleFingerTwist();

        // UpdateMiddleFingerAbduction();
        UpdateMiddleFingerAbductionByZ();

        if (MiddleAngle3Center != null)
            MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);

        UpdateFingertipExtension(
            triggerRightMiddleTip.isRightMiddleTipTouched,
            jointAngle.middleAngle2,
            302f,
            "Middle2",
            ref currentMiddleTipRotationZ,
            rotationSpeed,
            middleJoint4Renderer,
            purpleColor,
            originalColor,
            MiddleAngle4Center
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
            Debug.Log("ResetFingerRotations called from Update");
            ResetFingerRotations();
        }
    }

    // ==============================
    // 🔹 Index Finger Abduction (Y-axis)
    // ==============================

    // private void UpdateIndexFingerAbduction()
    // {
    //     maxIndexYAxisAngle = NormalizeAngle(indexFingerJoint1MaxRotationVector.y);
    //     Quaternion targetRotation = IndexAngle1CenterInitialRotation;

    //     if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.5f)
    //     {
    //         currentIndexRotationY -= rotationSpeed * Time.deltaTime;
    //         currentIndexRotationY = Mathf.Max(currentIndexRotationY, -60f);

    //         indexFingerJoint1MaxRotationVector =
    //             (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationY, 0f)).eulerAngles;

    //         indexJoint1Renderer.material.color = yellowColor;
    //     }
    //     else
    //     {
    //         indexJoint1Renderer.material.color = originalColor;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, currentIndexRotationY, 0f);

    //     if (jointAngle.indexMiddleDistance < 3.5f && IndexAngle1Center != null)
    //     {
    //         float delta = maxIndexYAxisAngle;
    //         float targetY = isMapping
    //             ? maxIndexYAxisAngle + (30 - delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
    //             : indexFingerJoint1MaxRotationVector.y + 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

    //         Vector3 euler = targetRotation.eulerAngles;
    //         targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //         tt = targetY;
    //     }

    //     if (IndexAngle1Center != null)
    //         IndexAngle1Center.localRotation = targetRotation;
    // }

    // ==============================
    // 🔹 Middle Finger Abduction (Y-axis)
    // ==============================

    // private void UpdateMiddleFingerAbduction()
    // {
    //     maxMiddleYAxisAngle = NormalizeAngle(middleFingerJoint1MaxRotationVector.y);
    //     Quaternion targetRotation = MiddleAngle1CenterInitialRotation;

    //     if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.indexMiddleDistance > 3.5f)
    //     {
    //         currentMiddleRotationY += rotationSpeed * Time.deltaTime;
    //         currentMiddleRotationY = Mathf.Min(currentMiddleRotationY, 60f);

    //         middleFingerJoint1MaxRotationVector =
    //             (MiddleAngle1CenterInitialRotation * Quaternion.Euler(0f, currentMiddleRotationY, 0f)).eulerAngles;

    //         middleJoint1Renderer.material.color = yellowColor;
    //     }
    //     else
    //     {
    //         middleJoint1Renderer.material.color = originalColor;
    //     }

    //     targetRotation *= Quaternion.Euler(0f, currentMiddleRotationY, 0f);

    //     if (jointAngle.indexMiddleDistance < 3.5f && MiddleAngle1Center != null)
    //     {
    //         float delta = maxMiddleYAxisAngle;
    //         float targetY = isMapping
    //             ? maxMiddleYAxisAngle - (30 + delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
    //             : middleFingerJoint1MaxRotationVector.y - 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

    //         Vector3 euler = targetRotation.eulerAngles;
    //         targetRotation = Quaternion.Euler(euler.x, targetY, euler.z);
    //     }

    //     if (MiddleAngle1Center != null)
    //         MiddleAngle1Center.localRotation = targetRotation;
    // }

    private void UpdateThumbFingerTwist()
    {
        Quaternion targetRotation = ThumbAngle1CenterInitialRotation;

        if (triggerRightThumbTip.isRightThumbTipTouched && jointAngle.isPlaneActive)
        {
            currentThumbRotationY -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
            currentThumbRotationY = Mathf.Max(currentThumbRotationY, -60f);

            thumbJoint1Renderer.material.color = yellowColor;
        }
        else
        {
            thumbJoint1Renderer.material.color = originalColor;
        }

        targetRotation *= Quaternion.Euler(0f, currentThumbRotationY, 0f);

        if (ThumbAngle1Center != null)
            ThumbAngle1Center.localRotation = targetRotation;
    }

    private void UpdateIndexFingerTwist()
    {
        Quaternion targetRotation = IndexAngle1CenterInitialRotation;

        if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.isPlaneActive)
        {
            currentIndexRotationY -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
            currentIndexRotationY = Mathf.Max(currentIndexRotationY, -60f);

            indexJoint1Renderer.material.color = yellowColor;
        }
        else
        {
            indexJoint1Renderer.material.color = originalColor;
        }

        targetRotation *= Quaternion.Euler(0f, currentIndexRotationY, 0f);

        if (IndexAngle1Center != null)
            IndexAngle1Center.localRotation = targetRotation;
    }

    private void UpdateMiddleFingerTwist()
    {
        Quaternion targetRotation = MiddleAngle1CenterInitialRotation;

        if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.isPlaneActive)
        {
            currentMiddleRotationY -= jointAngle.isClockWise * rotationSpeed * Time.deltaTime;
            currentMiddleRotationY = Mathf.Max(currentMiddleRotationY, -60f);

            middleJoint1Renderer.material.color = yellowColor;
        }
        else
        {
            middleJoint1Renderer.material.color = originalColor;
        }

        targetRotation *= Quaternion.Euler(0f, currentMiddleRotationY, 0f);

        if (MiddleAngle1Center != null)
            MiddleAngle1Center.localRotation = targetRotation;
    }


    // ==============================
    // 🔹 Index Finger Abduction (Z-axis)
    // ==============================
    private void UpdateIndexFingerAbductionByZ()
    {
        maxIndexZAxisAngle = NormalizeAngle(indexFingerJoint2MaxRotationVector.z);
        Quaternion targetRotation = IndexAngle2CenterInitialRotation;

        if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentIndexRotationZ -= rotationSpeed * Time.deltaTime;
            // currentIndexRotationZ = Mathf.Max(currentIndexRotationZ, -60f);

            indexFingerJoint2MaxRotationVector =
                (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZ)).eulerAngles;

            indexJoint2Renderer.material.color = yellowColor;
        }
        else
        {
            indexJoint2Renderer.material.color = originalColor;
        }

        targetRotation *= Quaternion.Euler(0f, 0f, currentIndexRotationZ);

        if (jointAngle.indexMiddleDistance < 3.5f && IndexAngle2Center != null)
        {
            float delta = maxIndexZAxisAngle;
            float targetZ = isMapping
                ? maxIndexZAxisAngle + (30 - delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
                : indexFingerJoint2MaxRotationVector.z + 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

            Vector3 euler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
        }

        if (IndexAngle2Center != null)
            IndexAngle2Center.localRotation = targetRotation;
    }

    // ==============================
    // 🔹 Middle Finger Abduction (Z-axis)
    // ==============================
    private void UpdateMiddleFingerAbductionByZ()
    {
        maxMiddleZAxisAngle = NormalizeAngle(middleFingerJoint2MaxRotationVector.z);
        Quaternion targetRotation = MiddleAngle2CenterInitialRotation;

        if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentMiddleRotationZ += rotationSpeed * Time.deltaTime;
            currentMiddleRotationZ = Mathf.Min(currentMiddleRotationZ, 60f);

            middleFingerJoint2MaxRotationVector =
                (MiddleAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentMiddleRotationZ)).eulerAngles;

            middleJoint2Renderer.material.color = yellowColor;
        }
        else
        {
            middleJoint2Renderer.material.color = originalColor;
        }

        targetRotation *= Quaternion.Euler(0f, 0f, currentMiddleRotationZ);

        if (jointAngle.indexMiddleDistance < 3.5f && MiddleAngle2Center != null)
        {
            float delta = maxMiddleZAxisAngle;
            float targetZ = isMapping
                ? maxMiddleZAxisAngle - (30 + delta) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)
                : middleFingerJoint2MaxRotationVector.z - 30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f);

            Vector3 euler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler(euler.x, euler.y, targetZ);
        }

        if (MiddleAngle2Center != null)
            MiddleAngle2Center.localRotation = targetRotation;
    }

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
        ref float currentTipRotation,
        float rotationSpeed,
        Renderer jointRenderer,
        Color activeColor,
        Color inactiveColor,
        Transform jointTransform)
    {
        // Initialize rotation based on jointAngleValue for the base angle
        Quaternion targetRotation = Quaternion.Euler(jointAngleValue + currentTipRotation, 0f, 0f);

        // Initialize touch duration if not already present
        if (!fingerTipTouchDurations.ContainsKey(jointName))
        {
            fingerTipTouchDurations[jointName] = 0f;
        }

        // Update the touch duration
        if (isTipTouched && jointAngle.joints.ContainsKey(jointName) &&
            jointAngle.joints[jointName].localRotation.eulerAngles.x > requiredAngleThreshold)
        {
            fingerTipTouchDurations[jointName] += Time.deltaTime;
            // Change color to show it's being touched
            jointRenderer.material.color = Color.Lerp(inactiveColor, activeColor, Mathf.Min(fingerTipTouchDurations[jointName], 1f));
        }
        else
        {
            // Reset the timer if no longer touched
            fingerTipTouchDurations[jointName] = 0f;
            jointRenderer.material.color = inactiveColor;
        }

        // Only apply rotation if touched for more than 1 second
        if (fingerTipTouchDurations[jointName] > 1.0f &&
            jointAngle.joints.ContainsKey(jointName) &&
            jointAngle.joints[jointName].localRotation.eulerAngles.x > requiredAngleThreshold)
        {
            // Smoothly increase the rotation while the tip is touched
            currentTipRotation -= rotationSpeed * Time.deltaTime;
            jointRenderer.material.color = activeColor;
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