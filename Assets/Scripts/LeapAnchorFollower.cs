using UnityEngine;

public class LeapAnchorFollower : MonoBehaviour
{
    [Header("追蹤的目標（如 R_Wrist）")]
    public Transform handWrist;

    [Header("Leap Motion 在手上的相對位置（相對於手）")]
    public Vector3 offset = new Vector3(0.05f, 0.15f, 0f);

    [Header("是否跟隨旋轉")]
    public bool followRotation = true;

    void LateUpdate() // 在動畫與更新都結束後處理，避免 jitter
    {
        if (handWrist == null) return;

        // 計算手部前方某一點的位置（模擬 Leap 綁在這裡）
        transform.position = handWrist.TransformPoint(offset);

        if (followRotation)
        {
            transform.rotation = handWrist.rotation;
        }
    }
}