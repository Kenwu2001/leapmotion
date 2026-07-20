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
            kbCurrentSelectedRenderer.material.color = Color.green;
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

        float rotDelta = kbRotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q)) KbApplyRotation(kbCurrentRow, kbCurrentCol, -rotDelta);
        if (Input.GetKey(KeyCode.E)) KbApplyRotation(kbCurrentRow, kbCurrentCol, rotDelta);

        if (Input.GetKey(KeyCode.U)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(3, c, -rotDelta); }
        if (Input.GetKey(KeyCode.J)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(3, c, rotDelta); }

        if (Input.GetKey(KeyCode.I)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(2, c, -rotDelta); }
        if (Input.GetKey(KeyCode.K)) { for (int c = 0; c < KB_COLS; c++) KbApplyRotation(2, c, rotDelta); }

        if (Input.GetKeyDown(KeyCode.P))
        {
            controller.KeyboardForceDisengageOnReset();

            controller.currentThumbRotationY = 0f;
            controller.currentThumbRotationZ = 0f;
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
            controller.thumbGripperJoint2MaxRotationVector = controller.KeyboardThumbAngle2InitialRotation.eulerAngles;
            controller.indexGripperJoint1MaxRotationVector = controller.KeyboardGetIndexJoint1MaxRotationVector();
            controller.indexGripperJoint1MinRotationVector =
                (controller.KeyboardIndexAngle1InitialRotation * Quaternion.Euler(0f, 60f, 0f)).eulerAngles;
            controller.indexGripperJoint2MaxRotationVector = controller.KeyboardIndexAngle2InitialRotation.eulerAngles;
            controller.indexGripperJoint2MinRotationVector = controller.KeyboardIndexAngle2InitialRotation.eulerAngles;
            controller.middleGripperJoint1MaxRotationVector = controller.KeyboardGetMiddleJoint1MaxRotationVector();
            controller.middleGripperJoint1MinRotationVector = controller.KeyboardGetMiddleJoint1MinRotationVector();
            controller.middleGripperJoint2MaxRotationVector = controller.KeyboardGetMiddleJoint2MaxRotationVector();
            controller.middleGripperJoint2MinRotationVector = controller.KeyboardMiddleAngle2InitialRotation.eulerAngles;
        }
    }

    private void KbApplyRotation(int row, int col, float delta)
    {
        switch (row)
        {
            case 0:
                switch (col)
                {
                    case 0:
                        controller.currentThumbRotationY += delta;
                        controller.currentThumbRotationY = Mathf.Clamp(controller.currentThumbRotationY, -60f, 60f);
                        controller.thumbGripperJoint1MaxRotationVector =
                            (controller.KeyboardThumbAngle1InitialRotation * Quaternion.Euler(0f, controller.currentThumbRotationY, 0f)).eulerAngles;
                        break;
                    case 1:
                        if (delta < 0f)
                        {
                            controller.currentIndexRotationYMax += delta;
                            controller.currentIndexRotationYMax = Mathf.Clamp(controller.currentIndexRotationYMax, -90f, 0f);
                            controller.indexGripperJoint1MaxRotationVector = controller.KeyboardGetIndexJoint1MaxRotationVector();
                        }
                        else if (delta > 0f)
                        {
                            if (controller.currentIndexRotationYMin <= 0f)
                                controller.currentIndexRotationYMin = 60f;

                            controller.currentIndexRotationYMin += delta;
                            controller.currentIndexRotationYMin = Mathf.Clamp(controller.currentIndexRotationYMin, 0f, 90f);
                            controller.indexGripperJoint1MinRotationVector =
                                (controller.KeyboardIndexAngle1InitialRotation * Quaternion.Euler(0f, controller.currentIndexRotationYMin, 0f)).eulerAngles;
                        }
                        break;
                    case 2:
                        if (delta < 0f)
                        {
                            controller.currentMiddleRotationYMax += delta;
                            controller.currentMiddleRotationYMax = Mathf.Clamp(controller.currentMiddleRotationYMax, -90f, 0f);
                            controller.middleGripperJoint1MaxRotationVector = controller.KeyboardGetMiddleJoint1MaxRotationVector();
                            controller.maxMiddleYAxisAngle = controller.KeyboardNormalizeMiddleJoint1MaxAngle(controller.middleGripperJoint1MaxRotationVector.y);
                            controller.KeyboardRefreshMiddleJoint1YDebug("KbApplyRotation:max");
                        }
                        else if (delta > 0f)
                        {
                            controller.currentMiddleRotationYMin += delta;
                            controller.currentMiddleRotationYMin = Mathf.Clamp(controller.currentMiddleRotationYMin, 0f, 90f);
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
                        controller.currentThumbRotationZ += delta;
                        controller.currentThumbRotationZ = Mathf.Clamp(controller.currentThumbRotationZ, -60f, 60f);
                        controller.hasThumbAbductionAdjustment = true;
                        controller.thumbGripperJoint2MaxRotationVector =
                            (controller.KeyboardThumbAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentThumbRotationZ)).eulerAngles;
                        break;
                    case 1:
                        controller.currentIndexRotationZMax += delta;
                        controller.currentIndexRotationZMax = Mathf.Clamp(controller.currentIndexRotationZMax, -58f, 0f);
                        controller.indexGripperJoint2MaxRotationVector =
                            (controller.KeyboardIndexAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentIndexRotationZMax)).eulerAngles;
                        break;
                    case 2:
                        controller.currentMiddleRotationZ += delta;
                        controller.currentMiddleRotationZ = Mathf.Clamp(controller.currentMiddleRotationZ, 0f, 58f);
                        controller.middleGripperJoint2MaxRotationVector =
                            (controller.KeyboardMiddleAngle2InitialRotation * Quaternion.Euler(0f, 0f, controller.currentMiddleRotationZ)).eulerAngles;
                        break;
                }
                break;

            case 2:
                switch (col)
                {
                    case 0:
                        controller.currentThumbInnerExtensionRotationZ += delta;
                        controller.currentThumbInnerExtensionRotationZ = Mathf.Clamp(controller.currentThumbInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        break;
                    case 1:
                        controller.currentIndexInnerExtensionRotationZ += delta;
                        controller.currentIndexInnerExtensionRotationZ = Mathf.Clamp(controller.currentIndexInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        break;
                    case 2:
                        controller.currentMiddleInnerExtensionRotationZ += delta;
                        controller.currentMiddleInnerExtensionRotationZ = Mathf.Clamp(controller.currentMiddleInnerExtensionRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        break;
                }
                break;

            case 3:
                switch (col)
                {
                    case 0:
                        controller.currentThumbTipRotationZ += delta;
                        controller.currentThumbTipRotationZ = Mathf.Clamp(controller.currentThumbTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        break;
                    case 1:
                        controller.currentIndexTipRotationZ += delta;
                        controller.currentIndexTipRotationZ = Mathf.Clamp(controller.currentIndexTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        break;
                    case 2:
                        controller.currentMiddleTipRotationZ += delta;
                        controller.currentMiddleTipRotationZ = Mathf.Clamp(controller.currentMiddleTipRotationZ, controller.KeyboardExtensionClampMin, controller.KeyboardExtensionClampMax);
                        break;
                }
                break;
        }
    }
}
