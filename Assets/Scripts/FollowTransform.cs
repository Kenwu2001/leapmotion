using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    [Header("Target to follow")]
    public Transform target;

    [Header("Offset")]
    public Vector3 localOffset = Vector3.zero; // 以 target 的局部座標做偏移
    public bool followRotation = true;

    void LateUpdate()
    {
        if (!target) return;

        // 跟隨位置（含局部偏移）
        transform.position = target.TransformPoint(localOffset);

        if (followRotation)
        {
            // 只取 target 的 X rotation
            Vector3 targetEuler = target.rotation.eulerAngles;

            // 只跟隨 X 軸旋轉，其餘固定為 0
            transform.rotation = Quaternion.Euler(targetEuler.x, 0f, 0f);
        }
    }
}