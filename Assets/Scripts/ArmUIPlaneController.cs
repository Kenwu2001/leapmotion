using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ArmUIPlaneController : MonoBehaviour
{
    private const int ThumbPaxiniMotorID = 13;
    private const int IndexPaxiniMotorID = 14;
    private const int MiddlePaxiniMotorID = 15;

    public enum ArmSelectionPhase
    {
        SelectingFingertip,
        SelectingMotor,
        MotorConfirmed
    }

    [System.Serializable]
    public class ButtonBinding
    {
        public string buttonName = "Button";
        public int motorID;
        public GameObject buttonObject;
        public Collider buttonCollider;
        public Renderer buttonRenderer;
        public Color toggledColor = Color.green;
        public bool isTouched;
        public bool isOn;
        public UnityEvent onEnter;
        public UnityEvent onExit;

        [HideInInspector] public Color originalColor;
        [HideInInspector] public int resolvedMotorID;

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

        public void ApplyColor(Color color)
        {
            if (buttonRenderer == null)
            {
                return;
            }

            buttonRenderer.material.color = color;
        }
    }

    [Header("Enter Arm UI Plane Collider")]
    public ButtonBinding enterArmUIPlaneButton = new ButtonBinding { buttonName = "Enter Arm UI Plane Button" };

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

    [Header("Arm UI Integration")]
    public ModeSwitching modeSwitching;
    public SelectMotorCollider selectMotorCollider;

    [Header("Arm UI Mode")]
    public bool armModeSelect = true;
    public bool armModeManipulate = false;
    public int armCurrentTouchedMotorID = 0;
    public int armRawCurrentTouchedMotorID = 0;
    public int armCurrentRedMotorID = 0;
    public int armConfirmedMotorID = 0;
    public int armConfirmedFingertipID = 0;
    public bool[] armSingleMotorFrozen = new bool[12];
    public bool armThumbPaxiniOn = false;
    public bool armIndexPaxiniOn = false;
    public bool armMiddlePaxiniOn = false;

    [Header("Debug")]
    public string currentTouchedButton = "None";
    public string currentTouchedCollider = "None";
    public string interactionDebug = "No Arm UI button touched";
    public int armRawTouchedMotorID = 0;
    public int armRejectedMotorID = 0;
    public string armRejectReason = "None";
    public string armUIProxyDebug = "N/A";
    [TextArea(6, 16)] public string armUIProxyHistory = "N/A";

    private readonly List<ButtonBinding> _motorButtons = new List<ButtonBinding>();
    private readonly Queue<string> _armUIHistoryEntries = new Queue<string>();
    private string _lastArmUIProxyDebugSnapshot = string.Empty;

    private void Awake()
    {
        if (modeSwitching == null)
        {
            modeSwitching = FindObjectOfType<ModeSwitching>();
        }

        if (selectMotorCollider == null)
        {
            selectMotorCollider = FindObjectOfType<SelectMotorCollider>();
        }

        BuildButtonList();
        InitializeButton(enterArmUIPlaneButton);
        InitializeButton(directAngleButton);
        InitializeButton(maxMinAngleButton);
        for (int i = 0; i < _motorButtons.Count; i++)
        {
            InitializeButton(_motorButtons[i]);
        }
    }

    private void Update()
    {
        if (!useArmUIPlane)
        {
            if (modeSwitching != null)
            {
                modeSwitching.ClearArmUIInput();
            }
            SyncMotorButtonColors();
            return;
        }

        if (modeSwitching != null)
        {
            armRawTouchedMotorID = GetRawTouchedArmMotorID();
            modeSwitching.ReceiveArmUIInput(enterArmUIPlaneButton.isTouched, armRawTouchedMotorID);
            armModeSelect = modeSwitching.armUIProxyModeSelect;
            armModeManipulate = modeSwitching.armUIProxyModeManipulate;
            armCurrentTouchedMotorID = modeSwitching.armUIProxyCurrentTouchedMotorID;
            armCurrentRedMotorID = modeSwitching.armUIProxyCurrentRedMotorID;
            armConfirmedMotorID = modeSwitching.armUIProxyConfirmedMotorID;
            armConfirmedFingertipID = modeSwitching.armUIProxyConfirmedFingertipID;
            armThumbPaxiniOn = modeSwitching.armUIProxyThumbPaxiniOn;
            armIndexPaxiniOn = modeSwitching.armUIProxyIndexPaxiniOn;
            armMiddlePaxiniOn = modeSwitching.armUIProxyMiddlePaxiniOn;
            armRejectedMotorID = modeSwitching.armUIProxyRejectedMotorID;
            armRejectReason = modeSwitching.armUIProxyRejectReason;
            if (armSingleMotorFrozen == null || armSingleMotorFrozen.Length != 12)
            {
                armSingleMotorFrozen = new bool[12];
            }
            System.Array.Copy(modeSwitching.armUIProxySingleMotorFrozen, armSingleMotorFrozen, 12);
            armUIProxyDebug = "Enter=" + enterArmUIPlaneButton.isTouched +
                " Raw=" + armRawTouchedMotorID +
                " ProxyTouched=" + armCurrentTouchedMotorID +
                " Red=" + armCurrentRedMotorID +
                " Confirmed=" + armConfirmedMotorID +
                " Tip=" + armConfirmedFingertipID +
                " Rejected=" + armRejectedMotorID +
                " Reason=" + armRejectReason;
            RecordArmUIHistoryIfChanged();
        }

        SyncMotorButtonColors();
    }

    private void RecordArmUIHistoryIfChanged()
    {
        if (armUIProxyDebug == _lastArmUIProxyDebugSnapshot)
        {
            return;
        }

        _lastArmUIProxyDebugSnapshot = armUIProxyDebug;
        string historyLine = "f=" + Time.frameCount + " t=" + Time.time.ToString("F3") + " " + armUIProxyDebug;
        _armUIHistoryEntries.Enqueue(historyLine);
        while (_armUIHistoryEntries.Count > 12)
        {
            _armUIHistoryEntries.Dequeue();
        }

        armUIProxyHistory = string.Join("\n", _armUIHistoryEntries.ToArray());
    }

    private void OnTriggerEnter(Collider other)
    {
        // Child button colliders use ArmUIButtonTriggerDetector to route events.
    }

    private void OnTriggerExit(Collider other)
    {
        // Child button colliders use ArmUIButtonTriggerDetector to route events.
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
        _motorButtons.Clear();
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

        _motorButtons.Add(button);
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
        button.resolvedMotorID = ResolveMotorID(button);
        AttachTriggerDetector(button);
    }

    private void AttachTriggerDetector(ButtonBinding button)
    {
        if (button == null || button.buttonCollider == null)
        {
            return;
        }

        ArmUIButtonTriggerDetector detector = button.buttonCollider.GetComponent<ArmUIButtonTriggerDetector>();
        if (detector == null)
        {
            detector = button.buttonCollider.gameObject.AddComponent<ArmUIButtonTriggerDetector>();
        }

        detector.Initialize(this, button);
    }

    internal void OnButtonTriggerEnter(ButtonBinding button, Collider other)
    {
        if (button == null)
        {
            return;
        }

        if (button == enterArmUIPlaneButton)
        {
            if (!enterArmUIPlaneButton.isTouched)
            {
                enterArmUIPlaneButton.isTouched = true;
                interactionDebug = "Touch enter: " + enterArmUIPlaneButton.buttonName + " (" + other.name + ")";
                currentTouchedCollider = other.name;
                enterArmUIPlaneButton.onEnter?.Invoke();
            }

            return;
        }

        if (!button.isTouched)
        {
            button.isTouched = true;
            currentTouchedButton = button.buttonName;
            currentTouchedCollider = other.name;
            interactionDebug = "Touch enter: " + button.buttonName + " (" + currentTouchedCollider + ")";
            button.onEnter?.Invoke();
            RefreshActiveArmMotor();
        }
    }

    internal void OnButtonTriggerExit(ButtonBinding button, Collider other)
    {
        if (button == null)
        {
            return;
        }

        if (button == enterArmUIPlaneButton)
        {
            if (enterArmUIPlaneButton.isTouched)
            {
                enterArmUIPlaneButton.isTouched = false;
                interactionDebug = "Touch exit: " + enterArmUIPlaneButton.buttonName + " (" + other.name + ")";
                enterArmUIPlaneButton.onExit?.Invoke();
            }

            return;
        }

        if (button.isTouched)
        {
            button.isTouched = false;
            interactionDebug = "Touch exit: " + button.buttonName + " (" + other.name + ")";
            button.onExit?.Invoke();
        }

        RefreshActiveArmMotor();
        RefreshCurrentTouchedButton();
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
            RefreshActiveArmMotor();
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

        RefreshActiveArmMotor();
        RefreshCurrentTouchedButton();
        return true;
    }

    private void RefreshCurrentTouchedButton()
    {
        for (int i = _motorButtons.Count - 1; i >= 0; i--)
        {
            ButtonBinding button = _motorButtons[i];
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

    public bool IsPointerInsideEnterArmUIPlane()
    {
        return useArmUIPlane && enterArmUIPlaneButton.isTouched;
    }

    public int GetActiveArmMotorID()
    {
        return armCurrentTouchedMotorID;
    }

    public int GetRawTouchedArmMotorID()
    {
        if (!useArmUIPlane || !enterArmUIPlaneButton.isTouched)
        {
            return 0;
        }

        return armRawCurrentTouchedMotorID;
    }

    private void RefreshActiveArmMotor()
    {
        armRawCurrentTouchedMotorID = 0;

        for (int i = _motorButtons.Count - 1; i >= 0; i--)
        {
            ButtonBinding button = _motorButtons[i];
            if (button != null && button.isTouched && button.resolvedMotorID >= 1 && button.resolvedMotorID <= 15)
            {
                armRawCurrentTouchedMotorID = button.resolvedMotorID;
                return;
            }
        }
    }

    private void SyncMotorButtonColors()
    {
        for (int i = 0; i < _motorButtons.Count; i++)
        {
            ButtonBinding button = _motorButtons[i];
            if (button == null || button.buttonRenderer == null)
            {
                continue;
            }

            if (modeSwitching != null && button.resolvedMotorID >= 1 && button.resolvedMotorID <= 15)
            {
                button.ApplyColor(modeSwitching.GetArmUIProxyMotorDisplayColor(button.resolvedMotorID, button.originalColor));
            }
            else
            {
                button.ApplyCurrentColor();
            }
        }
    }

    private int ResolveMotorID(ButtonBinding button)
    {
        if (button == null)
        {
            return 0;
        }

        if (button.motorID >= 1 && button.motorID <= 15)
        {
            return button.motorID;
        }

        int parsedMotorID = ExtractFirstInteger(button.buttonName);
        if (parsedMotorID == 0 && button.buttonObject != null)
        {
            parsedMotorID = ExtractFirstInteger(button.buttonObject.name);
        }
        if (parsedMotorID == 0 && button.buttonCollider != null)
        {
            parsedMotorID = ExtractFirstInteger(button.buttonCollider.name);
        }

        return parsedMotorID;
    }

    private int ExtractFirstInteger(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int value = 0;
        bool foundDigit = false;
        for (int i = 0; i < text.Length; i++)
        {
            if (!char.IsDigit(text[i]))
            {
                if (foundDigit)
                {
                    break;
                }

                continue;
            }

            foundDigit = true;
            value = (value * 10) + (text[i] - '0');
        }

        return foundDigit ? value : 0;
    }
}

internal class ArmUIButtonTriggerDetector : MonoBehaviour
{
    private ArmUIPlaneController _controller;
    private ArmUIPlaneController.ButtonBinding _button;
    private int _touchCount = 0;
    private bool _isActive = false;

    public void Initialize(ArmUIPlaneController controller, ArmUIPlaneController.ButtonBinding button)
    {
        _controller = controller;
        _button = button;
        _touchCount = 0;
        _isActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        _touchCount++;
        if (_controller != null && !_isActive)
        {
            _isActive = true;
            _controller.OnButtonTriggerEnter(_button, other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (_controller != null && _isActive && _button != null && _button == _controller.enterArmUIPlaneButton)
        {
            _controller.currentTouchedCollider = other.name;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _touchCount = Mathf.Max(0, _touchCount - 1);
        if (_controller != null && _isActive && _touchCount == 0)
        {
            _isActive = false;
            _controller.OnButtonTriggerExit(_button, other);
        }
    }
}