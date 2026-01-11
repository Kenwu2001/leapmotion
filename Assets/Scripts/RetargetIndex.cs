using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetargetIndex : MonoBehaviour
{
    public string[] targetTags = { "L_IndexTip", "L_ThumbTip" };
    public Transform handIndexTip;
    public Transform gripperIndexTip;
    public LeapAnchorOffset leapAnchorOffset;
    private int touchCount = 0;

    // Store touched points and their positions
    private Dictionary<string, Vector3> touchedPoints = new Dictionary<string, Vector3>();

    // Recorded positions when L_IndexTip first touches
    private Vector3 recordedHandIndexTipPosition;
    private Vector3 recordedGripperIndexTipPosition;
    private Vector3 recordedLeftThumbTipPosition;
    public bool hasRecordedPositions = false;

    private void OnTriggerEnter(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchCount++;
                // Add or update the touched point position
                touchedPoints[tag] = other.transform.position;

                // Record positions when L_ThumbTip enters for the first time
                if (tag == "L_ThumbTip" && !hasRecordedPositions)
                {
                    // Record the touch point position at the moment of contact
                    recordedLeftThumbTipPosition = other.transform.position;
                    
                    if (handIndexTip != null)
                        recordedHandIndexTipPosition = handIndexTip.position;
                    if (gripperIndexTip != null)
                        recordedGripperIndexTipPosition = gripperIndexTip.position;
                    
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
    public Vector3 GetRecordedHandIndexTipPosition()
    {
        return recordedHandIndexTipPosition;
    }

    public Vector3 GetRecordedGripperIndexTipPosition()
    {
        return recordedGripperIndexTipPosition;
    }

    public Vector3 GetRecordedLeftThumbTipPosition()
    {
        return recordedLeftThumbTipPosition;
    }

    public bool HasRecordedPositions()
    {
        return hasRecordedPositions;
    }

    // Reset recorded positions
    public void ResetRecordedPositions()
    {
        hasRecordedPositions = false;
        recordedHandIndexTipPosition = Vector3.zero;
        recordedGripperIndexTipPosition = Vector3.zero;
        recordedLeftThumbTipPosition = Vector3.zero;
    }
}
