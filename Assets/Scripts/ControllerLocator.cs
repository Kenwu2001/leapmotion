using UnityEngine;
using UnityEngine.XR;

public class ControllerLocator : MonoBehaviour
{
    [Header("選擇哪支控制器")]
    public XRNode which = XRNode.RightHand; // LeftHand 也行

    [Header("若要把控制器當手腕，可加一點偏移(公尺)")]
    public Vector3 wristLocalOffset = new Vector3(0f, -0.05f, -0.07f); // 依需要調整
    public Vector3 wristLocalEuler = Vector3.zero;

    void Update()
    {
        var device = InputDevices.GetDeviceAtXRNode(which);
        if (!device.isValid) return;

        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out var pos) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out var rot))
        {
            // 這裡的 pos/rot 是相對於 XRRig/Camera Offset 的座標
            transform.localPosition = pos;
            transform.localRotation = rot;

            // 若要近似手腕，套一個本地偏移（由控制器到手腕的向量）
            var wristWorldPos = transform.TransformPoint(wristLocalOffset);
            var wristWorldRot = transform.rotation * Quaternion.Euler(wristLocalEuler);

            // 1) 控制器自身的世界座標
            Debug.Log($"[Controller {which}] world pos = {transform.position}");

            // 2) 近似手腕的世界座標（如果你要拿控制器當臨時手腕）
            Debug.Log($"[Proxy Wrist {which}] world pos = {wristWorldPos}, rot = {wristWorldRot}");
        }
    }
}