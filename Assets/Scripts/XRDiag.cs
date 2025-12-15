using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRDiag : MonoBehaviour
{
    public XRNode which = XRNode.RightHand;

    float nextListTime;

    void Update()
    {
        // List all XR devices every 3 seconds
        if (Time.time >= nextListTime)
        {
            nextListTime = Time.time + 3f;
            var devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            // Debug.Log($"[XRDiag] devices count = {devices.Count}");
            foreach (var d in devices)
            {
                // Debug.Log($"[XRDiag] device: name='{d.name}', role={d.characteristics}");
            }
        }

        // Try to read pose of specified controller
        var device = InputDevices.GetDeviceAtXRNode(which);
        if (!device.isValid)
        {
            // Debug.Log($"[XRDiag] {which} device.isValid = false (possibly not connected, in hand tracking mode, or platform settings incorrect)");
            return;
        }

        bool hasPos = device.TryGetFeatureValue(CommonUsages.devicePosition, out var pos);
        bool hasRot = device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rot);
        if (hasPos || hasRot)
        {
            // Debug.Log($"[XRDiag] {which} posOK={hasPos} rotOK={hasRot} pos={pos}");
        }
        else
        {
            // Debug.Log($"[XRDiag] {which} device present, but no pose returned (possibly still initializing/waking up)");
        }
    }
}