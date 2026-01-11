using UnityEngine;

public class LeapAnchorOffset : MonoBehaviour
{
    public RetargetIndex retargetIndex;
    public Transform leftThumbTip;
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

        if (_isRetargeting && leftThumbTip != null && retargetIndex != null)
        {
            // Get current gripper position for real-time tracking
            Transform gripperTip = retargetIndex.GetGripperIndexTip();
            if (gripperTip != null)
            {
                Vector3 currentGripperPos = gripperTip.position;
                
                // Recalculate scale factors with current gripper position
                Vector3 touchToHandIndexTip = _recordedHandIndexTipPos - _recordedLeftThumbTipPos;
                Vector3 touchToGripperIndexTip = currentGripperPos - _recordedLeftThumbTipPos;
                
                _scaleFactors = new Vector3(
                    Mathf.Abs(touchToHandIndexTip.x) > 0.0001f ? touchToGripperIndexTip.x / touchToHandIndexTip.x : 1f,
                    Mathf.Abs(touchToHandIndexTip.y) > 0.0001f ? touchToGripperIndexTip.y / touchToHandIndexTip.y : 1f,
                    Mathf.Abs(touchToHandIndexTip.z) > 0.0001f ? touchToGripperIndexTip.z / touchToHandIndexTip.z : 1f
                );
            }
            
            // Calculate offset from the initial touch point (recorded IndexTip position)
            Vector3 currentIndexPos = leftThumbTip.position;
            Vector3 offsetFromTouchPoint = currentIndexPos - _recordedLeftThumbTipPos;
            
            // Apply scale factors to retarget to Object2 space
            Vector3 scaledOffset = new Vector3(
                -offsetFromTouchPoint.x * _scaleFactors.x,
                offsetFromTouchPoint.y * _scaleFactors.y,
                offsetFromTouchPoint.z * _scaleFactors.z
            );
            
            // Use base position as reference point (like in else block)
            Vector3 targetPos = baseLeapAnchorPosition.position + scaledOffset;
            
            // Smoothly interpolate to target position
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothSpeed);
        }
        else
        {
            // Vector3 offset = new Vector3(0f, 0f, 0.2f);
            // transform.position = baseLeapAnchorPosition.position + offset;
            transform.position = baseLeapAnchorPosition.position;
        }
    }

    // Called by TriggerRightIndexTip when L_IndexTip enters
    public void StartRetargeting()
    {
        if (retargetIndex == null || leftThumbTip == null)
        {
            Debug.LogWarning("retargetIndex or LeftThumbTip not assigned!");
            return;
        }

        if (!retargetIndex.HasRecordedPositions())
        {
            Debug.LogWarning("Positions not recorded yet!");
            return;
        }

        _recordedHandIndexTipPos = retargetIndex.GetRecordedHandIndexTipPosition();
        _recordedGripperIndexTipPos = retargetIndex.GetRecordedGripperIndexTipPosition();
        _recordedLeftThumbTipPos = retargetIndex.GetRecordedLeftThumbTipPosition();

        // Calculate scale factors based on the distance between Object1 and Object2
        // relative to the distance from touch point to Object1
        Vector3 touchToHandIndexTip = _recordedHandIndexTipPos - _recordedLeftThumbTipPos;
        Vector3 touchToGripperIndexTip = _recordedGripperIndexTipPos - _recordedLeftThumbTipPos;

        // Scale factor = (touch to Object2) / (touch to Object1)
        _scaleFactors = new Vector3(
            Mathf.Abs(touchToHandIndexTip.x) > 0.0001f ? touchToGripperIndexTip.x / touchToHandIndexTip.x : 1f,
            Mathf.Abs(touchToHandIndexTip.y) > 0.0001f ? touchToGripperIndexTip.y / touchToHandIndexTip.y : 1f,
            Mathf.Abs(touchToHandIndexTip.z) > 0.0001f ? touchToGripperIndexTip.z / touchToHandIndexTip.z : 1f
        );

        _isRetargeting = true;
        Debug.Log($"Touch point: {_recordedLeftThumbTipPos}");
        Debug.Log($"IndexTipPos: {_recordedHandIndexTipPos}, GripperIndexTipPos: {_recordedGripperIndexTipPos}");
        Debug.Log($"Scale factors: {_scaleFactors}");
    }

    // Called by TriggerRightIndexTip when L_LeftThumbTip exits
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
