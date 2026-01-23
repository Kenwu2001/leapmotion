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
    
    [Header("Switch Settings")]
    [Tooltip("Minimum time (seconds) between motor switches to prevent rapid switching")]
    public float switchCooldown = 0f;
    
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
    
    // Switch cooldown tracking
    private float lastSwitchTime = -999f;
    
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
            // Check switch cooldown
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && timeSinceLastSwitch < switchCooldown)
            {
                // Debug.Log($"[SelectMotor] ⏱ COOLDOWN BLOCKED: Motor {motorID} touch ignored (cooldown: {timeSinceLastSwitch:F3}s / {switchCooldown:F2}s)");
                return; // Ignore this touch during cooldown
            }
            
            // Force release the previous motor's detector
            if (activeTouchedMotorID != 0)
            {
                int prevIndex = activeTouchedMotorID - 1;
                // Debug.Log($"[SelectMotor] ⚠ SWITCHING: Motor {activeTouchedMotorID} (element[{activeTouchedMotorID-1}]) -> Motor {motorID} (element[{motorID-1}])");
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    // Debug.Log($"[SelectMotor] 🔄 Calling ForceRelease on Motor {activeTouchedMotorID}");
                    triggerDetectors[prevIndex].ForceRelease();
                    // Debug.Log($"[SelectMotor] ✓ ForceRelease completed for Motor {activeTouchedMotorID}");
                }
                else
                {
                    // Debug.LogWarning($"[SelectMotor] ⚠ Cannot ForceRelease Motor {activeTouchedMotorID} - prevIndex={prevIndex}, detector null={triggerDetectors[prevIndex] == null}");
                }
            }
            
            activeTouchedMotorID = motorID;
            activeTouchPosition = position;
            lastSwitchTime = Time.time; // Record switch time
            // Debug.Log($"[SelectMotor] ✓ Motor {motorID} (element[{motorID-1}]) NOW ACTIVE at {position} (switch time: {Time.time:F3})");
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
            // Debug.Log($"[SelectMotor] ✗ Motor {motorID} (element[{motorID-1}]) RELEASED");
        }
        else
        {
            // Debug.Log($"[SelectMotor] ⚠ Motor {motorID} tried to release but it's not active (current active: {activeTouchedMotorID})");
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
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ▶ TriggerEnter - touchCount: {touchCount}, isActive: {isActiveMotor}, Time: {Time.time:F3}, Object: {other.name}");
            if (touchCount == 1 && manager != null)
            {
                isActiveMotor = true;
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) 📞 Calling OnMotorTouched (setting isActive=true)");
                manager.OnMotorTouched(motorID, other.transform.position);
            }
            else if (touchCount > 1)
            {
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ⚠ TouchCount > 1, not calling OnMotorTouched");
            }
        }
        else
        {
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) TriggerEnter IGNORED - Wrong tag: {other.tag} (need: {targetTag}), Object: {other.name}");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(targetTag) && touchCount > 0 && isActiveMotor && manager != null)
        {
            // Only log for motors 9-12 to reduce spam
            if (motorID >= 9)
            {
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) TriggerStay - updating position");
            }
            manager.OnMotorTouched(motorID, other.transform.position);
        }
        else if (other.CompareTag(targetTag))
        {
            // Log why we're not updating
            if (motorID >= 9)
            {
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) TriggerStay BLOCKED - touchCount: {touchCount}, isActive: {isActiveMotor}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            int prevCount = touchCount;
            touchCount = Mathf.Max(0, touchCount - 1);
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ◀ TriggerExit - touchCount: {prevCount} -> {touchCount}, isActive: {isActiveMotor}, Time: {Time.time:F3}, Object: {other.name}");
            if (touchCount == 0 && isActiveMotor && manager != null)
            {
                isActiveMotor = false;
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) 📞 Calling OnMotorReleased");
                manager.OnMotorReleased(motorID);
            }
            else if (touchCount == 0 && !isActiveMotor)
            {
                // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ⚠ TouchCount=0 but NOT active (was force released?)");
            }
        }
        else
        {
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) TriggerExit IGNORED - Wrong tag: {other.tag}, Object: {other.name}");
        }
    }

    // Force this detector to stop being active (called when another motor takes over)
    public void ForceRelease()
    {
        // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ForceRelease called - isActive: {isActiveMotor}, touchCount: {touchCount}");
        if (isActiveMotor)
        {
            // Debug.Log($"[Detector] Motor {motorID} (element[{motorID-1}]) ❌ FORCE RELEASED (touchCount still: {touchCount})");
            isActiveMotor = false;
            // Don't reset touchCount - the collider might still be physically touching
            // But we stop reporting to the manager
        }
        else
        {
            // Debug.LogWarning($"[Detector] Motor {motorID} (element[{motorID-1}]) ⚠ ForceRelease called but was NOT active! (touchCount: {touchCount})");
        }
    }
}
