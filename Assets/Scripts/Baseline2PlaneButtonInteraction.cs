using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

public class Baseline2PlaneButtonInteraction : MonoBehaviour
{
    [System.Serializable]
    public class ButtonBinding
    {
        public string buttonName = "Button";
        public KeyCode keyboardKey = KeyCode.None;
        public GameObject buttonObject;
        public Collider buttonCollider;
        public Renderer buttonRenderer;
        public Color activeColor = Color.green;
        public bool isTouched;
        public UnityEvent onEnter;
        public UnityEvent onExit;

        [HideInInspector] public Color originalColor;

        public void Initialize()
        {
            if (buttonObject != null)
            {
                if (buttonCollider == null)
                {
                    buttonCollider = buttonObject.GetComponent<Collider>();
                }

                if (buttonRenderer == null)
                {
                    buttonRenderer = buttonObject.GetComponent<Renderer>();
                }
            }

            if (buttonRenderer == null && buttonCollider != null)
            {
                buttonRenderer = buttonCollider.GetComponent<Renderer>();
            }

            if (buttonRenderer != null)
            {
                originalColor = buttonRenderer.material.color;
                ApplyVisual(false);
            }
        }

        public void ApplyVisual(bool isActive)
        {
            if (buttonRenderer == null)
            {
                return;
            }

            buttonRenderer.material.color = isActive ? activeColor : originalColor;
        }
    }

    [Header("Button Setup")]
    public ButtonBinding baseline2KeyW = new ButtonBinding { buttonName = "Baseline2KeyW" };
    public ButtonBinding baseline2KeyA = new ButtonBinding { buttonName = "Baseline2KeyA" };
    public ButtonBinding baseline2KeyS = new ButtonBinding { buttonName = "Baseline2KeyS" };
    public ButtonBinding baseline2KeyD = new ButtonBinding { buttonName = "Baseline2KeyD" };
    public ButtonBinding baseline2KeyQ = new ButtonBinding { buttonName = "Baseline2KeyQ" };
    public ButtonBinding baseline2KeyE = new ButtonBinding { buttonName = "Baseline2KeyE" };
    public ButtonBinding baseline2KeyF = new ButtonBinding { buttonName = "Baseline2KeyF" };
    public ButtonBinding baseline2KeyReset = new ButtonBinding { buttonName = "Baseline2KeyReset" };
    public ButtonBinding baseline2Key1 = new ButtonBinding { buttonName = "Baseline2Key1" };
    public ButtonBinding baseline2Key2 = new ButtonBinding { buttonName = "Baseline2Key2" };
    public ButtonBinding baseline2Key3 = new ButtonBinding { buttonName = "Baseline2Key3" };

    [Header("State Sources")]
    public BaselineTwo baselineTwo;

    public GameObject Baseline2Plane;

    [Header("Right Hand Follow")]
    [Tooltip("World X offset from right-hand controller to Baseline2Plane")]
    public float rightHandPlaneXOffset = 0f;
    [Tooltip("Fallback Y offset from right-hand controller when head pose is unavailable")]
    public float rightHandPlaneYOffset = 0f;
    [Tooltip("World Z offset from right-hand controller to Baseline2Plane")]
    public float rightHandPlaneZOffset = 0.3f;
    [Tooltip("Fixed world Euler rotation (X/Y/Z) for Baseline2Plane while following right-hand position")]
    public Vector3 rightHandPlaneRotationOffsetEuler;

    [Header("Head Follow")]
    [Tooltip("Optional head transform used for the plane Y position. If null, XRNode.Head or the main camera will be used.")]
    public Transform headTransform;
    [Tooltip("Extra Y offset applied from the headset/head position when positioning the plane")]
    public float headPlaneYOffset = 0f;

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string interactionDebug = "No Baseline2 plane button touched";

    private Vector3 baseline2PlaneInitialLocalScale = Vector3.one;

    private void Awake()
    {
        if (baselineTwo == null)
        {
            baselineTwo = FindObjectOfType<BaselineTwo>();
        }

        if (Baseline2Plane != null)
        {
            baseline2PlaneInitialLocalScale = Baseline2Plane.transform.localScale;
            Baseline2Plane.transform.SetParent(null, true);
        }

        AssignDefaultKeyboardBindings();
        InitializeButton(baseline2KeyW);
        InitializeButton(baseline2KeyA);
        InitializeButton(baseline2KeyS);
        InitializeButton(baseline2KeyD);
        InitializeButton(baseline2KeyQ);
        InitializeButton(baseline2KeyE);
        InitializeButton(baseline2KeyF);
        InitializeButton(baseline2KeyReset);
        InitializeButton(baseline2Key1);
        InitializeButton(baseline2Key2);
        InitializeButton(baseline2Key3);
        SyncButtonStates();
    }

    private void Update()
    {
        UpdateBaseline2PlanePose();
        SyncButtonStates();
    }

    private void Reset()
    {
        Collider colliderComponent = GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (TryHandleEnter(baseline2KeyW, other)) return;
        if (TryHandleEnter(baseline2KeyA, other)) return;
        if (TryHandleEnter(baseline2KeyS, other)) return;
        if (TryHandleEnter(baseline2KeyD, other)) return;
        if (TryHandleEnter(baseline2KeyQ, other)) return;
        if (TryHandleEnter(baseline2KeyE, other)) return;
        if (TryHandleEnter(baseline2KeyF, other)) return;
        if (TryHandleEnter(baseline2KeyReset, other)) return;
        if (TryHandleEnter(baseline2Key1, other)) return;
        if (TryHandleEnter(baseline2Key2, other)) return;
        if (TryHandleEnter(baseline2Key3, other)) return;
    }

    private void OnTriggerStay(Collider other)
    {
        if (TryHandleEnter(baseline2KeyW, other)) return;
        if (TryHandleEnter(baseline2KeyA, other)) return;
        if (TryHandleEnter(baseline2KeyS, other)) return;
        if (TryHandleEnter(baseline2KeyD, other)) return;
        if (TryHandleEnter(baseline2KeyQ, other)) return;
        if (TryHandleEnter(baseline2KeyE, other)) return;
        if (TryHandleEnter(baseline2KeyF, other)) return;
        if (TryHandleEnter(baseline2KeyReset, other)) return;
        if (TryHandleEnter(baseline2Key1, other)) return;
        if (TryHandleEnter(baseline2Key2, other)) return;
        if (TryHandleEnter(baseline2Key3, other)) return;
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryHandleExit(baseline2KeyW, other)) return;
        if (TryHandleExit(baseline2KeyA, other)) return;
        if (TryHandleExit(baseline2KeyS, other)) return;
        if (TryHandleExit(baseline2KeyD, other)) return;
        if (TryHandleExit(baseline2KeyQ, other)) return;
        if (TryHandleExit(baseline2KeyE, other)) return;
        if (TryHandleExit(baseline2KeyF, other)) return;
        if (TryHandleExit(baseline2KeyReset, other)) return;
        if (TryHandleExit(baseline2Key1, other)) return;
        if (TryHandleExit(baseline2Key2, other)) return;
        if (TryHandleExit(baseline2Key3, other)) return;
    }

    private void InitializeButton(ButtonBinding button)
    {
        if (button == null)
        {
            return;
        }

        button.Initialize();
    }

    private void AssignDefaultKeyboardBindings()
    {
        AssignKeyboardBinding(baseline2KeyW, KeyCode.W);
        AssignKeyboardBinding(baseline2KeyA, KeyCode.A);
        AssignKeyboardBinding(baseline2KeyS, KeyCode.S);
        AssignKeyboardBinding(baseline2KeyD, KeyCode.D);
        AssignKeyboardBinding(baseline2KeyQ, KeyCode.Q);
        AssignKeyboardBinding(baseline2KeyE, KeyCode.E);
        AssignKeyboardBinding(baseline2KeyF, KeyCode.F);
        AssignKeyboardBinding(baseline2KeyReset, KeyCode.Space);
        AssignKeyboardBinding(baseline2Key1, KeyCode.Alpha1);
        AssignKeyboardBinding(baseline2Key2, KeyCode.Alpha2);
        AssignKeyboardBinding(baseline2Key3, KeyCode.Alpha3);
    }

    private static void AssignKeyboardBinding(ButtonBinding button, KeyCode keyCode)
    {
        if (button != null && button.keyboardKey == KeyCode.None)
        {
            button.keyboardKey = keyCode;
        }
    }

    private void SyncButtonStates()
    {
        SyncButtonState(baseline2KeyW, baselineTwo != null && baselineTwo.IsMoveUpPressed);
        SyncButtonState(baseline2KeyA, baselineTwo != null && baselineTwo.IsMoveLeftPressed);
        SyncButtonState(baseline2KeyS, baselineTwo != null && baselineTwo.IsMoveDownPressed);
        SyncButtonState(baseline2KeyD, baselineTwo != null && baselineTwo.IsMoveRightPressed);
        SyncButtonState(baseline2KeyQ, baselineTwo != null && baselineTwo.IsRotateNegativePressed);
        SyncButtonState(baseline2KeyE, baselineTwo != null && baselineTwo.IsRotatePositivePressed);
        SyncButtonState(baseline2KeyF, baselineTwo != null && baselineTwo.IsFreezePressed);
        SyncButtonState(baseline2KeyReset, baselineTwo != null && baselineTwo.IsResetPressed);
        SyncButtonState(baseline2Key1, baselineTwo != null && baselineTwo.IsIndexMiddleIndividualModeActive);
        SyncButtonState(baseline2Key2, baselineTwo != null && baselineTwo.IsSmallRangeMappingActive);
        SyncButtonState(baseline2Key3, baselineTwo != null && baselineTwo.IsSnappingModeActive);
    }

    private void SyncButtonState(ButtonBinding button, bool keyboardPressed)
    {
        if (button == null)
        {
            return;
        }

        button.ApplyVisual(button.isTouched || keyboardPressed);
    }

    private bool TryHandleEnter(ButtonBinding button, Collider other)
    {
        if (button == null || button.buttonCollider == null || other != button.buttonCollider)
        {
            return false;
        }

        if (!button.isTouched)
        {
            button.isTouched = true;
            HandleButtonPress(button);
            currentTouchedButton = button.buttonName;
            interactionDebug = "Touch enter: " + button.buttonName;
            button.onEnter?.Invoke();
        }

        return true;
    }

    private bool TryHandleExit(ButtonBinding button, Collider other)
    {
        if (button == null || button.buttonCollider == null || other != button.buttonCollider)
        {
            return false;
        }

        if (button.isTouched)
        {
            button.isTouched = false;
            interactionDebug = "Touch exit: " + button.buttonName;
            button.onExit?.Invoke();
        }

        RefreshCurrentTouchedButton();
        return true;
    }

    private void RefreshCurrentTouchedButton()
    {
        if (baseline2KeyW.isTouched)
        {
            currentTouchedButton = baseline2KeyW.buttonName;
            interactionDebug = "Touch stay: " + baseline2KeyW.buttonName;
            return;
        }

        if (baseline2KeyA.isTouched)
        {
            currentTouchedButton = baseline2KeyA.buttonName;
            interactionDebug = "Touch stay: " + baseline2KeyA.buttonName;
            return;
        }

        if (baseline2KeyS.isTouched)
        {
            currentTouchedButton = baseline2KeyS.buttonName;
            interactionDebug = "Touch stay: " + baseline2KeyS.buttonName;
            return;
        }

        if (baseline2KeyD.isTouched)
        {
            currentTouchedButton = baseline2KeyD.buttonName;
            interactionDebug = "Touch stay: " + baseline2KeyD.buttonName;
            return;
        }

        if (baseline2KeyQ.isTouched)
        {
            currentTouchedButton = baseline2KeyQ.buttonName;
            interactionDebug = "Touch stay: " + baseline2KeyQ.buttonName;
            return;
        }

        if (baseline2KeyE.isTouched)
        {
            currentTouchedButton = baseline2KeyE.buttonName;
            interactionDebug = "Touch stay: " + baseline2KeyE.buttonName;
            return;
        }

        if (baseline2KeyF.isTouched)
        {
            currentTouchedButton = baseline2KeyF.buttonName;
            interactionDebug = "Touch stay: " + baseline2KeyF.buttonName;
            return;
        }

        if (baseline2KeyReset.isTouched)
        {
            currentTouchedButton = baseline2KeyReset.buttonName;
            interactionDebug = "Touch stay: " + baseline2KeyReset.buttonName;
            return;
        }

        if (baseline2Key1.isTouched)
        {
            currentTouchedButton = baseline2Key1.buttonName;
            interactionDebug = "Touch stay: " + baseline2Key1.buttonName;
            return;
        }

        if (baseline2Key2.isTouched)
        {
            currentTouchedButton = baseline2Key2.buttonName;
            interactionDebug = "Touch stay: " + baseline2Key2.buttonName;
            return;
        }

        if (baseline2Key3.isTouched)
        {
            currentTouchedButton = baseline2Key3.buttonName;
            interactionDebug = "Touch stay: " + baseline2Key3.buttonName;
            return;
        }

        currentTouchedButton = "None";
        interactionDebug = "No Baseline2 plane button touched";
    }

    private void HandleButtonPress(ButtonBinding button)
    {
        if (baselineTwo == null)
        {
            return;
        }

        if (button == baseline2KeyW)
        {
            baselineTwo.MoveSelectionUp();
            return;
        }

        if (button == baseline2KeyA)
        {
            baselineTwo.MoveSelectionLeft();
            return;
        }

        if (button == baseline2KeyS)
        {
            baselineTwo.MoveSelectionDown();
            return;
        }

        if (button == baseline2KeyD)
        {
            baselineTwo.MoveSelectionRight();
            return;
        }

        if (button == baseline2KeyReset)
        {
            baselineTwo.TriggerReset();
            return;
        }

        if (button == baseline2Key1)
        {
            baselineTwo.ToggleIndexMiddleIndividualMode();
            return;
        }

        if (button == baseline2Key2)
        {
            baselineTwo.ToggleSmallRangeMapping();
            return;
        }

        if (button == baseline2Key3)
        {
            baselineTwo.ToggleSnappingMode();
            return;
        }

        if (button == baseline2KeyF)
        {
            baselineTwo.ToggleCurrentSelectionFreeze();
        }
    }

    public ButtonBinding GetButtonBinding(KeyCode keyCode)
    {
        if (baseline2KeyW != null && baseline2KeyW.keyboardKey == keyCode) return baseline2KeyW;
        if (baseline2KeyA != null && baseline2KeyA.keyboardKey == keyCode) return baseline2KeyA;
        if (baseline2KeyS != null && baseline2KeyS.keyboardKey == keyCode) return baseline2KeyS;
        if (baseline2KeyD != null && baseline2KeyD.keyboardKey == keyCode) return baseline2KeyD;
        if (baseline2KeyQ != null && baseline2KeyQ.keyboardKey == keyCode) return baseline2KeyQ;
        if (baseline2KeyE != null && baseline2KeyE.keyboardKey == keyCode) return baseline2KeyE;
        if (baseline2KeyF != null && baseline2KeyF.keyboardKey == keyCode) return baseline2KeyF;
        if (baseline2KeyReset != null && baseline2KeyReset.keyboardKey == keyCode) return baseline2KeyReset;
        if (baseline2Key1 != null && baseline2Key1.keyboardKey == keyCode) return baseline2Key1;
        if (baseline2Key2 != null && baseline2Key2.keyboardKey == keyCode) return baseline2Key2;
        if (baseline2Key3 != null && baseline2Key3.keyboardKey == keyCode) return baseline2Key3;
        return null;
    }

    private void UpdateBaseline2PlanePose()
    {
        if (Baseline2Plane == null)
        {
            return;
        }

        if (!TryGetRightHandControllerWorldPose(out Vector3 rightHandWorldPosition, out _))
        {
            return;
        }

        Vector3 worldPosition = new Vector3(
            rightHandWorldPosition.x + rightHandPlaneXOffset,
            rightHandWorldPosition.y,
            rightHandWorldPosition.z + rightHandPlaneZOffset
        );

        if (TryGetHeadWorldPosition(out Vector3 headWorldPosition))
        {
            worldPosition.y = headWorldPosition.y + headPlaneYOffset;
        }
        else
        {
            worldPosition.y += rightHandPlaneYOffset;
        }

        Transform planeTransform = Baseline2Plane.transform;
        planeTransform.SetPositionAndRotation(worldPosition, Quaternion.Euler(rightHandPlaneRotationOffsetEuler));
        planeTransform.localScale = baseline2PlaneInitialLocalScale;
    }

    private bool TryGetHeadWorldPosition(out Vector3 headWorldPosition)
    {
        if (headTransform != null)
        {
            headWorldPosition = headTransform.position;
            return true;
        }

        if (Camera.main != null)
        {
            headTransform = Camera.main.transform;
            headWorldPosition = headTransform.position;
            return true;
        }

        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (headDevice.isValid &&
            headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out headWorldPosition))
        {
            return true;
        }

        headWorldPosition = Vector3.zero;
        return false;
    }

    private bool TryGetRightHandControllerWorldPose(out Vector3 rightHandWorldPosition, out Quaternion rightHandWorldRotation)
    {
        rightHandWorldPosition = Vector3.zero;
        rightHandWorldRotation = Quaternion.identity;

        InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!rightDevice.isValid)
        {
            return false;
        }

        if (!rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightHandTrackingPosition) ||
            !rightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightHandTrackingRotation))
        {
            return false;
        }

        Transform trackingRoot = null;
        if (headTransform != null && headTransform.parent != null)
        {
            trackingRoot = headTransform.parent;
        }
        else if (Camera.main != null && Camera.main.transform.parent != null)
        {
            trackingRoot = Camera.main.transform.parent;
        }

        if (trackingRoot == null)
        {
            rightHandWorldPosition = rightHandTrackingPosition;
            rightHandWorldRotation = rightHandTrackingRotation;
            return true;
        }

        rightHandWorldPosition = trackingRoot.TransformPoint(rightHandTrackingPosition);
        rightHandWorldRotation = trackingRoot.rotation * rightHandTrackingRotation;

        return true;
    }
}
