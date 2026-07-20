using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

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
    // public GameObject leftHandOriginalSkin;
    public GameObject leftQuad;
    public GameObject canvasPlane;
    public GameObject LeftCanvasStateArea;
    public Renderer LeftCanvasStateRenderer;
    public GameObject baseline2CanvasPlane;
    public GameObject Baseline2CanvasStateArea;
    public Renderer Baseline2CanvasStateRenderer;
    public Material leftCanvasOnMaterial;
    public Material leftCanvasOffMaterial;
    public bool leftCanvasStateIsOn = false;
    public bool baseline2CanvasStateIsOn = false;
    [Tooltip("Tag of the collider that can toggle left canvas state")]
    public string leftCanvasStateAreaTag = "LeftCanvasStateArea";
    [Tooltip("Tag of the collider that can toggle baseline2 canvas state")]
    public string baseline2CanvasStateAreaTag = "Baseline2CanvasStateArea";
    [Tooltip("Objects on the same hierarchy level that should follow canvasPlane visibility (e.g. Cube)")]
    public GameObject[] canvasLinkedObjects;
    public bool alwaysShowCanvasPlane = false;
    [Tooltip("Ignore hand-separation thresholds and always show toy hand + canvas fallback. Canvas follows the right-hand controller pose.")]
    public bool alwaysAllowToyHandAndCanvas = false;
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
    [Tooltip("Local offset from right-hand controller to LeftCanvasStateArea (X)")]
    public float leftCanvasStateAreaRightHandXOffset = 0f;
    [Tooltip("Local offset from right-hand controller to LeftCanvasStateArea (Z)")]
    public float leftCanvasStateAreaRightHandZOffset = 0f;

    [Header("Head Follow")]
    [Tooltip("Optional head transform used for the canvas Y position. If null, XRNode.Head or the main camera will be used.")]
    public Transform headTransform;
    [Tooltip("Extra Y offset applied from the headset/head position when positioning the canvas plane")]
    public float headCanvasYOffset = 0f;

    [Header("Mode Switching")]
    public ModeSwitching modeSwitching;

    [Header("Baseline2")]
    public BaselineTwo baselineTwo;

    [Header("Arm UI")]
    public ArmUIPlaneCollider armUIPlaneCollider;

    [Header("Fingertip Distance Thresholds")]
    [Tooltip("ON: use fingertip distance and fingertip thresholds for left-hand hide/fallback logic. OFF: use controller distance thresholds.")]
    public bool distanceThresholdByFingertips = false;
    public JointAngle jointAngle;
    public float currentFingertipSeparate;
    public bool isFingertipSeparationValid;
    [Tooltip("Left hand hide threshold when distanceThresholdByFingertips is ON")]
    public float leftHandHideSeparationThresholdByFingertip = 0.16f;
    [Tooltip("Fallback show threshold when distanceThresholdByFingertips is ON")]
    public float fallbackShowSeparationThresholdByFingertip = 0.16f;
    [Tooltip("Fallback hide threshold when distanceThresholdByFingertips is ON")]
    public float fallbackHideSeparationThresholdByFingertip = 0.16f;

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
    private bool isLeftCanvasStateAreaDetached;
    private bool isBaseline2CanvasStateAreaDetached;
    private readonly HashSet<Collider> touchingCanvasStateColliders = new HashSet<Collider>();
    private readonly HashSet<Collider> touchingBaseline2CanvasStateColliders = new HashSet<Collider>();

    void Awake()
    {
        if (modeSwitching == null)
        {
            modeSwitching = FindObjectOfType<ModeSwitching>();
        }

        if (baselineTwo == null)
        {
            baselineTwo = FindObjectOfType<BaselineTwo>();
        }

        if (jointAngle == null)
        {
            if (modeSwitching != null && modeSwitching.jointAngle != null)
            {
                jointAngle = modeSwitching.jointAngle;
            }
            else
            {
                jointAngle = FindObjectOfType<JointAngle>();
            }
        }

        if (armUIPlaneCollider == null)
        {
            armUIPlaneCollider = FindObjectOfType<ArmUIPlaneCollider>();
        }

        EnsureLeftCanvasStateRenderer();
        EnsureBaseline2CanvasStateRenderer();
        SetLeftCanvasState(false);
        SetBaseline2CanvasState(false);

        if (baseline2CanvasPlane != null)
        {
            baseline2CanvasPlane.SetActive(false);
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
        if (armUIPlaneCollider == null)
        {
            armUIPlaneCollider = FindObjectOfType<ArmUIPlaneCollider>();
        }

        RefreshControllerSeparationState();
        RefreshFingertipSeparationState();
        RefreshLeftHandSeparationHideState();
        UpdateLeftHandVisualsHiddenState();
        UpdateCanvasLinkedObjectsVisibility();
        UpdateCanvasStateAreaModeVisibility();
        UpdateActiveCanvasStateAreaPose();

        if (alwaysAllowToyHandAndCanvas)
        {
            InputDevice alwaysAllowDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!alwaysAllowDevice.isValid)
            {
                controllerPoseDebug = "LeftHand controller not tracked";
            }
            else if (alwaysAllowDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 alwaysAllowPos) &&
                     alwaysAllowDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion alwaysAllowRot))
            {
                currentControllerPosition = alwaysAllowPos;
                currentControllerEulerAngles = alwaysAllowRot.eulerAngles;
                controllerPoseDebug = "LeftHand Pos: (" +
                                      alwaysAllowPos.x.ToString("F3") + ", " +
                                      alwaysAllowPos.y.ToString("F3") + ", " +
                                      alwaysAllowPos.z.ToString("F3") + ") Rot: (" +
                                      currentControllerEulerAngles.x.ToString("F1") + ", " +
                                      currentControllerEulerAngles.y.ToString("F1") + ", " +
                                      currentControllerEulerAngles.z.ToString("F1") + ")";

                transform.localPosition = alwaysAllowPos;
                transform.localRotation = alwaysAllowRot;
            }
            else
            {
                controllerPoseDebug = "LeftHand controller pose unavailable";
            }

            wasPreviewCanvasPlaneOffset = false;
            wasAlwaysShowCanvasPlane = false;
            UpdateCanvasPlaneVisibility();
            return;
        }

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
        if (IsBaseline2ToyHandModeActive())
        {
            if (canvasPlane != null && canvasPlane.activeSelf)
            {
                HideCanvasPlane();
            }

            SetBaseline2CanvasPlaneVisibility(baseline2CanvasStateIsOn);
            return;
        }

        if (baseline2CanvasPlane != null)
        {
            baseline2CanvasPlane.SetActive(false);
        }

        if (canvasPlane == null)
        {
            return;
        }

        // Keep fallback hand visuals on their original distance-based logic.
        bool shouldUseFallbackVisuals = ShouldUseFallbackVisuals();

        if (shouldUseFallbackVisuals)
        {
            if (!isFallbackVisualsActive)
            {
                isFallbackVisualsActive = true;
                ShowFallbackVisuals();
            }
        }
        else if (isFallbackVisualsActive)
        {
            isFallbackVisualsActive = false;
            HideFallbackVisuals();
        }

        if (leftCanvasStateIsOn)
        {
            if (!canvasPlane.activeSelf || !isCanvasFrozenInWorld)
            {
                ShowCanvasPlaneFollowingRightHand();
            }

            SetCanvasVisibility(true);
            UpdateCanvasPlaneFollowingRightHandPose();
            return;
        }

        if (canvasPlane.activeSelf || isCanvasFrozenInWorld)
        {
            HideCanvasPlane();
        }
    }

    private bool ShouldUseFallbackVisuals()
    {
        if (alwaysAllowToyHandAndCanvas)
        {
            return true;
        }

        if (!IsModeSelectActive())
        {
            return false;
        }

        if (!TryGetActiveSeparationDistance(out float activeDistance))
        {
            return false;
        }

        if (!isFallbackVisualsActive)
        {
            return activeDistance > GetActiveFallbackShowThreshold();
        }

        return activeDistance >= GetActiveFallbackHideThreshold();
    }

    private bool IsModeSelectActive()
    {
        if (modeSwitching == null)
        {
            modeSwitching = FindObjectOfType<ModeSwitching>();
        }

        return modeSwitching != null && modeSwitching.modeSelect && !modeSwitching.modeManipulate;
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
    }

    private void RefreshFingertipSeparationState()
    {
        if (jointAngle == null && modeSwitching != null && modeSwitching.jointAngle != null)
        {
            jointAngle = modeSwitching.jointAngle;
        }

        if (jointAngle == null)
        {
            isFingertipSeparationValid = false;
            currentFingertipSeparate = 0f;
            return;
        }

        float fingertipsDistance = jointAngle.GetLIndexToIndex2Distance();
        currentFingertipSeparate = fingertipsDistance;
        isFingertipSeparationValid = !float.IsNaN(fingertipsDistance) && !float.IsInfinity(fingertipsDistance);
    }

    private void RefreshLeftHandSeparationHideState()
    {
        if (!TryGetActiveSeparationDistance(out float activeDistance))
        {
            shouldHideLeftHandByDistance = false;
            return;
        }

        shouldHideLeftHandByDistance = activeDistance > GetActiveLeftHandHideThreshold();
    }

    private bool TryGetActiveSeparationDistance(out float activeDistance)
    {
        activeDistance = 0f;

        if (distanceThresholdByFingertips)
        {
            if (!isFingertipSeparationValid)
            {
                return false;
            }

            activeDistance = currentFingertipSeparate;
            return true;
        }

        if (!isControllerSeparationValid)
        {
            return false;
        }

        activeDistance = currentControllerSeparationDistance;
        return true;
    }

    private float GetActiveLeftHandHideThreshold()
    {
        return distanceThresholdByFingertips
            ? leftHandHideSeparationThresholdByFingertip
            : leftHandHideSeparationThreshold;
    }

    private float GetActiveFallbackShowThreshold()
    {
        return distanceThresholdByFingertips
            ? fallbackShowSeparationThresholdByFingertip
            : fallbackShowSeparationThreshold;
    }

    private float GetActiveFallbackHideThreshold()
    {
        return distanceThresholdByFingertips
            ? fallbackHideSeparationThresholdByFingertip
            : fallbackHideSeparationThreshold;
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
        bool shouldForceBaseline2ToyHand = IsBaseline2ToyHandModeActive();
        bool shouldHideLeftHandVisuals = shouldForceBaseline2ToyHand ||
                                        (!alwaysAllowToyHandAndCanvas &&
                                        (shouldHideLeftHandByDistance || isFallbackVisualsActive));

        if (leftHandRetargetingSkin != null)
        {
            leftHandRetargetingSkin.SetActive(!shouldHideLeftHandVisuals);
        }

        // if (leftHandOriginalSkin != null)
        // {
        //     leftHandOriginalSkin.SetActive(!shouldHideLeftHandVisuals);
        // }

        if (leftQuad != null)
        {
            leftQuad.SetActive(!shouldHideLeftHandVisuals);
        }
    }

    private void KeepCanvasPlaneAttachedForPreview(bool restoreInitialTransform)
    {
        UpdateCanvasPlaneVisibility();
    }

    private void ShowCanvasPlaneAttached(bool restoreInitialTransform)
    {
        UpdateCanvasPlaneVisibility();
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
        if (!leftCanvasStateIsOn || canvasPlane == null)
        {
            return;
        }

        if (!TryGetRightHandControllerWorldPose(out Vector3 rightHandWorldPosition, out Quaternion rightHandWorldRotation))
        {
            return;
        }

        Transform canvasTransform = canvasPlane.transform;
        Vector3 worldPosition = new Vector3(
            rightHandWorldPosition.x + rightHandCanvasXOffset,
            rightHandWorldPosition.y,
            rightHandWorldPosition.z + rightHandCanvasZOffset
        );
        if (TryGetHeadWorldPosition(out Vector3 headWorldPosition))
        {
            worldPosition.y = headWorldPosition.y + headCanvasYOffset;
        }
        else
        {
            worldPosition.y += rightHandCanvasYOffset;
        }

        Quaternion worldRotation = Quaternion.Euler(rightHandCanvasRotationOffsetEuler);

        canvasTransform.SetPositionAndRotation(worldPosition, worldRotation);
        canvasTransform.localScale = canvasInitialLocalScale;
    }

    private bool TryGetHeadWorldPosition(out Vector3 headWorldPosition)
    {
        if (headTransform != null)
        {
            headWorldPosition = headTransform.position;
            return true;
        }

        InputDevice headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if (headDevice.isValid &&
            headDevice.TryGetFeatureValue(CommonUsages.devicePosition, out headWorldPosition))
        {
            return true;
        }

        if (Camera.main != null)
        {
            headWorldPosition = Camera.main.transform.position;
            return true;
        }

        headWorldPosition = Vector3.zero;
        return false;
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
    }

    private void SetBaseline2CanvasPlaneVisibility(bool isVisible)
    {
        if (baseline2CanvasPlane != null)
        {
            baseline2CanvasPlane.SetActive(isVisible);
        }
    }

    private void UpdateActiveCanvasStateAreaPose()
    {
        if (IsBaseline2ToyHandModeActive())
        {
            UpdateCanvasStateAreaPose(Baseline2CanvasStateArea, ref isBaseline2CanvasStateAreaDetached);
            return;
        }

        UpdateCanvasStateAreaPose(LeftCanvasStateArea, ref isLeftCanvasStateAreaDetached);
    }

    private void UpdateCanvasStateAreaPose(GameObject stateArea, ref bool isDetached)
    {
        if (stateArea == null)
        {
            return;
        }

        if (!TryGetRightHandControllerWorldPose(out Vector3 rightHandWorldPosition, out _))
        {
            return;
        }

        Transform areaTransform = stateArea.transform;
        if (!isDetached)
        {
            areaTransform.SetParent(null, true);
            isDetached = true;
        }

        Vector3 worldPosition = new Vector3(
            rightHandWorldPosition.x + leftCanvasStateAreaRightHandXOffset,
            rightHandWorldPosition.y,
            rightHandWorldPosition.z + leftCanvasStateAreaRightHandZOffset
        );

        if (TryGetHeadWorldPosition(out Vector3 headWorldPosition))
        {
            worldPosition.y = headWorldPosition.y + headCanvasYOffset;
        }
        else
        {
            worldPosition.y += rightHandCanvasYOffset;
        }

        areaTransform.SetPositionAndRotation(worldPosition, Quaternion.identity);
    }

    private void UpdateCanvasStateAreaModeVisibility()
    {
        bool isBaseline2Active = IsBaseline2ToyHandModeActive();

        if (LeftCanvasStateArea != null)
        {
            LeftCanvasStateArea.SetActive(!isBaseline2Active);
        }

        if (Baseline2CanvasStateArea != null)
        {
            Baseline2CanvasStateArea.SetActive(isBaseline2Active);
        }
    }

    private void UpdateCanvasLinkedObjectsVisibility()
    {
        if (canvasLinkedObjects == null)
        {
            return;
        }

        bool shouldShowToyHand = ShouldShowToyHand();

        for (int i = 0; i < canvasLinkedObjects.Length; i++)
        {
            GameObject linkedObject = canvasLinkedObjects[i];
            if (linkedObject != null)
            {
                linkedObject.SetActive(shouldShowToyHand);
            }
        }
    }

    private bool ShouldShowToyHand()
    {
        if (IsBaseline2ToyHandModeActive())
        {
            return true;
        }

        if (alwaysAllowToyHandAndCanvas)
        {
            return true;
        }

        if (IsArmUIAreaActive())
        {
            return true;
        }

        if (!distanceThresholdByFingertips)
        {
            return false;
        }

        return ShouldUseFallbackVisuals();
    }

    private bool IsBaseline2ToyHandModeActive()
    {
        if (baselineTwo == null)
        {
            baselineTwo = FindObjectOfType<BaselineTwo>();
        }

        return baselineTwo != null && baselineTwo.useKeyboardControl;
    }

    private bool IsArmUIAreaActive()
    {
        if (armUIPlaneCollider == null)
        {
            return false;
        }

        return armUIPlaneCollider.inArmUIArea;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsBaseline2CanvasStateAreaCollider(other))
        {
            if (!touchingBaseline2CanvasStateColliders.Add(other))
            {
                return;
            }

            if (touchingBaseline2CanvasStateColliders.Count == 1)
            {
                SetBaseline2CanvasState(!baseline2CanvasStateIsOn);
            }

            return;
        }

        if (!IsLeftCanvasStateAreaCollider(other))
        {
            return;
        }

        if (!touchingCanvasStateColliders.Add(other))
        {
            return;
        }

        if (touchingCanvasStateColliders.Count == 1)
        {
            SetLeftCanvasState(!leftCanvasStateIsOn);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsBaseline2CanvasStateAreaCollider(other))
        {
            touchingBaseline2CanvasStateColliders.Remove(other);
            return;
        }

        if (!IsLeftCanvasStateAreaCollider(other))
        {
            return;
        }

        touchingCanvasStateColliders.Remove(other);
    }

    private bool IsLeftCanvasStateAreaCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (LeftCanvasStateArea != null)
        {
            Transform areaTransform = LeftCanvasStateArea.transform;
            return other.transform == areaTransform || other.transform.IsChildOf(areaTransform);
        }

        return !string.IsNullOrEmpty(leftCanvasStateAreaTag) && other.CompareTag(leftCanvasStateAreaTag);
    }

    private bool IsBaseline2CanvasStateAreaCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (Baseline2CanvasStateArea != null)
        {
            Transform areaTransform = Baseline2CanvasStateArea.transform;
            return other.transform == areaTransform || other.transform.IsChildOf(areaTransform);
        }

        return !string.IsNullOrEmpty(baseline2CanvasStateAreaTag) && other.CompareTag(baseline2CanvasStateAreaTag);
    }

    private void EnsureLeftCanvasStateRenderer()
    {
        if (LeftCanvasStateRenderer == null && LeftCanvasStateArea != null)
        {
            LeftCanvasStateRenderer = LeftCanvasStateArea.GetComponent<Renderer>();
        }
    }

    private void EnsureBaseline2CanvasStateRenderer()
    {
        if (Baseline2CanvasStateRenderer == null && Baseline2CanvasStateArea != null)
        {
            Baseline2CanvasStateRenderer = Baseline2CanvasStateArea.GetComponent<Renderer>();
        }
    }

    private void SetLeftCanvasState(bool isOn)
    {
        leftCanvasStateIsOn = isOn;
        UpdateLeftCanvasStateMaterial();
        UpdateCanvasPlaneVisibility();
    }

    private void SetBaseline2CanvasState(bool isOn)
    {
        baseline2CanvasStateIsOn = isOn;
        UpdateBaseline2CanvasStateMaterial();
        UpdateCanvasPlaneVisibility();
    }

    private void UpdateLeftCanvasStateMaterial()
    {
        if (LeftCanvasStateRenderer == null)
        {
            return;
        }

        Material targetMaterial = leftCanvasStateIsOn ? leftCanvasOnMaterial : leftCanvasOffMaterial;
        if (targetMaterial == null || LeftCanvasStateRenderer.sharedMaterial == targetMaterial)
        {
            return;
        }

        LeftCanvasStateRenderer.material = targetMaterial;
    }

    private void UpdateBaseline2CanvasStateMaterial()
    {
        if (Baseline2CanvasStateRenderer == null)
        {
            return;
        }

        Material targetMaterial = baseline2CanvasStateIsOn ? leftCanvasOnMaterial : leftCanvasOffMaterial;
        if (targetMaterial == null || Baseline2CanvasStateRenderer.sharedMaterial == targetMaterial)
        {
            return;
        }

        Baseline2CanvasStateRenderer.material = targetMaterial;
    }
}