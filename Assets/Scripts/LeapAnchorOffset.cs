using UnityEngine;

public class LeapAnchorOffset : MonoBehaviour
{
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
    private Vector3 _recordedHandIndexTipPos; // right index finger tip
    private Vector3 _recordedGripperIndexTipPos; // gripper index finger tip
    private Vector3 _recordedLeftThumbTipPos; // left index finger tip
    
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
            Transform currentFingerTip = null;
            
            // Get appropriate transforms based on active retarget script
            if (useIndex)
            {
                gripperTip = retargetIndex.GetGripperIndexTip();
                currentFingerTip = leftThumbTip;
            }
            else if (useMiddle)
            {
                gripperTip = retargetMiddle.GetGripperMiddleTip();
                currentFingerTip = leftIndexTip;
            }
            else if (useThumb)
            {
                gripperTip = retargetThumb.GetGripperThumbTip();
                currentFingerTip = leftThumbTip;
            }
            else if (useThumbAbduction)
            {
                gripperTip = retargetThumbAbduction.GetGripperThumbTip();
                currentFingerTip = leftIndexTip;
            }
            
            if (gripperTip != null && currentFingerTip != null)
            {
                Vector3 currentGripperPos = gripperTip.position;
                
                // Recalculate scale factors with current gripper position
                Vector3 touchToHandTip = _recordedHandIndexTipPos - _recordedLeftThumbTipPos;
                Vector3 touchToGripperTip = currentGripperPos - _recordedLeftThumbTipPos;
                
                _scaleFactors = new Vector3(
                    Mathf.Abs(touchToHandTip.x) > 0.0001f ? touchToGripperTip.x / touchToHandTip.x : 1f,
                    Mathf.Abs(touchToHandTip.y) > 0.0001f ? touchToGripperTip.y / touchToHandTip.y : 1f,
                    Mathf.Abs(touchToHandTip.z) > 0.0001f ? touchToGripperTip.z / touchToHandTip.z : 1f
                );
            
                // Calculate offset from the initial touch point
                Vector3 currentFingerPos = currentFingerTip.position;
                Vector3 offsetFromTouchPoint = currentFingerPos - _recordedLeftThumbTipPos;
                
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
            Debug.LogWarning("No retarget script has recorded positions!");
            return;
        }

        // Get positions from the active retarget script
        if (useIndex)
        {
            if (leftThumbTip == null)
            {
                Debug.LogWarning("leftThumbTip not assigned for RetargetIndex!");
                return;
            }
            _recordedHandIndexTipPos = retargetIndex.GetRecordedHandIndexTipPosition();
            _recordedGripperIndexTipPos = retargetIndex.GetRecordedGripperIndexTipPosition();
            _recordedLeftThumbTipPos = retargetIndex.GetRecordedLeftThumbTipPosition();
            Debug.Log("Starting retargeting with RetargetIndex");
        }
        else if (useMiddle)
        {
            if (leftIndexTip == null)
            {
                Debug.LogWarning("leftIndexTip not assigned for RetargetMiddle!");
                return;
            }
            _recordedHandIndexTipPos = retargetMiddle.GetRecordedHandMiddleTipPosition();
            _recordedGripperIndexTipPos = retargetMiddle.GetRecordedGripperMiddleTipPosition();
            _recordedLeftThumbTipPos = retargetMiddle.GetRecordedLeftIndexTipPosition();
            Debug.Log("Starting retargeting with RetargetMiddle");
        }
        else if (useThumb)
        {
            if (leftThumbTip == null)
            {
                Debug.LogWarning("leftThumbTip not assigned for RetargetThumb!");
                return;
            }
            _recordedHandIndexTipPos = retargetThumb.GetRecordedHandThumbTipPosition();
            _recordedGripperIndexTipPos = retargetThumb.GetRecordedGripperThumbTipPosition();
            _recordedLeftThumbTipPos = retargetThumb.GetRecordedLeftThumbTipPosition();
            Debug.Log("Starting retargeting with RetargetThumb");
        }
        else if (useThumbAbduction)
        {
            if (leftIndexTip == null)
            {
                Debug.LogWarning("leftIndexTip not assigned for RetargetThumbAbduction!");
                return;
            }
            _recordedHandIndexTipPos = retargetThumbAbduction.GetRecordedHandThumbTipPosition();
            _recordedGripperIndexTipPos = retargetThumbAbduction.GetRecordedGripperThumbTipPosition();
            _recordedLeftThumbTipPos = retargetThumbAbduction.GetRecordedLeftIndexTipPosition();
            Debug.Log("Starting retargeting with RetargetThumbAbduction");
        }

        // Calculate scale factors based on the distance between hand and gripper
        // relative to the distance from touch point to hand
        Vector3 touchToHandTip = _recordedHandIndexTipPos - _recordedLeftThumbTipPos;
        Vector3 touchToGripperTip = _recordedGripperIndexTipPos - _recordedLeftThumbTipPos;

        // Scale factor = (touch to gripper) / (touch to hand)
        _scaleFactors = new Vector3(
            Mathf.Abs(touchToHandTip.x) > 0.0001f ? touchToGripperTip.x / touchToHandTip.x : 1f,
            Mathf.Abs(touchToHandTip.y) > 0.0001f ? touchToGripperTip.y / touchToHandTip.y : 1f,
            Mathf.Abs(touchToHandTip.z) > 0.0001f ? touchToGripperTip.z / touchToHandTip.z : 1f
        );

        _isRetargeting = true;
        Debug.Log($"Touch point: {_recordedLeftThumbTipPos}");
        Debug.Log($"HandTipPos: {_recordedHandIndexTipPos}, GripperTipPos: {_recordedGripperIndexTipPos}");
        Debug.Log($"Scale factors: {_scaleFactors}");
    }

    // Called by RetargetIndex, RetargetMiddle, RetargetThumb, or RetargetThumbAbduction when all triggers exit
    public void StopRetargeting()
    {
        _isRetargeting = false;
        Debug.Log("Stopped retargeting - returning to normal");
    }

    public bool IsRetargeting()
    {
        return _isRetargeting;
    }
}
