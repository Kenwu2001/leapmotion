using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class LeapToHeadMapper : MonoBehaviour
{
    public Transform headTransform;      // Main Camera of the XR Rig (user's head)
    public Transform leftHandModel;      // Left hand model
    public Transform rightHandModel;     // Right hand model

    void Update()
    {
        UpdateHand(XRNode.LeftHand, leftHandModel);
        UpdateHand(XRNode.RightHand, rightHandModel);
    }

    void UpdateHand(XRNode handNode, Transform handModel)
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(handNode, devices);

        foreach (var device in devices)
        {
            if (device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 devicePosition) &&
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRotation))
            {
                // Convert the hand position to a local coordinate system with the head as the origin
                Vector3 localToHead = headTransform.InverseTransformPoint(devicePosition);
                Quaternion localRotToHead = Quaternion.Inverse(headTransform.rotation) * deviceRotation;

                handModel.localPosition = localToHead;
                handModel.localRotation = localRotToHead;
            }
        }
    }
}