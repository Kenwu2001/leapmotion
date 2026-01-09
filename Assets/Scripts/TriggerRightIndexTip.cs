using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRightIndexTip : MonoBehaviour
{
    public string[] targetTags = { "L_IndexTip", "L_ThumbTip" };
    public bool isRightIndexTipTouched => touchCount > 0;

    [Header("Objects to Record Position on L_IndexTip Touch")]
    public Transform object1;
    public Transform object2;

    [Header("Retargeting Control")]
    public LeapAnchorOffset leapAnchorOffset;

    private int touchCount = 0;

    // Store touched points and their positions
    private Dictionary<string, Vector3> touchedPoints = new Dictionary<string, Vector3>();

    // Recorded positions when L_IndexTip first touches
    private Vector3 recordedObject1Position;
    private Vector3 recordedObject2Position;
    private Vector3 recordedThumbTipPosition;
    private bool hasRecordedPositions = false;

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
                if (tag == "L_ThumbTip" && !hasRecordedPositions)
                {
                    // Record the touch point position at the moment of contact
                    recordedThumbTipPosition = other.transform.position;
                    
                    if (object1 != null)
                        recordedObject1Position = object1.position;
                    if (object2 != null)
                        recordedObject2Position = object2.position;
                    hasRecordedPositions = true;
                    Debug.Log($"Recorded IndexTip position: {recordedThumbTipPosition}");
                    Debug.Log($"Recorded positions - Object1: {recordedObject1Position}, Object2: {recordedObject2Position}");
                }

                // Start retargeting when L_IndexTip enters
                if (tag == "L_ThumbTip" && leapAnchorOffset != null)
                {
                    leapAnchorOffset.StartRetargeting();
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

                // Stop retargeting when L_IndexTip exits
                if (tag == "L_IndexTip" && leapAnchorOffset != null)
                {
                    leapAnchorOffset.StopRetargeting();
                    hasRecordedPositions = false; // Reset recorded positions
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
    public Vector3 GetRecordedObject1Position()
    {
        return recordedObject1Position;
    }

    public Vector3 GetRecordedObject2Position()
    {
        return recordedObject2Position;
    }

    public Vector3 GetRecordedThumbTipPosition()
    {
        return recordedThumbTipPosition;
    }

    public bool HasRecordedPositions()
    {
        return hasRecordedPositions;
    }

    // Reset recorded positions
    public void ResetRecordedPositions()
    {
        hasRecordedPositions = false;
        recordedObject1Position = Vector3.zero;
        recordedObject2Position = Vector3.zero;
        recordedThumbTipPosition = Vector3.zero;
    }
}