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

    [Header("Initial Hidden Objects")]
    [Tooltip("Assign up to three GameObjects to hide at Start")] 
    public GameObject colliderBall;
    public GameObject rightFingertipBall;
    public GameObject gripperBall;
    public GameObject leftFingertipBall;

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
    private Vector3 _targetScaleFactors; // Target scale factors to smoothly interpolate towards
    
    [Header("Scale Factor Stability")]
    [Tooltip("Speed of scale factor updates (lower = more stable, higher = more responsive)")]
    [Range(0.01f, 0.5f)]
    public float scaleFactorDamping = 0.05f;
    
    [Tooltip("Minimum allowed scale factor to prevent extreme values")]
    public float minScaleFactor = -5f;
    
    [Tooltip("Maximum allowed scale factor to prevent extreme values")]
    public float maxScaleFactor = 5f;

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
                
                Vector3 currentLeftFingerPos = currentLeftFingerTip.position;
                
                // DEBUG: Key positions
                Debug.Log($"[Positions] Gripper:{currentGripperPos.ToString("F3")} | RightHand:{currentRightHandTipPos.ToString("F3")} | TouchPoint:{dynamicTouchPoint.ToString("F3")} | LeftFinger:{currentLeftFingerPos.ToString("F3")}");
                
                // Recalculate scale factors with current positions
                Vector3 touchToRightHandTip = currentRightHandTipPos - dynamicTouchPoint;
                Vector3 touchToGripperTip = currentGripperPos - dynamicTouchPoint;
                
                // DEBUG: Vector distances
                Debug.Log($"[Distances] touchToRightHand:{touchToRightHandTip.magnitude:F4} | touchToGripper:{touchToGripperTip.magnitude:F4}");
                
                // Calculate target scale factors with clamping to prevent extreme values
                _targetScaleFactors = new Vector3(
                    Mathf.Abs(touchToRightHandTip.x) > 0.001f ? Mathf.Clamp(touchToGripperTip.x / touchToRightHandTip.x, minScaleFactor, maxScaleFactor) : 1f,
                    Mathf.Abs(touchToRightHandTip.y) > 0.001f ? Mathf.Clamp(touchToGripperTip.y / touchToRightHandTip.y, minScaleFactor, maxScaleFactor) : 1f,
                    Mathf.Abs(touchToRightHandTip.z) > 0.001f ? Mathf.Clamp(touchToGripperTip.z / touchToRightHandTip.z, minScaleFactor, maxScaleFactor) : 1f
                );
                
                // Smoothly interpolate scale factors to prevent sudden jumps
                _scaleFactors = Vector3.Lerp(_scaleFactors, _targetScaleFactors, scaleFactorDamping);
                
                // DEBUG: Scale factors stability
                Debug.Log($"[ScaleFactors] Target:{_targetScaleFactors.ToString("F3")} | Smoothed:{_scaleFactors.ToString("F3")} | Delta:{(_targetScaleFactors - _scaleFactors).magnitude:F4}");
            
                // Calculate offset from the dynamic touch point
                Vector3 offsetFromTouchPoint = currentLeftFingerPos - dynamicTouchPoint;

                // Apply smoothed scale factors directly (no dynamic sign calculation)
                Vector3 scaledOffset = new Vector3(
                    offsetFromTouchPoint.x * _scaleFactors.x,
                    offsetFromTouchPoint.y * _scaleFactors.y,
                    offsetFromTouchPoint.z * _scaleFactors.z
                );
                
                // DEBUG: Offset calculation
                Debug.Log($"[Offsets] offsetFromTouch:{offsetFromTouchPoint.magnitude:F4} | scaledOffset:{scaledOffset.magnitude:F4}");
                
                // float xMultiplier = useIndex ? -1f : 1f;
                // float zMultiplier = (useThumb || useThumbAbduction) ? -1f : 1f;
                // Vector3 scaledOffset = new Vector3(
                //     xMultiplier * offsetFromTouchPoint.x * _scaleFactors.x,
                //     offsetFromTouchPoint.y * _scaleFactors.y,
                //     zMultiplier * offsetFromTouchPoint.z * _scaleFactors.z
                // );
                
                // Use base position as reference point
                Vector3 targetPos = baseLeapAnchorPosition.position + scaledOffset;
                
                // DEBUG: Final output
                Debug.Log($"[Output] scaledOffset:{scaledOffset.ToString("F3")} | targetPos:{targetPos.ToString("F3")} | currentPos:{transform.position.ToString("F3")} | smoothSpeed:{smoothSpeed}");
                
                // Smoothly interpolate to target position
                transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed);

                // Show and position the debug/visualization balls when retargeting
                if (gripperBall != null)
                {
                    gripperBall.SetActive(true);
                    gripperBall.transform.position = currentGripperPos;
                }
                if (rightFingertipBall != null)
                {
                    rightFingertipBall.SetActive(true);
                    rightFingertipBall.transform.position = currentRightHandTipPos;
                }
                if (colliderBall != null)
                {
                    colliderBall.SetActive(true);
                    colliderBall.transform.position = dynamicTouchPoint;
                }
                if (leftFingertipBall != null)
                {
                    leftFingertipBall.SetActive(true);
                    leftFingertipBall.transform.position = currentLeftFingerPos;
                }
            }
            else
            {
                if (gripperBall != null) gripperBall.SetActive(false);
                if (rightFingertipBall != null) rightFingertipBall.SetActive(false);
                if (colliderBall != null) colliderBall.SetActive(false);
                if (leftFingertipBall != null) leftFingertipBall.SetActive(false);
            }
        }
        else
        {
            // Vector3 offset = new Vector3(0f, 0f, 0.2f);
            // transform.position = baseLeapAnchorPosition.position + offset;
            transform.position = baseLeapAnchorPosition.position;
            if (gripperBall != null) gripperBall.SetActive(false);
            if (rightFingertipBall != null) rightFingertipBall.SetActive(false);
            if (colliderBall != null) colliderBall.SetActive(false);
            if (leftFingertipBall != null) leftFingertipBall.SetActive(false);
        }
    }

    void Start()
    {
        if (colliderBall != null) colliderBall.SetActive(false);
        if (rightFingertipBall != null) rightFingertipBall.SetActive(false);
        if (gripperBall != null) gripperBall.SetActive(false);
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
        // IMPORTANT: Also clamp initial scale factors to prevent extreme starting values
        _scaleFactors = new Vector3(
            Mathf.Abs(touchToRightHandTip.x) > 0.001f ? Mathf.Clamp(touchToGripperTip.x / touchToRightHandTip.x, minScaleFactor, maxScaleFactor) : 1f,
            Mathf.Abs(touchToRightHandTip.y) > 0.001f ? Mathf.Clamp(touchToGripperTip.y / touchToRightHandTip.y, minScaleFactor, maxScaleFactor) : 1f,
            Mathf.Abs(touchToRightHandTip.z) > 0.001f ? Mathf.Clamp(touchToGripperTip.z / touchToRightHandTip.z, minScaleFactor, maxScaleFactor) : 1f
        );
        
        // Initialize target scale factors to match current (prevents initial jump)
        _targetScaleFactors = _scaleFactors;

        _isRetargeting = true;
        Debug.Log($"[StartRetargeting] Initial scale factors: {_scaleFactors.ToString("F3")} | Distances: RH={touchToRightHandTip.magnitude:F4}, G={touchToGripperTip.magnitude:F4}");
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
