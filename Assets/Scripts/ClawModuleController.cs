using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClawModuleController : MonoBehaviour
{
    public JointAngle jointAngle;
    public TriggerRightIndexTip triggerRightIndexTip;

    public Transform ThumbAngle1Center;
    public Transform ThumbAngle2Center;
    public Transform ThumbAngle3Center;
    public Transform ThumbAngle4Center;

    public Transform IndexAngle1Center;
    public Transform IndexAngle2Center;
    public Transform IndexAngle3Center;
    public Transform IndexAngle4Center;

    public Transform MiddleAngle1Center;
    public Transform MiddleAngle2Center;
    public Transform MiddleAngle3Center;
    public Transform MiddleAngle4Center;

    public float maxIndexYAxisAngle;

    private Quaternion IndexAngle1CenterInitialRotation;
    private Quaternion MiddleAngle1CenterInitialRotation;  // Added for middle finger
    public Vector3 indexFingerJoint1MaxRotationVector;
    public Vector3 middleFingerJoint1MaxRotationVector;    // Added for middle finger
    public float currentIndexRotationY = 0f;
    public float currentMiddleRotationY = 0f;              // Added for middle finger
    public float maxMiddleYAxisAngle;                      // Added for middle finger
    private float rotationSpeed = 8f; // degrees per second
    public TriggerRightMiddleTip triggerRightMiddleTip;    // Added reference for middle finger touch detection
    // public Renderer indexJoint1Renderer;
    public bool isMapping = true;
    public float tt = 0f;

    private Quaternion IndexAngle2CenterInitialRotation;
    public Vector3 indexFingerJoint2MaxRotationVector;
    public float currentIndexRotationZ = 0f;
    public float maxIndexZAxisAngle;

    private Quaternion MiddleAngle2CenterInitialRotation;
    public Vector3 middleFingerJoint2MaxRotationVector;
    public float currentMiddleRotationZ = 0f;
    public float maxMiddleZAxisAngle;



    // Start is called before the first frame update
    void Start()
    {
        if (jointAngle == null)
        {
            Debug.LogError("JointAngle is not assigned in the inspector for " + gameObject.name);
        }

        IndexAngle1CenterInitialRotation = IndexAngle1Center.localRotation;
        MiddleAngle1CenterInitialRotation = MiddleAngle1Center.localRotation;  // Initialize middle finger rotation

        maxIndexYAxisAngle = IndexAngle1CenterInitialRotation.eulerAngles.y;
        maxMiddleYAxisAngle = MiddleAngle1CenterInitialRotation.eulerAngles.y; // Initialize middle finger max angle

        indexFingerJoint1MaxRotationVector = IndexAngle1Center.localRotation.eulerAngles;
        middleFingerJoint1MaxRotationVector = MiddleAngle1Center.localRotation.eulerAngles; // Initialize middle finger vector

        IndexAngle2CenterInitialRotation = IndexAngle2Center.localRotation;
        indexFingerJoint2MaxRotationVector = IndexAngle2Center.localRotation.eulerAngles;
        maxIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;

        MiddleAngle2CenterInitialRotation = MiddleAngle2Center.localRotation;
        middleFingerJoint2MaxRotationVector = MiddleAngle2Center.localRotation.eulerAngles;
        maxMiddleZAxisAngle = MiddleAngle2CenterInitialRotation.eulerAngles.z;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            isMapping = !isMapping;
            // Debug.Log("isMapping: " + isMapping);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("ResetFingerRotations called from Update");
            ResetFingerRotations();
        }

        if (ThumbAngle3Center != null)
            ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle0, 0f, 0f); // fix

        if (ThumbAngle4Center != null)
            ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1, 0f, 0f);

        //TODO: index finger ------------------------------------------------------------------------------------------

        // UpdateIndexFingerAbduction();
        UpdateIndexFingerAbductionByZ();

        if (IndexAngle3Center != null)
            IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);

        if (IndexAngle4Center != null)
            IndexAngle4Center.localRotation = Quaternion.Euler(jointAngle.indexAngle2, 0f, 0f);

        //TODO: middle finger ------------------------------------------------------------------------------------------

        // UpdateMiddleFingerAbduction();
        UpdateMiddleFingerAbductionByZ();

        if (MiddleAngle3Center != null)
            MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);

        if (MiddleAngle4Center != null)
            MiddleAngle4Center.localRotation = Quaternion.Euler(jointAngle.middleAngle2, 0f, 0f);
    }

    private void UpdateIndexFingerAbduction()
    {
        // Step 1: normalize angle range
        maxIndexYAxisAngle = indexFingerJoint1MaxRotationVector.y >= 300
            ? indexFingerJoint1MaxRotationVector.y - 360
            : indexFingerJoint1MaxRotationVector.y;

        Quaternion targetIndexJoint1Rotation = IndexAngle1CenterInitialRotation;

        // Step 2: handle manual control when tip touched
        if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentIndexRotationY -= rotationSpeed * Time.deltaTime;
            if (currentIndexRotationY < -60f) currentIndexRotationY = -60f;

            indexFingerJoint1MaxRotationVector =
                (IndexAngle1CenterInitialRotation * Quaternion.Euler(0f, currentIndexRotationY, 0f)).eulerAngles;
        }

        targetIndexJoint1Rotation *= Quaternion.Euler(0f, currentIndexRotationY, 0f);

        // Step 3: mapping mode control
        if (jointAngle.indexMiddleDistance < 3.5f && IndexAngle1Center != null)
        {
            float targetYRotation;
            if (isMapping)
            {
                float deltaAngle = maxIndexYAxisAngle;
                targetYRotation = maxIndexYAxisAngle +
                    (float)((30 - deltaAngle) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f));
                tt = targetYRotation;
            }
            else
            {
                targetYRotation = indexFingerJoint1MaxRotationVector.y +
                    (float)(30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f));
            }

            Vector3 currentEuler = targetIndexJoint1Rotation.eulerAngles;
            targetIndexJoint1Rotation = Quaternion.Euler(currentEuler.x, targetYRotation, currentEuler.z);
        }

        // Step 4: apply rotation
        if (IndexAngle1Center != null)
            IndexAngle1Center.localRotation = targetIndexJoint1Rotation;
    }

    private void UpdateMiddleFingerAbduction()
    {
        // Step 1: normalize angle range
        maxMiddleYAxisAngle = middleFingerJoint1MaxRotationVector.y >= 300
            ? middleFingerJoint1MaxRotationVector.y - 360
            : middleFingerJoint1MaxRotationVector.y;

        Quaternion targetMiddleJoint1Rotation = MiddleAngle1CenterInitialRotation;

        // Step 2: handle manual control when tip touched (方向與食指相反)
        if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentMiddleRotationY += rotationSpeed * Time.deltaTime;
            if (currentMiddleRotationY > 60f) currentMiddleRotationY = 60f;

            middleFingerJoint1MaxRotationVector =
                (MiddleAngle1CenterInitialRotation * Quaternion.Euler(0f, currentMiddleRotationY, 0f)).eulerAngles;
        }

        targetMiddleJoint1Rotation *= Quaternion.Euler(0f, currentMiddleRotationY, 0f);

        // Step 3: mapping mode control
        if (jointAngle.indexMiddleDistance < 3.5f && MiddleAngle1Center != null)
        {
            float targetYRotation;
            if (isMapping)
            {
                float deltaAngle = maxMiddleYAxisAngle;
                targetYRotation = maxMiddleYAxisAngle -
                    (float)((30 + deltaAngle) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)); // 方向相反所以用減號
            }
            else
            {
                targetYRotation = middleFingerJoint1MaxRotationVector.y -
                    (float)(30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)); // 同樣相反方向
            }

            Vector3 currentEuler = targetMiddleJoint1Rotation.eulerAngles;
            targetMiddleJoint1Rotation = Quaternion.Euler(currentEuler.x, targetYRotation, currentEuler.z);
        }

        // Step 4: apply rotation
        if (MiddleAngle1Center != null)
            MiddleAngle1Center.localRotation = targetMiddleJoint1Rotation;
    }

    private void UpdateIndexFingerAbductionByZ()
    {
        // Step 1: normalize angle range
        maxIndexZAxisAngle = indexFingerJoint2MaxRotationVector.z >= 300
            ? indexFingerJoint2MaxRotationVector.z - 360
            : indexFingerJoint2MaxRotationVector.z;

        Quaternion targetIndexJoint2Rotation = IndexAngle2CenterInitialRotation;

        // Step 2: handle manual control when tip touched (繞Z軸)
        if (triggerRightIndexTip.isRightIndexTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentIndexRotationZ -= rotationSpeed * Time.deltaTime;
            if (currentIndexRotationZ < -60f) currentIndexRotationZ = -60f;

            indexFingerJoint2MaxRotationVector =
                (IndexAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentIndexRotationZ)).eulerAngles;
        }

        targetIndexJoint2Rotation *= Quaternion.Euler(0f, 0f, currentIndexRotationZ);

        // Step 3: mapping mode control
        if (jointAngle.indexMiddleDistance < 3.5f && IndexAngle2Center != null)
        {
            float targetZRotation;
            if (isMapping)
            {
                float deltaAngle = maxIndexZAxisAngle;
                targetZRotation = maxIndexZAxisAngle +
                    (float)((30 - deltaAngle) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f));
            }
            else
            {
                targetZRotation = indexFingerJoint2MaxRotationVector.z +
                    (float)(30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f));
            }

            Vector3 currentEuler = targetIndexJoint2Rotation.eulerAngles;
            targetIndexJoint2Rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, targetZRotation);
        }

        // Step 4: apply rotation
        if (IndexAngle2Center != null)
            IndexAngle2Center.localRotation = targetIndexJoint2Rotation;
    }

    private void UpdateMiddleFingerAbductionByZ()
    {
        // Step 1: normalize angle range
        maxMiddleZAxisAngle = middleFingerJoint2MaxRotationVector.z >= 300
            ? middleFingerJoint2MaxRotationVector.z - 360
            : middleFingerJoint2MaxRotationVector.z;

        Quaternion targetMiddleJoint2Rotation = MiddleAngle2CenterInitialRotation;

        // Step 2: handle manual control when tip touched (方向與食指相反)
        if (triggerRightMiddleTip.isRightMiddleTipTouched && jointAngle.indexMiddleDistance > 3.5f)
        {
            currentMiddleRotationZ += rotationSpeed * Time.deltaTime;
            if (currentMiddleRotationZ > 60f) currentMiddleRotationZ = 60f;

            middleFingerJoint2MaxRotationVector =
                (MiddleAngle2CenterInitialRotation * Quaternion.Euler(0f, 0f, currentMiddleRotationZ)).eulerAngles;
        }

        targetMiddleJoint2Rotation *= Quaternion.Euler(0f, 0f, currentMiddleRotationZ);

        // Step 3: mapping mode control
        if (jointAngle.indexMiddleDistance < 3.5f && MiddleAngle2Center != null)
        {
            float targetZRotation;
            if (isMapping)
            {
                float deltaAngle = maxMiddleZAxisAngle;
                targetZRotation = maxMiddleZAxisAngle -
                    (float)((30 + deltaAngle) * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)); // 相反方向 => 減號
            }
            else
            {
                targetZRotation = middleFingerJoint2MaxRotationVector.z -
                    (float)(30 * ((3.5f - jointAngle.indexMiddleDistance) / 1.6f)); // 相反方向 => 減號
            }

            Vector3 currentEuler = targetMiddleJoint2Rotation.eulerAngles;
            targetMiddleJoint2Rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, targetZRotation);
        }

        // Step 4: apply rotation
        if (MiddleAngle2Center != null)
            MiddleAngle2Center.localRotation = targetMiddleJoint2Rotation;
    }



    public void ResetFingerRotations()
    {
        // currentThumbRotationY = 0f;
        // currentThumbRotationZ = 0f;
        // currentThumbTipRotationX = 0f;

        // currentIndexRotationZ = 0f;
        currentIndexRotationY = 0f;
        currentMiddleRotationY = 0f;  // Reset middle finger rotation Y
        currentIndexRotationZ = 0f;   // Reset index finger Z rotation
        currentMiddleRotationZ = 0f;  // Reset middle finger Z rotation

        isMapping = true;

        // index & middle mapping things
        indexFingerJoint1MaxRotationVector = IndexAngle1CenterInitialRotation.eulerAngles;
        maxIndexYAxisAngle = IndexAngle1CenterInitialRotation.eulerAngles.y;
        
        // Index Z-axis reset
        indexFingerJoint2MaxRotationVector = IndexAngle2CenterInitialRotation.eulerAngles;
        maxIndexZAxisAngle = IndexAngle2CenterInitialRotation.eulerAngles.z;

        // Add middle finger reset values
        middleFingerJoint1MaxRotationVector = MiddleAngle1CenterInitialRotation.eulerAngles;
        maxMiddleYAxisAngle = MiddleAngle1CenterInitialRotation.eulerAngles.y;
        
        // Middle Z-axis reset
        middleFingerJoint2MaxRotationVector = MiddleAngle2CenterInitialRotation.eulerAngles;
        maxMiddleZAxisAngle = MiddleAngle2CenterInitialRotation.eulerAngles.z;

        // Reset transforms
        if (ThumbAngle1Center != null)
        {
            // ThumbAngle1Center.localRotation = thumbAngle1CenterInitialRotation;
        }
        if (ThumbAngle2Center != null)
        {
            // ThumbAngle2Center.localRotation = thumbAngle2CenterInitialRotation;
        }
        if (ThumbAngle3Center != null)
        {
            ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle0, 0f, 0f);
        }
        if (ThumbAngle4Center != null)
        {
            ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1, 0f, 0f);
        }

        if (IndexAngle1Center != null)
        {
            IndexAngle1Center.localRotation = IndexAngle1CenterInitialRotation;
        }
        if (IndexAngle2Center != null)
        {
            IndexAngle2Center.localRotation = IndexAngle2CenterInitialRotation;
        }
        if (IndexAngle3Center != null)
        {
            IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);
        }
        if (IndexAngle4Center != null)
        {
            IndexAngle4Center.localRotation = Quaternion.Euler(jointAngle.indexAngle2, 0f, 0f);
        }

        if (MiddleAngle1Center != null)
        {
            MiddleAngle1Center.localRotation = MiddleAngle1CenterInitialRotation;  // Reset middle finger rotation
        }
        if (MiddleAngle2Center != null)
        {
            MiddleAngle2Center.localRotation = MiddleAngle2CenterInitialRotation;
        }
        if (MiddleAngle3Center != null)
        {
            MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);
        }
        if (MiddleAngle4Center != null)
        {
            MiddleAngle4Center.localRotation = Quaternion.Euler(jointAngle.middleAngle2, 0f, 0f);
        }

        // Reset the displayed value
        tt = 0f;
    }
}
