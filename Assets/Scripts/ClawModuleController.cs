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

    // ==============================
    // 🔹 Thumb Transforms
    // ==============================
    public Transform ThumbAngle1Center;
    public Transform ThumbAngle2Center;
    public Transform ThumbAngle3Center;
    public Transform ThumbAngle4Center;

    // ==============================
    // 🔹 Index Finger Transforms
    // ==============================
    public Transform IndexAngle1Center;
    public Transform IndexAngle2Center;
    public Transform IndexAngle3Center;
    public Transform IndexAngle4Center;

    // ==============================
    // 🔹 Middle Finger Transforms
    // ==============================
    public Transform MiddleAngle1Center;
    public Transform MiddleAngle2Center;
    public Transform MiddleAngle3Center;
    public Transform MiddleAngle4Center;

    // ==============================
    // 🔹 Configuration
    // ==============================
    private float rotationSpeed = 8f; // degrees per second
    public bool isMapping = true;
    public float tt = 0f;

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

    // ==============================
    // 🔹 Unity Lifecycle
    // ==============================
    void Start()
    {
        if (jointAngle == null)
        {
            Debug.LogError("JointAngle is not assigned in the inspector for " + gameObject.name);
        }

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

        // --- Thumb ---
        if (ThumbAngle3Center != null)
            ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle0, 0f, 0f);
        if (ThumbAngle4Center != null)
            ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1, 0f, 0f);

        // --- Index ---
        UpdateIndexFingerAbduction(); // ← Uncomment if using Y-axis logic
        // UpdateIndexFingerAbductionByZ();

        if (IndexAngle3Center != null)
            IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);
        if (IndexAngle4Center != null)
            IndexAngle4Center.localRotation = Quaternion.Euler(jointAngle.indexAngle2, 0f, 0f);

        // --- Middle ---
        UpdateMiddleFingerAbduction(); // ← Uncomment if using Y-axis logic
        // UpdateMiddleFingerAbductionByZ();

        if (MiddleAngle3Center != null)
            MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);
        if (MiddleAngle4Center != null)
            MiddleAngle4Center.localRotation = Quaternion.Euler(jointAngle.middleAngle2, 0f, 0f);
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
    private void UpdateIndexFingerAbduction()
    {
        maxIndexYAxisAngle = NormalizeAngle(indexFingerJoint1MaxRotationVector.y);
        Quaternion targetRotation = IndexAngle1CenterInitialRotation;

        if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentIndexRotationY -= rotationSpeed * Time.deltaTime;
            currentIndexRotationY = Mathf.Max(currentIndexRotationY, -60f);

            indexFingerJoint1MaxRotationVector =
                (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationY, 0f)).eulerAngles;
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
            tt = targetY;
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

        if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentMiddleRotationY += rotationSpeed * Time.deltaTime;
            currentMiddleRotationY = Mathf.Min(currentMiddleRotationY, 60f);

            middleFingerJoint1MaxRotationVector =
                (MiddleAngle1CenterInitialRotation * Quaternion.Euler(0f, currentMiddleRotationY, 0f)).eulerAngles;
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
    // 🔹 Index Finger Abduction (Z-axis)
    // ==============================
    private void UpdateIndexFingerAbductionByZ()
    {
        maxIndexZAxisAngle = NormalizeAngle(indexFingerJoint2MaxRotationVector.z);
        Quaternion targetRotation = IndexAngle2CenterInitialRotation;

        if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentIndexRotationZ -= rotationSpeed * Time.deltaTime;
            currentIndexRotationZ = Mathf.Max(currentIndexRotationZ, -60f);

            indexFingerJoint2MaxRotationVector =
                (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZ)).eulerAngles;
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
    // 🔹 Reset Function
    // ==============================
    public void ResetFingerRotations()
    {
        currentIndexRotationY = currentIndexRotationZ = 0f;
        currentMiddleRotationY = currentMiddleRotationZ = 0f;
        isMapping = true;

        indexFingerJoint1MaxRotationVector = IndexAngle1CenterInitialRotation.eulerAngles;
        indexFingerJoint2MaxRotationVector = IndexAngle2CenterInitialRotation.eulerAngles;
        middleFingerJoint1MaxRotationVector = MiddleAngle1CenterInitialRotation.eulerAngles;
        middleFingerJoint2MaxRotationVector = MiddleAngle2CenterInitialRotation.eulerAngles;

        maxIndexYAxisAngle = IndexAngle1CenterInitialRotation.eulerAngles.y;
        maxIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;
        maxMiddleYAxisAngle = MiddleAngle1CenterInitialRotation.eulerAngles.y;
        maxMiddleZAxisAngle = MiddleAngle2CenterInitialRotation.eulerAngles.z;

        ApplyResetRotations();
        tt = 0f;
    }

    private void ApplyResetRotations()
    {
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

    // ==============================
    // 🔹 Utility
    // ==============================
    private float NormalizeAngle(float angle)
    {
        return angle >= 300 ? angle - 360 : angle;
    }
}