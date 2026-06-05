using UnityEngine;
using UnityEngine.Events;

public class VirtualLeftHandButtonInteractor : MonoBehaviour
{
    [System.Serializable]
    public class ButtonBinding
    {
        public string buttonName = "Button";
        public GameObject buttonObject;
        public Collider buttonCollider;
        public Renderer buttonRenderer;
        public Color toggledColor = Color.green;
        public bool isTouched;
        public bool isOn;
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
                ApplyCurrentColor();
            }
        }

        public void SetVisible(bool visible)
        {
            if (buttonObject != null)
            {
                buttonObject.SetActive(visible);
            }

            if (buttonCollider != null)
            {
                buttonCollider.enabled = visible;
            }

            if (buttonRenderer != null)
            {
                buttonRenderer.enabled = visible;
            }

            if (!visible)
            {
                isTouched = false;
            }
        }

        public void ApplyCurrentColor()
        {
            if (buttonRenderer == null)
            {
                return;
            }

            buttonRenderer.material.color = isOn ? toggledColor : originalColor;
        }
    }

    [Header("Button Setup")]
    public ButtonBinding button1 = new ButtonBinding { buttonName = "Button 1" };
    public ButtonBinding button2 = new ButtonBinding { buttonName = "Button 2" };
    public ButtonBinding button3 = new ButtonBinding { buttonName = "Button 3" };

    [Header("State Sources")]
    public ClawModuleController clawModuleController;

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string interactionDebug = "No button touched";

    private void Awake()
    {
        ApplyDefaultNames();
        InitializeButton(button1);
        InitializeButton(button2);
        InitializeButton(button3);
        SyncButtonStates();
    }

    private void Update()
    {
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
        if (TryHandleEnter(button1, other)) return;
        if (TryHandleEnter(button2, other)) return;
        TryHandleEnter(button3, other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryHandleExit(button1, other)) return;
        if (TryHandleExit(button2, other)) return;
        TryHandleExit(button3, other);
    }

    private bool TryHandleEnter(ButtonBinding button, Collider other)
    {
        if (button.buttonCollider == null || other != button.buttonCollider)
        {
            return false;
        }

        if (!button.isTouched)
        {
            button.isTouched = true;
            HandleButtonPress(button);
            currentTouchedButton = button.buttonName;
            button.onEnter?.Invoke();
        }

        return true;
    }

    private bool TryHandleExit(ButtonBinding button, Collider other)
    {
        if (button.buttonCollider == null || other != button.buttonCollider)
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
        if (button1.isTouched)
        {
            currentTouchedButton = button1.buttonName;
            interactionDebug = "Touch stay: " + button1.buttonName;
            return;
        }

        if (button2.isTouched)
        {
            currentTouchedButton = button2.buttonName;
            interactionDebug = "Touch stay: " + button2.buttonName;
            return;
        }

        if (button3.isTouched)
        {
            currentTouchedButton = button3.buttonName;
            interactionDebug = "Touch stay: " + button3.buttonName;
            return;
        }

        currentTouchedButton = "None";
        interactionDebug = "No button touched";
    }

    private void InitializeButton(ButtonBinding button)
    {
        if (button == null)
        {
            return;
        }

        button.Initialize();
    }

    private void SyncButtonStates()
    {
        if (clawModuleController != null)
        {
            SetButtonState(button1, !clawModuleController.isFullRangeMapping);
            button2.SetVisible(!clawModuleController.IsResetState);
            SetButtonState(button2, clawModuleController.IsResetState);
            SetButtonState(button3, clawModuleController.useIndexMiddleIndividualMode);
        }
    }

    private void SetButtonState(ButtonBinding button, bool isOn)
    {
        if (button == null || button.isOn == isOn)
        {
            return;
        }

        button.isOn = isOn;
        button.ApplyCurrentColor();
    }

    private void ApplyDefaultNames()
    {
        if (button1.buttonName == "Button 1")
        {
            button1.buttonName = "FullRangeMapping";
        }

        if (button2.buttonName == "Button 2")
        {
            button2.buttonName = "Reset";
        }

        if (button3.buttonName == "Button 3")
        {
            button3.buttonName = "IndexMiddleIndividual";
        }
    }

    private void HandleButtonPress(ButtonBinding button)
    {
        if (clawModuleController == null)
        {
            return;
        }

        if (button == button1)
        {
            clawModuleController.isFullRangeMapping = !clawModuleController.isFullRangeMapping;
            SetButtonState(button1, !clawModuleController.isFullRangeMapping);
            interactionDebug = "Touch enter: " + button.buttonName + " -> " + clawModuleController.isFullRangeMapping;
            return;
        }

        if (button == button2)
        {
            if (!clawModuleController.IsResetState)
            {
                clawModuleController.ResetFingerRotations();
                SetButtonState(button2, clawModuleController.IsResetState);
                interactionDebug = "Touch enter: " + button.buttonName + " -> true";
            }
            else
            {
                interactionDebug = "Touch enter: " + button.buttonName + " ignored";
            }
        }

        if (button == button3)
        {
            clawModuleController.useIndexMiddleIndividualMode = !clawModuleController.useIndexMiddleIndividualMode;
            SetButtonState(button3, clawModuleController.useIndexMiddleIndividualMode);
            interactionDebug = "Touch enter: " + button.buttonName + " -> " + clawModuleController.useIndexMiddleIndividualMode;
        }
    }
}