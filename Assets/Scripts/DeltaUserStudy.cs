using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeltaUserStudy : MonoBehaviour
{
    public JointAngle jointAngle;

    [Header("=== Collider/Mode References (disable in keyboard-only mode) ===")]
    public ModeSwitching modeSwitching;
    public ClawModuleController clawModuleController;
    public ArmUIPlaneController armUIPlaneController;
    public ArmUIPlaneCollider armUIPlaneCollider;
    [Tooltip("If true, block Arm UI plane input while DeltaUserStudy is active.")]
    public bool disableArmUIPlaneDuringDelta = true;
    [Tooltip("If true, disable ClawModuleController while DeltaUserStudy is active so hand-joint updates cannot drive motor angles.")]
    public bool disableClawModuleControllerDuringDelta = true;
    public DeltaUserStudyPlaneButtonInteraction planeButtonInteraction;
    public TriggerRightIndexTip triggerRightIndexTip;
    public TriggerRightMiddleTip triggerRightMiddleTip;
    public TriggerRightThumbTip triggerRightThumbTip;
    public TriggerRightThumbAbduction triggerRightThumbAbduction;

    [Header("Optional Direct Arrow References (when not using ClawModuleController)")]
    public GameObject motor3UpArrow;
    public GameObject motor3DownArrow;
    public GameObject motor4UpArrow;
    public GameObject motor4DownArrow;
    public GameObject motor7UpArrow;
    public GameObject motor7DownArrow;
    public GameObject motor8UpArrow;
    public GameObject motor8DownArrow;
    public GameObject motor11UpArrow;
    public GameObject motor11DownArrow;
    public GameObject motor12UpArrow;
    public GameObject motor12DownArrow;

    public GameObject thumb3LeftLeftArrow;
    public GameObject thumb3LeftRightArrow;
    public GameObject thumb3RightLeftArrow;
    public GameObject thumb3RightRightArrow;
    public GameObject thumb4LeftLeftArrow;
    public GameObject thumb4LeftRightArrow;
    public GameObject thumb4RightLeftArrow;
    public GameObject thumb4RightRightArrow;
    public GameObject index3LeftLeftArrow;
    public GameObject index3LeftRightArrow;
    public GameObject index3RightLeftArrow;
    public GameObject index3RightRightArrow;
    public GameObject index4LeftLeftArrow;
    public GameObject index4LeftRightArrow;
    public GameObject index4RightLeftArrow;
    public GameObject index4RightRightArrow;
    public GameObject middle3LeftLeftArrow;
    public GameObject middle3LeftRightArrow;
    public GameObject middle3RightLeftArrow;
    public GameObject middle3RightRightArrow;
    public GameObject middle4LeftLeftArrow;
    public GameObject middle4LeftRightArrow;
    public GameObject middle4RightLeftArrow;
    public GameObject middle4RightRightArrow;

    public Transform ThumbAngle1Center;
    public Transform ThumbAngle2Center;
    public Transform ThumbAngle3Center;
    public Transform ThumbAngle4Center;
    public Renderer thumbJoint1Renderer;
    public Renderer thumbJoint2Renderer;
    public Renderer thumbJoint3Renderer;
    public Renderer thumbJoint4Renderer;

    [Header("Thumb Cube Renderers (Color Sync)")]
    public Renderer thumbCube1Renderer;
    public Renderer thumbCube2Renderer;
    public Renderer thumbCube3Renderer;
    public Renderer thumbCube4Renderer;

    public Transform IndexAngle1Center;
    public Transform IndexAngle2Center;
    public Transform IndexAngle3Center;
    public Transform IndexAngle4Center;
    public Renderer indexJoint1Renderer;
    public Renderer indexJoint2Renderer;
    public Renderer indexJoint3Renderer;
    public Renderer indexJoint4Renderer;

    [Header("Index Cube Renderers (Color Sync)")]
    public Renderer indexCube1Renderer;
    public Renderer indexCube2Renderer;
    public Renderer indexCube3Renderer;
    public Renderer indexCube4Renderer;

    public Transform MiddleAngle1Center;
    public Transform MiddleAngle2Center;
    public Transform MiddleAngle3Center;
    public Transform MiddleAngle4Center;
    public Renderer middleJoint1Renderer;
    public Renderer middleJoint2Renderer;
    public Renderer middleJoint3Renderer;
    public Renderer middleJoint4Renderer;

    [Header("Middle Cube Renderers (Color Sync)")]
    public Renderer middleCube1Renderer;
    public Renderer middleCube2Renderer;
    public Renderer middleCube3Renderer;
    public Renderer middleCube4Renderer;

    Transform[,] motorArray;
    Renderer[,] rendererArray;
    
    // Currently selected index (initial: IndexAngle4Center: row 3, col 1)
    private int currentRow = 3;
    private int currentCol = 1;
    
    // Array size
    private const int ROWS = 4;
    private const int COLS = 3;
    
    // Save original colors and currently selected object
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private Renderer currentSelectedRenderer;

    public Color lightRedColor = new Color(1f, 0.5f, 0.5f, 1f);
    private float rotationSpeed = 18f;
    
    // Store initial rotation of each joint
    private Quaternion[,] initialRotations = new Quaternion[4, 3];
    
    // Store current angle of each joint
    private float[,] currentRotations = new float[4, 3];
    private bool arrowDirtyThisFrame;
    private int arrowMotorIDThisFrame;
    private float arrowDeltaSignThisFrame;
    private bool groupArrowDirtyThisFrame;
    private float groupArrowDeltaSignThisFrame;
    private int groupArrowMotorCountThisFrame;
    private readonly int[] groupArrowMotorIDsThisFrame = new int[6];
    private readonly Dictionary<int, GameObject> localMotorUpArrows = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, GameObject> localMotorDownArrows = new Dictionary<int, GameObject>();
    private bool wasPlaneWPressed;
    private bool wasPlaneAPressed;
    private bool wasPlaneSPressed;
    private bool wasPlaneDPressed;
    private bool wasPlaneResetPressed;
    private bool restoreArmUIPlaneColliderEnabled;
    private bool hadArmUIPlaneColliderAtEnable;
    private bool restoreArmUIPlaneVisualRootActive;
    private bool hadArmUIPlaneVisualRootAtEnable;
        
    // Start is called before the first frame update
    /// <summary>
    /// Awake runs before ALL Start() calls.
    /// Disable ModeSwitching here so its Start() never sets joints to gray.
    /// </summary>
    void Awake()
    {
        if (!this.enabled) return;

        if (planeButtonInteraction == null)
        {
            planeButtonInteraction = FindObjectOfType<DeltaUserStudyPlaneButtonInteraction>();
        }

        if (armUIPlaneController == null)
        {
            armUIPlaneController = FindObjectOfType<ArmUIPlaneController>();
        }

        if (armUIPlaneCollider == null)
        {
            armUIPlaneCollider = FindObjectOfType<ArmUIPlaneCollider>();
        }

        if (disableClawModuleControllerDuringDelta && clawModuleController != null)
        {
            clawModuleController.enabled = false;
        }

        if (modeSwitching != null)
        {
            modeSwitching.enabled = false;
        }
    }

    void Start()
    {
        // Initialize array
        motorArray = new Transform[ROWS, COLS] {
            {ThumbAngle1Center, IndexAngle1Center, MiddleAngle1Center},
            {ThumbAngle2Center, IndexAngle2Center, MiddleAngle2Center},
            {ThumbAngle3Center, IndexAngle3Center, MiddleAngle3Center},
            {ThumbAngle4Center, IndexAngle4Center, MiddleAngle4Center}
        };
        
        // Initialize Renderer array
        rendererArray = new Renderer[ROWS, COLS] {
            {thumbJoint1Renderer, indexJoint1Renderer, middleJoint1Renderer},
            {thumbJoint2Renderer, indexJoint2Renderer, middleJoint2Renderer},
            {thumbJoint3Renderer, indexJoint3Renderer, middleJoint3Renderer},
            {thumbJoint4Renderer, indexJoint4Renderer, middleJoint4Renderer}
        };

        InitializeLocalArrowMappings();
        
        // Disable ModeSwitching and all colliders FIRST, before saving colors
        // (prevents ModeSwitching.Start from setting joints to gray)
        DisableCollidersAndModes();

        // Save original colors (should all be originalColor now, not gray)
        SaveOriginalColors();
        
        // Initialize initial rotations
        InitializeRotations();

        // Ensure all joints are originalColor
        SetAllOriginalColors();

        // Avoid stale arrows when entering DeltaUserStudy mode.
        ClearDirectAngleArrows();
        
        // Set initial position to IndexAngle4Center
        UpdateSelection();
    }

    void OnDisable()
    {
        RestoreArmUIPlaneState();
        ClearDirectAngleArrows();
    }
    
    void InitializeRotations()
    {
        // Save initial rotation of each joint
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                Transform t = motorArray[row, col];
                if (t != null)
                {
                    initialRotations[row, col] = t.localRotation;
                    currentRotations[row, col] = 0f;
                }
            }
        }
    }
    
    void SaveOriginalColors()
    {
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                Renderer renderer = rendererArray[row, col];
                if (renderer != null && renderer.material != null)
                {
                    originalColors[renderer] = renderer.material.color;
                }
            }
        }
    }
    
    void UpdateSelection()
    {
        // Restore color of previously selected object
        if (currentSelectedRenderer != null && originalColors.ContainsKey(currentSelectedRenderer))
        {
            currentSelectedRenderer.material.color = originalColors[currentSelectedRenderer];
        }
        
        // Set new selected object
        currentSelectedRenderer = rendererArray[currentRow, currentCol];
        if (currentSelectedRenderer != null)
        {
            currentSelectedRenderer.material.color = lightRedColor;
            // Debug.Log($"Selected: Row {currentRow}, Col {currentCol} - {selectedTransform.name}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Reset desired arrow state; applied at end of frame in LateUpdate.
        arrowDirtyThisFrame = false;
        arrowMotorIDThisFrame = 0;
        arrowDeltaSignThisFrame = 0f;
        groupArrowDirtyThisFrame = false;
        groupArrowDeltaSignThisFrame = 0f;
        groupArrowMotorCountThisFrame = 0;

        // WASD navigation control
        HandleNavigation();

        // QE rotation control
        HandleRotation();

        // RF keys - Control all Row 3 motors simultaneously
        HandleRow3Rotation();

        // TG keys - Control all Row 2 motors simultaneously
        HandleRow2Rotation();

        // R key - Set all Angle3 joints (row 2) to 89 degrees
        if (Input.GetKeyDown(KeyCode.J))
        {
            // SetRow2To89Degrees();
        }

        bool resetPressed = Input.GetKeyDown(KeyCode.Space) || ConsumePlaneButtonPress(KeyCode.Space, ref wasPlaneResetPressed);
        if (resetPressed)
        {
            ResetAll();
        }
        
        // if (ThumbAngle3Center != null)
        //     ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f);

        // if (IndexAngle3Center != null)
        //     IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);

        // if (IndexAngle4Center != null)
        //     IndexAngle4Center.localRotation = Quaternion.Euler(jointAngle.indexAngle2, 0f, 0f);

        // if (MiddleAngle3Center != null)
        //     MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);

        // if (MiddleAngle4Center != null)
        //     MiddleAngle4Center.localRotation = Quaternion.Euler(jointAngle.middleAngle2, 0f, 0f);
    }

    void LateUpdate()
    {
        ApplyArrowStateForFrame();
        SyncCubeRendererColors();
    }
    
    void HandleNavigation()
    {
        bool moved = false;
        bool moveUpPressed = Input.GetKeyDown(KeyCode.W) || ConsumePlaneButtonPress(KeyCode.W, ref wasPlaneWPressed);
        bool moveDownPressed = Input.GetKeyDown(KeyCode.S) || ConsumePlaneButtonPress(KeyCode.S, ref wasPlaneSPressed);
        bool moveLeftPressed = Input.GetKeyDown(KeyCode.A) || ConsumePlaneButtonPress(KeyCode.A, ref wasPlaneAPressed);
        bool moveRightPressed = Input.GetKeyDown(KeyCode.D) || ConsumePlaneButtonPress(KeyCode.D, ref wasPlaneDPressed);
        
        // W - Up (go down one row, wrapping)
        if (moveUpPressed)
        {
            currentRow = (currentRow + 1) % ROWS;
            moved = true;
        }
        // S - Down (go up one row, wrapping)
        else if (moveDownPressed)
        {
            currentRow = (currentRow - 1 + ROWS) % ROWS;
            moved = true;
        }
        // A - Cycle: Index -> Thumb -> Middle -> Index
        else if (moveLeftPressed)
        {
            currentCol = (currentCol - 1 + COLS) % COLS;
            moved = true;
        }
        // D - Cycle: Index -> Middle -> Thumb -> Index
        else if (moveRightPressed)
        {
            currentCol = (currentCol + 1) % COLS;
            moved = true;
        }
        
        if (moved)
        {
            // Changing selection should not keep previous motor arrow highlighted.
            ClearDirectAngleArrows();
            UpdateSelection();
        }
    }

    private bool ConsumePlaneButtonPress(KeyCode keyCode, ref bool wasPressedLastFrame)
    {
        bool isPressedThisFrame = IsPlaneButtonTouched(keyCode);
        bool pressedThisFrame = isPressedThisFrame && !wasPressedLastFrame;
        wasPressedLastFrame = isPressedThisFrame;
        return pressedThisFrame;
    }

    private bool IsPlaneButtonTouched(KeyCode keyCode)
    {
        if (planeButtonInteraction == null)
        {
            return false;
        }

        DeltaUserStudyPlaneButtonInteraction.ButtonBinding button = planeButtonInteraction.GetButtonBinding(keyCode);
        return button != null && button.isTouched;
    }
    
    void HandleRotation()
    {
        Transform currentTransform = motorArray[currentRow, currentCol];
        if (currentTransform == null)
        {
            ClearDirectAngleArrows();
            return;
        }
        
        float rotationDelta = rotationSpeed * Time.deltaTime;
        float signedInputDelta = 0f;
        bool rotateNegativePressed = Input.GetKey(KeyCode.Q) || IsPlaneButtonTouched(KeyCode.Q);
        bool rotatePositivePressed = Input.GetKey(KeyCode.E) || IsPlaneButtonTouched(KeyCode.E);
        
        bool reverseDirection = currentRow >= 2; // Only Angle3/Angle4 motors: 3,4,7,8,11,12

        // Q key - decrease angle (rows 0/1), increase angle (rows 2/3)
        if (rotateNegativePressed)
        {
            signedInputDelta += reverseDirection ? rotationDelta : -rotationDelta;
        }
        
        // E key - increase angle (rows 0/1), decrease angle (rows 2/3)
        if (rotatePositivePressed)
        {
            signedInputDelta += reverseDirection ? -rotationDelta : rotationDelta;
        }

        if (Mathf.Abs(signedInputDelta) <= 0.0001f)
        {
            ClearDirectAngleArrows();
            return;
        }

        float previousAngle = currentRotations[currentRow, currentCol];
        float nextAngle = Mathf.Clamp(previousAngle + signedInputDelta, -89f, 89f);
        if (Mathf.Approximately(previousAngle, nextAngle))
        {
            // Input exists but angle is clamped, so hide arrows.
            ClearDirectAngleArrows();
            return;
        }

        currentRotations[currentRow, currentCol] = nextAngle;
        float appliedDelta = nextAngle - previousAngle;
        
        // Apply rotation: initial rotation * current angle change
        Quaternion initialRotation = initialRotations[currentRow, currentCol];
        float currentAngle = currentRotations[currentRow, currentCol];
        Quaternion deltaRotation;

        // Determine rotation axis based on row
        if (currentRow == 0) // Row 0: ThumbAngle1, IndexAngle1, MiddleAngle1 - Y axis
        {
            deltaRotation = Quaternion.Euler(0f, currentAngle, 0f);
        }
        else if (currentRow == 1) // Row 1: ThumbAngle2, IndexAngle2, MiddleAngle2 - Z axis
        {
            deltaRotation = Quaternion.Euler(0f, 0f, currentAngle);
        }
        else // Row 2 & 3: Angle3, Angle4 - X axis
        {
            deltaRotation = Quaternion.Euler(currentAngle, 0f, 0f);
        }

        currentTransform.localRotation = initialRotation * deltaRotation;
        MarkArrowForCurrentSelection(appliedDelta);
    }
    
    void HandleRow3Rotation()
    {
        // R key - decrease angle for all Row 3 motors
        // F key - increase angle for all Row 3 motors
        
        int targetRow = 3; // Row 3: ThumbAngle4Center, IndexAngle4Center, MiddleAngle4Center
        float rotationDelta = rotationSpeed * Time.deltaTime;
        bool row3DecreasePressed = Input.GetKey(KeyCode.R) || IsPlaneButtonTouched(KeyCode.R);
        bool row3IncreasePressed = Input.GetKey(KeyCode.F) || IsPlaneButtonTouched(KeyCode.F);
        float signedInputDelta = 0f;

        if (row3DecreasePressed)
        {
            signedInputDelta -= rotationDelta;
        }

        if (row3IncreasePressed)
        {
            signedInputDelta += rotationDelta;
        }

        if (Mathf.Abs(signedInputDelta) <= 0.0001f)
        {
            return;
        }
        
        // R key - decrease angle
        // F key - increase angle
        bool anyMotorChanged = false;

        // Apply rotation to all motors in Row 3
        for (int col = 0; col < COLS; col++)
        {
            Transform t = motorArray[targetRow, col];
            if (t != null)
            {
                float previousAngle = currentRotations[targetRow, col];
                float nextAngle = Mathf.Clamp(previousAngle + signedInputDelta, -60f, 60f);
                if (Mathf.Approximately(previousAngle, nextAngle))
                {
                    continue;
                }

                anyMotorChanged = true;
                currentRotations[targetRow, col] = nextAngle;

                // Apply rotation: initial rotation * current angle change
                Quaternion initialRotation = initialRotations[targetRow, col];
                float currentAngle = currentRotations[targetRow, col];

                // Row 3 uses X axis (same as Row 2)
                Quaternion deltaRotation = Quaternion.Euler(currentAngle, 0f, 0f);
                t.localRotation = initialRotation * deltaRotation;

                MarkGroupArrowForMotor(GetMotorIDForSelection(targetRow, col), signedInputDelta);
            }
        }

        if (!anyMotorChanged)
        {
            ClearDirectAngleArrows();
        }
    }
    
    void HandleRow2Rotation()
    {
        // T key - decrease angle for all Row 2 motors
        // G key - increase angle for all Row 2 motors
        
        int targetRow = 2; // Row 2: ThumbAngle3Center, IndexAngle3Center, MiddleAngle3Center
        float rotationDelta = rotationSpeed * Time.deltaTime;
        bool row2DecreasePressed = Input.GetKey(KeyCode.T) || IsPlaneButtonTouched(KeyCode.T);
        bool row2IncreasePressed = Input.GetKey(KeyCode.G) || IsPlaneButtonTouched(KeyCode.G);
        float signedInputDelta = 0f;

        if (row2DecreasePressed)
        {
            signedInputDelta -= rotationDelta;
        }

        if (row2IncreasePressed)
        {
            signedInputDelta += rotationDelta;
        }

        if (Mathf.Abs(signedInputDelta) <= 0.0001f)
        {
            return;
        }
        
        // T key - decrease angle
        // G key - increase angle
        bool anyMotorChanged = false;

        // Apply rotation to all motors in Row 2
        for (int col = 0; col < COLS; col++)
        {
            Transform t = motorArray[targetRow, col];
            if (t != null)
            {
                float previousAngle = currentRotations[targetRow, col];
                float nextAngle = Mathf.Clamp(previousAngle + signedInputDelta, -60f, 60f);
                if (Mathf.Approximately(previousAngle, nextAngle))
                {
                    continue;
                }

                anyMotorChanged = true;
                currentRotations[targetRow, col] = nextAngle;

                // Apply rotation: initial rotation * current angle change
                Quaternion initialRotation = initialRotations[targetRow, col];
                float currentAngle = currentRotations[targetRow, col];

                // Row 2 uses X axis
                Quaternion deltaRotation = Quaternion.Euler(currentAngle, 0f, 0f);
                t.localRotation = initialRotation * deltaRotation;

                MarkGroupArrowForMotor(GetMotorIDForSelection(targetRow, col), signedInputDelta);
            }
        }

        if (!anyMotorChanged)
        {
            ClearDirectAngleArrows();
        }
    }
    
    void SetRow2To89Degrees()
    {
        // Set all joints in row 2 (ThumbAngle3Center, IndexAngle3Center, MiddleAngle3Center) to 89 degrees
        int targetRow = 2;
        float targetAngle = 75f;
        
        for (int col = 0; col < COLS; col++)
        {
            Transform t = motorArray[targetRow, col];
            if (t != null)
            {
                currentRotations[targetRow, col] = targetAngle;
                Quaternion initialRotation = initialRotations[targetRow, col];
                Quaternion deltaRotation = Quaternion.Euler(targetAngle, 0f, 0f); // Row 2 uses X axis
                t.localRotation = initialRotation * deltaRotation;
            }
        }
    }

    void ResetAll()
    {
        ClearDirectAngleArrows();

        // Reset all joints to initial rotations
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                Transform t = motorArray[row, col];
                if (t != null)
                {
                    currentRotations[row, col] = 0f;
                    t.localRotation = initialRotations[row, col];
                }
            }
        }
    }

    /// <summary>
    /// Disable ModeSwitching script and all trigger colliders.
    /// This ensures keyboard-only mode has no mode transitions and no accidental collider triggers.
    /// </summary>
    void DisableCollidersAndModes()
    {
        if (modeSwitching != null)
        {
            modeSwitching.modeSelect = false;
            modeSwitching.modeManipulate = false;
            modeSwitching.motorSelected = false;
            modeSwitching.confirmedMotorID = 0;
            modeSwitching.enabled = false;

            if (modeSwitching.SelectMotorCollider != null)
                modeSwitching.SelectMotorCollider.enabled = false;

            modeSwitching.ClearArmUIInput();
        }

        DisableArmUIPlane();

        DisableTriggerCollider(triggerRightIndexTip);
        DisableTriggerCollider(triggerRightMiddleTip);
        DisableTriggerCollider(triggerRightThumbTip);
        DisableTriggerCollider(triggerRightThumbAbduction);
    }

    void DisableTriggerCollider(MonoBehaviour trigger)
    {
        if (trigger == null) return;
        Collider col = trigger.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void DisableArmUIPlane()
    {
        if (!disableArmUIPlaneDuringDelta)
        {
            return;
        }

        if (armUIPlaneController == null)
        {
            armUIPlaneController = FindObjectOfType<ArmUIPlaneController>();
        }

        if (armUIPlaneCollider == null)
        {
            armUIPlaneCollider = FindObjectOfType<ArmUIPlaneCollider>();
        }

        if (armUIPlaneController != null)
        {
            armUIPlaneController.useArmUIPlane = false;

            if (armUIPlaneController.armUIPlaneVisualRoot != null)
            {
                hadArmUIPlaneVisualRootAtEnable = true;
                restoreArmUIPlaneVisualRootActive = armUIPlaneController.armUIPlaneVisualRoot.activeSelf;
                armUIPlaneController.armUIPlaneVisualRoot.SetActive(false);
            }
        }

        if (armUIPlaneCollider != null)
        {
            hadArmUIPlaneColliderAtEnable = true;
            restoreArmUIPlaneColliderEnabled = armUIPlaneCollider.enabled;
            armUIPlaneCollider.enabled = false;
        }
    }

    private void RestoreArmUIPlaneState()
    {
        if (!disableArmUIPlaneDuringDelta)
        {
            return;
        }

        if (armUIPlaneController != null)
        {
            armUIPlaneController.useArmUIPlane = false;

            if (hadArmUIPlaneVisualRootAtEnable && armUIPlaneController.armUIPlaneVisualRoot != null)
            {
                armUIPlaneController.armUIPlaneVisualRoot.SetActive(restoreArmUIPlaneVisualRootActive);
            }
        }

        if (hadArmUIPlaneColliderAtEnable && armUIPlaneCollider != null)
        {
            armUIPlaneCollider.enabled = restoreArmUIPlaneColliderEnabled;
        }

        hadArmUIPlaneColliderAtEnable = false;
        hadArmUIPlaneVisualRootAtEnable = false;
    }

    /// <summary>
    /// Sets all 12 motor renderers to originalColor.
    /// </summary>
    void SetAllOriginalColors()
    {
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                Renderer renderer = rendererArray[row, col];
                if (renderer != null && originalColors.ContainsKey(renderer))
                {
                    renderer.material.color = originalColors[renderer];
                }
            }
        }
    }

    private void SyncCubeRendererColors()
    {
        SyncRendererColor(thumbJoint1Renderer, thumbCube1Renderer);
        SyncRendererColor(thumbJoint2Renderer, thumbCube2Renderer);
        SyncRendererColor(thumbJoint3Renderer, thumbCube3Renderer);
        SyncRendererColor(thumbJoint4Renderer, thumbCube4Renderer);

        SyncRendererColor(indexJoint1Renderer, indexCube1Renderer);
        SyncRendererColor(indexJoint2Renderer, indexCube2Renderer);
        SyncRendererColor(indexJoint3Renderer, indexCube3Renderer);
        SyncRendererColor(indexJoint4Renderer, indexCube4Renderer);

        SyncRendererColor(middleJoint1Renderer, middleCube1Renderer);
        SyncRendererColor(middleJoint2Renderer, middleCube2Renderer);
        SyncRendererColor(middleJoint3Renderer, middleCube3Renderer);
        SyncRendererColor(middleJoint4Renderer, middleCube4Renderer);
    }

    private static void SyncRendererColor(Renderer sourceRenderer, Renderer targetRenderer)
    {
        if (sourceRenderer == null || targetRenderer == null)
        {
            return;
        }

        Material sourceMaterial = sourceRenderer.material;
        Material targetMaterial = targetRenderer.material;
        if (sourceMaterial == null || targetMaterial == null)
        {
            return;
        }

        targetMaterial.color = sourceMaterial.color;
    }

    private void MarkArrowForCurrentSelection(float rawDeltaSign)
    {
        if (Mathf.Abs(rawDeltaSign) <= 0.0001f)
        {
            return;
        }

        int motorID = GetMotorIDForSelection(currentRow, currentCol);
        if (motorID <= 0)
        {
            return;
        }

        arrowDirtyThisFrame = true;
        arrowMotorIDThisFrame = motorID;
        arrowDeltaSignThisFrame = rawDeltaSign;
    }

    private void InitializeLocalArrowMappings()
    {
        localMotorUpArrows.Clear();
        localMotorDownArrows.Clear();

        localMotorUpArrows[3] = motor3UpArrow;
        localMotorDownArrows[3] = motor3DownArrow;
        localMotorUpArrows[4] = motor4UpArrow;
        localMotorDownArrows[4] = motor4DownArrow;
        localMotorUpArrows[7] = motor7UpArrow;
        localMotorDownArrows[7] = motor7DownArrow;
        localMotorUpArrows[8] = motor8UpArrow;
        localMotorDownArrows[8] = motor8DownArrow;
        localMotorUpArrows[11] = motor11UpArrow;
        localMotorDownArrows[11] = motor11DownArrow;
        localMotorUpArrows[12] = motor12UpArrow;
        localMotorDownArrows[12] = motor12DownArrow;
    }

    private static void SetArrowActive(GameObject arrow, bool active)
    {
        if (arrow != null)
        {
            arrow.SetActive(active);
        }
    }

    private void SetLocalVerticalArrowState(int motorID, bool upActive, bool downActive)
    {
        GameObject upArrow;
        if (localMotorUpArrows.TryGetValue(motorID, out upArrow))
        {
            SetArrowActive(upArrow, upActive);
        }

        GameObject downArrow;
        if (localMotorDownArrows.TryGetValue(motorID, out downArrow))
        {
            SetArrowActive(downArrow, downActive);
        }
    }

    private void SetLocalFixedHorizontalArrow(GameObject leftLeftArrow, GameObject rightRightArrow, float rawDeltaSign)
    {
        SetArrowActive(leftLeftArrow, rawDeltaSign < 0f);
        SetArrowActive(rightRightArrow, rawDeltaSign > 0f);
    }

    private void ClearLocalDirectAngleArrows()
    {
        SetLocalVerticalArrowState(3, false, false);
        SetLocalVerticalArrowState(4, false, false);
        SetLocalVerticalArrowState(7, false, false);
        SetLocalVerticalArrowState(8, false, false);
        SetLocalVerticalArrowState(11, false, false);
        SetLocalVerticalArrowState(12, false, false);

        SetArrowActive(thumb3LeftLeftArrow, false);
        SetArrowActive(thumb3LeftRightArrow, false);
        SetArrowActive(thumb3RightLeftArrow, false);
        SetArrowActive(thumb3RightRightArrow, false);
        SetArrowActive(thumb4LeftLeftArrow, false);
        SetArrowActive(thumb4LeftRightArrow, false);
        SetArrowActive(thumb4RightLeftArrow, false);
        SetArrowActive(thumb4RightRightArrow, false);
        SetArrowActive(index3LeftLeftArrow, false);
        SetArrowActive(index3LeftRightArrow, false);
        SetArrowActive(index3RightLeftArrow, false);
        SetArrowActive(index3RightRightArrow, false);
        SetArrowActive(index4LeftLeftArrow, false);
        SetArrowActive(index4LeftRightArrow, false);
        SetArrowActive(index4RightLeftArrow, false);
        SetArrowActive(index4RightRightArrow, false);
        SetArrowActive(middle3LeftLeftArrow, false);
        SetArrowActive(middle3LeftRightArrow, false);
        SetArrowActive(middle3RightLeftArrow, false);
        SetArrowActive(middle3RightRightArrow, false);
        SetArrowActive(middle4LeftLeftArrow, false);
        SetArrowActive(middle4LeftRightArrow, false);
        SetArrowActive(middle4RightLeftArrow, false);
        SetArrowActive(middle4RightRightArrow, false);
    }

    private void SyncLocalDirectAngleArrows(int motorID, float rawDeltaSign)
    {
        ClearLocalDirectAngleArrows();

        if (Mathf.Abs(rawDeltaSign) <= 0.0001f)
        {
            return;
        }

        ApplyArrowForMotor(motorID, rawDeltaSign);
    }

    private void ApplyArrowForMotor(int motorID, float rawDeltaSign)
    {
        if (Mathf.Abs(rawDeltaSign) <= 0.0001f)
        {
            return;
        }

        switch (motorID)
        {
            case 1:
                SetLocalFixedHorizontalArrow(thumb4LeftLeftArrow, thumb4RightRightArrow, rawDeltaSign);
                break;
            case 2:
                SetLocalFixedHorizontalArrow(thumb3LeftLeftArrow, thumb3RightRightArrow, rawDeltaSign);
                break;
            case 3:
            case 4:
            case 7:
            case 8:
            case 11:
            case 12:
                SetLocalVerticalArrowState(motorID, rawDeltaSign < 0f, rawDeltaSign > 0f);
                break;
            case 5:
                SetLocalFixedHorizontalArrow(index4LeftLeftArrow, index4RightRightArrow, rawDeltaSign);
                break;
            case 6:
                SetLocalFixedHorizontalArrow(index3LeftLeftArrow, index3RightRightArrow, rawDeltaSign);
                break;
            case 9:
                SetLocalFixedHorizontalArrow(middle4LeftLeftArrow, middle4RightRightArrow, rawDeltaSign);
                break;
            case 10:
                SetLocalFixedHorizontalArrow(middle3LeftLeftArrow, middle3RightRightArrow, rawDeltaSign);
                break;
        }
    }

    private void ApplyArrowStateForFrame()
    {
        // Always clear first so stale arrows from other logic cannot persist in Delta mode.
        ClearLocalDirectAngleArrows();

        if (groupArrowDirtyThisFrame && groupArrowMotorCountThisFrame > 0)
        {
            for (int i = 0; i < groupArrowMotorCountThisFrame; i++)
            {
                ApplyArrowForMotor(groupArrowMotorIDsThisFrame[i], groupArrowDeltaSignThisFrame);
            }
            return;
        }

        if (!arrowDirtyThisFrame)
        {
            return;
        }

        ApplyArrowForMotor(arrowMotorIDThisFrame, arrowDeltaSignThisFrame);
    }

    private void MarkGroupArrowForMotor(int motorID, float rawDeltaSign)
    {
        if (motorID <= 0 || Mathf.Abs(rawDeltaSign) <= 0.0001f)
        {
            return;
        }

        if (!groupArrowDirtyThisFrame)
        {
            groupArrowDirtyThisFrame = true;
            groupArrowDeltaSignThisFrame = rawDeltaSign;
        }

        for (int i = 0; i < groupArrowMotorCountThisFrame; i++)
        {
            if (groupArrowMotorIDsThisFrame[i] == motorID)
            {
                return;
            }
        }

        if (groupArrowMotorCountThisFrame < groupArrowMotorIDsThisFrame.Length)
        {
            groupArrowMotorIDsThisFrame[groupArrowMotorCountThisFrame] = motorID;
            groupArrowMotorCountThisFrame++;
        }
    }

    private void ClearDirectAngleArrows()
    {
        ClearLocalDirectAngleArrows();
    }

    private static int GetMotorIDForSelection(int row, int col)
    {
        if (row < 0 || row >= ROWS || col < 0 || col >= COLS)
        {
            return 0;
        }

        // Motor IDs are grouped by finger columns:
        // row0: 1,5,9 / row1: 2,6,10 / row2: 3,7,11 / row3: 4,8,12
        switch (row)
        {
            case 0:
                return col == 0 ? 1 : (col == 1 ? 5 : 9);
            case 1:
                return col == 0 ? 2 : (col == 1 ? 6 : 10);
            case 2:
                return col == 0 ? 3 : (col == 1 ? 7 : 11);
            case 3:
                return col == 0 ? 4 : (col == 1 ? 8 : 12);
            default:
                return 0;
        }
    }
}
