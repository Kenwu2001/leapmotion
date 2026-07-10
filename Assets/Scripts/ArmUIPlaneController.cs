using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ArmUIPlaneController : MonoBehaviour
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

        public void ApplyCurrentColor()
        {
            if (buttonRenderer == null)
            {
                return;
            }

            buttonRenderer.material.color = isOn ? toggledColor : originalColor;
        }
    }

    [Header("Arm UI Buttons")]
    public ButtonBinding directAngleButton = new ButtonBinding { buttonName = "Direct Angle Button" };
    public ButtonBinding maxMinAngleButton = new ButtonBinding { buttonName = "Max Min Angle Button" };

    [Header("Arm Motor Id Buttons")]
    [Tooltip("Thumb motor id buttons (count is flexible, e.g. 5).")]
    public List<ButtonBinding> thumbMotorIdButtons = new List<ButtonBinding>();

    [Tooltip("Index motor id buttons (count is flexible, e.g. 5).")]
    public List<ButtonBinding> indexMotorIdButtons = new List<ButtonBinding>();

    [Tooltip("Middle motor id buttons (count is flexible, e.g. 5).")]
    public List<ButtonBinding> middleMotorIdButtons = new List<ButtonBinding>();

    [Header("Selection Suppression")]
    [Tooltip("When enabled, motor selection/highlight logic is suppressed so the Arm UI plane can take over interaction.")]
    public bool useArmUIPlane = false;

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string currentTouchedCollider = "None";
    public string interactionDebug = "No Arm UI button touched";

    private readonly List<ButtonBinding> _allButtons = new List<ButtonBinding>();

    private void Awake()
    {
        BuildButtonList();
        for (int i = 0; i < _allButtons.Count; i++)
        {
            InitializeButton(_allButtons[i]);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        for (int i = 0; i < _allButtons.Count; i++)
        {
            if (TryHandleEnter(_allButtons[i], other))
            {
                return;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        for (int i = 0; i < _allButtons.Count; i++)
        {
            if (TryHandleExit(_allButtons[i], other))
            {
                return;
            }
        }
    }

    private void Reset()
    {
        Collider colliderComponent = GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.isTrigger = true;
        }
    }

    private void BuildButtonList()
    {
        _allButtons.Clear();
        AddButtonIfValid(directAngleButton);
        AddButtonIfValid(maxMinAngleButton);
        AddButtonsIfValid(thumbMotorIdButtons);
        AddButtonsIfValid(indexMotorIdButtons);
        AddButtonsIfValid(middleMotorIdButtons);
    }

    private void AddButtonIfValid(ButtonBinding button)
    {
        if (button == null)
        {
            return;
        }

        _allButtons.Add(button);
    }

    private void AddButtonsIfValid(List<ButtonBinding> buttons)
    {
        if (buttons == null)
        {
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            AddButtonIfValid(buttons[i]);
        }
    }

    private void InitializeButton(ButtonBinding button)
    {
        if (button == null)
        {
            return;
        }

        button.Initialize();
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
            currentTouchedButton = button.buttonName;
            currentTouchedCollider = other.name;
            interactionDebug = "Touch enter: " + button.buttonName + " (" + currentTouchedCollider + ")";
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
        for (int i = 0; i < _allButtons.Count; i++)
        {
            ButtonBinding button = _allButtons[i];
            if (button != null && button.isTouched)
            {
                currentTouchedButton = button.buttonName;
                currentTouchedCollider = button.buttonCollider != null ? button.buttonCollider.name : "None";
                interactionDebug = "Touch stay: " + currentTouchedButton;
                return;
            }
        }

        currentTouchedButton = "None";
        currentTouchedCollider = "None";
        interactionDebug = "No Arm UI button touched";
    }
}