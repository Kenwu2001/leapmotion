using UnityEngine;

/// <summary>
/// Similar to LeftHandTouchDetector but used during retargeting mode (modeSelect=false)
/// Detects when left hand touches right hand finger zones with 2-joint fingers
/// </summary>
public class RetargetTouchDetector : MonoBehaviour
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
        
        Debug.Log($"[RetargetTouchDetector] Started on: {gameObject.name}");
        
        // Check if this object has a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[RetargetTouchDetector] ERROR: No Collider on {gameObject.name}! OnTriggerStay will never be called!");
        }
        else
        {
            Debug.Log($"[RetargetTouchDetector] Found Collider: {col.GetType().Name}, IsTrigger={col.isTrigger}");
        }
        
        // Check for Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning($"[RetargetTouchDetector] No Rigidbody on {gameObject.name}. Make sure at least one colliding object has Rigidbody!");
        }
        else
        {
            Debug.Log($"[RetargetTouchDetector] Found Rigidbody: IsKinematic={rb.isKinematic}");
        }
        
        if (leftHandPoint == null) Debug.LogError("[RetargetTouchDetector] leftHandPoint is NULL!");
        if (rightFingerPoint == null) Debug.LogWarning("[RetargetTouchDetector] rightFingerPoint is NULL!");
        if (clawFingerPoint == null) Debug.LogWarning("[RetargetTouchDetector] clawFingerPoint is NULL!");
        if (modeSwitching == null) Debug.LogError("[RetargetTouchDetector] modeSwitching is NULL!");
    }

    private void OnTriggerStay(Collider other)
    {
        // Only drive when NOT in modeSelect (i.e., during retargeting)
        if (modeSwitching == null || modeSwitching.modeSelect)
        {
            Debug.Log($"[RetargetTouchDetector] Blocked: modeSwitching={(modeSwitching != null ? modeSwitching.modeSelect.ToString() : "null")}");
            return;
        }

        Debug.Log($"[RetargetTouchDetector] OnTriggerStay with: {other.gameObject.name}");

        RightFingerTouchZone zone =
            other.GetComponent<RightFingerTouchZone>();
        if (zone == null)
        {
            Debug.LogWarning($"[RetargetTouchDetector] No RightFingerTouchZone on: {other.gameObject.name}");
            return;
        }
        
        Debug.Log($"[RetargetTouchDetector] Found zone! RightFinger={(zone.rightFinger != null ? zone.rightFinger.name : "null")}, ClawFinger={(zone.clawFinger != null ? zone.clawFinger.name : "null")}");
        
        // DEBUG: Check joint count
        if (zone.clawFinger != null)
        {
            int jointCount = zone.clawFinger.GetJointCount();
            Debug.LogWarning($"[RetargetTouchDetector] ClawFinger '{zone.clawFinger.name}' has {jointCount} joints. Expected 2 for modeSelect=false!");
            if (jointCount != 2)
            {
                Debug.LogError($"[RetargetTouchDetector] ERROR: Wrong FingerPath! You assigned a {jointCount}-joint FingerPath instead of a 2-joint one!");
            }
        }

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

        Debug.Log($"[RetargetTouchDetector] Set rightFingerPoint to: {rightPos}");

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

        Debug.Log($"[RetargetTouchDetector] Set clawFingerPoint to: {clawPos}");

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
        // ✅ Connect to Motor / LED / Servo here for retargeting mode
        // Debug.Log($"[Retarget Mode] {clawFinger.name} t={t}");
    }
}
