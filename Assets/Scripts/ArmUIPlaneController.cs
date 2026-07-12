using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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

    [Header("Arm Sliders")]
    public GameObject thumbSliderObject;
    public GameObject indexSliderObject;
    public GameObject middleSliderObject;

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
    public ClawModuleController clawModuleController;

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
    // [TextArea(6, 16)] public string armUIProxyHistory = "N/A";

    private readonly List<ButtonBinding> _motorButtons = new List<ButtonBinding>();
    // private readonly Queue<string> _armUIHistoryEntries = new Queue<string>();
    // private string _lastArmUIProxyDebugSnapshot = string.Empty;
    private bool _thumbSliderVisible;
    private bool _indexSliderVisible;
    private bool _middleSliderVisible;
    private bool _lastArmModeManipulate;
    private Slider _thumbSlider;
    private Slider _indexSlider;
    private Slider _middleSlider;

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

        if (clawModuleController == null)
        {
            clawModuleController = FindObjectOfType<ClawModuleController>();
        }

        CacheAndConfigureSliders();

        BuildButtonList();
        ConfigureArmModeButtonDefaults();
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
            armModeManipulate = false;
            armModeSelect = true;
            SyncSliderVisibility();
            SyncMotorButtonVisibility();
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
                " Reason=" + armRejectReason +
                " | MotorState=" + BuildMotorIDStateSnapshot() +
                " | ArmMotorState=" + BuildArmMotorIDStateSnapshot();
            // RecordArmUIHistoryIfChanged();
        }

        SyncSliderVisibility();
        SyncMotorButtonVisibility();
        SyncMotorButtonColors();
        EnsureArmModeButtonSelectionIntegrity();
        SyncArmModeButtonColors();
        SyncDirectAngleSliderValue();
        _lastArmModeManipulate = armModeManipulate;
    }

    private void ConfigureArmModeButtonDefaults()
    {
        if (directAngleButton == null || maxMinAngleButton == null)
        {
            return;
        }

        directAngleButton.isOn = true;
        maxMinAngleButton.isOn = false;
    }

    private void EnsureArmModeButtonSelectionIntegrity()
    {
        if (directAngleButton == null || maxMinAngleButton == null)
        {
            return;
        }

        if (!directAngleButton.isOn && !maxMinAngleButton.isOn)
        {
            directAngleButton.isOn = true;
        }
        else if (directAngleButton.isOn && maxMinAngleButton.isOn)
        {
            maxMinAngleButton.isOn = false;
        }
    }

    private void SyncArmModeButtonColors()
    {
        if (directAngleButton != null)
        {
            directAngleButton.ApplyCurrentColor();
        }

        if (maxMinAngleButton != null)
        {
            maxMinAngleButton.ApplyCurrentColor();
        }
    }

    private bool IsArmModeButton(ButtonBinding button)
    {
        return button == directAngleButton || button == maxMinAngleButton;
    }

    private void SetArmModeButtonSelection(ButtonBinding selectedButton)
    {
        if (selectedButton == directAngleButton)
        {
            directAngleButton.isOn = true;
            maxMinAngleButton.isOn = false;
        }
        else if (selectedButton == maxMinAngleButton)
        {
            maxMinAngleButton.isOn = true;
            directAngleButton.isOn = false;
        }

        EnsureArmModeButtonSelectionIntegrity();
        SyncArmModeButtonColors();
    }

    private void SyncSliderVisibility()
    {
        bool showThumb = _thumbSliderVisible;
        bool showIndex = _indexSliderVisible;
        bool showMiddle = _middleSliderVisible;

        if (!useArmUIPlane)
        {
            showThumb = false;
            showIndex = false;
            showMiddle = false;
        }
        else if (armModeManipulate)
        {
            int confirmedMotorID = armConfirmedMotorID;
            showThumb = false;
            showIndex = false;
            showMiddle = false;
            if (confirmedMotorID >= 1 && confirmedMotorID <= 4)
            {
                showThumb = true;
            }
            else if (confirmedMotorID >= 5 && confirmedMotorID <= 8)
            {
                showIndex = true;
            }
            else if (confirmedMotorID >= 9 && confirmedMotorID <= 12)
            {
                showMiddle = true;
            }
        }
        else if (armModeSelect)
        {
            // Hide sliders when manipulate transitions back to select.
            if (_lastArmModeManipulate || _thumbSliderVisible || _indexSliderVisible || _middleSliderVisible)
            {
                showThumb = false;
                showIndex = false;
                showMiddle = false;
            }
        }

        SetSliderActive(thumbSliderObject, showThumb, ref _thumbSliderVisible);
        SetSliderActive(indexSliderObject, showIndex, ref _indexSliderVisible);
        SetSliderActive(middleSliderObject, showMiddle, ref _middleSliderVisible);
    }

    private void SetSliderActive(GameObject sliderObject, bool shouldBeVisible, ref bool currentState)
    {
        if (sliderObject == null)
        {
            return;
        }

        if (currentState == shouldBeVisible && sliderObject.activeSelf == shouldBeVisible)
        {
            return;
        }

        sliderObject.SetActive(shouldBeVisible);
        currentState = shouldBeVisible;
    }

    private void CacheAndConfigureSliders()
    {
        _thumbSlider = FindSliderComponent(thumbSliderObject);
        _indexSlider = FindSliderComponent(indexSliderObject);
        _middleSlider = FindSliderComponent(middleSliderObject);

        ConfigureSliderRange(_thumbSlider);
        ConfigureSliderRange(_indexSlider);
        ConfigureSliderRange(_middleSlider);
    }

    private Slider FindSliderComponent(GameObject sliderObject)
    {
        if (sliderObject == null)
        {
            return null;
        }

        Slider slider = sliderObject.GetComponent<Slider>();
        if (slider == null)
        {
            slider = sliderObject.GetComponentInChildren<Slider>(true);
        }

        return slider;
    }

    private void ConfigureSliderRange(Slider slider)
    {
        if (slider == null)
        {
            return;
        }

        slider.minValue = 0f;
        slider.maxValue = 180f;
        slider.wholeNumbers = false;
    }

    private void SyncDirectAngleSliderValue()
    {
        if (!useArmUIPlane || !armModeManipulate)
        {
            return;
        }

        if (!directAngleButton.isOn)
        {
            return;
        }

        if (armConfirmedMotorID < 1 || armConfirmedMotorID > 12)
        {
            return;
        }

        if (!TryGetSliderValueFromMotor(armConfirmedMotorID, out float sliderValue))
        {
            return;
        }

        Slider targetSlider = GetSliderForMotor(armConfirmedMotorID);
        if (targetSlider == null)
        {
            return;
        }

        ConfigureSliderRange(targetSlider);
        if (!Mathf.Approximately(targetSlider.value, sliderValue))
        {
            targetSlider.SetValueWithoutNotify(sliderValue);
        }
    }

    private Slider GetSliderForMotor(int motorID)
    {
        if (motorID >= 1 && motorID <= 4)
        {
            return _thumbSlider;
        }

        if (motorID >= 5 && motorID <= 8)
        {
            return _indexSlider;
        }

        if (motorID >= 9 && motorID <= 12)
        {
            return _middleSlider;
        }

        return null;
    }

    private bool TryGetSliderValueFromMotor(int motorID, out float sliderValue)
    {
        sliderValue = 0f;
        if (clawModuleController == null)
        {
            return false;
        }

        Transform targetTransform = null;
        float rawAngle = 0f;
        bool descendingSegments = false;

        switch (motorID)
        {
            case 1:
                targetTransform = clawModuleController.ThumbAngle1Center;
                descendingSegments = true;
                break;
            case 2:
                targetTransform = clawModuleController.ThumbAngle2Center;
                descendingSegments = true;
                break;
            case 3:
                targetTransform = clawModuleController.ThumbAngle3Center;
                descendingSegments = false;
                break;
            case 4:
                targetTransform = clawModuleController.ThumbAngle4Center;
                descendingSegments = false;
                break;
            case 5:
                targetTransform = clawModuleController.IndexAngle1Center;
                descendingSegments = true;
                break;
            case 6:
                targetTransform = clawModuleController.IndexAngle2Center;
                descendingSegments = true;
                break;
            case 7:
                targetTransform = clawModuleController.IndexAngle3Center;
                descendingSegments = false;
                break;
            case 8:
                targetTransform = clawModuleController.IndexAngle4Center;
                descendingSegments = false;
                break;
            case 9:
                targetTransform = clawModuleController.MiddleAngle1Center;
                descendingSegments = true;
                break;
            case 10:
                targetTransform = clawModuleController.MiddleAngle2Center;
                descendingSegments = true;
                break;
            case 11:
                targetTransform = clawModuleController.MiddleAngle3Center;
                descendingSegments = false;
                break;
            case 12:
                targetTransform = clawModuleController.MiddleAngle4Center;
                descendingSegments = false;
                break;
        }

        if (targetTransform == null)
        {
            return false;
        }

        Vector3 euler = targetTransform.localRotation.eulerAngles;
        switch (motorID)
        {
            case 1:
            case 5:
            case 9:
                rawAngle = euler.y;
                break;
            case 2:
            case 6:
            case 10:
                rawAngle = euler.z;
                break;
            default:
                rawAngle = euler.x;
                break;
        }

        sliderValue = MapWrappedAngleToSliderValue(rawAngle, descendingSegments);
        return true;
    }

    private float MapWrappedAngleToSliderValue(float rawAngle, bool descendingSegments)
    {
        float angle = Mathf.Repeat(rawAngle, 360f);

        if (descendingSegments)
        {
            if (angle <= 90f)
            {
                return 90f - angle;
            }

            if (angle >= 270f)
            {
                return 450f - angle;
            }

            return angle < 180f ? 0f : 180f;
        }

        if (angle >= 270f)
        {
            return angle - 270f;
        }

        if (angle <= 90f)
        {
            return angle + 90f;
        }

        return angle < 180f ? 180f : 0f;
    }

    private void SyncMotorButtonVisibility()
    {
        for (int i = 0; i < _motorButtons.Count; i++)
        {
            ButtonBinding button = _motorButtons[i];
            if (button == null)
            {
                continue;
            }

            bool shouldHide = ShouldHideMotorButtonForActiveSlider(button.resolvedMotorID);
            SetMotorButtonVisible(button, !shouldHide);
        }

        RefreshActiveArmMotor();
        RefreshCurrentTouchedButton();
    }

    private bool ShouldHideMotorButtonForActiveSlider(int motorID)
    {
        if (_thumbSliderVisible)
        {
            return (motorID >= 1 && motorID <= 4) || motorID == ThumbPaxiniMotorID;
        }

        if (_indexSliderVisible)
        {
            return (motorID >= 5 && motorID <= 8) || motorID == IndexPaxiniMotorID;
        }

        if (_middleSliderVisible)
        {
            return (motorID >= 9 && motorID <= 12) || motorID == MiddlePaxiniMotorID;
        }

        return false;
    }

    private void SetMotorButtonVisible(ButtonBinding button, bool shouldBeVisible)
    {
        if (button == null)
        {
            return;
        }

        // Keep Arm UI hierarchy alive; hide motor buttons by disabling visuals/interactions
        // instead of SetActive(false), which can accidentally disable parent containers.
        if (button.buttonCollider != null)
        {
            button.buttonCollider.enabled = shouldBeVisible;
        }

        if (button.buttonRenderer != null)
        {
            button.buttonRenderer.enabled = shouldBeVisible;
        }

        if (button.buttonObject != null)
        {
            Collider[] colliders = button.buttonObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = shouldBeVisible;
            }

            Renderer[] renderers = button.buttonObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = shouldBeVisible;
            }
        }

        if (!shouldBeVisible && button.isTouched)
        {
            button.isTouched = false;
        }
    }

    // private void RecordArmUIHistoryIfChanged()
    // {
    //     if (armUIProxyDebug == _lastArmUIProxyDebugSnapshot)
    //     {
    //         return;
    //     }

    //     _lastArmUIProxyDebugSnapshot = armUIProxyDebug;
    //     string historyLine = "f=" + Time.frameCount + " t=" + Time.time.ToString("F3") + " " + armUIProxyDebug;
    //     _armUIHistoryEntries.Enqueue(historyLine);
    //     while (_armUIHistoryEntries.Count > 12)
    //     {
    //         _armUIHistoryEntries.Dequeue();
    //     }

    //     armUIProxyHistory = string.Join("\n", _armUIHistoryEntries.ToArray());
    // }

    private string BuildMotorIDStateSnapshot()
    {
        if (modeSwitching == null)
        {
            return "N/A";
        }

        int touchedMotorID = selectMotorCollider != null ? selectMotorCollider.currentTouchedMotorID : 0;
        int redMotorID = modeSwitching.currentRedMotorID;
        int confirmedMotorID = modeSwitching.confirmedMotorID;
        int confirmedFingertipID = modeSwitching.confirmedFingertipID;
        bool[] frozen = modeSwitching.singleMotorFrozen;
        bool thumbPaxiniOn = selectMotorCollider != null && selectMotorCollider.thumbFreezeEnabled;
        bool indexPaxiniOn = selectMotorCollider != null && selectMotorCollider.indexFreezeEnabled;
        bool middlePaxiniOn = selectMotorCollider != null && selectMotorCollider.middleFreezeEnabled;

        return BuildStateSnapshot(
            touchedMotorID,
            redMotorID,
            confirmedMotorID,
            confirmedFingertipID,
            frozen,
            thumbPaxiniOn,
            indexPaxiniOn,
            middlePaxiniOn
        );
    }

    private string BuildArmMotorIDStateSnapshot()
    {
        return BuildStateSnapshot(
            armRawTouchedMotorID,
            armCurrentRedMotorID,
            armConfirmedMotorID,
            armConfirmedFingertipID,
            armSingleMotorFrozen,
            armThumbPaxiniOn,
            armIndexPaxiniOn,
            armMiddlePaxiniOn
        );
    }

    private string BuildStateSnapshot(
        int touchedMotorID,
        int redMotorID,
        int confirmedMotorID,
        int confirmedFingertipID,
        bool[] frozen,
        bool thumbPaxiniOn,
        bool indexPaxiniOn,
        bool middlePaxiniOn)
    {
        string[] states = new string[15];
        for (int motorID = 1; motorID <= 15; motorID++)
        {
            states[motorID - 1] = motorID + ":" + BuildMotorStateToken(
                motorID,
                touchedMotorID,
                redMotorID,
                confirmedMotorID,
                confirmedFingertipID,
                frozen,
                thumbPaxiniOn,
                indexPaxiniOn,
                middlePaxiniOn);
        }

        return "[" + string.Join(",", states) + "]";
    }

    private string BuildMotorStateToken(
        int motorID,
        int touchedMotorID,
        int redMotorID,
        int confirmedMotorID,
        int confirmedFingertipID,
        bool[] frozen,
        bool thumbPaxiniOn,
        bool indexPaxiniOn,
        bool middlePaxiniOn)
    {
        string token = "0";

        if (motorID >= 1 && motorID <= 12 && frozen != null && frozen.Length >= 12 && frozen[motorID - 1])
        {
            token = "F";
        }

        if (motorID == ThumbPaxiniMotorID && thumbPaxiniOn)
        {
            token = "P";
        }
        else if (motorID == IndexPaxiniMotorID && indexPaxiniOn)
        {
            token = "P";
        }
        else if (motorID == MiddlePaxiniMotorID && middlePaxiniOn)
        {
            token = "P";
        }

        if (confirmedFingertipID == motorID)
        {
            token = "T";
        }

        if (redMotorID == motorID)
        {
            token = "R";
        }

        if (confirmedMotorID == motorID)
        {
            token = "C";
        }

        if (touchedMotorID == motorID)
        {
            token = token == "0" ? "H" : token + "H";
        }

        return token;
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

        if (IsArmModeButton(button))
        {
            if (!button.isTouched)
            {
                button.isTouched = true;
                currentTouchedButton = button.buttonName;
                currentTouchedCollider = other.name;
                interactionDebug = "Touch enter: " + button.buttonName + " (" + currentTouchedCollider + ")";
                button.onEnter?.Invoke();
            }

            SetArmModeButtonSelection(button);
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

        if (IsArmModeButton(button))
        {
            if (button.isTouched)
            {
                button.isTouched = false;
                interactionDebug = "Touch exit: " + button.buttonName + " (" + other.name + ")";
                button.onExit?.Invoke();
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