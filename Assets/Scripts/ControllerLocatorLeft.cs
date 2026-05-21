using UnityEngine;
using UnityEngine.XR;

public class ControllerLocatorLeft : MonoBehaviour
{
    [Header("Debug")]
    public Vector3 currentControllerPosition;
    public Vector3 currentControllerEulerAngles;
    public string controllerPoseDebug = "LeftHand controller not tracked";

    [Header("Fallback Display")]
    public GameObject leftHandModel;
    public GameObject canvasPlane;
    public bool alwaysShowCanvasPlane = false;
    public bool previewCanvasPlaneOffset = false;
    public float canvasShowDelaySeconds = 1f;

    private Transform canvasOriginalParent;
    private Vector3 canvasInitialLocalPosition;
    private Quaternion canvasInitialLocalRotation;
    private Vector3 canvasInitialLocalScale;
    private bool isCanvasFrozenInWorld;
    private bool wasPreviewCanvasPlaneOffset;
    private bool wasAlwaysShowCanvasPlane;
    private float leftHandHiddenTimer;

    void Awake()
    {
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

        bool isLeftHandVisible = IsLeftHandModelVisible();

        if (isLeftHandVisible)
        {
            leftHandHiddenTimer = 0f;
            HideCanvasPlane();
            return;
        }

        leftHandHiddenTimer += Time.deltaTime;

        if (leftHandHiddenTimer < canvasShowDelaySeconds)
        {
            HideCanvasPlane();
            return;
        }

        ShowCanvasPlaneAtFrozenWorldPose();
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

    private bool IsLeftHandModelVisible()
    {
        if (leftHandModel == null)
        {
            return false;
        }

        if (!leftHandModel.activeInHierarchy)
        {
            return false;
        }

        Renderer[] renderers = leftHandModel.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return leftHandModel.activeInHierarchy;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer.enabled && renderer.isVisible)
            {
                return true;
            }
        }

        return false;
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