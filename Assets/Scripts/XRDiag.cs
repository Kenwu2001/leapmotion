using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRDiag : MonoBehaviour
{
    public XRNode which = XRNode.RightHand;

    float nextListTime;

    void Update()
    {
        // 每 3 秒列一次所有 XR 裝置
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

        // 嘗試讀取指定控制器的 pose
        var device = InputDevices.GetDeviceAtXRNode(which);
        if (!device.isValid)
        {
            // Debug.Log($"[XRDiag] {which} device.isValid = false (可能沒連線、在手追蹤模式、或平台設定不對)");
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
            // Debug.Log($"[XRDiag] {which} 有裝置，但沒有回傳 pose（可能還在初始化/喚醒中）");
        }
    }
}