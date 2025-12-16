using UnityEngine;

public static class WristAngleUtil
{
    // 回傳 0~360 度
    public static float GetWristAngleDeg(Transform rWrist)
    {
        // 1. 取手背朝向（Leap 的 R_wrist，up 通常是手背法線）
        Vector3 up = rWrist.up;

        // 2. 投影到水平面（忽略 y）
        Vector3 flat = new Vector3(up.x, 0f, up.z);

        if (flat.sqrMagnitude < 1e-6f)
            return 0f;

        flat.Normalize();

        // 3. Z+ 為 0 度，順時針 0~360
        float angle = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}
