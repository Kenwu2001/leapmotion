using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Text;

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

    public enum ArmUISliderMode
    {
        DirectAngle,
        MinMaxAngle,
        OneMotorDirectAngle
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

    [Header("Arm UI Buttons (Legacy)")]
    [Tooltip("Legacy plane mode button. Runtime mode now comes from sliderMode in inspector.")]
    public ButtonBinding directAngleButton = new ButtonBinding { buttonName = "Direct Angle Button" };
    [Tooltip("Legacy plane mode button. Runtime mode now comes from sliderMode in inspector.")]
    public ButtonBinding maxMinAngleButton = new ButtonBinding { buttonName = "Max Min Angle Button" };

    [Header("Arm UI Slider Mode")]
    [Tooltip("Inspector-driven slider mode. Plane mode buttons are disabled.")]
    public ArmUISliderMode sliderMode = ArmUISliderMode.OneMotorDirectAngle;

    [Header("Arm Sliders")]
    public GameObject thumbSliderObject;
    public GameObject indexSliderObject;
    public GameObject middleSliderObject;
    public GameObject thumbMaxSliderObject;
    public GameObject thumbMinSliderObject;
    public GameObject indexMaxSliderObject;
    public GameObject indexMinSliderObject;
    public GameObject middleMaxSliderObject;
    public GameObject middleMinSliderObject;
    public GameObject thumbExtensionSliderObject;
    public GameObject indexExtensionSliderObject;
    public GameObject middleExtensionSliderObject;

    [Header("Max/Min Slider Segment Mapping")]
    [Tooltip("Upper normalized bound for Max slider segment. 0.4 means 0%-40%.")]
    [Range(0.05f, 0.95f)] public float maxSliderUpperNormalized = 0.4f;
    [Tooltip("Lower normalized bound for Min slider segment. 0.6 means 60%-100%.")]
    [Range(0.05f, 0.95f)] public float minSliderLowerNormalized = 0.6f;

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

    [Header("Arm UI Plane Visual Root")]
    [Tooltip("Optional external ArmUIPlane root. When useArmUIPlane is false, this root will be moved far away instead of being deactivated.")]
    public GameObject armUIPlaneVisualRoot;
    [Tooltip("Local-space offset applied to armUIPlaneVisualRoot while useArmUIPlane is false.")]
    public Vector3 armUIPlaneHiddenOffset = new Vector3(100f, 100f, 100f);

    [Header("Arm UI Integration")]
    public ModeSwitching modeSwitching;
    public SelectMotorCollider selectMotorCollider;
    public ClawModuleController clawModuleController;
    public ArmUIPlaneCollider armUIPlaneCollider;

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

    [Header("Debug History")]
    [Tooltip("Enable/disable ArmUI debug history recording.")]
    public bool logArmUIDebugHistory = true;
    [Tooltip("How many recent history lines are kept.")]
    [Range(10, 400)] public int armUIDebugHistoryMaxLines = 120;
    [TextArea(10, 36)] public string armUIDebugHistory = "N/A";

    private readonly List<ButtonBinding> _motorButtons = new List<ButtonBinding>();
    private readonly Queue<string> _armUIHistoryEntries = new Queue<string>();
    private string _lastHistoryStateSummary = string.Empty;
    private bool _thumbSliderVisible;
    private bool _indexSliderVisible;
    private bool _middleSliderVisible;
    private bool _thumbMaxSliderVisible;
    private bool _thumbMinSliderVisible;
    private bool _indexMaxSliderVisible;
    private bool _indexMinSliderVisible;
    private bool _middleMaxSliderVisible;
    private bool _middleMinSliderVisible;
    private bool _thumbExtensionSliderVisible;
    private bool _indexExtensionSliderVisible;
    private bool _middleExtensionSliderVisible;
    private bool _lastArmModeManipulate;
    private Slider _thumbSlider;
    private Slider _indexSlider;
    private Slider _middleSlider;
    private Slider _thumbMaxSlider;
    private Slider _thumbMinSlider;
    private Slider _indexMaxSlider;
    private Slider _indexMinSlider;
    private Slider _middleMaxSlider;
    private Slider _middleMinSlider;
    private Slider _thumbExtensionSlider;
    private Slider _indexExtensionSlider;
    private Slider _middleExtensionSlider;
    private GameObject _cachedArmUIPlaneVisualRoot;
    private bool _armUIPlaneOriginalPositionCached;
    private Vector3 _armUIPlaneOriginalLocalPosition;
    private Quaternion _armUIPlaneOriginalLocalRotation;
    private Vector3 _armUIPlaneOriginalLocalScale;
    private bool _armUIPlaneOffsetApplied;
    private bool _wasUseArmUIPlaneLastFrame;
    private bool _legacyModeButtonsHidden;

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

        if (armUIPlaneCollider == null)
        {
            armUIPlaneCollider = FindObjectOfType<ArmUIPlaneCollider>();
        }

        CacheAndConfigureSliders();
        BindSliderEvents();

        BuildButtonList();
        ConfigureArmModeButtonDefaults();
        InitializeButton(enterArmUIPlaneButton);
        InitializeButton(directAngleButton);
        InitializeButton(maxMinAngleButton);
        HideLegacyArmModeButtons();
        for (int i = 0; i < _motorButtons.Count; i++)
        {
            InitializeButton(_motorButtons[i]);
        }
    }

    private void Update()
    {
        // Arm UI enable state is fully driven by arm-area touch state.
        useArmUIPlane = armUIPlaneCollider != null && armUIPlaneCollider.inArmUIArea;
        bool justEnteredArmUIArea = useArmUIPlane && !_wasUseArmUIPlaneLastFrame;

        if (justEnteredArmUIArea)
        {
            AppendArmUIDebugHistory("AREA enter ArmUI area");
        }
        else if (!useArmUIPlane && _wasUseArmUIPlaneLastFrame)
        {
            AppendArmUIDebugHistory("AREA exit ArmUI area");
        }

        SyncArmUIPlaneVisualRootPosition(!useArmUIPlane);

        if (!useArmUIPlane)
        {
            SyncSliderVisibility();
            SyncMotorButtonVisibility();
            SyncMotorButtonColors();
            _wasUseArmUIPlaneLastFrame = useArmUIPlane;
            return;
        }

        if (modeSwitching != null)
        {
            armRawTouchedMotorID = GetRawTouchedArmMotorID();
            // Mode transitions are driven by enterArmUIPlaneButton touch state.
            modeSwitching.ReceiveArmUIInput(enterArmUIPlaneButton.isTouched, armRawTouchedMotorID);
            // Avoid one-frame flicker from stale proxy state on the inArmUIArea rising edge.
            if (!justEnteredArmUIArea)
            {
                armModeSelect = modeSwitching.armUIProxyModeSelect;
                armModeManipulate = modeSwitching.armUIProxyModeManipulate;
            }
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

            if (armConfirmedMotorID >= 1 && armConfirmedMotorID <= 12 && armSingleMotorFrozen[armConfirmedMotorID - 1])
            {
                armModeManipulate = false;
                if (!enterArmUIPlaneButton.isTouched)
                {
                    armModeSelect = true;
                }
            }

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
            RecordArmUIHistoryIfChanged();
        }

        SyncSliderVisibility();
        SyncMotorButtonVisibility();
        SyncMotorButtonColors();
        SyncLegacyModeButtonStateFromInspector();
        HideLegacyArmModeButtons();
        SyncDirectAngleSliderValue();
        SyncMaxMinSliderValue();
        _lastArmModeManipulate = armModeManipulate;
        _wasUseArmUIPlaneLastFrame = useArmUIPlane;
    }

    [ContextMenu("ArmUI Debug/Copy Payload To Clipboard")]
    private void CopyArmUIDebugPayloadToClipboard()
    {
        string payload = BuildArmUIDebugPayload();
        GUIUtility.systemCopyBuffer = payload;
        AppendArmUIDebugHistory("ACTION copied ArmUI debug payload to clipboard");
    }

    [ContextMenu("ArmUI Debug/Clear History")]
    private void ClearArmUIDebugHistory()
    {
        _armUIHistoryEntries.Clear();
        armUIDebugHistory = "N/A";
        _lastHistoryStateSummary = string.Empty;
    }

    public string BuildArmUIDebugPayload()
    {
        StringBuilder builder = new StringBuilder(2048);
        builder.AppendLine("=== ArmUI Debug Payload ===");
        builder.AppendLine("frame=" + Time.frameCount + " time=" + Time.time.ToString("F3"));
        builder.AppendLine("interaction=" + interactionDebug);
        builder.AppendLine("proxy=" + armUIProxyDebug);
        builder.AppendLine("currentTouchedButton=" + currentTouchedButton + " currentTouchedCollider=" + currentTouchedCollider);
        builder.AppendLine("history:");
        builder.AppendLine(armUIDebugHistory);
        return builder.ToString();
    }

    private void SyncArmUIPlaneVisualRootPosition(bool shouldHide)
    {
        if (armUIPlaneVisualRoot == null)
        {
            return;
        }

        if (_cachedArmUIPlaneVisualRoot != armUIPlaneVisualRoot)
        {
            _cachedArmUIPlaneVisualRoot = armUIPlaneVisualRoot;
            _armUIPlaneOriginalPositionCached = false;
            _armUIPlaneOffsetApplied = false;
        }

        Transform rootTransform = armUIPlaneVisualRoot.transform;

        if (!_armUIPlaneOriginalPositionCached || (!shouldHide && !_armUIPlaneOffsetApplied))
        {
            _armUIPlaneOriginalLocalPosition = rootTransform.localPosition;
            _armUIPlaneOriginalLocalRotation = rootTransform.localRotation;
            _armUIPlaneOriginalLocalScale = rootTransform.localScale;
            _armUIPlaneOriginalPositionCached = true;
        }

        if (shouldHide)
        {
            if (_armUIPlaneOffsetApplied)
            {
                return;
            }

            rootTransform.localPosition = _armUIPlaneOriginalLocalPosition + armUIPlaneHiddenOffset;
            _armUIPlaneOffsetApplied = true;
            return;
        }

        if (_armUIPlaneOffsetApplied)
        {
            rootTransform.localPosition = _armUIPlaneOriginalLocalPosition;
            rootTransform.localRotation = _armUIPlaneOriginalLocalRotation;
            rootTransform.localScale = _armUIPlaneOriginalLocalScale;
            _armUIPlaneOffsetApplied = false;
        }
    }

    private void OnDestroy()
    {
        UnbindSliderEvents();
    }

    private void ConfigureArmModeButtonDefaults()
    {
        SyncLegacyModeButtonStateFromInspector();
    }

    private void SyncLegacyModeButtonStateFromInspector()
    {
        if (directAngleButton == null || maxMinAngleButton == null)
        {
            return;
        }

        if (sliderMode == ArmUISliderMode.MinMaxAngle)
        {
            directAngleButton.isOn = false;
            maxMinAngleButton.isOn = true;
        }
        else
        {
            directAngleButton.isOn = true;
            maxMinAngleButton.isOn = false;
        }

        directAngleButton.ApplyCurrentColor();
        maxMinAngleButton.ApplyCurrentColor();
    }

    private void HideLegacyArmModeButtons()
    {
        if (_legacyModeButtonsHidden)
        {
            return;
        }

        DisableLegacyModeButton(directAngleButton);
        DisableLegacyModeButton(maxMinAngleButton);
        _legacyModeButtonsHidden = true;
    }

    private void DisableLegacyModeButton(ButtonBinding button)
    {
        if (button == null)
        {
            return;
        }

        button.isTouched = false;

        if (button.buttonCollider != null)
        {
            button.buttonCollider.enabled = false;
        }

        if (button.buttonRenderer != null)
        {
            button.buttonRenderer.enabled = false;
        }

        if (button.buttonObject != null)
        {
            Collider[] colliders = button.buttonObject.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Renderer[] renderers = button.buttonObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }
    }

    private bool IsArmModeButton(ButtonBinding button)
    {
        return button == directAngleButton || button == maxMinAngleButton;
    }

    private void SyncSliderVisibility()
    {
        bool showThumb = _thumbSliderVisible;
        bool showIndex = _indexSliderVisible;
        bool showMiddle = _middleSliderVisible;
        bool showThumbMax = _thumbMaxSliderVisible;
        bool showThumbMin = _thumbMinSliderVisible;
        bool showIndexMax = _indexMaxSliderVisible;
        bool showIndexMin = _indexMinSliderVisible;
        bool showMiddleMax = _middleMaxSliderVisible;
        bool showMiddleMin = _middleMinSliderVisible;
        bool showThumbExtension = _thumbExtensionSliderVisible;
        bool showIndexExtension = _indexExtensionSliderVisible;
        bool showMiddleExtension = _middleExtensionSliderVisible;

        if (!useArmUIPlane)
        {
            showThumb = false;
            showIndex = false;
            showMiddle = false;
            showThumbMax = false;
            showThumbMin = false;
            showIndexMax = false;
            showIndexMin = false;
            showMiddleMax = false;
            showMiddleMin = false;
            showThumbExtension = false;
            showIndexExtension = false;
            showMiddleExtension = false;
        }
        else if (armModeManipulate)
        {
            int confirmedMotorID = armConfirmedMotorID;
            showThumb = false;
            showIndex = false;
            showMiddle = false;

            showThumbMax = false;
            showThumbMin = false;
            showIndexMax = false;
            showIndexMin = false;
            showMiddleMax = false;
            showMiddleMin = false;
            showThumbExtension = false;
            showIndexExtension = false;
            showMiddleExtension = false;

            if (IsDirectAngleModeActive())
            {
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
            else if (IsMaxMinAngleModeActive())
            {
                if (confirmedMotorID == 1 || confirmedMotorID == 2)
                {
                    showThumbMax = true;
                    showThumbMin = true;
                }
                else if (confirmedMotorID == 5 || confirmedMotorID == 6)
                {
                    showIndexMax = true;
                    showIndexMin = true;
                }
                else if (confirmedMotorID == 9 || confirmedMotorID == 10)
                {
                    showMiddleMax = true;
                    showMiddleMin = true;
                }
                else if (confirmedMotorID == 3 || confirmedMotorID == 4)
                {
                    showThumbExtension = true;
                }
                else if (confirmedMotorID == 7 || confirmedMotorID == 8)
                {
                    showIndexExtension = true;
                }
                else if (confirmedMotorID == 11 || confirmedMotorID == 12)
                {
                    showMiddleExtension = true;
                }
            }
        }
        else if (armModeSelect)
        {
            // Hide sliders when manipulate transitions back to select.
            if (_lastArmModeManipulate ||
                _thumbSliderVisible || _indexSliderVisible || _middleSliderVisible ||
                _thumbMaxSliderVisible || _thumbMinSliderVisible ||
                _indexMaxSliderVisible || _indexMinSliderVisible ||
                _middleMaxSliderVisible || _middleMinSliderVisible)
            {
                showThumb = false;
                showIndex = false;
                showMiddle = false;
                showThumbMax = false;
                showThumbMin = false;
                showIndexMax = false;
                showIndexMin = false;
                showMiddleMax = false;
                showMiddleMin = false;
                showThumbExtension = false;
                showIndexExtension = false;
                showMiddleExtension = false;
            }
        }

        SetSliderActive(thumbSliderObject, showThumb, ref _thumbSliderVisible);
        SetSliderActive(indexSliderObject, showIndex, ref _indexSliderVisible);
        SetSliderActive(middleSliderObject, showMiddle, ref _middleSliderVisible);
        SetSliderActive(thumbMaxSliderObject, showThumbMax, ref _thumbMaxSliderVisible);
        SetSliderActive(thumbMinSliderObject, showThumbMin, ref _thumbMinSliderVisible);
        SetSliderActive(indexMaxSliderObject, showIndexMax, ref _indexMaxSliderVisible);
        SetSliderActive(indexMinSliderObject, showIndexMin, ref _indexMinSliderVisible);
        SetSliderActive(middleMaxSliderObject, showMiddleMax, ref _middleMaxSliderVisible);
        SetSliderActive(middleMinSliderObject, showMiddleMin, ref _middleMinSliderVisible);
        SetSliderActive(thumbExtensionSliderObject, showThumbExtension, ref _thumbExtensionSliderVisible);
        SetSliderActive(indexExtensionSliderObject, showIndexExtension, ref _indexExtensionSliderVisible);
        SetSliderActive(middleExtensionSliderObject, showMiddleExtension, ref _middleExtensionSliderVisible);
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
        _thumbMaxSlider = FindSliderComponent(thumbMaxSliderObject);
        _thumbMinSlider = FindSliderComponent(thumbMinSliderObject);
        _indexMaxSlider = FindSliderComponent(indexMaxSliderObject);
        _indexMinSlider = FindSliderComponent(indexMinSliderObject);
        _middleMaxSlider = FindSliderComponent(middleMaxSliderObject);
        _middleMinSlider = FindSliderComponent(middleMinSliderObject);
        _thumbExtensionSlider = FindSliderComponent(thumbExtensionSliderObject);
        _indexExtensionSlider = FindSliderComponent(indexExtensionSliderObject);
        _middleExtensionSlider = FindSliderComponent(middleExtensionSliderObject);

        ConfigureSliderRange(_thumbSlider);
        ConfigureSliderRange(_indexSlider);
        ConfigureSliderRange(_middleSlider);
        ConfigureSliderRange(_thumbMaxSlider);
        ConfigureSliderRange(_thumbMinSlider);
        ConfigureSliderRange(_indexMaxSlider);
        ConfigureSliderRange(_indexMinSlider);
        ConfigureSliderRange(_middleMaxSlider);
        ConfigureSliderRange(_middleMinSlider);
        ConfigureSliderRange(_thumbExtensionSlider);
        ConfigureSliderRange(_indexExtensionSlider);
        ConfigureSliderRange(_middleExtensionSlider);
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

    private void BindSliderEvents()
    {
        BindSliderEvent(_thumbSlider, HandleThumbSliderValueChanged);
        BindSliderEvent(_indexSlider, HandleIndexSliderValueChanged);
        BindSliderEvent(_middleSlider, HandleMiddleSliderValueChanged);
        BindSliderEvent(_thumbMaxSlider, HandleThumbMaxSliderValueChanged);
        BindSliderEvent(_thumbMinSlider, HandleThumbMinSliderValueChanged);
        BindSliderEvent(_indexMaxSlider, HandleIndexMaxSliderValueChanged);
        BindSliderEvent(_indexMinSlider, HandleIndexMinSliderValueChanged);
        BindSliderEvent(_middleMaxSlider, HandleMiddleMaxSliderValueChanged);
        BindSliderEvent(_middleMinSlider, HandleMiddleMinSliderValueChanged);
        BindSliderEvent(_thumbExtensionSlider, HandleThumbExtensionSliderValueChanged);
        BindSliderEvent(_indexExtensionSlider, HandleIndexExtensionSliderValueChanged);
        BindSliderEvent(_middleExtensionSlider, HandleMiddleExtensionSliderValueChanged);
    }

    private void UnbindSliderEvents()
    {
        UnbindSliderEvent(_thumbSlider, HandleThumbSliderValueChanged);
        UnbindSliderEvent(_indexSlider, HandleIndexSliderValueChanged);
        UnbindSliderEvent(_middleSlider, HandleMiddleSliderValueChanged);
        UnbindSliderEvent(_thumbMaxSlider, HandleThumbMaxSliderValueChanged);
        UnbindSliderEvent(_thumbMinSlider, HandleThumbMinSliderValueChanged);
        UnbindSliderEvent(_indexMaxSlider, HandleIndexMaxSliderValueChanged);
        UnbindSliderEvent(_indexMinSlider, HandleIndexMinSliderValueChanged);
        UnbindSliderEvent(_middleMaxSlider, HandleMiddleMaxSliderValueChanged);
        UnbindSliderEvent(_middleMinSlider, HandleMiddleMinSliderValueChanged);
        UnbindSliderEvent(_thumbExtensionSlider, HandleThumbExtensionSliderValueChanged);
        UnbindSliderEvent(_indexExtensionSlider, HandleIndexExtensionSliderValueChanged);
        UnbindSliderEvent(_middleExtensionSlider, HandleMiddleExtensionSliderValueChanged);
    }

    private void BindSliderEvent(Slider slider, UnityAction<float> handler)
    {
        if (slider == null || handler == null)
        {
            return;
        }

        slider.onValueChanged.RemoveListener(handler);
        slider.onValueChanged.AddListener(handler);
    }

    private void UnbindSliderEvent(Slider slider, UnityAction<float> handler)
    {
        if (slider == null || handler == null)
        {
            return;
        }

        slider.onValueChanged.RemoveListener(handler);
    }

    public bool IsDirectAngleModeActive()
    {
        return useArmUIPlane && armModeManipulate
            && (sliderMode == ArmUISliderMode.DirectAngle || sliderMode == ArmUISliderMode.OneMotorDirectAngle);
    }

    public bool IsMaxMinAngleModeActive()
    {
        return useArmUIPlane && armModeManipulate && sliderMode == ArmUISliderMode.MinMaxAngle;
    }

    public bool IsOneMotorDirectAngleModeActive()
    {
        return useArmUIPlane && armModeManipulate && sliderMode == ArmUISliderMode.OneMotorDirectAngle;
    }

    public bool IsWholeHandDirectAngleModeActive()
    {
        return useArmUIPlane && armModeManipulate && sliderMode == ArmUISliderMode.DirectAngle;
    }

    public bool ShouldAutoFreezeConfirmedMotorOnManipulateExit()
    {
        return useArmUIPlane && sliderMode == ArmUISliderMode.OneMotorDirectAngle;
    }

    public void SetSliderMode(ArmUISliderMode mode)
    {
        sliderMode = mode;
        SyncLegacyModeButtonStateFromInspector();
    }

    public bool IsDirectAngleMotorTarget(int motorID)
    {
        return IsDirectAngleModeActive() && armConfirmedMotorID == motorID;
    }

    public float MaxSliderUpperNormalizedForInspector
    {
        get
        {
            GetMaxMinSliderSegmentBoundsNormalized(out float maxUpper, out _);
            return maxUpper;
        }
    }

    public float MinSliderLowerNormalizedForInspector
    {
        get
        {
            GetMaxMinSliderSegmentBoundsNormalized(out _, out float minLower);
            return minLower;
        }
    }

    private void HandleThumbSliderValueChanged(float value)
    {
        ApplyDirectAngleSliderValue(value, 1, 4);
    }

    private void HandleIndexSliderValueChanged(float value)
    {
        ApplyDirectAngleSliderValue(value, 5, 8);
    }

    private void HandleMiddleSliderValueChanged(float value)
    {
        ApplyDirectAngleSliderValue(value, 9, 12);
    }

    private void HandleThumbMaxSliderValueChanged(float value)
    {
        ApplyMaxMinSliderValue(value, true);
    }

    private void HandleThumbMinSliderValueChanged(float value)
    {
        ApplyMaxMinSliderValue(value, false);
    }

    private void HandleIndexMaxSliderValueChanged(float value)
    {
        ApplyMaxMinSliderValue(value, true);
    }

    private void HandleIndexMinSliderValueChanged(float value)
    {
        ApplyMaxMinSliderValue(value, false);
    }

    private void HandleMiddleMaxSliderValueChanged(float value)
    {
        ApplyMaxMinSliderValue(value, true);
    }

    private void HandleMiddleMinSliderValueChanged(float value)
    {
        ApplyMaxMinSliderValue(value, false);
    }

    private void HandleThumbExtensionSliderValueChanged(float value)
    {
        ApplyExtensionSliderValue(value, _thumbExtensionSlider);
    }

    private void HandleIndexExtensionSliderValueChanged(float value)
    {
        ApplyExtensionSliderValue(value, _indexExtensionSlider);
    }

    private void HandleMiddleExtensionSliderValueChanged(float value)
    {
        ApplyExtensionSliderValue(value, _middleExtensionSlider);
    }

    private void ApplyDirectAngleSliderValue(float sliderValue, int minMotorID, int maxMotorID)
    {
        if (!IsDirectAngleModeActive())
        {
            return;
        }

        if (armConfirmedMotorID < minMotorID || armConfirmedMotorID > maxMotorID)
        {
            return;
        }

        ApplyDirectAngleToMotor(armConfirmedMotorID, sliderValue);
    }

    private void ApplyMaxMinSliderValue(float sliderValue, bool isMaxSlider)
    {
        if (!IsMaxMinAngleModeActive())
        {
            return;
        }

        if (clawModuleController == null)
        {
            return;
        }

        GetMaxMinSliderSegmentBoundsValues(out float maxSliderUpperValue, out float minSliderLowerValue);

        if (armConfirmedMotorID == 1)
        {
            // Motor1: Thumb Y Max/Min
            if (isMaxSlider)
            {
                EnsureThumbYMinDefaultForMaxMinControl();

                float t = Mathf.InverseLerp(0f, maxSliderUpperValue, Mathf.Clamp(sliderValue, 0f, maxSliderUpperValue));
                // Max slider segment: 0% -> -90deg, configured upper bound -> 0deg
                clawModuleController.currentThumbRotationYMax = Mathf.Lerp(-90f, 0f, t);

                Vector3 maxVec = clawModuleController.thumbGripperJoint1MaxRotationVector;
                maxVec.y = Mathf.Abs(clawModuleController.currentThumbRotationYMax) <= 0.0001f
                    ? 360f
                    : Mathf.Repeat(clawModuleController.currentThumbRotationYMax, 360f);
                clawModuleController.thumbGripperJoint1MaxRotationVector = maxVec;
                clawModuleController.maxThumbYAxisAngle = NormalizeAngleForArmUI(maxVec.y);
            }
            else
            {
                float t = Mathf.InverseLerp(minSliderLowerValue, 180f, Mathf.Clamp(sliderValue, minSliderLowerValue, 180f));
                clawModuleController.currentThumbRotationYMin = Mathf.Lerp(0f, 90f, t);

                Vector3 minVec = clawModuleController.thumbGripperJoint1MinRotationVector;
                minVec.y = Mathf.Repeat(clawModuleController.currentThumbRotationYMin, 360f);
                clawModuleController.thumbGripperJoint1MinRotationVector = minVec;
                clawModuleController.minThumbYAxisAngle = NormalizeAngleForArmUI(minVec.y);
            }
        }
        else if (armConfirmedMotorID == 2)
        {
            // Motor2: Thumb Z Max/Min
            if (isMaxSlider)
            {
                float t = Mathf.InverseLerp(0f, maxSliderUpperValue, Mathf.Clamp(sliderValue, 0f, maxSliderUpperValue));
                // Max slider segment: 0% -> -90deg, configured upper bound -> 0deg
                clawModuleController.currentThumbRotationZMax = Mathf.Lerp(-90f, 0f, t);

                Vector3 maxVec = clawModuleController.thumbGripperJoint2MaxRotationVector;
                maxVec.z = Mathf.Abs(clawModuleController.currentThumbRotationZMax) <= 0.0001f
                    ? 360f
                    : Mathf.Repeat(clawModuleController.currentThumbRotationZMax, 360f);
                clawModuleController.thumbGripperJoint2MaxRotationVector = maxVec;
                clawModuleController.maxThumbZAxisAngle = maxVec.z;
            }
            else
            {
                float t = Mathf.InverseLerp(minSliderLowerValue, 180f, Mathf.Clamp(sliderValue, minSliderLowerValue, 180f));
                clawModuleController.currentThumbRotationZMin = Mathf.Lerp(0f, 90f, t);

                Vector3 minVec = clawModuleController.thumbGripperJoint2MinRotationVector;
                minVec.z = Mathf.Repeat(clawModuleController.currentThumbRotationZMin, 360f);
                clawModuleController.thumbGripperJoint2MinRotationVector = minVec;
                clawModuleController.minThumbZAxisAngle = minVec.z;
            }

            clawModuleController.hasThumbAbductionAdjustment = true;
        }
        else if (armConfirmedMotorID == 5)
        {
            // Motor5: Index Y Max/Min
            if (isMaxSlider)
            {
                EnsureIndexYMinDefaultForMaxMinControl();

                float t = Mathf.InverseLerp(0f, maxSliderUpperValue, Mathf.Clamp(sliderValue, 0f, maxSliderUpperValue));
                clawModuleController.currentIndexRotationYMax = Mathf.Lerp(-90f, 0f, t);

                Vector3 maxVec = clawModuleController.indexGripperJoint1MaxRotationVector;
                maxVec.y = Mathf.Abs(clawModuleController.currentIndexRotationYMax) <= 0.0001f
                    ? 360f
                    : Mathf.Repeat(clawModuleController.currentIndexRotationYMax, 360f);
                clawModuleController.indexGripperJoint1MaxRotationVector = maxVec;
            }
            else
            {
                float t = Mathf.InverseLerp(minSliderLowerValue, 180f, Mathf.Clamp(sliderValue, minSliderLowerValue, 180f));
                clawModuleController.currentIndexRotationYMin = Mathf.Lerp(0f, 90f, t);

                Vector3 minVec = clawModuleController.indexGripperJoint1MinRotationVector;
                minVec.y = Mathf.Repeat(clawModuleController.currentIndexRotationYMin, 360f);
                clawModuleController.indexGripperJoint1MinRotationVector = minVec;
            }
        }
        else if (armConfirmedMotorID == 6)
        {
            // Motor6: Index Z Max/Min
            if (isMaxSlider)
            {
                float t = Mathf.InverseLerp(0f, maxSliderUpperValue, Mathf.Clamp(sliderValue, 0f, maxSliderUpperValue));
                clawModuleController.currentIndexRotationZMax = Mathf.Lerp(-90f, 0f, t);

                Vector3 maxVec = clawModuleController.indexGripperJoint2MaxRotationVector;
                maxVec.z = Mathf.Abs(clawModuleController.currentIndexRotationZMax) <= 0.0001f
                    ? 360f
                    : Mathf.Repeat(clawModuleController.currentIndexRotationZMax, 360f);
                clawModuleController.indexGripperJoint2MaxRotationVector = maxVec;
            }
            else
            {
                float t = Mathf.InverseLerp(minSliderLowerValue, 180f, Mathf.Clamp(sliderValue, minSliderLowerValue, 180f));
                clawModuleController.currentIndexRotationZMin = Mathf.Lerp(0f, 90f, t);

                Vector3 minVec = clawModuleController.indexGripperJoint2MinRotationVector;
                minVec.z = Mathf.Repeat(clawModuleController.currentIndexRotationZMin, 360f);
                clawModuleController.indexGripperJoint2MinRotationVector = minVec;
            }
        }
        else if (armConfirmedMotorID == 9)
        {
            // Motor9: Middle Y Max/Min
            if (isMaxSlider)
            {
                float t = Mathf.InverseLerp(0f, maxSliderUpperValue, Mathf.Clamp(sliderValue, 0f, maxSliderUpperValue));
                clawModuleController.currentMiddleRotationYMax = Mathf.Lerp(-90f, 0f, t);

                Vector3 maxVec = clawModuleController.middleGripperJoint1MaxRotationVector;
                maxVec.y = Mathf.Abs(clawModuleController.currentMiddleRotationYMax) <= 0.0001f
                    ? 360f
                    : Mathf.Repeat(clawModuleController.currentMiddleRotationYMax, 360f);
                clawModuleController.middleGripperJoint1MaxRotationVector = maxVec;
            }
            else
            {
                float t = Mathf.InverseLerp(minSliderLowerValue, 180f, Mathf.Clamp(sliderValue, minSliderLowerValue, 180f));
                clawModuleController.currentMiddleRotationYMin = Mathf.Lerp(0f, 90f, t);

                Vector3 minVec = clawModuleController.middleGripperJoint1MinRotationVector;
                minVec.y = Mathf.Repeat(clawModuleController.currentMiddleRotationYMin, 360f);
                clawModuleController.middleGripperJoint1MinRotationVector = minVec;
            }
        }
        else if (armConfirmedMotorID == 10)
        {
            // Motor10: Middle Z Max/Min
            if (isMaxSlider)
            {
                float t = Mathf.InverseLerp(0f, maxSliderUpperValue, Mathf.Clamp(sliderValue, 0f, maxSliderUpperValue));
                clawModuleController.currentMiddleRotationZMax = Mathf.Lerp(-90f, 0f, t);

                Vector3 maxVec = clawModuleController.middleGripperJoint2MaxRotationVector;
                maxVec.z = Mathf.Abs(clawModuleController.currentMiddleRotationZMax) <= 0.0001f
                    ? 360f
                    : Mathf.Repeat(clawModuleController.currentMiddleRotationZMax, 360f);
                clawModuleController.middleGripperJoint2MaxRotationVector = maxVec;
            }
            else
            {
                float t = Mathf.InverseLerp(minSliderLowerValue, 180f, Mathf.Clamp(sliderValue, minSliderLowerValue, 180f));
                clawModuleController.currentMiddleRotationZMin = Mathf.Lerp(0f, 90f, t);

                Vector3 minVec = clawModuleController.middleGripperJoint2MinRotationVector;
                minVec.z = Mathf.Repeat(clawModuleController.currentMiddleRotationZMin, 360f);
                clawModuleController.middleGripperJoint2MinRotationVector = minVec;
            }
        }
    }

    private void ApplyExtensionSliderValue(float sliderValue, Slider sourceSlider)
    {
        if (!IsMaxMinAngleModeActive())
        {
            return;
        }

        if (clawModuleController == null)
        {
            return;
        }

        if (sourceSlider == null)
        {
            return;
        }

        int confirmedMotorID = armConfirmedMotorID;
        if ((confirmedMotorID == 3 || confirmedMotorID == 4) && sourceSlider != _thumbExtensionSlider)
        {
            return;
        }

        if ((confirmedMotorID == 7 || confirmedMotorID == 8) && sourceSlider != _indexExtensionSlider)
        {
            return;
        }

        if ((confirmedMotorID == 11 || confirmedMotorID == 12) && sourceSlider != _middleExtensionSlider)
        {
            return;
        }

        float clampedValue = Mathf.Clamp(sliderValue, 0f, 180f);
        float extensionRotation = Mathf.Lerp(90f, -90f, clampedValue / 180f);

        switch (confirmedMotorID)
        {
            case 3:
                clawModuleController.currentThumbInnerExtensionRotationZ = extensionRotation;
                break;
            case 4:
                clawModuleController.currentThumbTipRotationZ = extensionRotation;
                break;
            case 7:
                clawModuleController.currentIndexInnerExtensionRotationZ = extensionRotation;
                break;
            case 8:
                clawModuleController.currentIndexTipRotationZ = extensionRotation;
                break;
            case 11:
                clawModuleController.currentMiddleInnerExtensionRotationZ = extensionRotation;
                break;
            case 12:
                clawModuleController.currentMiddleTipRotationZ = extensionRotation;
                break;
        }
    }

    private void EnsureThumbYMinDefaultForMaxMinControl()
    {
        if (clawModuleController == null)
        {
            return;
        }

        // Align runtime control state with the same baseline used in debug display:
        // when YMin is still 0 but min-vector indicates default 60deg, materialize 60 into YMin.
        if (!Mathf.Approximately(clawModuleController.currentThumbRotationYMin, 0f))
        {
            return;
        }

        float wrappedY = Mathf.Repeat(clawModuleController.thumbGripperJoint1MinRotationVector.y, 360f);
        bool isStillAtDefaultMin = Mathf.Abs(wrappedY - 60f) <= 0.5f;
        if (!isStillAtDefaultMin)
        {
            return;
        }

        clawModuleController.currentThumbRotationYMin = 60f;
        Vector3 minVec = clawModuleController.thumbGripperJoint1MinRotationVector;
        minVec.y = 60f;
        clawModuleController.thumbGripperJoint1MinRotationVector = minVec;
        clawModuleController.minThumbYAxisAngle = NormalizeAngleForArmUI(minVec.y);
    }

    private void EnsureIndexYMinDefaultForMaxMinControl()
    {
        if (clawModuleController == null)
        {
            return;
        }

        if (!Mathf.Approximately(clawModuleController.currentIndexRotationYMin, 0f))
        {
            return;
        }

        float wrappedY = Mathf.Repeat(clawModuleController.indexGripperJoint1MinRotationVector.y, 360f);
        bool isStillAtDefaultMin = Mathf.Abs(wrappedY - 60f) <= 0.5f;
        if (!isStillAtDefaultMin)
        {
            return;
        }

        clawModuleController.currentIndexRotationYMin = 60f;
        Vector3 minVec = clawModuleController.indexGripperJoint1MinRotationVector;
        minVec.y = 60f;
        clawModuleController.indexGripperJoint1MinRotationVector = minVec;
    }

    private void ApplyDirectAngleToMotor(int motorID, float sliderValue)
    {
        if (clawModuleController == null)
        {
            return;
        }

        Transform targetTransform = GetMotorTransform(motorID);
        if (targetTransform == null)
        {
            return;
        }

        bool descendingSegments = IsDescendingSegmentMotor(motorID);
        float rawAngle = MapSliderValueToWrappedAngle(sliderValue, descendingSegments);
        Vector3 euler = targetTransform.localRotation.eulerAngles;

        switch (motorID)
        {
            case 1:
            case 5:
            case 9:
                euler.y = rawAngle;
                break;
            case 2:
            case 6:
            case 10:
                euler.z = rawAngle;
                break;
            default:
                euler.x = rawAngle;
                break;
        }

        targetTransform.localRotation = Quaternion.Euler(euler.x, euler.y, euler.z);
    }

    private bool IsDescendingSegmentMotor(int motorID)
    {
        return motorID == 1 || motorID == 2 || motorID == 5 || motorID == 6 || motorID == 9 || motorID == 10;
    }

    private Transform GetMotorTransform(int motorID)
    {
        if (clawModuleController == null)
        {
            return null;
        }

        switch (motorID)
        {
            case 1:
                return clawModuleController.ThumbAngle1Center;
            case 2:
                return clawModuleController.ThumbAngle2Center;
            case 3:
                return clawModuleController.ThumbAngle3Center;
            case 4:
                return clawModuleController.ThumbAngle4Center;
            case 5:
                return clawModuleController.IndexAngle1Center;
            case 6:
                return clawModuleController.IndexAngle2Center;
            case 7:
                return clawModuleController.IndexAngle3Center;
            case 8:
                return clawModuleController.IndexAngle4Center;
            case 9:
                return clawModuleController.MiddleAngle1Center;
            case 10:
                return clawModuleController.MiddleAngle2Center;
            case 11:
                return clawModuleController.MiddleAngle3Center;
            case 12:
                return clawModuleController.MiddleAngle4Center;
            default:
                return null;
        }
    }

    private float MapSliderValueToWrappedAngle(float sliderValue, bool descendingSegments)
    {
        float value = Mathf.Clamp(sliderValue, 0f, 180f);

        if (descendingSegments)
        {
            if (value <= 90f)
            {
                return 90f - value;
            }

            return 450f - value;
        }

        if (value <= 90f)
        {
            return 270f + value;
        }

        return value - 90f;
    }

    private void SyncDirectAngleSliderValue()
    {
        if (!IsDirectAngleModeActive())
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

    private void SyncMaxMinSliderValue()
    {
        if (!IsMaxMinAngleModeActive() || clawModuleController == null)
        {
            return;
        }

        GetMaxMinSliderSegmentBoundsValues(out float maxSliderUpperValue, out float minSliderLowerValue);

        Slider maxSlider;
        Slider minSlider;
        float maxAngle;
        float minAngle;
        Slider extensionSlider;
        float extensionAngle;

        switch (armConfirmedMotorID)
        {
            case 1:
                maxSlider = _thumbMaxSlider;
                minSlider = _thumbMinSlider;
                maxAngle = GetThumbYMaxForUI();
                minAngle = GetThumbYMinDisplayValue();
                extensionSlider = null;
                extensionAngle = 0f;
                break;
            case 2:
                maxSlider = _thumbMaxSlider;
                minSlider = _thumbMinSlider;
                maxAngle = GetThumbZMaxForUI();
                minAngle = GetThumbZMinForUI();
                extensionSlider = null;
                extensionAngle = 0f;
                break;
            case 3:
                maxSlider = null;
                minSlider = null;
                maxAngle = 0f;
                minAngle = 0f;
                extensionSlider = _thumbExtensionSlider;
                extensionAngle = GetThumbInnerExtensionForUI();
                break;
            case 4:
                maxSlider = null;
                minSlider = null;
                maxAngle = 0f;
                minAngle = 0f;
                extensionSlider = _thumbExtensionSlider;
                extensionAngle = GetThumbTipExtensionForUI();
                break;
            case 5:
                maxSlider = _indexMaxSlider;
                minSlider = _indexMinSlider;
                maxAngle = GetIndexYMaxForUI();
                minAngle = GetIndexYMinDisplayValue();
                extensionSlider = null;
                extensionAngle = 0f;
                break;
            case 6:
                maxSlider = _indexMaxSlider;
                minSlider = _indexMinSlider;
                maxAngle = GetIndexZMaxForUI();
                minAngle = GetIndexZMinForUI();
                extensionSlider = null;
                extensionAngle = 0f;
                break;
            case 7:
                maxSlider = null;
                minSlider = null;
                maxAngle = 0f;
                minAngle = 0f;
                extensionSlider = _indexExtensionSlider;
                extensionAngle = GetIndexInnerExtensionForUI();
                break;
            case 8:
                maxSlider = null;
                minSlider = null;
                maxAngle = 0f;
                minAngle = 0f;
                extensionSlider = _indexExtensionSlider;
                extensionAngle = GetIndexTipExtensionForUI();
                break;
            case 9:
                maxSlider = _middleMaxSlider;
                minSlider = _middleMinSlider;
                maxAngle = GetMiddleYMaxForUI();
                minAngle = GetMiddleYMinForUI();
                extensionSlider = null;
                extensionAngle = 0f;
                break;
            case 10:
                maxSlider = _middleMaxSlider;
                minSlider = _middleMinSlider;
                maxAngle = GetMiddleZMaxForUI();
                minAngle = GetMiddleZMinForUI();
                extensionSlider = null;
                extensionAngle = 0f;
                break;
            case 11:
                maxSlider = null;
                minSlider = null;
                maxAngle = 0f;
                minAngle = 0f;
                extensionSlider = _middleExtensionSlider;
                extensionAngle = GetMiddleInnerExtensionForUI();
                break;
            case 12:
                maxSlider = null;
                minSlider = null;
                maxAngle = 0f;
                minAngle = 0f;
                extensionSlider = _middleExtensionSlider;
                extensionAngle = GetMiddleTipExtensionForUI();
                break;
            default:
                return;
        }

        if (extensionSlider != null)
        {
            ConfigureSliderRange(extensionSlider);
            float extensionSliderValue = Mathf.Lerp(0f, 180f, Mathf.InverseLerp(90f, -90f, extensionAngle));
            if (!Mathf.Approximately(extensionSlider.value, extensionSliderValue))
            {
                extensionSlider.SetValueWithoutNotify(extensionSliderValue);
            }

            return;
        }

        if (maxSlider == null || minSlider == null)
        {
            return;
        }

        ConfigureSliderRange(maxSlider);
        ConfigureSliderRange(minSlider);

        float maxSliderValue = Mathf.Lerp(0f, maxSliderUpperValue, Mathf.InverseLerp(-90f, 0f, maxAngle));
        float minSliderValue = Mathf.Lerp(minSliderLowerValue, 180f, Mathf.InverseLerp(0f, 90f, minAngle));

        if (!Mathf.Approximately(maxSlider.value, maxSliderValue))
        {
            maxSlider.SetValueWithoutNotify(maxSliderValue);
        }

        if (!Mathf.Approximately(minSlider.value, minSliderValue))
        {
            minSlider.SetValueWithoutNotify(minSliderValue);
        }
    }

    private float GetThumbYMinDisplayValue()
    {
        if (!Mathf.Approximately(clawModuleController.currentThumbRotationYMin, 0f))
        {
            return Mathf.Clamp(clawModuleController.currentThumbRotationYMin, 0f, 90f);
        }

        float wrappedY = Mathf.Repeat(clawModuleController.thumbGripperJoint1MinRotationVector.y, 360f);
        bool isStillAtDefaultMin = Mathf.Abs(wrappedY - 60f) <= 0.5f;
        return isStillAtDefaultMin ? 60f : Mathf.Clamp(clawModuleController.currentThumbRotationYMin, 0f, 90f);
    }

    private float GetThumbYMaxForUI()
    {
        return Mathf.Clamp(clawModuleController.currentThumbRotationYMax, -90f, 0f);
    }

    private float GetThumbZMaxForUI()
    {
        return Mathf.Clamp(clawModuleController.currentThumbRotationZMax, -90f, 0f);
    }

    private float GetThumbZMinForUI()
    {
        return Mathf.Clamp(clawModuleController.currentThumbRotationZMin, 0f, 90f);
    }

    private float GetIndexYMinDisplayValue()
    {
        if (!Mathf.Approximately(clawModuleController.currentIndexRotationYMin, 0f))
        {
            return Mathf.Clamp(clawModuleController.currentIndexRotationYMin, 0f, 90f);
        }

        float wrappedY = Mathf.Repeat(clawModuleController.indexGripperJoint1MinRotationVector.y, 360f);
        bool isStillAtDefaultMin = Mathf.Abs(wrappedY - 60f) <= 0.5f;
        return isStillAtDefaultMin ? 60f : Mathf.Clamp(clawModuleController.currentIndexRotationYMin, 0f, 90f);
    }

    private float GetIndexYMaxForUI()
    {
        return Mathf.Clamp(clawModuleController.currentIndexRotationYMax, -90f, 0f);
    }

    private float GetIndexZMaxForUI()
    {
        return Mathf.Clamp(clawModuleController.currentIndexRotationZMax, -90f, 0f);
    }

    private float GetIndexZMinForUI()
    {
        return Mathf.Clamp(clawModuleController.currentIndexRotationZMin, 0f, 90f);
    }

    private float GetMiddleYMaxForUI()
    {
        return Mathf.Clamp(clawModuleController.currentMiddleRotationYMax, -90f, 0f);
    }

    private float GetMiddleYMinForUI()
    {
        return Mathf.Clamp(clawModuleController.currentMiddleRotationYMin, 0f, 90f);
    }

    private float GetMiddleZMaxForUI()
    {
        return Mathf.Clamp(clawModuleController.currentMiddleRotationZMax, -90f, 0f);
    }

    private float GetMiddleZMinForUI()
    {
        return Mathf.Clamp(clawModuleController.currentMiddleRotationZMin, 0f, 90f);
    }

    private float GetThumbInnerExtensionForUI()
    {
        return Mathf.Clamp(clawModuleController.currentThumbInnerExtensionRotationZ, -90f, 90f);
    }

    private float GetThumbTipExtensionForUI()
    {
        return Mathf.Clamp(clawModuleController.currentThumbTipRotationZ, -90f, 90f);
    }

    private float GetIndexInnerExtensionForUI()
    {
        return Mathf.Clamp(clawModuleController.currentIndexInnerExtensionRotationZ, -90f, 90f);
    }

    private float GetIndexTipExtensionForUI()
    {
        return Mathf.Clamp(clawModuleController.currentIndexTipRotationZ, -90f, 90f);
    }

    private float GetMiddleInnerExtensionForUI()
    {
        return Mathf.Clamp(clawModuleController.currentMiddleInnerExtensionRotationZ, -90f, 90f);
    }

    private float GetMiddleTipExtensionForUI()
    {
        return Mathf.Clamp(clawModuleController.currentMiddleTipRotationZ, -90f, 90f);
    }

    private void GetMaxMinSliderSegmentBoundsNormalized(out float maxUpperNormalized, out float minLowerNormalized)
    {
        float maxUpper = Mathf.Clamp(maxSliderUpperNormalized, 0f, 1f);
        float minLower = Mathf.Clamp(minSliderLowerNormalized, 0f, 1f);

        const float minGap = 0.01f;
        if (minLower <= maxUpper + minGap)
        {
            float center = Mathf.Clamp01((maxUpper + minLower) * 0.5f);
            maxUpper = Mathf.Clamp01(center - (minGap * 0.5f));
            minLower = Mathf.Clamp01(center + (minGap * 0.5f));

            if (minLower <= maxUpper)
            {
                maxUpper = Mathf.Clamp01(maxUpper - minGap);
                minLower = Mathf.Clamp01(maxUpper + minGap);
            }
        }

        maxUpperNormalized = maxUpper;
        minLowerNormalized = minLower;
    }

    private void GetMaxMinSliderSegmentBoundsValues(out float maxSliderUpperValue, out float minSliderLowerValue)
    {
        GetMaxMinSliderSegmentBoundsNormalized(out float maxUpperNormalized, out float minLowerNormalized);
        maxSliderUpperValue = Mathf.Lerp(0f, 180f, maxUpperNormalized);
        minSliderLowerValue = Mathf.Lerp(0f, 180f, minLowerNormalized);
    }

    private float NormalizeAngleForArmUI(float angle)
    {
        return angle >= 300f ? angle - 360f : angle;
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
        if (_thumbSliderVisible || _thumbMaxSliderVisible || _thumbMinSliderVisible || _thumbExtensionSliderVisible)
        {
            return (motorID >= 1 && motorID <= 4) || motorID == ThumbPaxiniMotorID;
        }

        if (_indexSliderVisible || _indexMaxSliderVisible || _indexMinSliderVisible || _indexExtensionSliderVisible)
        {
            return (motorID >= 5 && motorID <= 8) || motorID == IndexPaxiniMotorID;
        }

        if (_middleSliderVisible || _middleMaxSliderVisible || _middleMinSliderVisible || _middleExtensionSliderVisible)
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

    private void RecordArmUIHistoryIfChanged()
    {
        if (!logArmUIDebugHistory)
        {
            return;
        }

        string stateSummary =
            "Enter=" + enterArmUIPlaneButton.isTouched +
            " Raw=" + armRawTouchedMotorID +
            " ProxyTouched=" + armCurrentTouchedMotorID +
            " Red=" + armCurrentRedMotorID +
            " Confirmed=" + armConfirmedMotorID +
            " Rejected=" + armRejectedMotorID +
            " Reason=" + armRejectReason;

        if (stateSummary == _lastHistoryStateSummary)
        {
            return;
        }

        _lastHistoryStateSummary = stateSummary;
        AppendArmUIDebugHistory("STATE " + stateSummary + " | ArmMotorState=" + BuildArmMotorIDStateSnapshot());
    }

    private void AppendArmUIDebugHistory(string message)
    {
        if (!logArmUIDebugHistory || string.IsNullOrEmpty(message))
        {
            return;
        }

        string historyLine = "f=" + Time.frameCount + " t=" + Time.time.ToString("F3") + " " + message;
        _armUIHistoryEntries.Enqueue(historyLine);

        int targetCount = Mathf.Max(10, armUIDebugHistoryMaxLines);
        while (_armUIHistoryEntries.Count > targetCount)
        {
            _armUIHistoryEntries.Dequeue();
        }

        armUIDebugHistory = string.Join("\n", _armUIHistoryEntries.ToArray());
    }

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
                AppendArmUIDebugHistory("EVENT enter " + enterArmUIPlaneButton.buttonName + " collider=" + other.name);
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
                AppendArmUIDebugHistory("EVENT enter " + button.buttonName + " collider=" + other.name);
                button.onEnter?.Invoke();
            }
            return;
        }

        if (!button.isTouched)
        {
            button.isTouched = true;
            currentTouchedButton = button.buttonName;
            currentTouchedCollider = other.name;
            interactionDebug = "Touch enter: " + button.buttonName + " (" + currentTouchedCollider + ")";
            AppendArmUIDebugHistory("EVENT enter " + button.buttonName + " motor=" + button.resolvedMotorID + " collider=" + other.name);
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
                AppendArmUIDebugHistory("EVENT exit " + enterArmUIPlaneButton.buttonName + " collider=" + other.name);
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
                AppendArmUIDebugHistory("EVENT exit " + button.buttonName + " collider=" + other.name);
                button.onExit?.Invoke();
            }

            return;
        }

        if (button.isTouched)
        {
            button.isTouched = false;
            interactionDebug = "Touch exit: " + button.buttonName + " (" + other.name + ")";
            AppendArmUIDebugHistory("EVENT exit " + button.buttonName + " motor=" + button.resolvedMotorID + " collider=" + other.name);
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
    private const float ExitGraceSeconds = 0.08f;
    private const float StayTimeoutSeconds = 0.22f;

    private ArmUIPlaneController _controller;
    private ArmUIPlaneController.ButtonBinding _button;
    private int _touchCount = 0;
    private bool _isActive = false;
    private float _pendingExitAt = -1f;
    private Collider _pendingExitCollider;
    private float _lastSeenTouchTime = -1f;
    private Collider _lastSeenCollider;

    public void Initialize(ArmUIPlaneController controller, ArmUIPlaneController.ButtonBinding button)
    {
        _controller = controller;
        _button = button;
        _touchCount = 0;
        _isActive = false;
        _pendingExitAt = -1f;
        _pendingExitCollider = null;
        _lastSeenTouchTime = -1f;
        _lastSeenCollider = null;
    }

    private void Update()
    {
        if (_controller == null || !_isActive)
        {
            return;
        }

        if (_touchCount == 0)
        {
            if (_pendingExitAt > 0f && Time.time >= _pendingExitAt)
            {
                ForceExit(_pendingExitCollider);
            }
            return;
        }

        _pendingExitAt = -1f;

        // If a touch gets stuck active without stay heartbeats, force release.
        if (_lastSeenTouchTime > 0f && (Time.time - _lastSeenTouchTime) > StayTimeoutSeconds)
        {
            _touchCount = 0;
            ForceExit(_lastSeenCollider);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        _touchCount++;
        _lastSeenTouchTime = Time.time;
        _lastSeenCollider = other;
        _pendingExitAt = -1f;
        _pendingExitCollider = null;
        if (_controller != null && !_isActive)
        {
            _isActive = true;
            _controller.OnButtonTriggerEnter(_button, other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        _lastSeenTouchTime = Time.time;
        _lastSeenCollider = other;

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
            _pendingExitAt = Time.time + ExitGraceSeconds;
            _pendingExitCollider = other;
        }
    }

    private void ForceExit(Collider other)
    {
        if (_controller == null || !_isActive)
        {
            return;
        }

        _isActive = false;
        _pendingExitAt = -1f;
        _pendingExitCollider = null;
        _touchCount = 0;
        _controller.OnButtonTriggerExit(_button, other != null ? other : _lastSeenCollider);
    }
}