using UnityEngine;
using UnityEngine.XR;

public class ControllerLocatorLeft : MonoBehaviour
{
    [Header("Debug")]
    public Vector3 currentControllerPosition;
    public Vector3 currentControllerEulerAngles;
    public string controllerPoseDebug = "LeftHand controller not tracked";

    [Header("Fallback Display")]
    public GameObject leftHandSkin;
    public GameObject leftQuad;
    public GameObject canvasPlane;
    public bool alwaysShowCanvasPlane = false;
    public bool previewCanvasPlaneOffset = false;
    public float canvasShowDelaySeconds = 1f;

    [Header("Mode Switching")]
    public ModeSwitching modeSwitching;

    [Header("Hand Separation")]
    [Tooltip("When the two hands are farther apart than this distance (meters), switch to the cube + panel fallback")]
    public float handSeparationThreshold = 0.4f;

    private Transform canvasOriginalParent;
    private Vector3 canvasInitialLocalPosition;
    private Quaternion canvasInitialLocalRotation;
    private Vector3 canvasInitialLocalScale;
    private bool isCanvasFrozenInWorld;
    private bool wasPreviewCanvasPlaneOffset;
    private bool wasAlwaysShowCanvasPlane;
    private float leftHandHiddenTimer;
    private bool isFallbackVisualsActive;

    void Awake()
    {
        if (modeSwitching == null)
        {
            modeSwitching = FindObjectOfType<ModeSwitching>();
        }

        if (canvasPlane == null)
        {
            return;
        }

        canvasOriginalParent = canvasPlane.transform.parent;
        canvasInitialLocalPosition = canvasPlane.transform.localPosition;
        canvasInitialLocalRotation = canvasPlane.transform.localRotation;
        canvasInitialLocalScale = canvasPlane.transform.localScale;

        if (previewCanvasPlaneOffset)
        {
            canvasPlane.SetActive(true);
        }
        else
        {
            canvasPlane.SetActive(false);
        }
    }

    void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!device.isValid)
        {
            controllerPoseDebug = "LeftHand controller not tracked";
            if (previewCanvasPlaneOffset)
            {
                KeepCanvasPlaneAttachedForPreview(!wasPreviewCanvasPlaneOffset);
                wasPreviewCanvasPlaneOffset = true;
                wasAlwaysShowCanvasPlane = false;
                return;
            }

            wasPreviewCanvasPlaneOffset = false;

            if (alwaysShowCanvasPlane)
            {
                ShowCanvasPlaneAttached(!wasAlwaysShowCanvasPlane);
                wasAlwaysShowCanvasPlane = true;
            }
            else
            {
                wasAlwaysShowCanvasPlane = false;
                UpdateCanvasPlaneVisibility();
            }
            return;
        }

        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
        {
            currentControllerPosition = pos;
            currentControllerEulerAngles = rot.eulerAngles;
            controllerPoseDebug = "LeftHand Pos: (" +
                                  pos.x.ToString("F3") + ", " +
                                  pos.y.ToString("F3") + ", " +
                                  pos.z.ToString("F3") + ") Rot: (" +
                                  currentControllerEulerAngles.x.ToString("F1") + ", " +
                                  currentControllerEulerAngles.y.ToString("F1") + ", " +
                                  currentControllerEulerAngles.z.ToString("F1") + ")";

            transform.localPosition = pos;
            transform.localRotation = rot;

            if (previewCanvasPlaneOffset)
            {
                KeepCanvasPlaneAttachedForPreview(!wasPreviewCanvasPlaneOffset);
                wasPreviewCanvasPlaneOffset = true;
                wasAlwaysShowCanvasPlane = false;
                return;
            }

            wasPreviewCanvasPlaneOffset = false;

            if (alwaysShowCanvasPlane)
            {
                ShowCanvasPlaneAttached(!wasAlwaysShowCanvasPlane);
                wasAlwaysShowCanvasPlane = true;
                return;
            }

            wasAlwaysShowCanvasPlane = false;

            UpdateCanvasPlaneVisibility();
        }
        else
        {
            controllerPoseDebug = "LeftHand controller pose unavailable";
            if (previewCanvasPlaneOffset)
            {
                KeepCanvasPlaneAttachedForPreview(!wasPreviewCanvasPlaneOffset);
                wasPreviewCanvasPlaneOffset = true;
                wasAlwaysShowCanvasPlane = false;
                return;
            }

            wasPreviewCanvasPlaneOffset = false;

            if (alwaysShowCanvasPlane)
            {
                ShowCanvasPlaneAttached(!wasAlwaysShowCanvasPlane);
                wasAlwaysShowCanvasPlane = true;
            }
            else
            {
                wasAlwaysShowCanvasPlane = false;
                UpdateCanvasPlaneVisibility();
            }
        }
    }

    private void UpdateCanvasPlaneVisibility()
    {
        if (canvasPlane == null)
        {
            return;
        }

        bool shouldUseFallbackVisuals = AreHandsSeparatedBeyondThreshold();
        if (shouldUseFallbackVisuals == isFallbackVisualsActive)
        {
            return;
        }

        isFallbackVisualsActive = shouldUseFallbackVisuals;

        if (isFallbackVisualsActive)
        {
            ShowFallbackVisuals();
            ShowCanvasPlaneAtFrozenWorldPose();
        }
        else
        {
            HideFallbackVisuals();
            HideCanvasPlane();
        }
    }

    private bool AreHandsSeparatedBeyondThreshold()
    {
        if (modeSwitching == null)
        {
            return false;
        }

        return modeSwitching.currentHandSeparationDistance > handSeparationThreshold;
    }

    private void ShowFallbackVisuals()
    {
        if (leftHandSkin != null)
        {
            leftHandSkin.SetActive(false);
            leftQuad.SetActive(false);
        }

        leftHandHiddenTimer = 0f;
    }

    private void HideFallbackVisuals()
    {
        if (leftHandSkin != null)
        {
            leftHandSkin.SetActive(true);
            leftQuad.SetActive(true);
        }

        leftHandHiddenTimer = 0f;
    }

    private void KeepCanvasPlaneAttachedForPreview(bool restoreInitialTransform)
    {
        if (canvasPlane == null)
        {
            return;
        }

        Transform canvasTransform = canvasPlane.transform;
        if (canvasOriginalParent != null && canvasTransform.parent != canvasOriginalParent)
        {
            canvasTransform.SetParent(canvasOriginalParent, false);
        }

        if (restoreInitialTransform)
        {
            canvasTransform.localPosition = canvasInitialLocalPosition;
            canvasTransform.localRotation = canvasInitialLocalRotation;
            canvasTransform.localScale = canvasInitialLocalScale;
        }

        canvasPlane.SetActive(true);
        isCanvasFrozenInWorld = false;
        leftHandHiddenTimer = 0f;
    }

    private void ShowCanvasPlaneAttached(bool restoreInitialTransform)
    {
        if (canvasPlane == null)
        {
            return;
        }

        Transform canvasTransform = canvasPlane.transform;
        if (canvasOriginalParent != null && canvasTransform.parent != canvasOriginalParent)
        {
            canvasTransform.SetParent(canvasOriginalParent, false);
        }

        if (restoreInitialTransform)
        {
            canvasTransform.localPosition = canvasInitialLocalPosition;
            canvasTransform.localRotation = canvasInitialLocalRotation;
            canvasTransform.localScale = canvasInitialLocalScale;
        }

        canvasPlane.SetActive(true);
        isCanvasFrozenInWorld = false;
        leftHandHiddenTimer = 0f;
    }

    private void ShowCanvasPlaneAtFrozenWorldPose()
    {
        if (isCanvasFrozenInWorld)
        {
            return;
        }

        Transform canvasTransform = canvasPlane.transform;
        Vector3 worldPosition = transform.TransformPoint(canvasInitialLocalPosition);
        Quaternion worldRotation = transform.rotation * canvasInitialLocalRotation;

        canvasTransform.SetParent(null, false);
        canvasTransform.SetPositionAndRotation(worldPosition, worldRotation);
        canvasTransform.localScale = canvasInitialLocalScale;
        canvasPlane.SetActive(true);
        isCanvasFrozenInWorld = true;
    }

    private void HideCanvasPlane()
    {
        if (canvasPlane == null)
        {
            return;
        }

        canvasPlane.SetActive(false);

        Transform canvasTransform = canvasPlane.transform;
        if (canvasOriginalParent != null)
        {
            canvasTransform.SetParent(canvasOriginalParent, false);
        }

        canvasTransform.localPosition = canvasInitialLocalPosition;
        canvasTransform.localRotation = canvasInitialLocalRotation;
        canvasTransform.localScale = canvasInitialLocalScale;
        isCanvasFrozenInWorld = false;

        if (alwaysShowCanvasPlane || previewCanvasPlaneOffset)
        {
            leftHandHiddenTimer = 0f;
        }
    }
}