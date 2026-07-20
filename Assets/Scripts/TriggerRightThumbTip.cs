using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerRightThumbTip : MonoBehaviour
{
    private const int ThumbPaxiniMotorID = 13;

    public ModeSwitching modeSwitching;
    public string[] targetTags = { "L_IndexTip", "L_ThumbTip" };
    // During modeSelect: only the left index tip (L_IndexTip) counts — left thumb tip must not trigger motor selection
    public bool isRightThumbTipTouched => modeSwitching != null && modeSwitching.modeSelect
        ? touchedPoints.ContainsKey("L_IndexTip")
        : touchedColliders.Count > 0;

    // Store actual colliders that are touching (prevents duplicate counting)
    private HashSet<Collider> touchedColliders = new HashSet<Collider>();
    
    // Store touched points and their positions
    private Dictionary<string, Vector3> touchedPoints = new Dictionary<string, Vector3>();

    public Renderer thumbPaxiniRenderer;
    public bool showFreezeColor = false;
    public Color freezeDisplayColor = Color.yellow;
    public Color originalColor;
    private bool isInitialized = false;
    private BaselineTwo baselineTwo;

    private void Awake()
    {
        if (thumbPaxiniRenderer != null)
            originalColor = thumbPaxiniRenderer.material.color;
    }

    private void Start()
    {
        if (baselineTwo == null)
            baselineTwo = FindObjectOfType<BaselineTwo>();

        if (thumbPaxiniRenderer != null)
        {
            isInitialized = true;
        }
    }

    private void Update()
    {
        UpdateThumbColor();
    }

    private void UpdateThumbColor()
    {
        if (!isInitialized || thumbPaxiniRenderer == null)
            return;

        if (baselineTwo == null)
            baselineTwo = FindObjectOfType<BaselineTwo>();

        if (baselineTwo != null && baselineTwo.useKeyboardControl)
        {
            thumbPaxiniRenderer.material.color = baselineTwo.GetKeyboardVisualColorForMotor(ThumbPaxiniMotorID, originalColor);
            return;
        }

        if (modeSwitching == null)
            return;

        // Check if collider is enabled
        Collider col = GetComponent<Collider>();
        bool isColliderEnabled = col != null && col.enabled;

        if (touchedColliders.Count > 0 && modeSwitching.modeManipulate && isColliderEnabled)
        {
            thumbPaxiniRenderer.material.color = Color.green;
        }
        else if (showFreezeColor)
        {
            thumbPaxiniRenderer.material.color = freezeDisplayColor;
        }
        else
        {
            thumbPaxiniRenderer.material.color = modeSwitching.GetPaxiniDisplayColor(ThumbPaxiniMotorID, originalColor);
        }
    }

    // Public method to clear all touch records (called when collider is disabled)
    public void ClearTouches()
    {
        touchedColliders.Clear();
        touchedPoints.Clear();
        if (modeSwitching != null && modeSwitching.SelectMotorCollider != null)
            modeSwitching.SelectMotorCollider.OnPseudoMotorReleased(ThumbPaxiniMotorID);
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                // Add the actual collider object (HashSet prevents duplicates)
                touchedColliders.Add(other);
                // Add or update the touched point position (use world position)
                touchedPoints[tag] = other.transform.position;
                if (modeSwitching != null && modeSwitching.modeSelect && modeSwitching.SelectMotorCollider != null)
                    modeSwitching.SelectMotorCollider.OnPseudoMotorTouched(ThumbPaxiniMotorID, other.transform.position);
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
                // Remove the actual collider object
                touchedColliders.Remove(other);
                // Remove the touched point
                touchedPoints.Remove(tag);
                if (touchedColliders.Count == 0 && modeSwitching != null && modeSwitching.SelectMotorCollider != null)
                    modeSwitching.SelectMotorCollider.OnPseudoMotorReleased(ThumbPaxiniMotorID);
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
                if (modeSwitching != null && modeSwitching.modeSelect && modeSwitching.SelectMotorCollider != null)
                    modeSwitching.SelectMotorCollider.OnPseudoMotorTouched(ThumbPaxiniMotorID, other.transform.position);
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
