using UnityEngine;

public class LeapAnchorOffset : MonoBehaviour
{
    public TriggerRightIndexTip triggerScript;
    public Transform leftIndexTip;
    public Transform baseLeapAnchorPosition;

    [Header("Smooth Transition")]
    [Tooltip("Speed of smooth transition (higher = faster, 0 = instant)")]
    [Range(0f, 1f)]
    public float smoothSpeed = 0.1f;

    // private Vector3 _initialLocalPos;
    private bool _isRetargeting = false;
    
    // Recorded positions when trigger first detected
    private Vector3 _recordedObject1Pos; // right index finger tip
    private Vector3 _recordedObject2Pos; // gripper index finger tip
    private Vector3 _recordedThumbTipPos; // left index finger tip
    
    // Scale factors for retargeting
    private Vector3 _scaleFactors;

    // void Awake()
    // {
    //     _initialLocalPos = transform.localPosition;
    // }

    void LateUpdate()
    {

        if (_isRetargeting && leftIndexTip != null && triggerScript != null)
        {
            // Calculate offset from the initial touch point (recorded IndexTip position)
            Vector3 currentIndexPos = leftIndexTip.position;
            Vector3 offsetFromTouchPoint = currentIndexPos - _recordedThumbTipPos;
            
            // Apply scale factors to retarget to Object2 space
            Vector3 scaledOffset = new Vector3(
                offsetFromTouchPoint.x * _scaleFactors.x,
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
        if (triggerScript == null || leftIndexTip == null)
        {
            Debug.LogWarning("TriggerScript or LeftIndexTip not assigned!");
            return;
        }

        if (!triggerScript.HasRecordedPositions())
        {
            Debug.LogWarning("Positions not recorded yet!");
            return;
        }

        _recordedObject1Pos = triggerScript.GetRecordedObject1Position();
        _recordedObject2Pos = triggerScript.GetRecordedObject2Position();
        _recordedThumbTipPos = triggerScript.GetRecordedThumbTipPosition();

        // Calculate scale factors based on the distance between Object1 and Object2
        // relative to the distance from touch point to Object1
        Vector3 touchToObject1 = _recordedObject1Pos - _recordedThumbTipPos;
        Vector3 touchToObject2 = _recordedObject2Pos - _recordedThumbTipPos;

        // Scale factor = (touch to Object2) / (touch to Object1)
        _scaleFactors = new Vector3(
            Mathf.Abs(touchToObject1.x) > 0.0001f ? touchToObject2.x / touchToObject1.x : 1f,
            Mathf.Abs(touchToObject1.y) > 0.0001f ? touchToObject2.y / touchToObject1.y : 1f,
            Mathf.Abs(touchToObject1.z) > 0.0001f ? touchToObject2.z / touchToObject1.z : 1f
        );

        _isRetargeting = true;
        Debug.Log($"Touch point: {_recordedThumbTipPos}");
        Debug.Log($"Object1: {_recordedObject1Pos}, Object2: {_recordedObject2Pos}");
        Debug.Log($"Scale factors: {_scaleFactors}");
    }

    // Called by TriggerRightIndexTip when L_IndexTip exits
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
