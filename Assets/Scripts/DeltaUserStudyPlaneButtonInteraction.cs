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
    public ButtonBinding deltaKeyQ = new ButtonBinding { buttonName = "DeltaKeyQ" };
    public ButtonBinding deltaKeyE = new ButtonBinding { buttonName = "DeltaKeyE" };
    public ButtonBinding deltaKeyR = new ButtonBinding { buttonName = "DeltaKeyR" };
    public ButtonBinding deltaKeyF = new ButtonBinding { buttonName = "DeltaKeyF" };
    public ButtonBinding deltaKeyT = new ButtonBinding { buttonName = "DeltaKeyT" };
    public ButtonBinding deltaKeyG = new ButtonBinding { buttonName = "DeltaKeyG" };
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
        InitializeButton(deltaKeyQ);
        InitializeButton(deltaKeyE);
        InitializeButton(deltaKeyR);
        InitializeButton(deltaKeyF);
        InitializeButton(deltaKeyT);
        InitializeButton(deltaKeyG);
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
        if (TryHandleEnter(deltaKeyQ, other)) return;
        if (TryHandleEnter(deltaKeyE, other)) return;
        if (TryHandleEnter(deltaKeyR, other)) return;
        if (TryHandleEnter(deltaKeyF, other)) return;
        if (TryHandleEnter(deltaKeyT, other)) return;
        if (TryHandleEnter(deltaKeyG, other)) return;
        if (TryHandleEnter(deltaKeyReset, other)) return;
    }

    private void OnTriggerStay(Collider other)
    {
        if (TryHandleEnter(deltaKeyW, other)) return;
        if (TryHandleEnter(deltaKeyA, other)) return;
        if (TryHandleEnter(deltaKeyS, other)) return;
        if (TryHandleEnter(deltaKeyD, other)) return;
        if (TryHandleEnter(deltaKeyQ, other)) return;
        if (TryHandleEnter(deltaKeyE, other)) return;
        if (TryHandleEnter(deltaKeyR, other)) return;
        if (TryHandleEnter(deltaKeyF, other)) return;
        if (TryHandleEnter(deltaKeyT, other)) return;
        if (TryHandleEnter(deltaKeyG, other)) return;
        if (TryHandleEnter(deltaKeyReset, other)) return;
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryHandleExit(deltaKeyW, other)) return;
        if (TryHandleExit(deltaKeyA, other)) return;
        if (TryHandleExit(deltaKeyS, other)) return;
        if (TryHandleExit(deltaKeyD, other)) return;
        if (TryHandleExit(deltaKeyQ, other)) return;
        if (TryHandleExit(deltaKeyE, other)) return;
        if (TryHandleExit(deltaKeyR, other)) return;
        if (TryHandleExit(deltaKeyF, other)) return;
        if (TryHandleExit(deltaKeyT, other)) return;
        if (TryHandleExit(deltaKeyG, other)) return;
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
        AssignKeyboardBinding(deltaKeyQ, KeyCode.Q);
        AssignKeyboardBinding(deltaKeyE, KeyCode.E);
        AssignKeyboardBinding(deltaKeyR, KeyCode.R);
        AssignKeyboardBinding(deltaKeyF, KeyCode.F);
        AssignKeyboardBinding(deltaKeyT, KeyCode.T);
        AssignKeyboardBinding(deltaKeyG, KeyCode.G);
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
        SyncButtonVisualState(deltaKeyQ);
        SyncButtonVisualState(deltaKeyE);
        SyncButtonVisualState(deltaKeyR);
        SyncButtonVisualState(deltaKeyF);
        SyncButtonVisualState(deltaKeyT);
        SyncButtonVisualState(deltaKeyG);
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

        if (deltaKeyQ.isTouched)
        {
            currentTouchedButton = deltaKeyQ.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyQ.buttonName;
            return;
        }

        if (deltaKeyE.isTouched)
        {
            currentTouchedButton = deltaKeyE.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyE.buttonName;
            return;
        }

        if (deltaKeyR.isTouched)
        {
            currentTouchedButton = deltaKeyR.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyR.buttonName;
            return;
        }

        if (deltaKeyF.isTouched)
        {
            currentTouchedButton = deltaKeyF.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyF.buttonName;
            return;
        }

        if (deltaKeyT.isTouched)
        {
            currentTouchedButton = deltaKeyT.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyT.buttonName;
            return;
        }

        if (deltaKeyG.isTouched)
        {
            currentTouchedButton = deltaKeyG.buttonName;
            interactionDebug = "Touch stay: " + deltaKeyG.buttonName;
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

        if (deltaKeyQ != null && deltaKeyQ.keyboardKey == keyCode)
        {
            return deltaKeyQ;
        }

        if (deltaKeyE != null && deltaKeyE.keyboardKey == keyCode)
        {
            return deltaKeyE;
        }

        if (deltaKeyR != null && deltaKeyR.keyboardKey == keyCode)
        {
            return deltaKeyR;
        }

        if (deltaKeyF != null && deltaKeyF.keyboardKey == keyCode)
        {
            return deltaKeyF;
        }

        if (deltaKeyT != null && deltaKeyT.keyboardKey == keyCode)
        {
            return deltaKeyT;
        }

        if (deltaKeyG != null && deltaKeyG.keyboardKey == keyCode)
        {
            return deltaKeyG;
        }

        if (deltaKeyReset != null && deltaKeyReset.keyboardKey == keyCode)
        {
            return deltaKeyReset;
        }

        return null;
    }
}
