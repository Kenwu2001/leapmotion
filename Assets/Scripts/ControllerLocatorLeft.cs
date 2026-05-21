using UnityEngine;
using UnityEngine.XR;

public class ControllerLocatorLeft : MonoBehaviour
{
    [Header("Debug")]
    public Vector3 currentControllerPosition;
    public Vector3 currentControllerEulerAngles;
    public string controllerPoseDebug = "LeftHand controller not tracked";

    void Update()
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!device.isValid)
        {
            controllerPoseDebug = "LeftHand controller not tracked";
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
        }
        else
        {
            controllerPoseDebug = "LeftHand controller pose unavailable";
        }
    }
}