using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TriggerSnappingCollider : MonoBehaviour
{
    [Header("Snapping Source")]
    public ClawModuleController clawModuleController;

    [Header("Touch")]
    public string targetTag = "L_IndexTip";

    [Header("Button Visual")]
    public GameObject buttonObject;
    public Collider buttonCollider;
    public Renderer buttonRenderer;
    public TMP_Text buttonText;
    public Color onColor = Color.green;

    [Header("Behavior")]
    [Tooltip("If true, the button only shows when a snapping mode is available or enabled.")]
    public bool showOnlyWhenSnappingAvailable = true;

    [Tooltip("If true, touching again while on will turn the snapping state off.")]
    public bool toggleOffOnSecondTouch = true;

    [Header("Debug")]
    public bool isTouched;
    public bool isOn;
    public string interactionDebug = "Snapping button idle";

    private readonly HashSet<Collider> touchingColliders = new HashSet<Collider>();
    private Color originalColor;
    private bool hasOriginalColor;

    private void Awake()
    {
        if (clawModuleController == null)
        {
            clawModuleController = FindObjectOfType<ClawModuleController>();
        }

        if (buttonObject == null)
        {
            buttonObject = gameObject;
        }

        if (buttonCollider == null)
        {
            buttonCollider = GetComponent<Collider>();
        }

        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponent<Renderer>();
        }

        if (buttonText == null && buttonObject != null)
        {
            buttonText = buttonObject.GetComponentInChildren<TMP_Text>(true);
        }

        if (buttonRenderer != null)
        {
            originalColor = buttonRenderer.material.color;
            hasOriginalColor = true;
        }

        SyncVisualState(true);
    }

    private void Update()
    {
        if (clawModuleController != null)
        {
            isOn = clawModuleController.IsCurrentSnappingEnabled();
        }

        SyncVisualState(false);
    }

    private void Reset()
    {
        Collider selfCollider = GetComponent<Collider>();
        if (selfCollider != null)
        {
            selfCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsTargetCollider(other))
        {
            return;
        }

        if (!touchingColliders.Add(other))
        {
            return;
        }

        isTouched = true;
        ToggleSnappingState();
        SyncVisualState(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsTargetCollider(other))
        {
            return;
        }

        touchingColliders.Remove(other);
        isTouched = touchingColliders.Count > 0;
        SyncVisualState(false);
    }

    private bool IsTargetCollider(Collider other)
    {
        return other != null && other.CompareTag(targetTag);
    }

    private void ToggleSnappingState()
    {
        if (clawModuleController == null)
        {
            interactionDebug = "No ClawModuleController";
            return;
        }

        if (!CanInteractWithSnapping())
        {
            interactionDebug = "Snapping button hidden by state";
            return;
        }

        if (isOn && toggleOffOnSecondTouch)
        {
            clawModuleController.ToggleCurrentSnapping();
            isOn = false;
            interactionDebug = "Snapping OFF";
            return;
        }

        clawModuleController.ToggleCurrentSnapping();
        isOn = clawModuleController.IsCurrentSnappingEnabled();
        interactionDebug = isOn ? "Snapping ON" : "Snapping OFF";
    }

    private bool CanInteractWithSnapping()
    {
        if (clawModuleController == null)
        {
            return false;
        }

        if (!showOnlyWhenSnappingAvailable)
        {
            return true;
        }

        return clawModuleController.hasAnySnappingVisible || isOn;
    }

    private void SyncVisualState(bool forceTextRefresh)
    {
        bool shouldShow = CanInteractWithSnapping();

        if (buttonObject != null && buttonObject != gameObject && buttonObject.activeSelf != shouldShow)
        {
            buttonObject.SetActive(shouldShow);
        }

        if (buttonCollider != null)
        {
            buttonCollider.enabled = shouldShow;
        }

        if (buttonRenderer != null)
        {
            buttonRenderer.enabled = shouldShow;

            if (!hasOriginalColor)
            {
                originalColor = buttonRenderer.material.color;
                hasOriginalColor = true;
            }

            Color targetColor = isOn ? onColor : originalColor;
            buttonRenderer.material.color = targetColor;
        }

        if (buttonText != null)
        {
            bool shouldShowText = shouldShow;
            if (buttonText.gameObject.activeSelf != shouldShowText)
            {
                buttonText.gameObject.SetActive(shouldShowText);
            }

            if (shouldShowText && (forceTextRefresh || clawModuleController != null))
            {
                if (clawModuleController != null)
                {
                    buttonText.text = clawModuleController.GetCurrentSnappingText();
                }
            }
        }
    }
}
