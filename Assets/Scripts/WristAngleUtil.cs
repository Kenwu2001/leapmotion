using UnityEngine;

public static class WristAngleUtil
{
    // Return 0~360 degrees
    public static float GetWristAngleDeg(Transform rWrist)
    {
        // 1. Get the back of the hand direction (Leap's R_wrist, up is usually the back of the hand normal)
        Vector3 up = rWrist.up;

        // 2. Project onto the horizontal plane (ignore y)
        Vector3 flat = new Vector3(up.x, 0f, up.z);

        if (flat.sqrMagnitude < 1e-6f)
            return 0f;

        flat.Normalize();

        // 3. Z+ is 0 degrees, clockwise 0~360
        float angle = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        return angle;
    }
}
