using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRightIndexTip : MonoBehaviour
{
    public ModeSwitching modeSwitching;
    
    public string[] targetTags = { "L_IndexTip", "L_ThumbTip" };
    public bool isRightIndexTipTouched => touchCount > 0;

    private int touchCount = 0;

    // Store touched points and their positions
    private Dictionary<string, Vector3> touchedPoints = new Dictionary<string, Vector3>();

    public Renderer indexPaxiniRenderer;
    private Color originalColor;
    private bool isInitialized = false;

    private void Start()
    {
        if (indexPaxiniRenderer != null)
        {
            originalColor = indexPaxiniRenderer.material.color;
            isInitialized = true;
        }
    }

    private void Update()
    {
        UpdateIndexColor();
    }

    private void UpdateIndexColor()
    {
        if (!isInitialized || indexPaxiniRenderer == null || modeSwitching == null)
            return;

        if (touchCount > 0 && modeSwitching.modeManipulate)
        {
            indexPaxiniRenderer.material.color = Color.green;
        }
        else
        {
            indexPaxiniRenderer.material.color = originalColor;
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
}