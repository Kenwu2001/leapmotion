using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectMotorCollider : MonoBehaviour
{
    [Header("12 Motor Colliders")]
    [Tooltip("Drag and drop 12 collider GameObjects here (index 0-11 = motor ID 1-12)")]
    public GameObject[] motorColliders = new GameObject[12];
    
    [Header("Target Tag")]
    public string targetTag = "L_IndexTipSmall";
    
    [Header("Debug Info")]
    [Tooltip("Currently touched motor ID (0 = none)")]
    public int currentTouchedMotorID = 0;
    
    [Tooltip("Touch position on the active motor")]
    public Vector3 touchPosition = Vector3.zero;
    
    [Tooltip("Is any motor currently being touched?")]
    public bool isAnyMotorTouched = false;

    // Track which motor is currently touched (1-12, 0 = none)
    private int activeTouchedMotorID = 0;
    private Vector3 activeTouchPosition = Vector3.zero;
    
    // Child trigger components for each motor
    private MotorTriggerDetector[] triggerDetectors = new MotorTriggerDetector[12];

    private void Start()
    {
        // Setup trigger detectors for each motor collider
        for (int i = 0; i < motorColliders.Length; i++)
        {
            if (motorColliders[i] != null)
            {
                // Add or get the detector component
                MotorTriggerDetector detector = motorColliders[i].GetComponent<MotorTriggerDetector>();
                if (detector == null)
                {
                    detector = motorColliders[i].AddComponent<MotorTriggerDetector>();
                }
                
                // Initialize with motorID 1-12 (i+1)
                detector.Initialize(i + 1, targetTag, this);
                triggerDetectors[i] = detector;
            }
            else
            {
                // Debug.LogWarning($"Motor collider at index {i} is not assigned!");
            }
        }
    }

    private void Update()
    {
        // Update debug info
        currentTouchedMotorID = activeTouchedMotorID;
        touchPosition = activeTouchPosition;
        isAnyMotorTouched = activeTouchedMotorID != 0;
    }

    // Called by MotorTriggerDetector when a motor is touched
    internal void OnMotorTouched(int motorID, Vector3 position)
    {
        // Only allow one motor at a time
        if (activeTouchedMotorID != motorID)
        {
            // Force release the previous motor's detector
            if (activeTouchedMotorID != 0)
            {
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    // Debug.Log($"Motor {activeTouchedMotorID} auto-released (new touch on Motor {motorID})");
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            activeTouchedMotorID = motorID;
            activeTouchPosition = position;
            // Debug.Log($"Motor {motorID} touched!");
        }
        else
        {
            // Update position for the currently active motor
            activeTouchPosition = position;
        }
    }

    // Called by MotorTriggerDetector when a motor is released
    internal void OnMotorReleased(int motorID)
    {
        if (activeTouchedMotorID == motorID)
        {
            activeTouchedMotorID = 0;
            activeTouchPosition = Vector3.zero;
            // Debug.Log($"Motor {motorID} released!");
        }
    }

    // Public methods to query motor state
    public int GetTouchedMotorID()
    {
        return activeTouchedMotorID;
    }

    public bool IsTouched()
    {
        return activeTouchedMotorID != 0;
    }

    public bool IsMotorTouched(int motorID)
    {
        return activeTouchedMotorID == motorID;
    }

    public bool TryGetTouchPosition(out Vector3 position)
    {
        position = activeTouchPosition;
        return activeTouchedMotorID != 0;
    }

    public Vector3 GetTouchPosition()
    {
        return activeTouchPosition;
    }

    public GameObject GetTouchedMotorGameObject()
    {
        if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= motorColliders.Length)
        {
            return motorColliders[activeTouchedMotorID - 1];
        }
        return null;
    }
}

// Helper component attached to each motor collider
internal class MotorTriggerDetector : MonoBehaviour
{
    private int motorID;
    private string targetTag;
    private SelectMotorCollider manager;
    private int touchCount = 0;
    private bool isActiveMotor = false;

    public void Initialize(int id, string tag, SelectMotorCollider managerRef)
    {
        motorID = id;
        targetTag = tag;
        manager = managerRef;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            touchCount++;
            if (touchCount == 1 && manager != null)
            {
                isActiveMotor = true;
                manager.OnMotorTouched(motorID, other.transform.position);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(targetTag) && touchCount > 0 && isActiveMotor && manager != null)
        {
            manager.OnMotorTouched(motorID, other.transform.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            touchCount = Mathf.Max(0, touchCount - 1);
            if (touchCount == 0 && isActiveMotor && manager != null)
            {
                isActiveMotor = false;
                manager.OnMotorReleased(motorID);
            }
        }
    }

    // Force this detector to stop being active (called when another motor takes over)
    public void ForceRelease()
    {
        if (isActiveMotor)
        {
            isActiveMotor = false;
            // Don't reset touchCount - the collider might still be physically touching
            // But we stop reporting to the manager
        }
    }
}
