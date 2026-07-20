using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(ClawModuleController))]
public class BaselineTwo : MonoBehaviour
{
    [Header("=== Keyboard Control ===")]
    [Tooltip("ON: Enable WASD+QE keyboard control for motor offsets")]
    public bool useKeyboardControl = false;
    public Color lightRedColor = new Color(1f, 0.5f, 0.5f, 1f);

    private const int KB_ROWS = 5;
    private const int KB_COLS = 3;

    private ClawModuleController controller;
    private int kbCurrentRow = 3;
    private int kbCurrentCol = 1;
    private int[,] kbMotorIdArray;
    private Renderer[,] kbRendererArray;
    private float kbRotationSpeed = 18f;
    private bool prevUseKeyboardControl;
    private bool hadArrowInputLastFrame;
    private readonly bool[] kbSingleFrozen = new bool[12];
    private int previousSelectedMotorID;
    private int sideLockedMotorID;
    private bool sideLockedUseLeftSide;
    private bool hasPendingArrow;
    private int pendingArrowMotorID;
    private bool pendingArrowUseHorizontal;
    private bool pendingArrowUseLeftSide;
    private float pendingArrowDelta;
    private bool selectedFrozenUsesLightRed;
    public Baseline2PlaneButtonInteraction planeButtonInteraction;

    public bool IsMoveUpPressed => useKeyboardControl && (Input.GetKey(KeyCode.W) || IsPlaneButtonTouched(KeyCode.W));
    public bool IsMoveLeftPressed => useKeyboardControl && (Input.GetKey(KeyCode.A) || IsPlaneButtonTouched(KeyCode.A));
    public bool IsMoveDownPressed => useKeyboardControl && (Input.GetKey(KeyCode.S) || IsPlaneButtonTouched(KeyCode.S));
    public bool IsMoveRightPressed => useKeyboardControl && (Input.GetKey(KeyCode.D) || IsPlaneButtonTouched(KeyCode.D));
    public bool IsRotateNegativePressed => useKeyboardControl && (Input.GetKey(KeyCode.Q) || IsPlaneButtonTouched(KeyCode.Q));
    public bool IsRotatePositivePressed => useKeyboardControl && (Input.GetKey(KeyCode.E) || IsPlaneButtonTouched(KeyCode.E));
    public bool IsFreezePressed => useKeyboardControl && (Input.GetKey(KeyCode.F) || IsPlaneButtonTouched(KeyCode.F));
    public bool IsResetPressed => useKeyboardControl && (Input.GetKey(KeyCode.Space) || IsPlaneButtonTouched(KeyCode.Space));
    public bool IsIndexMiddleIndividualModeActive => useKeyboardControl && controller != null && controller.useIndexMiddleIndividualMode;
    public bool IsSmallRangeMappingActive => useKeyboardControl && controller != null && !controller.isFullRangeMapping;
    public bool IsSnappingModeActive => useKeyboardControl && controller != null && controller.IsCurrentSnappingEnabled();
    public bool IsCurrentSelectionFrozen => IsSelectionFrozen(GetMotorIDForCell(kbCurrentRow, kbCurrentCol));

    private void Awake()
    {
        controller = GetComponent<ClawModuleController>();

        if (planeButtonInteraction == null)
        {
            planeButtonInteraction = FindObjectOfType<Baseline2PlaneButtonInteraction>();
        }

        if (useKeyboardControl && controller != null && controller.modeSwitching != null)
        {
            controller.modeSwitching.enabled = false;
        }
    }

    private void Start()
    {
        if (controller == null)
        {
            controller = GetComponent<ClawModuleController>();
            if (controller == null)
            {
                enabled = false;
                return;
            }
        }

        kbMotorIdArray = new int[KB_ROWS, KB_COLS]
        {
            { 1, 5, 9 },
            { 2, 6, 10 },
            { 3, 7, 11 },
            { 4, 8, 12 },
            { 13, 14, 15 }
        };

        kbRendererArray = new Renderer[KB_ROWS, KB_COLS]
        {
            { controller.thumbJoint1Renderer, controller.indexJoint1Renderer, controller.middleJoint1Renderer },
            { controller.thumbJoint2Renderer, controller.indexJoint2Renderer, controller.middleJoint2Renderer },
            { controller.thumbJoint3Renderer, controller.indexJoint3Renderer, controller.middleJoint3Renderer },
            { controller.thumbJoint4Renderer, controller.indexJoint4Renderer, controller.middleJoint4Renderer },
            { GetPaxiniRenderer(13), GetPaxiniRenderer(14), GetPaxiniRenderer(15) }
        };

        prevUseKeyboardControl = useKeyboardControl;
        if (useKeyboardControl)
        {
            EnterKeyboardMode();
        }
    }

    private void Update()
    {
        if (controller == null)
        {
            return;
        }

        if (useKeyboardControl != prevUseKeyboardControl)
        {
            prevUseKeyboardControl = useKeyboardControl;
            if (useKeyboardControl)
            {
                EnterKeyboardMode();
            }
            else
            {
                ExitKeyboardMode();
            }
        }

        if (useKeyboardControl)
        {
            HandleKeyboardControl();
        }
    }

    private void LateUpdate()
    {
        if (controller == null)
        {
            return;
        }

        if (!useKeyboardControl)
        {
            return;
        }

        if (hasPendingArrow)
        {
            if (pendingArrowUseHorizontal)
            {
                controller.SyncArmUIHorizontalArrowState(pendingArrowMotorID, pendingArrowUseLeftSide, pendingArrowDelta);
            }
            else
            {
                controller.SyncArmUIDirectAngleArrowState(pendingArrowMotorID, pendingArrowDelta);
            }
        }
    }

    private void EnterKeyboardMode()
    {
        controller.ResetFingerRotations();
        controller.KeyboardResetModeSwitchingState();
        SetCollidersEnabled(false);

        if (controller.modeSwitching != null)
        {
            controller.modeSwitching.enabled = false;
        }

        kbCurrentRow = 3;
        kbCurrentCol = 1;
        previousSelectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        sideLockedMotorID = 0;
        sideLockedUseLeftSide = false;
        ClearAllSingleFreezeStates();
        KbSetAllColors(controller.KeyboardOriginalColor);
        KbUpdateSelection();
    }

    private void ExitKeyboardMode()
    {
        controller.ResetFingerRotations();
        controller.KeyboardResetModeSwitchingState();
        SetCollidersEnabled(true);

        if (controller.modeSwitching != null)
        {
            controller.modeSwitching.enabled = true;
        }

        controller.KeyboardSetEmbodimentInitialColors();
        controller.ClearArmUIDirectAngleArrowState();
        controller.KeyboardClearSingleMotorFreezeOverrides();
        hadArrowInputLastFrame = false;
        sideLockedMotorID = 0;
        hasPendingArrow = false;
        ClearAllSingleFreezeStates();
    }

    private void SetCollidersEnabled(bool enabledState)
    {
        if (controller.modeSwitching != null && controller.modeSwitching.SelectMotorCollider != null)
        {
            controller.modeSwitching.SelectMotorCollider.enabled = enabledState;
        }

        SetTriggerColliderEnabled(controller.triggerRightIndexTip, enabledState);
        SetTriggerColliderEnabled(controller.triggerRightMiddleTip, enabledState);
        SetTriggerColliderEnabled(controller.triggerRightThumbTip, enabledState);
        SetTriggerColliderEnabled(controller.triggerRightThumbAbduction, enabledState);
    }

    private static void SetTriggerColliderEnabled(MonoBehaviour trigger, bool enabledState)
    {
        if (trigger == null)
        {
            return;
        }

        Collider col = trigger.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = enabledState;
        }
    }

    private void KbSetAllColors(Color color)
    {
        for (int row = 0; row < KB_ROWS; row++)
        {
            for (int col = 0; col < KB_COLS; col++)
            {
                int motorID = GetMotorIDForCell(row, col);
                if (motorID > 0)
                {
                    ApplyMotorVisualState(motorID, color);
                }
            }
        }
    }

    private void KbUpdateSelection()
    {
        int previousMotorID = previousSelectedMotorID;
        int currentMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);

        if (previousMotorID > 0)
        {
            ApplyMotorVisualState(previousMotorID);
        }

        previousSelectedMotorID = currentMotorID;
        selectedFrozenUsesLightRed = IsMotorFrozen(currentMotorID);
        ApplyMotorVisualState(currentMotorID);
    }

    private void HandleKeyboardControl()
    {
        if (Input.GetKeyDown(KeyCode.W)) MoveSelectionUp();
        else if (Input.GetKeyDown(KeyCode.S)) MoveSelectionDown();
        else if (Input.GetKeyDown(KeyCode.A)) MoveSelectionLeft();
        else if (Input.GetKeyDown(KeyCode.D)) MoveSelectionRight();

        int selectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        if (selectedMotorID != previousSelectedMotorID)
        {
            sideLockedMotorID = 0;
            previousSelectedMotorID = selectedMotorID;
        }

        float rotDelta = kbRotationSpeed * Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleCurrentSelectionFreeze();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleIndexMiddleIndividualMode();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ToggleSmallRangeMapping();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ToggleSnappingMode();
        }

        bool isPaxiniSelection = selectedMotorID >= 13 && selectedMotorID <= 15;

        if (!isPaxiniSelection)
        {
            if (IsRotateNegativePressed) KbApplyRotation(kbCurrentRow, kbCurrentCol, -rotDelta);
            if (IsRotatePositivePressed) KbApplyRotation(kbCurrentRow, kbCurrentCol, rotDelta);
        }

        bool hasArrowInput = !isPaxiniSelection && (IsRotateNegativePressed || IsRotatePositivePressed);
        if (!hasArrowInput && hadArrowInputLastFrame)
        {
            controller.ClearArmUIDirectAngleArrowState();
            hasPendingArrow = false;
        }
        hadArrowInputLastFrame = hasArrowInput;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TriggerReset();
        }
    }

    public void ToggleIndexMiddleIndividualMode()
    {
        if (!useKeyboardControl || controller == null)
        {
            return;
        }

        controller.useIndexMiddleIndividualMode = !controller.useIndexMiddleIndividualMode;
    }

    public void ToggleSmallRangeMapping()
    {
        if (!useKeyboardControl || controller == null)
        {
            return;
        }

        controller.isFullRangeMapping = !controller.isFullRangeMapping;
    }

    public void ToggleSnappingMode()
    {
        if (!useKeyboardControl || controller == null)
        {
            return;
        }

        controller.ToggleCurrentSnapping();
    }

    public void TriggerReset()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        ResetKeyboardOffsets();
    }

    public void MoveSelectionUp()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        kbCurrentRow = (kbCurrentRow + 1) % KB_ROWS;
        HandleSelectionChanged();
    }

    public void MoveSelectionLeft()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        kbCurrentCol = (kbCurrentCol - 1 + KB_COLS) % KB_COLS;
        HandleSelectionChanged();
    }

    public void MoveSelectionDown()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        kbCurrentRow = (kbCurrentRow - 1 + KB_ROWS) % KB_ROWS;
        HandleSelectionChanged();
    }

    public void MoveSelectionRight()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        kbCurrentCol = (kbCurrentCol + 1) % KB_COLS;
        HandleSelectionChanged();
    }

    private void HandleSelectionChanged()
    {
        KbUpdateSelection();

        int selectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        if (selectedMotorID != previousSelectedMotorID)
        {
            sideLockedMotorID = 0;
            previousSelectedMotorID = selectedMotorID;
        }
    }

    private void ResetKeyboardOffsets()
    {
        controller.KeyboardForceDisengageOnReset();
        controller.KeyboardClearSingleMotorFreezeOverrides();
        ClearAllSingleFreezeStates();

        controller.currentThumbRotationY = 0f;
        controller.currentThumbRotationYMax = 0f;
        controller.currentThumbRotationYMin = 0f;
        controller.currentThumbRotationZ = 0f;
        controller.currentThumbRotationZMax = 0f;
        controller.currentThumbRotationZMin = 0f;
        controller.hasThumbAbductionAdjustment = false;
        controller.currentThumbInnerExtensionRotationZ = 0f;
        controller.currentThumbTipRotationZ = 0f;

        controller.currentIndexRotationYMax = 0f;
        controller.currentIndexRotationYMin = 0f;
        controller.currentIndexRotationZMax = 0f;
        controller.currentIndexRotationZMin = 0f;
        controller.currentIndexInnerExtensionRotationZ = 0f;
        controller.currentIndexTipRotationZ = 0f;

        controller.currentMiddleRotationYMax = -60f;
        controller.currentMiddleRotationYMin = 0f;
        controller.currentMiddleRotationZ = 0f;
        controller.currentMiddleRotationZMax = 0f;
        controller.currentMiddleRotationZMin = 0f;
        controller.currentMiddleInnerExtensionRotationZ = 0f;
        controller.currentMiddleTipRotationZ = 0f;

        controller.thumbGripperJoint1MaxRotationVector = controller.KeyboardThumbAngle1InitialRotation.eulerAngles;
        if (controller.thumbGripperJoint1MaxRotationVector.y < 1f)
        {
            controller.thumbGripperJoint1MaxRotationVector.y = 360f;
        }

        controller.thumbGripperJoint1MinRotationVector =
            (controller.KeyboardThumbAngle1InitialRotation * Quaternion.Euler(0f, 60f, 0f)).eulerAngles;
        controller.thumbGripperJoint2MaxRotationVector = controller.KeyboardThumbAngle2InitialRotation.eulerAngles;
        if (controller.thumbGripperJoint2MaxRotationVector.z < 1f)
        {
            controller.thumbGripperJoint2MaxRotationVector.z = 360f;
        }

        controller.thumbGripperJoint2MinRotationVector = controller.KeyboardThumbAngle2InitialRotation.eulerAngles;

        controller.indexGripperJoint1MaxRotationVector = controller.KeyboardGetIndexJoint1MaxRotationVector();
        controller.indexGripperJoint1MinRotationVector =
            (controller.KeyboardIndexAngle1InitialRotation * Quaternion.Euler(0f, 60f, 0f)).eulerAngles;
        controller.indexGripperJoint2MaxRotationVector = controller.KeyboardIndexAngle2InitialRotation.eulerAngles;
        controller.indexGripperJoint2MinRotationVector = controller.KeyboardIndexAngle2InitialRotation.eulerAngles;

        controller.middleGripperJoint1MaxRotationVector = controller.KeyboardGetMiddleJoint1MaxRotationVector();
        controller.middleGripperJoint1MinRotationVector = controller.KeyboardGetMiddleJoint1MinRotationVector();
        controller.middleGripperJoint2MaxRotationVector = controller.KeyboardGetMiddleJoint2MaxRotationVector();
        controller.middleGripperJoint2MinRotationVector = controller.KeyboardMiddleAngle2InitialRotation.eulerAngles;

        controller.maxThumbYAxisAngle = controller.KeyboardNormalizeAngle(controller.thumbGripperJoint1MaxRotationVector.y);
        controller.minThumbYAxisAngle = controller.KeyboardNormalizeAngle(controller.thumbGripperJoint1MinRotationVector.y);
        controller.maxThumbZAxisAngle = controller.thumbGripperJoint2MaxRotationVector.z;
        controller.minThumbZAxisAngle = controller.thumbGripperJoint2MinRotationVector.z;

        controller.maxIndexYAxisAngle = controller.KeyboardIndexAngle1InitialRotation.eulerAngles.y;
        controller.minIndexYAxisAngle = controller.KeyboardNormalizeAngle(controller.indexGripperJoint1MinRotationVector.y);
        controller.maxIndexZAxisAngle = controller.KeyboardIndexAngle2InitialRotation.eulerAngles.z;
        controller.minIndexZAxisAngle = controller.KeyboardIndexAngle2InitialRotation.eulerAngles.z;

        controller.maxMiddleYAxisAngle = controller.KeyboardNormalizeMiddleJoint1MaxAngle(controller.middleGripperJoint1MaxRotationVector.y);
        controller.minMiddleYAxisAngle = controller.KeyboardNormalizeAngle(controller.middleGripperJoint1MinRotationVector.y);
        controller.maxMiddleZAxisAngle = controller.KeyboardMiddleAngle2InitialRotation.eulerAngles.z;
        controller.minMiddleZAxisAngle = controller.middleGripperJoint2MinRotationVector.z;
        controller.KeyboardRefreshMiddleJoint1YDebug("BaselineTwo:ResetP");

        controller.ClearArmUIDirectAngleArrowState();
        hadArrowInputLastFrame = false;
        hasPendingArrow = false;
        sideLockedMotorID = 0;
        sideLockedUseLeftSide = false;
        previousSelectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
    }

    public void ToggleCurrentSelectionFreeze()
    {
        if (!useKeyboardControl)
        {
            return;
        }

        int motorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        if (motorID <= 0)
        {
            return;
        }

        if (motorID >= 13 && motorID <= 15)
        {
            TogglePaxiniFreezeForSelection(motorID);
            return;
        }

        bool newFrozenState = !IsSingleFrozen(motorID);
        SetSingleFrozen(motorID, newFrozenState);
    }

    private bool IsSelectionFrozen(int motorID)
    {
        if (motorID >= 13 && motorID <= 15)
        {
            return IsPaxiniSelectionFrozen(motorID);
        }

        return IsSingleFrozen(motorID);
    }

    private bool IsSingleFrozen(int motorID)
    {
        if (motorID < 1 || motorID > kbSingleFrozen.Length)
        {
            return false;
        }

        return kbSingleFrozen[motorID - 1];
    }

    private bool IsPaxiniSelectionFrozen(int motorID)
    {
        if (controller == null)
        {
            return false;
        }

        return controller.KeyboardIsPaxiniFrozen(motorID);
    }

    private void TogglePaxiniFreezeForSelection(int motorID)
    {
        if (controller == null)
        {
            return;
        }

        if (!controller.KeyboardTogglePaxiniFreeze(motorID))
        {
            return;
        }

        if (!controller.KeyboardIsPaxiniFrozen(motorID))
        {
            ClearSingleFreezeStateForGroup(GetGroupStartForPaxiniSelection(motorID));
        }

        if (motorID == GetMotorIDForCell(kbCurrentRow, kbCurrentCol))
        {
            selectedFrozenUsesLightRed = false;
        }

        RefreshAllMotorVisualStates();
    }

    private static int GetGroupStartForPaxiniSelection(int motorID)
    {
        if (motorID == 13) return 1;
        if (motorID == 14) return 5;
        if (motorID == 15) return 9;
        return 0;
    }

    private void SetSingleFrozen(int motorID, bool frozen)
    {
        if (motorID < 1 || motorID > kbSingleFrozen.Length)
        {
            return;
        }

        kbSingleFrozen[motorID - 1] = frozen;

        if (controller != null && controller.modeSwitching != null && controller.modeSwitching.singleMotorFrozen != null &&
            motorID - 1 < controller.modeSwitching.singleMotorFrozen.Length)
        {
            controller.KeyboardSetSingleMotorFreezeState(motorID, frozen);
        }

        if (motorID == GetMotorIDForCell(kbCurrentRow, kbCurrentCol))
        {
            selectedFrozenUsesLightRed = false;
        }

        ApplyMotorVisualState(motorID);
    }

    private void ClearAllSingleFreezeStates()
    {
        for (int i = 0; i < kbSingleFrozen.Length; i++)
        {
            kbSingleFrozen[i] = false;
        }

        if (controller != null && controller.modeSwitching != null && controller.modeSwitching.singleMotorFrozen != null)
        {
            int count = Mathf.Min(kbSingleFrozen.Length, controller.modeSwitching.singleMotorFrozen.Length);
            for (int i = 0; i < count; i++)
            {
                controller.KeyboardSetSingleMotorFreezeState(i + 1, false);
            }
        }

        RefreshAllMotorVisualStates();
    }

    private void ClearSingleFreezeStateForGroup(int groupStart)
    {
        int groupEnd = groupStart + 3;
        for (int motorID = groupStart; motorID <= groupEnd; motorID++)
        {
            int index = motorID - 1;
            if (index < 0 || index >= kbSingleFrozen.Length)
            {
                continue;
            }

            kbSingleFrozen[index] = false;
        }
    }

    private void RefreshAllMotorVisualStates()
    {
        for (int row = 0; row < KB_ROWS; row++)
        {
            for (int col = 0; col < KB_COLS; col++)
            {
                int motorID = GetMotorIDForCell(row, col);
                if (motorID > 0)
                {
                    ApplyMotorVisualState(motorID);
                }
            }
        }
    }

    private void ApplyMotorVisualState(int motorID, Color fallbackColor)
    {
        Renderer renderer = GetRendererForMotorID(motorID);
        if (renderer == null)
        {
            return;
        }

        renderer.material.color = GetKeyboardVisualColorForMotor(motorID, fallbackColor);
    }

    public Color GetKeyboardVisualColorForMotor(int motorID, Color fallbackColor)
    {
        if (motorID <= 0)
        {
            return fallbackColor;
        }

        bool isSelected = motorID == GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        bool isPaxiniFrozen = controller != null && controller.KeyboardIsPaxiniFrozen(motorID);
        bool isSingleFrozen = IsSingleFrozen(motorID);
        bool isFrozen = isPaxiniFrozen || isSingleFrozen;
        Color freezeColor = controller != null ? controller.yellowColor : Color.yellow;

        if (isSelected)
        {
            if (isFrozen && !selectedFrozenUsesLightRed)
            {
                return freezeColor;
            }

            return lightRedColor;
        }

        if (isPaxiniFrozen)
        {
            return freezeColor;
        }

        if (motorID >= 13 && motorID <= 15)
        {
            return fallbackColor;
        }

        if (isSingleFrozen)
        {
            return freezeColor;
        }

        return fallbackColor;
    }

    private bool IsMotorFrozen(int motorID)
    {
        if (motorID <= 0)
        {
            return false;
        }

        if (controller != null && controller.KeyboardIsPaxiniFrozen(motorID))
        {
            return true;
        }

        return IsSingleFrozen(motorID);
    }

    private void ApplyMotorVisualState(int motorID)
    {
        ApplyMotorVisualState(motorID, controller != null ? controller.KeyboardOriginalColor : Color.white);
    }

    private bool IsPlaneButtonTouched(KeyCode keyCode)
    {
        if (planeButtonInteraction == null)
        {
            planeButtonInteraction = FindObjectOfType<Baseline2PlaneButtonInteraction>();
        }

        if (planeButtonInteraction == null)
        {
            return false;
        }

        Baseline2PlaneButtonInteraction.ButtonBinding button = planeButtonInteraction.GetButtonBinding(keyCode);
        return button != null && button.isTouched;
    }

    private Renderer GetRendererForMotorID(int motorID)
    {
        switch (motorID)
        {
            case 1: return controller != null ? controller.thumbJoint1Renderer : null;
            case 2: return controller != null ? controller.thumbJoint2Renderer : null;
            case 3: return controller != null ? controller.thumbJoint3Renderer : null;
            case 4: return controller != null ? controller.thumbJoint4Renderer : null;
            case 5: return controller != null ? controller.indexJoint1Renderer : null;
            case 6: return controller != null ? controller.indexJoint2Renderer : null;
            case 7: return controller != null ? controller.indexJoint3Renderer : null;
            case 8: return controller != null ? controller.indexJoint4Renderer : null;
            case 9: return controller != null ? controller.middleJoint1Renderer : null;
            case 10: return controller != null ? controller.middleJoint2Renderer : null;
            case 11: return controller != null ? controller.middleJoint3Renderer : null;
            case 12: return controller != null ? controller.middleJoint4Renderer : null;
            case 13: return controller != null && controller.triggerRightThumbTip != null ? controller.triggerRightThumbTip.thumbPaxiniRenderer : null;
            case 14: return controller != null && controller.triggerRightIndexTip != null ? controller.triggerRightIndexTip.indexPaxiniRenderer : null;
            case 15: return controller != null && controller.triggerRightMiddleTip != null ? controller.triggerRightMiddleTip.middlePaxiniRenderer : null;
            default: return null;
        }
    }

    private Renderer GetPaxiniRenderer(int motorID)
    {
        if (controller == null)
        {
            return null;
        }

        if (motorID == 13 && controller.triggerRightThumbTip != null)
        {
            return controller.triggerRightThumbTip.thumbPaxiniRenderer;
        }

        if (motorID == 14 && controller.triggerRightIndexTip != null)
        {
            return controller.triggerRightIndexTip.indexPaxiniRenderer;
        }

        if (motorID == 15 && controller.triggerRightMiddleTip != null)
        {
            return controller.triggerRightMiddleTip.middlePaxiniRenderer;
        }

        return null;
    }

    private static int GetMotorIDForCell(int row, int col)
    {
        switch (row)
        {
            case 0:
                switch (col)
                {
                    case 0: return 1;
                    case 1: return 5;
                    case 2: return 9;
                }
                break;
            case 1:
                switch (col)
                {
                    case 0: return 2;
                    case 1: return 6;
                    case 2: return 10;
                }
                break;
            case 2:
                switch (col)
                {
                    case 0: return 3;
                    case 1: return 7;
                    case 2: return 11;
                }
                break;
            case 3:
                switch (col)
                {
                    case 0: return 4;
                    case 1: return 8;
                    case 2: return 12;
                }
                break;
            case 4:
                switch (col)
                {
                    case 0: return 13;
                    case 1: return 14;
                    case 2: return 15;
                }
                break;
        }

        return 0;
    }

    private void SyncKeyboardArrowForCell(int row, int col, float delta)
    {
        if (Mathf.Abs(delta) <= 0.0001f)
        {
            return;
        }

        int motorID = GetMotorIDForCell(row, col);
        if (motorID <= 0)
        {
            return;
        }

        if (row <= 1)
        {
            if (sideLockedMotorID != motorID)
            {
                sideLockedMotorID = motorID;
                sideLockedUseLeftSide = delta < 0f;
            }

            hasPendingArrow = true;
            pendingArrowMotorID = motorID;
            pendingArrowUseHorizontal = true;
            pendingArrowUseLeftSide = sideLockedUseLeftSide;
            pendingArrowDelta = delta;
            return;
        }

        hasPendingArrow = true;
        pendingArrowMotorID = motorID;
        pendingArrowUseHorizontal = false;
        pendingArrowUseLeftSide = false;
        pendingArrowDelta = delta;
    }

    private void EnsureThumbPronationMinBaselineInitialized()
    {
        if (!Mathf.Approximately(controller.currentThumbRotationYMin, 0f))
        {
            return;
        }

        float wrappedY = Mathf.Repeat(controller.thumbGripperJoint1MinRotationVector.y, 360f);
        if (Mathf.Abs(wrappedY - 60f) > 0.5f)
        {
            return;
        }

        controller.currentThumbRotationYMin = 60f;
        Vector3 minVec = controller.thumbGripperJoint1MinRotationVector;
        minVec.y = 60f;
        controller.thumbGripperJoint1MinRotationVector = minVec;
        controller.minThumbYAxisAngle = controller.KeyboardNormalizeAngle(minVec.y);
    }

    private void EnsureIndexPronationMinBaselineInitialized()
    {
        if (!Mathf.Approximately(controller.currentIndexRotationYMin, 0f))
        {
            return;
        }

        float wrappedY = Mathf.Repeat(controller.indexGripperJoint1MinRotationVector.y, 360f);
        if (Mathf.Abs(wrappedY - 60f) > 0.5f)
        {
            return;
        }

        controller.currentIndexRotationYMin = 60f;
        Vector3 minVec = controller.indexGripperJoint1MinRotationVector;
        minVec.y = 60f;
        controller.indexGripperJoint1MinRotationVector = minVec;
        controller.minIndexYAxisAngle = controller.KeyboardNormalizeAngle(minVec.y);
    }

    private void KbApplyRotation(int row, int col, float delta)
    {
        int motorID = GetMotorIDForCell(row, col);
        if (motorID >= 13 && motorID <= 15)
        {
            return;
        }

        if (motorID > 0 && IsSingleFrozen(motorID))
        {
            hasPendingArrow = false;
            controller.ClearArmUIDirectAngleArrowState();
            return;
        }

        float effectiveDelta = delta;
        // Extension rows use reversed keyboard direction (Q/E opposite of previous behavior).
        if (row >= 2)
        {
            effectiveDelta = -delta;
        }

        bool changed = false;

        switch (row)
        {
            case 0:
                switch (col)
                {
                    case 0:
                        if (sideLockedMotorID != 1)
                        {
                            sideLockedMotorID = 1;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevThumbYMax = controller.currentThumbRotationYMax;
                            controller.currentThumbRotationYMax += effectiveDelta;
                            controller.currentThumbRotationYMax = Mathf.Clamp(controller.currentThumbRotationYMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevThumbYMax, controller.currentThumbRotationYMax);
                            controller.thumbGripperJoint1MaxRotationVector =
                                (controller.KeyboardThumbAngle1InitialRotation * Quaternion.Euler(0f, controller.currentThumbRotationYMax, 0f)).eulerAngles;
                        }
                        else
                        {
                            EnsureThumbPronationMinBaselineInitialized();

                            float prevThumbYMin = controller.currentThumbRotationYMin;
                            controller.currentThumbRotationYMin += effectiveDelta;
                            controller.currentThumbRotationYMin = Mathf.Clamp(controller.currentThumbRotationYMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevThumbYMin, controller.currentThumbRotationYMin);
                            controller.thumbGripperJoint1MinRotationVector =
                                (controller.KeyboardThumbAngle1InitialRotation * Quaternion.Euler(0f, controller.currentThumbRotationYMin, 0f)).eulerAngles;
                        }
                        break;
                    case 1:
                        if (sideLockedMotorID != 5)
                        {
                            sideLockedMotorID = 5;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevIndexYMax = controller.currentIndexRotationYMax;
                            controller.currentIndexRotationYMax += effectiveDelta;
                            controller.currentIndexRotationYMax = Mathf.Clamp(controller.currentIndexRotationYMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevIndexYMax, controller.currentIndexRotationYMax);
                            controller.indexGripperJoint1MaxRotationVector = controller.KeyboardGetIndexJoint1MaxRotationVector();
                        }
                        else
                        {
                            EnsureIndexPronationMinBaselineInitialized();

                            float prevIndexYMin = controller.currentIndexRotationYMin;
                            controller.currentIndexRotationYMin += effectiveDelta;
                            controller.currentIndexRotationYMin = Mathf.Clamp(controller.currentIndexRotationYMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevIndexYMin, controller.currentIndexRotationYMin);
                            controller.indexGripperJoint1MinRotationVector =
                                (controller.KeyboardIndexAngle1InitialRotation * Quaternion.Euler(0f, controller.currentIndexRotationYMin, 0f)).eulerAngles;
                        }
                        break;
                    case 2:
                        if (sideLockedMotorID != 9)
                        {
                            sideLockedMotorID = 9;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevMiddleYMax = controller.currentMiddleRotationYMax;
                            controller.currentMiddleRotationYMax += effectiveDelta;
                            controller.currentMiddleRotationYMax = Mathf.Clamp(controller.currentMiddleRotationYMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevMiddleYMax, controller.currentMiddleRotationYMax);
                            controller.middleGripperJoint1MaxRotationVector = controller.KeyboardGetMiddleJoint1MaxRotationVector();
                            controller.maxMiddleYAxisAngle = controller.KeyboardNormalizeMiddleJoint1MaxAngle(controller.middleGripperJoint1MaxRotationVector.y);
                            controller.KeyboardRefreshMiddleJoint1YDebug("KbApplyRotation:max");
                        }
                        else
                        {
                            float prevMiddleYMin = controller.currentMiddleRotationYMin;
                            controller.currentMiddleRotationYMin += effectiveDelta;
                            controller.currentMiddleRotationYMin = Mathf.Clamp(controller.currentMiddleRotationYMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevMiddleYMin, controller.currentMiddleRotationYMin);
                            controller.middleGripperJoint1MinRotationVector = controller.KeyboardGetMiddleJoint1MinRotationVector();
                            controller.minMiddleYAxisAngle = controller.KeyboardNormalizeAngle(controller.middleGripperJoint1MinRotationVector.y);
                            controller.KeyboardRefreshMiddleJoint1YDebug("KbApplyRotation:min");
                        }
                        break;
                }
                break;

            case 1:
                switch (col)
                {
                    case 0:
                        if (sideLockedMotorID != 2)
                        {
                            sideLockedMotorID = 2;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevThumbZMax = controller.currentThumbRotationZMax;
                            controller.currentThumbRotationZMax += effectiveDelta;
                            controller.currentThumbRotationZMax = Mathf.Clamp(controller.currentThumbRotationZMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevThumbZMax, controller.currentThumbRotationZMax);
                            controller.thumbGripperJoint2MaxRotationVector =
                                (controller.KeyboardThumbAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentThumbRotationZMax)).eulerAngles;
                        }
                        else
                        {
                            float prevThumbZMin = controller.currentThumbRotationZMin;
                            controller.currentThumbRotationZMin += effectiveDelta;
                            controller.currentThumbRotationZMin = Mathf.Clamp(controller.currentThumbRotationZMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevThumbZMin, controller.currentThumbRotationZMin);
                            controller.thumbGripperJoint2MinRotationVector =
                                (controller.KeyboardThumbAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentThumbRotationZMin)).eulerAngles;
                        }

                        controller.hasThumbAbductionAdjustment = true;
                        break;
                    case 1:
                        if (sideLockedMotorID != 6)
                        {
                            sideLockedMotorID = 6;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevIndexZMax = controller.currentIndexRotationZMax;
                            controller.currentIndexRotationZMax += effectiveDelta;
                            controller.currentIndexRotationZMax = Mathf.Clamp(controller.currentIndexRotationZMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevIndexZMax, controller.currentIndexRotationZMax);
                            controller.indexGripperJoint2MaxRotationVector =
                                (controller.KeyboardIndexAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentIndexRotationZMax)).eulerAngles;
                        }
                        else
                        {
                            float prevIndexZMin = controller.currentIndexRotationZMin;
                            controller.currentIndexRotationZMin += effectiveDelta;
                            controller.currentIndexRotationZMin = Mathf.Clamp(controller.currentIndexRotationZMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevIndexZMin, controller.currentIndexRotationZMin);
                            controller.indexGripperJoint2MinRotationVector =
                                (controller.KeyboardIndexAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentIndexRotationZMin)).eulerAngles;
                        }
                        break;
                    case 2:
                        if (sideLockedMotorID != 10)
                        {
                            sideLockedMotorID = 10;
                            sideLockedUseLeftSide = effectiveDelta < 0f;
                        }

                        if (sideLockedUseLeftSide)
                        {
                            float prevMiddleZMax = controller.currentMiddleRotationZMax;
                            controller.currentMiddleRotationZMax += effectiveDelta;
                            controller.currentMiddleRotationZMax = Mathf.Clamp(controller.currentMiddleRotationZMax, -90f, 0f);
                            changed = !Mathf.Approximately(prevMiddleZMax, controller.currentMiddleRotationZMax);
                            controller.middleGripperJoint2MaxRotationVector =
                                (controller.KeyboardMiddleAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentMiddleRotationZMax)).eulerAngles;
                        }
                        else
                        {
                            float prevMiddleZMin = controller.currentMiddleRotationZMin;
                            controller.currentMiddleRotationZMin += effectiveDelta;
                            controller.currentMiddleRotationZMin = Mathf.Clamp(controller.currentMiddleRotationZMin, 0f, 90f);
                            changed = !Mathf.Approximately(prevMiddleZMin, controller.currentMiddleRotationZMin);
                            controller.middleGripperJoint2MinRotationVector =
                                (controller.KeyboardMiddleAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentMiddleRotationZMin)).eulerAngles;
                        }
                        break;
                }
                break;

            case 2:
                switch (col)
                {
                    case 0:
                        float prevThumbInner = controller.currentThumbInnerExtensionRotationZ;
                        controller.currentThumbInnerExtensionRotationZ += effectiveDelta;
                        controller.currentThumbInnerExtensionRotationZ = Mathf.Clamp(controller.currentThumbInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevThumbInner, controller.currentThumbInnerExtensionRotationZ);
                        break;
                    case 1:
                        float prevIndexInner = controller.currentIndexInnerExtensionRotationZ;
                        controller.currentIndexInnerExtensionRotationZ += effectiveDelta;
                        controller.currentIndexInnerExtensionRotationZ = Mathf.Clamp(controller.currentIndexInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevIndexInner, controller.currentIndexInnerExtensionRotationZ);
                        break;
                    case 2:
                        float prevMiddleInner = controller.currentMiddleInnerExtensionRotationZ;
                        controller.currentMiddleInnerExtensionRotationZ += effectiveDelta;
                        controller.currentMiddleInnerExtensionRotationZ = Mathf.Clamp(controller.currentMiddleInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevMiddleInner, controller.currentMiddleInnerExtensionRotationZ);
                        break;
                }
                break;

            case 3:
                switch (col)
                {
                    case 0:
                        float prevThumbTip = controller.currentThumbTipRotationZ;
                        controller.currentThumbTipRotationZ += effectiveDelta;
                        controller.currentThumbTipRotationZ = Mathf.Clamp(controller.currentThumbTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevThumbTip, controller.currentThumbTipRotationZ);
                        break;
                    case 1:
                        float prevIndexTip = controller.currentIndexTipRotationZ;
                        controller.currentIndexTipRotationZ += effectiveDelta;
                        controller.currentIndexTipRotationZ = Mathf.Clamp(controller.currentIndexTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevIndexTip, controller.currentIndexTipRotationZ);
                        break;
                    case 2:
                        float prevMiddleTip = controller.currentMiddleTipRotationZ;
                        controller.currentMiddleTipRotationZ += effectiveDelta;
                        controller.currentMiddleTipRotationZ = Mathf.Clamp(controller.currentMiddleTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        changed = !Mathf.Approximately(prevMiddleTip, controller.currentMiddleTipRotationZ);
                        break;
                }
                break;
        }

        if (changed)
        {
            SyncKeyboardArrowForCell(row, col, effectiveDelta);
        }
    }
}
