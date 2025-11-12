using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRightThumbTip : MonoBehaviour
{
    public string[] targetTags = { "L_IndexTip", "L_ThumbTip" };
    public bool isRightThumbTipTouched => touchCount > 0;

    private int touchCount = 0;

    // Store touched points and their positions
    private Dictionary<string, Vector3> touchedPoints = new Dictionary<string, Vector3>();

    private void OnTriggerEnter(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchCount++;
                // Add or update the touched point position (use world position)
                touchedPoints[tag] = other.transform.position;
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
                break;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Update position while staying in trigger (use world position)
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
}
