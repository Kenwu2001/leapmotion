using UnityEngine;

public class DeltaUserStudyPlaneButtonInteraction : MonoBehaviour
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
    public ButtonBinding deltaKeyW = new ButtonBinding { buttonName = "DeltaKeyW" };
    public ButtonBinding deltaKeyA = new ButtonBinding { buttonName = "DeltaKeyA" };
    public ButtonBinding deltaKeyS = new ButtonBinding { buttonName = "DeltaKeyS" };
    public ButtonBinding deltaKeyD = new ButtonBinding { buttonName = "DeltaKeyD" };
    public ButtonBinding deltaKeyU = new ButtonBinding { buttonName = "DeltaKeyU" };
    public ButtonBinding deltaKeyJ = new ButtonBinding { buttonName = "DeltaKeyJ" };
    public ButtonBinding deltaKeyI = new ButtonBinding { buttonName = "DeltaKeyI" };
    public ButtonBinding deltaKeyK = new ButtonBinding { buttonName = "DeltaKeyK" };
    public ButtonBinding deltaKeyReset = new ButtonBinding { buttonName = "DeltaKeyReset" };

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string interactionDebug = "No Delta plane button touched";

    private void Awake()
    {
        AssignDefaultKeyboardBindings();
        InitializeButton(deltaKeyW);
        InitializeButton(deltaKeyA);
        InitializeButton(deltaKeyS);
        InitializeButton(deltaKeyD);
        InitializeButton(deltaKeyU);
        InitializeButton(deltaKeyJ);
        InitializeButton(deltaKeyI);
        InitializeButton(deltaKeyK);
        InitializeButton(deltaKeyReset);
    }

    private void Update()
    {
        SyncButtonVisualStates();
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
        if (TryHandleEnter(deltaKeyW, other)) return;
        if (TryHandleEnter(deltaKeyA, other)) return;
        if (TryHandleEnter(deltaKeyS, other)) return;
        if (TryHandleEnter(deltaKeyD, other)) return;
        if (TryHandleEnter(deltaKeyU, other)) return;
        if (TryHandleEnter(deltaKeyJ, other)) return;
        if (TryHandleEnter(deltaKeyI, other)) return;
        if (TryHandleEnter(deltaKeyK, other)) return;
        if (TryHandleEnter(deltaKeyReset, other)) return;
    }

    private void OnTriggerStay(Collider other)
    {
        if (TryHandleEnter(deltaKeyW, other)) return;
        if (TryHandleEnter(deltaKeyA, other)) return;
        if (TryHandleEnter(deltaKeyS, other)) return;
        if (TryHandleEnter(deltaKeyD, other)) return;
        if (TryHandleEnter(deltaKeyU, other)) return;
        if (TryHandleEnter(deltaKeyJ, other)) return;
        if (TryHandleEnter(deltaKeyI, other)) return;
        if (TryHandleEnter(deltaKeyK, other)) return;
        if (TryHandleEnter(deltaKeyReset, other)) return;
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryHandleExit(deltaKeyW, other)) return;
        if (TryHandleExit(deltaKeyA, other)) return;
        if (TryHandleExit(deltaKeyS, other)) return;
        if (TryHandleExit(deltaKeyD, other)) return;
        if (TryHandleExit(deltaKeyU, other)) return;
        if (TryHandleExit(deltaKeyJ, other)) return;
        if (TryHandleExit(deltaKeyI, other)) return;
        if (TryHandleExit(deltaKeyK, other)) return;
        if (TryHandleExit(deltaKeyReset, other)) return;
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
        AssignKeyboardBinding(deltaKeyW, KeyCode.W);
        AssignKeyboardBinding(deltaKeyA, KeyCode.A);
        AssignKeyboardBinding(deltaKeyS, KeyCode.S);
        AssignKeyboardBinding(deltaKeyD, KeyCode.D);
        AssignKeyboardBinding(deltaKeyU, KeyCode.U);
        AssignKeyboardBinding(deltaKeyJ, KeyCode.J);
        AssignKeyboardBinding(deltaKeyI, KeyCode.I);
        AssignKeyboardBinding(deltaKeyK, KeyCode.K);
        AssignKeyboardBinding(deltaKeyReset, KeyCode.Space);
    }

    private static void AssignKeyboardBinding(ButtonBinding button, KeyCode keyCode)
    {
        if (button != null && button.keyboardKey == KeyCode.None)
        {
            button.keyboardKey = keyCode;
        }
    }

    private void SyncButtonVisualStates()
    {
        SyncButtonVisualState(deltaKeyW);
        SyncButtonVisualState(deltaKeyA);
        SyncButtonVisualState(deltaKeyS);
        SyncButtonVisualState(deltaKeyD);
        SyncButtonVisualState(deltaKeyU);
        SyncButtonVisualState(deltaKeyJ);
        SyncButtonVisualState(deltaKeyI);
        SyncButtonVisualState(deltaKeyK);
        SyncButtonVisualState(deltaKeyReset);
    }

    private void SyncButtonVisualState(ButtonBinding button)
    {
        if (button == null)
        {
            return;
        }

        bool isKeyboardPressed = button.keyboardKey != KeyCode.None && Input.GetKey(button.keyboardKey);
        button.ApplyVisual(button.isTouched || isKeyboardPressed);
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
            button.ApplyVisual(true);
            currentTouchedButton = button.buttonName;
            interactionDebug = "Touch enter: " + button.buttonName;
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
            button.ApplyVisual(false);
            interactionDebug = "Touch exit: " + button.buttonName;
        }

        RefreshCurrentTouchedButton();
        return true;
    }

    private void RefreshCurrentTouchedButton()
    {
        if (deltaKeyW.isTouched)
        {
            currentTouchedButton = deltaKeyW.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyW.buttonName;
            return;
        }

        if (deltaKeyA.isTouched)
        {
            currentTouchedButton = deltaKeyA.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyA.buttonName;
            return;
        }

        if (deltaKeyS.isTouched)
        {
            currentTouchedButton = deltaKeyS.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyS.buttonName;
            return;
        }

        if (deltaKeyD.isTouched)
        {
            currentTouchedButton = deltaKeyD.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyD.buttonName;
            return;
        }

        if (deltaKeyU.isTouched)
        {
            currentTouchedButton = deltaKeyU.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyU.buttonName;
            return;
        }

        if (deltaKeyJ.isTouched)
        {
            currentTouchedButton = deltaKeyJ.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyJ.buttonName;
            return;
        }

        if (deltaKeyI.isTouched)
        {
            currentTouchedButton = deltaKeyI.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyI.buttonName;
            return;
        }

        if (deltaKeyK.isTouched)
        {
            currentTouchedButton = deltaKeyK.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyK.buttonName;
            return;
        }

        if (deltaKeyReset.isTouched)
        {
            currentTouchedButton = deltaKeyReset.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyReset.buttonName;
            return;
        }

        currentTouchedButton = "None";
        interactionDebug = "No Delta plane button touched";
    }

    public ButtonBinding GetButtonBinding(KeyCode keyCode)
    {
        if (deltaKeyW != null && deltaKeyW.keyboardKey == keyCode)
        {
            return deltaKeyW;
        }

        if (deltaKeyA != null && deltaKeyA.keyboardKey == keyCode)
        {
            return deltaKeyA;
        }

        if (deltaKeyS != null && deltaKeyS.keyboardKey == keyCode)
        {
            return deltaKeyS;
        }

        if (deltaKeyD != null && deltaKeyD.keyboardKey == keyCode)
        {
            return deltaKeyD;
        }

        if (deltaKeyU != null && deltaKeyU.keyboardKey == keyCode)
        {
            return deltaKeyU;
        }

        if (deltaKeyJ != null && deltaKeyJ.keyboardKey == keyCode)
        {
            return deltaKeyJ;
        }

        if (deltaKeyI != null && deltaKeyI.keyboardKey == keyCode)
        {
            return deltaKeyI;
        }

        if (deltaKeyK != null && deltaKeyK.keyboardKey == keyCode)
        {
            return deltaKeyK;
        }

        if (deltaKeyReset != null && deltaKeyReset.keyboardKey == keyCode)
        {
            return deltaKeyReset;
        }

        return null;
    }
}
