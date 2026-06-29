using UnityEngine;
using UnityEngine.XR;

public class ControllerLocatorLeft : MonoBehaviour
{
    [Header("Debug")]
    public Vector3 currentControllerPosition;
    public Vector3 currentControllerEulerAngles;
    public string controllerPoseDebug = "LeftHand controller not tracked";
    public float currentControllerSeparationDistance;
    public bool isControllerSeparationValid;

    [Header("Fallback Display")]
    public GameObject leftHandRetargetingSkin;
    public GameObject leftHandOriginalSkin;
    public GameObject leftQuad;
    public GameObject canvasPlane;
    [Tooltip("Objects on the same hierarchy level that should follow canvasPlane visibility (e.g. Cube)")]
    public GameObject[] canvasLinkedObjects;
    public bool alwaysShowCanvasPlane = false;
    public bool previewCanvasPlaneOffset = false;
    public float canvasShowDelaySeconds = 1f;

    [Header("Right Hand Follow")]
    [Tooltip("Local offset from right-hand controller to fallback canvas (X)")]
    public float rightHandCanvasXOffset = 0f;
    [Tooltip("Local offset from right-hand controller to fallback canvas (Y)")]
    public float rightHandCanvasYOffset = 0f;
    [Tooltip("Local offset from right-hand controller to fallback canvas (Z)")]
    public float rightHandCanvasZOffset = 0.3f;
    [Tooltip("Fixed world Euler rotation (X/Y/Z) for fallback canvas while following right-hand position")]
    public Vector3 rightHandCanvasRotationOffsetEuler;

    [Header("Mode Switching")]
    public ModeSwitching modeSwitching;

    [Header("Hand Separation")]
    [Tooltip("When L/R controllers are farther apart than this distance (meters), hide the left hand skins and quad")]
    public float leftHandHideSeparationThreshold = 0.55f;
    [Tooltip("When L/R controllers are farther apart than this distance (meters), switch to the cube + panel fallback")]
    public float fallbackShowSeparationThreshold = 0.75f;
    [Tooltip("When fallback is active, hide it only after L/R controllers are closer than this distance (meters)")]
    public float fallbackHideSeparationThreshold = 0.5f;

    private Transform canvasOriginalParent;
    private Vector3 canvasInitialLocalPosition;
    private Quaternion canvasInitialLocalRotation;
    private Vector3 canvasInitialLocalScale;
    private bool isCanvasFrozenInWorld;
    private bool wasPreviewCanvasPlaneOffset;
    private bool wasAlwaysShowCanvasPlane;
    private float leftHandHiddenTimer;
    private bool isFallbackVisualsActive;
    private bool shouldHideLeftHandByDistance;

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
            SetCanvasVisibility(true);
        }
        else
        {
            SetCanvasVisibility(false);
        }
    }

    void Update()
    {
        RefreshControllerSeparationState();
        UpdateLeftHandVisualsHiddenState();

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

        bool shouldUseFallbackVisuals = ShouldUseFallbackVisuals();

        if (shouldUseFallbackVisuals)
        {
            if (!isFallbackVisualsActive)
            {
                isFallbackVisualsActive = true;
                ShowFallbackVisuals();
                ShowCanvasPlaneFollowingRightHand();
            }

            UpdateCanvasPlaneFollowingRightHandPose();
            return;
        }

        if (isFallbackVisualsActive)
        {
            isFallbackVisualsActive = false;
            HideFallbackVisuals();
            HideCanvasPlane();
        }
    }

    private bool ShouldUseFallbackVisuals()
    {
        if (!isControllerSeparationValid)
        {
            return false;
        }

        if (!isFallbackVisualsActive)
        {
            return currentControllerSeparationDistance > fallbackShowSeparationThreshold;
        }

        return currentControllerSeparationDistance >= fallbackHideSeparationThreshold;
    }

    private void RefreshControllerSeparationState()
    {
        if (!TryGetControllerSeparationDistance(out float controllerDistance))
        {
            isControllerSeparationValid = false;
            currentControllerSeparationDistance = 0f;
            shouldHideLeftHandByDistance = false;
            return;
        }

        isControllerSeparationValid = true;
        currentControllerSeparationDistance = controllerDistance;
        shouldHideLeftHandByDistance = controllerDistance > leftHandHideSeparationThreshold;
    }

    private bool TryGetControllerSeparationDistance(out float controllerDistance)
    {
        controllerDistance = 0f;

        InputDevice leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        if (!leftDevice.isValid || !rightDevice.isValid)
        {
            return false;
        }

        bool hasLeftPos = leftDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 leftPos);
        bool hasRightPos = rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightPos);
        if (!hasLeftPos || !hasRightPos)
        {
            return false;
        }

        controllerDistance = Vector3.Distance(leftPos, rightPos);
        return true;
    }

    private void ShowFallbackVisuals()
    {
        UpdateLeftHandVisualsHiddenState();
        leftHandHiddenTimer = 0f;
    }

    private void HideFallbackVisuals()
    {
        UpdateLeftHandVisualsHiddenState();
        leftHandHiddenTimer = 0f;
    }

    private void UpdateLeftHandVisualsHiddenState()
    {
        bool shouldHideLeftHandVisuals = shouldHideLeftHandByDistance || isFallbackVisualsActive;

        if (leftHandRetargetingSkin != null)
        {
            leftHandRetargetingSkin.SetActive(!shouldHideLeftHandVisuals);
        }

        if (leftHandOriginalSkin != null)
        {
            leftHandOriginalSkin.SetActive(!shouldHideLeftHandVisuals);
        }

        if (leftQuad != null)
        {
            leftQuad.SetActive(!shouldHideLeftHandVisuals);
        }
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

        SetCanvasVisibility(true);
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

        SetCanvasVisibility(true);
        isCanvasFrozenInWorld = false;
        leftHandHiddenTimer = 0f;
    }

    private void ShowCanvasPlaneFollowingRightHand()
    {
        Transform canvasTransform = canvasPlane.transform;
        canvasTransform.SetParent(null, false);
        SetCanvasVisibility(true);
        isCanvasFrozenInWorld = true;
        UpdateCanvasPlaneFollowingRightHandPose();
    }

    private void UpdateCanvasPlaneFollowingRightHandPose()
    {
        if (!isFallbackVisualsActive || canvasPlane == null)
        {
            return;
        }

        if (!TryGetRightHandControllerWorldPose(out Vector3 rightHandWorldPosition, out Quaternion rightHandWorldRotation))
        {
            return;
        }

        Transform canvasTransform = canvasPlane.transform;
        Vector3 localOffset = new Vector3(
            rightHandCanvasXOffset,
            rightHandCanvasYOffset,
            rightHandCanvasZOffset
        );

        Vector3 worldPosition = rightHandWorldPosition + (rightHandWorldRotation * localOffset);
        Quaternion worldRotation = Quaternion.Euler(rightHandCanvasRotationOffsetEuler);

        canvasTransform.SetPositionAndRotation(worldPosition, worldRotation);
        canvasTransform.localScale = canvasInitialLocalScale;
    }

    private bool TryGetRightHandControllerWorldPose(out Vector3 rightHandWorldPosition, out Quaternion rightHandWorldRotation)
    {
        rightHandWorldPosition = Vector3.zero;
        rightHandWorldRotation = Quaternion.identity;

        InputDevice rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!rightDevice.isValid)
        {
            return false;
        }

        if (!rightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 rightHandTrackingPosition) ||
            !rightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rightHandTrackingRotation))
        {
            return false;
        }

        Transform parentTransform = transform.parent;
        if (parentTransform != null)
        {
            rightHandWorldPosition = parentTransform.TransformPoint(rightHandTrackingPosition);
            rightHandWorldRotation = parentTransform.rotation * rightHandTrackingRotation;
        }
        else
        {
            rightHandWorldPosition = rightHandTrackingPosition;
            rightHandWorldRotation = rightHandTrackingRotation;
        }

        return true;
    }

    private void HideCanvasPlane()
    {
        if (canvasPlane == null)
        {
            return;
        }

        SetCanvasVisibility(false);

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

    private void SetCanvasVisibility(bool isVisible)
    {
        if (canvasPlane != null)
        {
            canvasPlane.SetActive(isVisible);
        }

        if (canvasLinkedObjects == null)
        {
            return;
        }

        for (int i = 0; i < canvasLinkedObjects.Length; i++)
        {
            GameObject linkedObject = canvasLinkedObjects[i];
            if (linkedObject != null)
            {
                linkedObject.SetActive(isVisible);
            }
        }
    }
}