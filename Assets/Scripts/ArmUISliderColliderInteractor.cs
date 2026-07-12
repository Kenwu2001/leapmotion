using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ArmUISliderColliderInteractor : MonoBehaviour
{
    [System.Serializable]
    public class SliderBinding
    {
        public string bindingName = "Slider";
        public Slider targetSlider;
        public RectTransform sliderRectTransform;
        public Collider fillCollider;
        public Collider handleCollider;
        public bool useVerticalAxis = true;
        public bool invertAxis = false;
        [Range(0f, 1f)] public float minNormalizedValue = 0f;
        [Range(0f, 1f)] public float maxNormalizedValue = 1f;

        [HideInInspector] public bool touchingFill;
        [HideInInspector] public bool touchingHandle;
    }

    [Header("Slider Bindings")]
    public SliderBinding thumbSlider = new SliderBinding { bindingName = "Thumb" };
    public SliderBinding indexSlider = new SliderBinding { bindingName = "Index" };
    public SliderBinding middleSlider = new SliderBinding { bindingName = "Middle" };
    public SliderBinding thumbMaxSlider = new SliderBinding { bindingName = "ThumbMax", maxNormalizedValue = 0.5f };
    public SliderBinding thumbMinSlider = new SliderBinding { bindingName = "ThumbMin", minNormalizedValue = 0.5f, maxNormalizedValue = 1f };

    [Header("Arm UI State")]
    public ArmUIPlaneController armUIPlaneController;
    public bool requireDirectAngleMode = true;
    public bool requireMaxMinModeForThumbMax = true;
    public bool requireMaxMinModeForThumbMin = true;

    [Header("Interaction")]
    public Transform fingerTipSource;
    public Collider fingerTipCollider;

    [Header("Debug")]
    public bool debugLogTransitions = false;
    public bool enableRuntimeHistory = true;
    [Range(10, 200)] public int runtimeHistoryMaxLines = 40;
    [SerializeField] private string debugLastEvent = "None";
    [SerializeField] private string debugLastBinding = "None";
    [SerializeField] private string debugLastCollider = "None";
    [SerializeField] private Vector3 debugSourceWorldPosition;
    [SerializeField] private Vector3 debugSourceLocalPosition;
    [SerializeField] private float debugLastNormalizedValue;
    [SerializeField] private float debugLastSliderValue;
    [SerializeField] private bool debugThumbTouchingFill;
    [SerializeField] private bool debugThumbTouchingHandle;
    [SerializeField] private bool debugIndexTouchingFill;
    [SerializeField] private bool debugIndexTouchingHandle;
    [SerializeField] private bool debugMiddleTouchingFill;
    [SerializeField] private bool debugMiddleTouchingHandle;
    [SerializeField] private bool debugThumbMaxTouchingFill;
    [SerializeField] private bool debugThumbMaxTouchingHandle;
    [SerializeField] private bool debugThumbMinTouchingFill;
    [SerializeField] private bool debugThumbMinTouchingHandle;
    [TextArea(6, 24)] [SerializeField] private string runtimeHistory = "";

    private readonly Queue<string> _historyLines = new Queue<string>();
    private int _lastHistoryUpdateFrame = -1;

    private void Awake()
    {
        CacheReferences();
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
        SliderBinding binding = GetBindingForCollider(other);
        if (binding == null)
        {
            return;
        }

        RegisterTouch(binding, other);
        RecordTransition("Enter", binding, other);
        SyncDebugTouchFlags();
    }

    private void OnTriggerStay(Collider other)
    {
        SliderBinding binding = GetBindingForCollider(other);
        if (binding == null)
        {
            return;
        }

        RegisterTouch(binding, other);
        // Only handle stay can drive value changes. Fill is a gate condition only.
        if (other == binding.handleCollider)
        {
            TryUpdateSlider(binding);
        }

        RecordTransition("Stay", binding, other);
        SyncDebugTouchFlags();
    }

    private void OnTriggerExit(Collider other)
    {
        SliderBinding binding = GetBindingForCollider(other);
        if (binding == null)
        {
            return;
        }

        UnregisterTouch(binding, other);
        RecordTransition("Exit", binding, other);
        SyncDebugTouchFlags();
    }

    private void CacheReferences()
    {
        CacheBindingReferences(thumbSlider);
        CacheBindingReferences(indexSlider);
        CacheBindingReferences(middleSlider);
        CacheBindingReferences(thumbMaxSlider);
        CacheBindingReferences(thumbMinSlider);

        if (armUIPlaneController == null)
        {
            armUIPlaneController = FindObjectOfType<ArmUIPlaneController>();
        }

        if (fingerTipSource == null)
        {
            fingerTipSource = transform;
        }

        if (fingerTipCollider == null)
        {
            fingerTipCollider = GetComponent<Collider>();
            if (fingerTipCollider == null)
            {
                fingerTipCollider = GetComponentInChildren<Collider>(true);
            }
        }
    }

    private void CacheBindingReferences(SliderBinding binding)
    {
        if (binding == null)
        {
            return;
        }

        if (binding.targetSlider == null)
        {
            binding.targetSlider = GetComponentInChildren<Slider>(true);
        }

        if (binding.sliderRectTransform == null && binding.targetSlider != null)
        {
            binding.sliderRectTransform = binding.targetSlider.GetComponent<RectTransform>();
        }
    }

    private SliderBinding GetBindingForCollider(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        if (MatchesBindingCollider(thumbSlider, other)) return thumbSlider;
        if (MatchesBindingCollider(indexSlider, other)) return indexSlider;
        if (MatchesBindingCollider(middleSlider, other)) return middleSlider;
        if (MatchesBindingCollider(thumbMaxSlider, other)) return thumbMaxSlider;
        if (MatchesBindingCollider(thumbMinSlider, other)) return thumbMinSlider;
        return null;
    }

    private bool MatchesBindingCollider(SliderBinding binding, Collider other)
    {
        if (binding == null || other == null)
        {
            return false;
        }

        return other == binding.fillCollider || other == binding.handleCollider;
    }

    private void RegisterTouch(SliderBinding binding, Collider other)
    {
        if (binding == null || other == null)
        {
            return;
        }

        if (other == binding.fillCollider)
        {
            binding.touchingFill = true;
        }
        else if (other == binding.handleCollider)
        {
            binding.touchingHandle = true;
        }
    }

    private void UnregisterTouch(SliderBinding binding, Collider other)
    {
        if (binding == null || other == null)
        {
            return;
        }

        if (other == binding.fillCollider)
        {
            binding.touchingFill = false;
        }
        else if (other == binding.handleCollider)
        {
            binding.touchingHandle = false;
        }
    }

    private void TryUpdateSlider(SliderBinding binding)
    {
        if (binding == null || binding.targetSlider == null || binding.sliderRectTransform == null)
        {
            return;
        }

        if (binding == thumbMaxSlider && requireMaxMinModeForThumbMax)
        {
            if (armUIPlaneController == null || !armUIPlaneController.IsMaxMinAngleModeActive())
            {
                return;
            }
        }
        else if (binding == thumbMinSlider && requireMaxMinModeForThumbMin)
        {
            if (armUIPlaneController == null || !armUIPlaneController.IsMaxMinAngleModeActive())
            {
                return;
            }
        }
        else if (requireDirectAngleMode && armUIPlaneController != null && !armUIPlaneController.IsDirectAngleModeActive())
        {
            return;
        }

        if (!binding.touchingFill || !binding.touchingHandle)
        {
            return;
        }

        Vector3 sourcePosition = GetFingerTipWorldPosition();
        Vector3 localPoint = binding.sliderRectTransform.InverseTransformPoint(sourcePosition);
        Rect rect = binding.sliderRectTransform.rect;

        float normalizedValue = binding.useVerticalAxis
            ? Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y)
            : Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);

        if (binding.invertAxis)
        {
            normalizedValue = 1f - normalizedValue;
        }

        float normalizedMin = Mathf.Clamp01(Mathf.Min(binding.minNormalizedValue, binding.maxNormalizedValue));
        float normalizedMax = Mathf.Clamp01(Mathf.Max(binding.minNormalizedValue, binding.maxNormalizedValue));
        normalizedValue = Mathf.Clamp(normalizedValue, normalizedMin, normalizedMax);
        float newValue = Mathf.Lerp(binding.targetSlider.minValue, binding.targetSlider.maxValue, normalizedValue);

        debugLastBinding = binding.bindingName;
        debugSourceWorldPosition = sourcePosition;
        debugSourceLocalPosition = localPoint;
        debugLastNormalizedValue = normalizedValue;
        debugLastSliderValue = newValue;

        if (!Mathf.Approximately(binding.targetSlider.value, newValue))
        {
            binding.targetSlider.value = newValue;
        }

        if (_lastHistoryUpdateFrame != Time.frameCount)
        {
            _lastHistoryUpdateFrame = Time.frameCount;
            AppendHistory("Update", binding, binding.handleCollider, newValue, normalizedValue, localPoint);
        }
    }

    private Vector3 GetFingerTipWorldPosition()
    {
        if (fingerTipCollider != null)
        {
            return fingerTipCollider.bounds.center;
        }

        if (fingerTipSource != null)
        {
            return fingerTipSource.position;
        }

        return transform.position;
    }

    private void RecordTransition(string phase, SliderBinding binding, Collider other)
    {
        debugLastEvent = phase;
        debugLastBinding = binding != null ? binding.bindingName : "None";
        debugLastCollider = other != null ? other.name : "None";

        AppendHistory(
            phase,
            binding,
            other,
            debugLastSliderValue,
            debugLastNormalizedValue,
            debugSourceLocalPosition);

        if (debugLogTransitions)
        {
            Debug.Log("[ArmUISliderColliderInteractor] " + phase +
                      " binding=" + debugLastBinding +
                      " collider=" + debugLastCollider +
                      " fill=" + (binding != null && binding.touchingFill) +
                      " handle=" + (binding != null && binding.touchingHandle));
        }
    }

    private void SyncDebugTouchFlags()
    {
        debugThumbTouchingFill = thumbSlider != null && thumbSlider.touchingFill;
        debugThumbTouchingHandle = thumbSlider != null && thumbSlider.touchingHandle;
        debugIndexTouchingFill = indexSlider != null && indexSlider.touchingFill;
        debugIndexTouchingHandle = indexSlider != null && indexSlider.touchingHandle;
        debugMiddleTouchingFill = middleSlider != null && middleSlider.touchingFill;
        debugMiddleTouchingHandle = middleSlider != null && middleSlider.touchingHandle;
        debugThumbMaxTouchingFill = thumbMaxSlider != null && thumbMaxSlider.touchingFill;
        debugThumbMaxTouchingHandle = thumbMaxSlider != null && thumbMaxSlider.touchingHandle;
        debugThumbMinTouchingFill = thumbMinSlider != null && thumbMinSlider.touchingFill;
        debugThumbMinTouchingHandle = thumbMinSlider != null && thumbMinSlider.touchingHandle;
    }

    private void AppendHistory(
        string phase,
        SliderBinding binding,
        Collider collider,
        float sliderValue,
        float normalizedValue,
        Vector3 localPoint)
    {
        if (!enableRuntimeHistory)
        {
            return;
        }

        int maxLines = Mathf.Max(10, runtimeHistoryMaxLines);
        string bindingName = binding != null ? binding.bindingName : "None";
        string colliderName = collider != null ? collider.name : "None";
        string line = "f=" + Time.frameCount +
                      " t=" + Time.time.ToString("F3") +
                      " " + phase +
                      " b=" + bindingName +
                      " c=" + colliderName +
                      " fill=" + (binding != null && binding.touchingFill) +
                      " handle=" + (binding != null && binding.touchingHandle) +
                      " n=" + normalizedValue.ToString("F4") +
                      " v=" + sliderValue.ToString("F3") +
                      " local=(" + localPoint.x.ToString("F3") + "," + localPoint.y.ToString("F3") + "," + localPoint.z.ToString("F3") + ")";

        _historyLines.Enqueue(line);
        while (_historyLines.Count > maxLines)
        {
            _historyLines.Dequeue();
        }

        StringBuilder builder = new StringBuilder();
        foreach (string historyLine in _historyLines)
        {
            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(historyLine);
        }

        runtimeHistory = builder.ToString();
    }
}