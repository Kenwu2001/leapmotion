using UnityEngine;
using UnityEngine.Events;

public class VirtualLeftHandButtonInteractor : MonoBehaviour
{
    [System.Serializable]
    public class ButtonBinding
    {
        public string buttonName = "Button";
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

        public void ToggleState()
        {
            isOn = !isOn;
            ApplyCurrentColor();
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

    [Header("Three Button Setup")]
    public ButtonBinding button1 = new ButtonBinding { buttonName = "Button 1" };
    public ButtonBinding button2 = new ButtonBinding { buttonName = "Button 2" };
    public ButtonBinding button3 = new ButtonBinding { buttonName = "Button 3" };

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string interactionDebug = "No button touched";

    private void Awake()
    {
        InitializeButton(button1);
        InitializeButton(button2);
        InitializeButton(button3);
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
            button.ToggleState();
            currentTouchedButton = button.buttonName;
            interactionDebug = "Touch enter: " + button.buttonName + " -> " + (button.isOn ? "ON" : "OFF");
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
}