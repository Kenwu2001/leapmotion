using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetargetMiddle : MonoBehaviour
{
    public string[] targetTags = { "L_IndexTip", "L_ThumbTip" };
    public Transform handMiddleTip;
    public Transform gripperMiddleTip;
    public LeapAnchorOffset leapAnchorOffset;
    private int touchCount = 0;

    // Store touched points and their positions
    private Dictionary<string, Vector3> touchedPoints = new Dictionary<string, Vector3>();

    // Recorded positions when L_IndexTip first touches
    private Vector3 recordedHandMiddleTipPosition;
    private Vector3 recordedGripperMiddleTipPosition;
    private Vector3 recordedLeftIndexTipPosition;
    private Vector3 recordedLeftIndexTipLocalPosition; // Local position relative to this collider
    public bool hasRecordedPositions = false;

    private void Update()
    {
        // Continuously update gripperMiddleTip position while retargeting is active
        if (hasRecordedPositions && gripperMiddleTip != null)
        {
            recordedGripperMiddleTipPosition = gripperMiddleTip.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchCount++;
                // Add or update the touched point position
                touchedPoints[tag] = other.transform.position;

                // Record positions when L_IndexTip enters for the first time
                if (tag == "L_IndexTip" && !hasRecordedPositions)
                {
                    // Record the touch point position at the moment of contact
                    recordedLeftIndexTipPosition = other.transform.position;
                    // Record local position so it moves with the collider
                    recordedLeftIndexTipLocalPosition = transform.InverseTransformPoint(other.transform.position);
                    
                    if (handMiddleTip != null)
                        recordedHandMiddleTipPosition = handMiddleTip.position;
                    if (gripperMiddleTip != null)
                        recordedGripperMiddleTipPosition = gripperMiddleTip.position;
                    
                    hasRecordedPositions = true;
                    
                    // Start retargeting ONLY when first recording positions
                    if (leapAnchorOffset != null)
                    {
                        leapAnchorOffset.StartRetargeting();
                    }
                }

                break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchCount = Mathf.Max(0, touchCount - 1);
                // Remove the touched point
                touchedPoints.Remove(tag);
                
                // Only reset and stop retargeting when no targets are touching anymore
                if (touchCount == 0)
                {
                    hasRecordedPositions = false;
                    
                    // Stop retargeting ONLY when all targets have exited
                    if (leapAnchorOffset != null)
                    {
                        leapAnchorOffset.StopRetargeting();
                    }
                }
                break;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Update position while staying in trigger
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchedPoints[tag] = other.transform.position;
                break;
            }
        }
    }

    // Public method to get the position of a touched point by tag
    public bool TryGetTouchedPointPosition(string tag, out Vector3 position)
    {
        return touchedPoints.TryGetValue(tag, out position);
    }

    // Public method to get all currently touched points and their positions
    public Dictionary<string, Vector3> GetAllTouchedPoints()
    {
        return new Dictionary<string, Vector3>(touchedPoints);
    }

    // Public methods to get recorded positions
    public Vector3 GetRecordedHandMiddleTipPosition()
    {
        return recordedHandMiddleTipPosition;
    }

    public Vector3 GetRecordedGripperMiddleTipPosition()
    {
        return recordedGripperMiddleTipPosition;
    }

    public Vector3 GetRecordedLeftIndexTipPosition()
    {
        return recordedLeftIndexTipPosition;
    }

    // Get the touch point position that moves with the collider
    public Vector3 GetDynamicLeftIndexTipPosition()
    {
        if (!hasRecordedPositions)
            return new Vector3(-1f, -1f, -1f);
        
        // Convert local position back to world space
        return transform.TransformPoint(recordedLeftIndexTipLocalPosition);
    }

    public bool HasRecordedPositions()
    {
        return hasRecordedPositions;
    }

    // Get current gripper transform for real-time tracking
    public Transform GetGripperMiddleTip()
    {
        return gripperMiddleTip;
    }

    // Reset recorded positions
    public void ResetRecordedPositions()
    {
        hasRecordedPositions = false;
        recordedHandMiddleTipPosition = Vector3.zero;
        recordedGripperMiddleTipPosition = Vector3.zero;
        recordedLeftIndexTipPosition = Vector3.zero;
        recordedLeftIndexTipLocalPosition = Vector3.zero;
    }
}
