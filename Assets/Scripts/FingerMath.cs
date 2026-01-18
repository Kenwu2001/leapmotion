using UnityEngine;

public static class FingerMath
{
    public static float DistancePointToSegment(
        Vector3 p,
        Vector3 a,
        Vector3 b,
        out float t
    )
    {
        Vector3 ab = b - a;
        t = Vector3.Dot(p - a, ab) / Vector3.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        Vector3 closest = a + ab * t;
        return Vector3.Distance(p, closest);
    }

    public static void ClosestPointOnFinger(
        Vector3 point,
        FingerPath finger,
        out int segmentIndex,
        out float segmentT,
        out Vector3 closestPoint
    )
    {
        float min = float.MaxValue;
        segmentIndex = 0;
        segmentT = 0f;
        closestPoint = Vector3.zero;

        // Use actual joint count instead of hardcoded 4
        int segmentCount = finger.GetJointCount() - 1;
        
        for (int i = 0; i < segmentCount; i++)
        {
            float t;
            float d = DistancePointToSegment(
                point,
                finger.GetJoint(i),
                finger.GetJoint(i + 1),
                out t
            );

            if (d < min)
            {
                min = d;
                segmentIndex = i;
                segmentT = t;
                closestPoint =
                    Vector3.Lerp(
                        finger.GetJoint(i),
                        finger.GetJoint(i + 1),
                        t
                    );
            }
        }
    }
}