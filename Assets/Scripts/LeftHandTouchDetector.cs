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

    public BaselineTwo baselineTwo;

    [Tooltip("Optional: lets this script report current projection mode (FivePoint/TwoPoint/FrozenLine)")]
    public SelectMotorCollider selectMotorCollider;

    [Header("Retargeting Debug")]
    [Tooltip("Print retargeting details to Console for verification")]
    public bool enableRetargetingDebug = false;

    [Tooltip("Minimum seconds between debug logs")]
    public float retargetingDebugInterval = 0.25f;

    [Header("Recorded Offset")]
    private Vector3 _recordedOffset = Vector3.zero;
    private bool _isInZone = false;
    private float _lastRetargetingDebugTime = -999f;
    public Vector3 RecordedOffset => _recordedOffset;
    public bool IsInZone => _isInZone;

    private void Start()
    {
        if (baselineTwo == null)
            baselineTwo = FindObjectOfType<BaselineTwo>();

        // Hide visual points initially
        if (rightFingerPoint != null)
            rightFingerPoint.gameObject.SetActive(false);
        if (clawFingerPoint != null)
            clawFingerPoint.gameObject.SetActive(false);
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsBaseline2Active())
        {
            ClearTrackingState();
            return;
        }

        // Only drive when in modeSelect
        if (modeSwitching == null || !modeSwitching.modeSelect)
            return;

        // DEBUG: Log which collider we're touching
        // Debug.Log($"[LeftHandTouchDetector] Touching: {other.gameObject.name}");

        RightFingerTouchZone zone =
            other.GetComponent<RightFingerTouchZone>();
        if (zone == null) return;

        if (leftHandPoint == null || zone.rightFinger == null || zone.clawFinger == null)
            return;

        int rightJointCount = zone.rightFinger.GetJointCount();
        int clawJointCount = zone.clawFinger.GetJointCount();
        if (rightJointCount < 2 || clawJointCount < 2)
            return;

        bool useTwoPoint = selectMotorCollider != null &&
            (selectMotorCollider.projectionMode == ProjectionMode.TwoPoint ||
             selectMotorCollider.projectionMode == ProjectionMode.FrozenLine);

        int seg;
        float segT;
        Vector3 rightPos;

        // rawSeg/rawSegT represent the location on the right-hand path before claw mapping.
        int rawSeg;
        float rawSegT;

        if (useTwoPoint)
        {
            // Two-point mode: project only on tip->base as one segment.
            Vector3 tipPoint = zone.rightFinger.GetJoint(0);
            Vector3 basePoint = zone.rightFinger.GetJoint(rightJointCount - 1);

            FingerMath.DistancePointToSegment(leftHandPoint.position, tipPoint, basePoint, out segT);
            segT = Mathf.Clamp01(segT);

            rightPos = Vector3.Lerp(tipPoint, basePoint, segT);
            rawSeg = 0;
            rawSegT = segT;

            // Map 0-100% to all claw segments uniformly.
            int clawSegmentCount = clawJointCount - 1;
            float percentage = segT * 100f;
            float step = 100f / clawSegmentCount;

            if (percentage >= 100f)
            {
                seg = clawSegmentCount - 1;
                segT = 1f;
            }
            else
            {
                seg = Mathf.Clamp((int)(percentage / step), 0, clawSegmentCount - 1);
                segT = Mathf.Clamp01((percentage - seg * step) / step);
            }
        }
        else
        {
            FingerMath.ClosestPointOnFinger(
                leftHandPoint.position,
                zone.rightFinger,
                out seg,
                out segT,
                out rightPos
            );

            // Clamp segT to ensure it's within valid range [0, 1]
            segT = Mathf.Clamp01(segT);
            rawSeg = seg;
            rawSegT = segT;

            // Align claw segment indexing with the 6-point strategy used by SelectMotorCollider.
            // When claw has 6+ points, element0 is the newly inserted point and normal mapping starts at element1.
            if (clawJointCount >= 6)
                seg = Mathf.Clamp(seg + 1, 0, clawJointCount - 2);
            else
                seg = Mathf.Clamp(seg, 0, clawJointCount - 2);
        }

        rightFingerPoint.position = rightPos;
        if (!rightFingerPoint.gameObject.activeSelf)
            rightFingerPoint.gameObject.SetActive(true);

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

        // MaybeLogRetargeting(
        //     zone,
        //     rightJointCount,
        //     clawJointCount,
        //     rawSeg,
        //     rawSegT,
        //     seg,
        //     dist,
        //     t
        // );

        ApplyToClaw(zone.clawFinger, t);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsBaseline2Active())
        {
            ClearTrackingState();
            return;
        }

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

    private bool IsBaseline2Active()
    {
        if (baselineTwo == null)
            baselineTwo = FindObjectOfType<BaselineTwo>();

        return baselineTwo != null && baselineTwo.useKeyboardControl;
    }

    private void ClearTrackingState()
    {
        _recordedOffset = Vector3.zero;
        _isInZone = false;

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

    private void MaybeLogRetargeting(
        RightFingerTouchZone zone,
        int rightJointCount,
        int clawJointCount,
        int rawSeg,
        float rawSegT,
        int mappedSeg,
        float dist,
        float t
    )
    {
        if (!enableRetargetingDebug)
            return;

        if (Time.time - _lastRetargetingDebugTime < retargetingDebugInterval)
            return;

        _lastRetargetingDebugTime = Time.time;

        string projectionInfo = "SelectMotorCollider=unassigned";
        if (selectMotorCollider != null)
            projectionInfo = $"projectionMode={selectMotorCollider.projectionMode}";

        Debug.Log(
            "[LeftHandTouchDetector] RetargetingDebug" +
            $" | zone={zone.name}" +
            $" | {projectionInfo}" +
            $" | rightJoints={rightJointCount} (segments={Mathf.Max(0, rightJointCount - 1)})" +
            $" | clawJoints={clawJointCount} (segments={Mathf.Max(0, clawJointCount - 1)})" +
            $" | rawSeg={rawSeg}, rawSegT={rawSegT:F3}" +
            $" | mappedClawSeg={mappedSeg}" +
            $" | dist={dist:F4}, t={t:F3}"
        );
    }
}