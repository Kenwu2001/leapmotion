using UnityEngine;
using UnityEngine.XR;

public class ControllerLocator : MonoBehaviour
{
    [Header("Select the controller to track")]
    public XRNode which = XRNode.RightHand;

    [Header("Object to compare (e.g., a target or prop)")]
    public Transform target;

    void Update()
    {
        // Get the specified controller
        var device = InputDevices.GetDeviceAtXRNode(which);
        if (!device.isValid) return;

        // Update controller position and rotation
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out var pos) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rot))
        {
            transform.localPosition = pos;
            transform.localRotation = rot;

            // If a target is specified, compute relative position
            if (target != null)
            {
                Vector3 localPos = transform.InverseTransformPoint(target.position);
            }
        }
    }
}