using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(ClawModuleController))]
public class BaselineTwo : MonoBehaviour
{
    [Header("=== Keyboard Control ===")]
    [Tooltip("ON: Enable WASD+QE keyboard control for motor offsets")]
    public bool useKeyboardControl = false;

    private const int KB_ROWS = 4;
    private const int KB_COLS = 3;

    private ClawModuleController controller;
    private int kbCurrentRow = 3;
    private int kbCurrentCol = 1;
    private Transform[,] kbMotorArray;
    private Renderer[,] kbRendererArray;
    private Renderer kbCurrentSelectedRenderer;
    private float kbRotationSpeed = 18f;
    private bool prevUseKeyboardControl;
    private bool hadArrowInputLastFrame;
    private int previousSelectedMotorID;
    private int sideLockedMotorID;
    private bool sideLockedUseLeftSide;
    private bool hasPendingArrow;
    private int pendingArrowMotorID;
    private bool pendingArrowUseHorizontal;
    private bool pendingArrowUseLeftSide;
    private float pendingArrowDelta;

    private void Awake()
    {
        controller = GetComponent<ClawModuleController>();

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

        kbMotorArray = new Transform[KB_ROWS, KB_COLS]
        {
            { controller.ThumbAngle1Center, controller.IndexAngle1Center, controller.MiddleAngle1Center },
            { controller.ThumbAngle2Center, controller.IndexAngle2Center, controller.MiddleAngle2Center },
            { controller.ThumbAngle3Center, controller.IndexAngle3Center, controller.MiddleAngle3Center },
            { controller.ThumbAngle4Center, controller.IndexAngle4Center, controller.MiddleAngle4Center }
        };

        kbRendererArray = new Renderer[KB_ROWS, KB_COLS]
        {
            { controller.thumbJoint1Renderer, controller.indexJoint1Renderer, controller.middleJoint1Renderer },
            { controller.thumbJoint2Renderer, controller.indexJoint2Renderer, controller.middleJoint2Renderer },
            { controller.thumbJoint3Renderer, controller.indexJoint3Renderer, controller.middleJoint3Renderer },
            { controller.thumbJoint4Renderer, controller.indexJoint4Renderer, controller.middleJoint4Renderer }
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
        hadArrowInputLastFrame = false;
        sideLockedMotorID = 0;
        hasPendingArrow = false;
        kbCurrentSelectedRenderer = null;
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
                Renderer renderer = kbRendererArray[row, col];
                if (renderer != null && renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }
    }

    private void KbUpdateSelection()
    {
        if (kbCurrentSelectedRenderer != null)
        {
            kbCurrentSelectedRenderer.material.color = controller.KeyboardOriginalColor;
        }

        kbCurrentSelectedRenderer = kbRendererArray[kbCurrentRow, kbCurrentCol];
        if (kbCurrentSelectedRenderer == null)
        {
            return;
        }

        Transform selectedTransform = kbMotorArray[kbCurrentRow, kbCurrentCol];
        if (selectedTransform == controller.IndexAngle1Center || selectedTransform == controller.IndexAngle2Center ||
            selectedTransform == controller.IndexAngle3Center || selectedTransform == controller.IndexAngle4Center)
        {
            kbCurrentSelectedRenderer.material.color = Color.red;
        }
        else
        {
            kbCurrentSelectedRenderer.material.color = Color.red;
        }
    }

    private void HandleKeyboardControl()
    {
        bool moved = false;
        if (Input.GetKeyDown(KeyCode.W)) { kbCurrentRow = (kbCurrentRow + 1) % KB_ROWS; moved = true; }
        else if (Input.GetKeyDown(KeyCode.S)) { kbCurrentRow = (kbCurrentRow - 1 + KB_ROWS) % KB_ROWS; moved = true; }
        else if (Input.GetKeyDown(KeyCode.A)) { kbCurrentCol = (kbCurrentCol - 1 + KB_COLS) % KB_COLS; moved = true; }
        else if (Input.GetKeyDown(KeyCode.D)) { kbCurrentCol = (kbCurrentCol + 1) % KB_COLS; moved = true; }
        if (moved) KbUpdateSelection();

        int selectedMotorID = GetMotorIDForCell(kbCurrentRow, kbCurrentCol);
        if (selectedMotorID != previousSelectedMotorID)
        {
            sideLockedMotorID = 0;
            previousSelectedMotorID = selectedMotorID;
        }

        float rotDelta = kbRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) KbApplyRotation(kbCurrentRow, kbCurrentCol, -rotDelta);
        if (Input.GetKey(KeyCode.E)) KbApplyRotation(kbCurrentRow, kbCurrentCol, rotDelta);

        if (Input.GetKey(KeyCode.U)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(3, c, -rotDelta); }
        if (Input.GetKey(KeyCode.J)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(3, c, rotDelta); }

        if (Input.GetKey(KeyCode.I)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(2, c, -rotDelta); }
        if (Input.GetKey(KeyCode.K)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(2, c, rotDelta); }

        bool hasArrowInput = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E) ||
                             Input.GetKey(KeyCode.U) || Input.GetKey(KeyCode.J) ||
                             Input.GetKey(KeyCode.I) || Input.GetKey(KeyCode.K);
        if (!hasArrowInput && hadArrowInputLastFrame)
        {
            controller.ClearArmUIDirectAngleArrowState();
            hasPendingArrow = false;
        }
        hadArrowInputLastFrame = hasArrowInput;

        if (Input.GetKeyDown(KeyCode.P))
        {
            ResetKeyboardOffsets();
        }
    }

    private void ResetKeyboardOffsets()
    {
        controller.KeyboardForceDisengageOnReset();

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
