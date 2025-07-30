using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class LeapToHeadMapper : MonoBehaviour
{
    public Transform headTransform;      // XR Rig 的 Main Camera (使用者頭部)
    public Transform leftHandModel;      // 左手模型
    public Transform rightHandModel;     // 右手模型

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
                // 將手的位置轉換為以頭部為原點的區域座標
                Vector3 localToHead = headTransform.InverseTransformPoint(devicePosition);
                Quaternion localRotToHead = Quaternion.Inverse(headTransform.rotation) * deviceRotation;

                handModel.localPosition = localToHead;
                handModel.localRotation = localRotToHead;
            }
        }
    }
}