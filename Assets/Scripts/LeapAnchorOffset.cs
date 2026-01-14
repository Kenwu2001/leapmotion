using UnityEngine;

public class LeapAnchorOffset : MonoBehaviour
{
    // scripts
    public RetargetIndex retargetIndex;
    public RetargetMiddle retargetMiddle;
    public RetargetThumb retargetThumb;
    public RetargetThumbAbduction retargetThumbAbduction;

    public Transform leftThumbTip;
    public Transform leftIndexTip;
    
    public Transform baseLeapAnchorPosition;

    [Header("Smooth Transition")]
    [Tooltip("Speed of smooth transition (higher = faster, 0 = instant)")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.1f;

    // private Vector3 _initialLocalPos;
    private bool _isRetargeting = false;
    
    // Recorded positions when trigger first detected
    private Vector3 _recordedRightHandTipPos;
    private Vector3 _recordedGripperTipPos;
    private Vector3 _recordedLeftTipPos; // left thumb tip or left index tip
    
    // Scale factors for retargeting
    private Vector3 _scaleFactors;

    // void Awake()
    // {
    //     _initialLocalPos = transform.localPosition;
    // }

    void LateUpdate()
    {
        // Determine which retarget script is active
        bool useIndex = retargetIndex != null && retargetIndex.HasRecordedPositions();
        bool useMiddle = retargetMiddle != null && retargetMiddle.HasRecordedPositions();
        bool useThumb = retargetThumb != null && retargetThumb.HasRecordedPositions();
        bool useThumbAbduction = retargetThumbAbduction != null && retargetThumbAbduction.HasRecordedPositions();

        if (_isRetargeting && (useIndex || useMiddle || useThumb || useThumbAbduction))
        {
            Transform gripperTip = null;
            Transform currentLeftFingerTip = null;
            Transform currentRightHandTip = null;
            
            // Get appropriate transforms based on active retarget script
            if (useIndex)
            {
                gripperTip = retargetIndex.GetGripperIndexTip();
                currentLeftFingerTip = leftThumbTip;
                currentRightHandTip = retargetIndex.handIndexTip;
            }
            else if (useMiddle)
            {
                gripperTip = retargetMiddle.GetGripperMiddleTip();
                currentLeftFingerTip = leftIndexTip;
                currentRightHandTip = retargetMiddle.handMiddleTip;
            }
            else if (useThumb)
            {
                gripperTip = retargetThumb.GetGripperThumbTip();
                currentLeftFingerTip = leftThumbTip;
                currentRightHandTip = retargetThumb.handThumbTip;
            }
            else if (useThumbAbduction)
            {
                gripperTip = retargetThumbAbduction.GetGripperThumbTip();
                currentLeftFingerTip = leftIndexTip;
                currentRightHandTip = retargetThumbAbduction.handThumbTip;
            }
            
            if (gripperTip != null && currentLeftFingerTip != null && currentRightHandTip != null)
            {
                Vector3 currentGripperPos = gripperTip.position;
                Vector3 currentRightHandTipPos = currentRightHandTip.position;
                
                // Get dynamic touch point that moves with the collider
                Vector3 dynamicTouchPoint = _recordedLeftTipPos;
                if (useIndex)
                    dynamicTouchPoint = retargetIndex.GetDynamicLeftThumbTipPosition();
                else if (useMiddle)
                    dynamicTouchPoint = retargetMiddle.GetDynamicLeftIndexTipPosition();
                else if (useThumb)
                    dynamicTouchPoint = retargetThumb.GetDynamicLeftThumbTipPosition();
                else if (useThumbAbduction)
                    dynamicTouchPoint = retargetThumbAbduction.GetDynamicLeftIndexTipPosition();
                
                // Recalculate scale factors with current positions
                Vector3 touchToRightHandTip = currentRightHandTipPos - dynamicTouchPoint;
                Vector3 touchToGripperTip = currentGripperPos - dynamicTouchPoint;
                
                _scaleFactors = new Vector3(
                    Mathf.Abs(touchToRightHandTip.x) > 0.0001f ? touchToGripperTip.x / touchToRightHandTip.x : 1f,
                    Mathf.Abs(touchToRightHandTip.y) > 0.0001f ? touchToGripperTip.y / touchToRightHandTip.y : 1f,
                    Mathf.Abs(touchToRightHandTip.z) > 0.0001f ? touchToGripperTip.z / touchToRightHandTip.z : 1f
                );
            
                // Calculate offset from the dynamic touch point
                Vector3 currentLeftFingerPos = currentLeftFingerTip.position;
                Vector3 offsetFromTouchPoint = currentLeftFingerPos - dynamicTouchPoint;
                
                // Apply scale factors to retarget to gripper space
                // Index and Thumb use negative x, Middle and ThumbAbduction use positive x
                float xMultiplier = useIndex ? -1f : 1f;
                float zMultiplier = (useThumb || useThumbAbduction) ? -1f : 1f;
                Vector3 scaledOffset = new Vector3(
                    xMultiplier * offsetFromTouchPoint.x * _scaleFactors.x,
                    offsetFromTouchPoint.y * _scaleFactors.y,
                    zMultiplier * offsetFromTouchPoint.z * _scaleFactors.z
                );
                
                // Use base position as reference point
                Vector3 targetPos = baseLeapAnchorPosition.position + scaledOffset;
                
                // Smoothly interpolate to target position
                transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
            }
        }
        else
        {
            // Vector3 offset = new Vector3(0f, 0f, 0.2f);
            // transform.position = baseLeapAnchorPosition.position + offset;
            transform.position = baseLeapAnchorPosition.position;
        }
    }

    // Called by RetargetIndex, RetargetMiddle, RetargetThumb, or RetargetThumbAbduction when trigger enters
    public void StartRetargeting()
    {
        // Determine which retarget script is calling
        bool useIndex = retargetIndex != null && retargetIndex.HasRecordedPositions();
        bool useMiddle = retargetMiddle != null && retargetMiddle.HasRecordedPositions();
        bool useThumb = retargetThumb != null && retargetThumb.HasRecordedPositions();
        bool useThumbAbduction = retargetThumbAbduction != null && retargetThumbAbduction.HasRecordedPositions();

        if (!useIndex && !useMiddle && !useThumb && !useThumbAbduction)
        {
            // Debug.LogWarning("No retarget script has recorded positions!");
            return;
        }

        // Get positions from the active retarget script
        if (useIndex)
        {
            if (leftThumbTip == null)
            {
                // Debug.LogWarning("leftThumbTip not assigned for RetargetIndex!");
                return;
            }
            _recordedRightHandTipPos = retargetIndex.GetRecordedHandIndexTipPosition();
            _recordedGripperTipPos = retargetIndex.GetRecordedGripperIndexTipPosition();
            _recordedLeftTipPos = retargetIndex.GetRecordedLeftThumbTipPosition(); // the point where the trigger was first touched when enterring collider
            // Debug.Log("Starting retargeting with RetargetIndex");
        }
        else if (useMiddle)
        {
            if (leftIndexTip == null)
            {
                // Debug.LogWarning("leftIndexTip not assigned for RetargetMiddle!");
                return;
            }
            _recordedRightHandTipPos = retargetMiddle.GetRecordedHandMiddleTipPosition();
            _recordedGripperTipPos = retargetMiddle.GetRecordedGripperMiddleTipPosition();
            _recordedLeftTipPos = retargetMiddle.GetRecordedLeftIndexTipPosition();
            // Debug.Log("Starting retargeting with RetargetMiddle");
        }
        else if (useThumb)
        {
            if (leftThumbTip == null)
            {
                // Debug.LogWarning("leftThumbTip not assigned for RetargetThumb!");
                return;
            }
            _recordedRightHandTipPos = retargetThumb.GetRecordedHandThumbTipPosition();
            _recordedGripperTipPos = retargetThumb.GetRecordedGripperThumbTipPosition();
            _recordedLeftTipPos = retargetThumb.GetRecordedLeftThumbTipPosition();
            // Debug.Log("Starting retargeting with RetargetThumb");
        }
        else if (useThumbAbduction)
        {
            if (leftIndexTip == null)
            {
                // Debug.LogWarning("leftIndexTip not assigned for RetargetThumbAbduction!");
                return;
            }
            _recordedRightHandTipPos = retargetThumbAbduction.GetRecordedHandThumbTipPosition();
            _recordedGripperTipPos = retargetThumbAbduction.GetRecordedGripperThumbTipPosition();
            _recordedLeftTipPos = retargetThumbAbduction.GetRecordedLeftIndexTipPosition();
            // Debug.Log("Starting retargeting with RetargetThumbAbduction");
        }

        // Calculate scale factors based on the distance between hand and gripper
        // relative to the distance from touch point to hand
        Vector3 touchToRightHandTip = _recordedRightHandTipPos - _recordedLeftTipPos;
        Vector3 touchToGripperTip = _recordedGripperTipPos - _recordedLeftTipPos;

        // Scale factor = (touch to gripper) / (touch to hand)
        _scaleFactors = new Vector3(
            Mathf.Abs(touchToRightHandTip.x) > 0.0001f ? touchToGripperTip.x / touchToRightHandTip.x : 1f,
            Mathf.Abs(touchToRightHandTip.y) > 0.0001f ? touchToGripperTip.y / touchToRightHandTip.y : 1f,
            Mathf.Abs(touchToRightHandTip.z) > 0.0001f ? touchToGripperTip.z / touchToRightHandTip.z : 1f
        );

        _isRetargeting = true;
        // Debug.Log($"Touch point: {_recordedLeftTipPos}");
        // Debug.Log($"HandTipPos: {_recordedRightHandTipPos}, GripperTipPos: {_recordedGripperTipPos}");
        // Debug.Log($"Scale factors: {_scaleFactors}");
    }

    // Called by RetargetIndex, RetargetMiddle, RetargetThumb, or RetargetThumbAbduction when all triggers exit
    public void StopRetargeting()
    {
        _isRetargeting = false;
        // Debug.Log("Stopped retargeting - returning to normal");
    }

    public bool IsRetargeting()
    {
        return _isRetargeting;
    }
}
