using UnityEngine;

public class LeftHandTouchDetector : MonoBehaviour
{
    public Transform leftHandPoint;

    [Header("Distance mapping")]
    public float maxDistance = 0.04f;
    public float minDistance = 0.005f;

    [Header("Visual points")]
    public Transform rightFingerPoint;
    public Transform clawFingerPoint;

    [Header("Mode Control")]
    public ModeSwitching modeSwitching;

    [Header("Recorded Offset")]
    private Vector3 _recordedOffset = Vector3.zero;
    private bool _isInZone = false;
    public Vector3 RecordedOffset => _recordedOffset;
    public bool IsInZone => _isInZone;

    private void Start()
    {
        // Hide visual points initially
        if (rightFingerPoint != null)
            rightFingerPoint.gameObject.SetActive(false);
        if (clawFingerPoint != null)
            clawFingerPoint.gameObject.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        // Only drive when in modeSelect
        if (modeSwitching == null || !modeSwitching.modeSelect)
            return;

        // DEBUG: Log which collider we're touching
        Debug.Log($"[LeftHandTouchDetector] Touching: {other.gameObject.name}");

        RightFingerTouchZone zone =
            other.GetComponent<RightFingerTouchZone>();
        if (zone == null) return;

        int seg;
        float segT;
        Vector3 rightPos;

        FingerMath.ClosestPointOnFinger(
            leftHandPoint.position,
            zone.rightFinger,
            out seg,
            out segT,
            out rightPos
        );

        // Clamp segT to ensure it's within valid range [0, 1]
        segT = Mathf.Clamp01(segT);

        rightFingerPoint.position = rightPos;
        if (!rightFingerPoint.gameObject.activeSelf)
            rightFingerPoint.gameObject.SetActive(true);

        // Ensure seg is within valid range for claw finger
        int clawJointCount = zone.clawFinger.GetJointCount();
        seg = Mathf.Clamp(seg, 0, clawJointCount - 2); // Max seg is jointCount - 2

        Vector3 clawPos =
            Vector3.Lerp(
                zone.clawFinger.GetJoint(seg),
                zone.clawFinger.GetJoint(seg + 1),
                segT
            );

        clawFingerPoint.position = clawPos;
        if (!clawFingerPoint.gameObject.activeSelf)
            clawFingerPoint.gameObject.SetActive(true);

        // Record the offset between clawFingerPoint and rightFingerPoint
        _recordedOffset = clawPos - rightPos;
        _isInZone = true;

        float dist =
            Vector3.Distance(leftHandPoint.position, rightPos);

        float t =
            Mathf.InverseLerp(maxDistance, minDistance, dist);
        t = Mathf.Clamp01(t);

        ApplyToClaw(zone.clawFinger, t);
    }

    private void OnTriggerExit(Collider other)
    {
        RightFingerTouchZone zone = other.GetComponent<RightFingerTouchZone>();
        if (zone == null) return;

        // Reset offset when leaving the zone
        _recordedOffset = Vector3.zero;
        _isInZone = false;
        
        // Hide visual points
        if (rightFingerPoint != null)
            rightFingerPoint.gameObject.SetActive(false);
        if (clawFingerPoint != null)
            clawFingerPoint.gameObject.SetActive(false);
    }

    void ApplyToClaw(FingerPath clawFinger, float t)
    {
        // ✅ Connect to Motor / LED / Servo here
        // Debug.Log($"{clawFinger.name} t={t}");
    }
}