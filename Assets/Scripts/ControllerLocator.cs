using UnityEngine;
using UnityEngine.XR;

public class ControllerLocator : MonoBehaviour
{
    [Header("選擇要追蹤的控制器")]
    public XRNode which = XRNode.RightHand;

    [Header("要比較的物件（例如某個目標或道具）")]
    public Transform target;

    void Update()
    {
        // 取得指定控制器
        var device = InputDevices.GetDeviceAtXRNode(which);
        if (!device.isValid) return;

        // 更新控制器位置與旋轉
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out var pos) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rot))
        {
            transform.localPosition = pos;
            transform.localRotation = rot;

            // 若指定了目標，就計算相對位置
            if (target != null)
            {
                Vector3 localPos = transform.InverseTransformPoint(target.position);
            }
        }
    }
}