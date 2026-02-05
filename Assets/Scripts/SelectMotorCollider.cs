using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectMotorCollider : MonoBehaviour
{
    [Header("12 Motor Colliders (for Thumb only - motors 1-4)")]
    [Tooltip("Drag and drop 12 collider GameObjects here (index 0-11 = motor ID 1-12)")]
    public GameObject[] motorColliders = new GameObject[12];
    
    [Header("Target Tag")]
    public string targetTag = "L_IndexTipSmall";
    
    [Header("Switch Settings")]
    [Tooltip("Minimum time (seconds) between motor switches to prevent rapid switching")]
    public float switchCooldown = 0f;
    
    [Header("Debug Settings")]
    [Tooltip("Toggle to show/hide debug spheres")]
    public bool showDebugSpheres = true;
    
    [Tooltip("Key to toggle debug spheres on/off")]
    public KeyCode debugToggleKey = KeyCode.D;
    
    [Header("Index Finger Projection Settings")]
    [Tooltip("Left hand point for projection (L_IndexTipSmall transform)")]
    public Transform leftHandPoint;
    
    [Tooltip("Right hand index FingerPath with 5 joints (4 segments = motors 5-8)")]
    public FingerPath rightIndexPath;
    
    [Tooltip("Claw index FingerPath")]
    public FingerPath clawIndexPath;
    
    [Tooltip("TriggerRightIndexTip to detect when index finger is being touched")]
    public TriggerRightIndexTip triggerRightIndexTip;
    
    [Header("Index Finger Debug Spheres")]
    [Tooltip("Sphere to show projection on right hand index finger")]
    public Transform indexRightFingerProjectionSphere;
    [Tooltip("Sphere to show projection on claw index finger")]
    public Transform indexClawProjectionSphere;
    
    [Header("Middle Finger Projection Settings")]
    [Tooltip("Right hand middle FingerPath with 5 joints (4 segments = motors 9-12)")]
    public FingerPath rightMiddlePath;
    
    [Tooltip("Claw middle FingerPath")]
    public FingerPath clawMiddlePath;
    
    [Tooltip("TriggerRightMiddleTip to detect when middle finger is being touched")]
    public TriggerRightMiddleTip triggerRightMiddleTip;
    
    [Header("Middle Finger Debug Spheres")]
    [Tooltip("Sphere to show projection on right hand middle finger")]
    public Transform middleRightFingerProjectionSphere;
    [Tooltip("Sphere to show projection on claw middle finger")]
    public Transform middleClawProjectionSphere;
    
    [Header("Debug Info")]
    [Tooltip("Currently touched motor ID (0 = none)")]
    public int currentTouchedMotorID = 0;
    
    [Tooltip("Touch position on the active motor")]
    public Vector3 touchPosition = Vector3.zero;
    
    [Tooltip("Is any motor currently being touched?")]
    public bool isAnyMotorTouched = false;
    
    [Header("Index Finger Projection Debug")]
    [Tooltip("Current segment index on index finger (0-3)")]
    public int indexSegmentIndex = -1;
    
    [Tooltip("Projection position on claw index finger")]
    public Vector3 indexClawProjectionPosition = Vector3.zero;
    
    [Header("Middle Finger Projection Debug")]
    [Tooltip("Current segment index on middle finger (0-3)")]
    public int middleSegmentIndex = -1;
    
    [Tooltip("Projection position on claw middle finger")]
    public Vector3 middleClawProjectionPosition = Vector3.zero;

    // Track which motor is currently touched (1-12, 0 = none)
    private int activeTouchedMotorID = 0;
    private Vector3 activeTouchPosition = Vector3.zero;
    
    // Switch cooldown tracking
    private float lastSwitchTime = -999f;
    
    // Child trigger components for each motor (only used for motors 1-4 Thumb)
    private MotorTriggerDetector[] triggerDetectors = new MotorTriggerDetector[12];
    
    // Index finger projection state
    private int indexProjectionMotorID = 0;
    private Vector3 indexProjectionPosition = Vector3.zero;
    private Vector3 indexClawPosition = Vector3.zero;
    
    // Middle finger projection state
    private int middleProjectionMotorID = 0;
    private Vector3 middleProjectionPosition = Vector3.zero;
    private Vector3 middleClawPosition = Vector3.zero;

    private void Start()
    {
        // Setup trigger detectors for motor colliders 1-4 (Thumb only)
        for (int i = 0; i < 4; i++) // Only motors 1-4
        {
            if (motorColliders[i] != null)
            {
                // Add or get the detector component
                MotorTriggerDetector detector = motorColliders[i].GetComponent<MotorTriggerDetector>();
                if (detector == null)
                {
                    detector = motorColliders[i].AddComponent<MotorTriggerDetector>();
                }
                
                // Initialize with motorID 1-4 (i+1)
                detector.Initialize(i + 1, targetTag, this);
                triggerDetectors[i] = detector;
            }
        }
        
        // Hide index finger debug spheres initially
        if (indexRightFingerProjectionSphere != null)
            indexRightFingerProjectionSphere.gameObject.SetActive(false);
        if (indexClawProjectionSphere != null)
            indexClawProjectionSphere.gameObject.SetActive(false);
        
        // Hide middle finger debug spheres initially
        if (middleRightFingerProjectionSphere != null)
            middleRightFingerProjectionSphere.gameObject.SetActive(false);
        if (middleClawProjectionSphere != null)
            middleClawProjectionSphere.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Toggle debug spheres with key press
        if (Input.GetKeyDown(debugToggleKey))
        {
            showDebugSpheres = !showDebugSpheres;
            Debug.Log($"[SelectMotorCollider] Debug spheres: {(showDebugSpheres ? "ON" : "OFF")}");
            
            // If turning off, hide all spheres immediately
            if (!showDebugSpheres)
            {
                HideAllDebugSpheres();
            }
        }
        
        // Handle Index finger projection-based selection (motors 5-8)
        UpdateIndexFingerProjection();
        
        // Handle Middle finger projection-based selection (motors 9-12)
        UpdateMiddleFingerProjection();
        
        // Update debug info
        currentTouchedMotorID = activeTouchedMotorID;
        touchPosition = activeTouchPosition;
        isAnyMotorTouched = activeTouchedMotorID != 0;
    }
    
    /// <summary>
    /// Hides all debug spheres
    /// </summary>
    private void HideAllDebugSpheres()
    {
        if (indexRightFingerProjectionSphere != null)
            indexRightFingerProjectionSphere.gameObject.SetActive(false);
        if (indexClawProjectionSphere != null)
            indexClawProjectionSphere.gameObject.SetActive(false);
        if (middleRightFingerProjectionSphere != null)
            middleRightFingerProjectionSphere.gameObject.SetActive(false);
        if (middleClawProjectionSphere != null)
            middleClawProjectionSphere.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Updates index finger motor selection based on projection
    /// </summary>
    private void UpdateIndexFingerProjection()
    {
        // Check if index finger is being touched
        bool isIndexTouched = triggerRightIndexTip != null && triggerRightIndexTip.isRightIndexTipTouched;
        
        if (!isIndexTouched || leftHandPoint == null || rightIndexPath == null)
        {
            // Index finger not touched - clear index projection state
            if (indexProjectionMotorID != 0)
            {
                // If index projection was active, release it
                if (activeTouchedMotorID == indexProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                indexProjectionMotorID = 0;
                indexSegmentIndex = -1;
            }
            
            // Hide debug spheres
            if (indexRightFingerProjectionSphere != null)
                indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null)
                indexClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Calculate projection on right hand index finger
        int segmentIndex;
        float segmentT;
        Vector3 closestPointOnRightFinger;
        
        FingerMath.ClosestPointOnFinger(
            leftHandPoint.position,
            rightIndexPath,
            out segmentIndex,
            out segmentT,
            out closestPointOnRightFinger
        );
        
        // Clamp values
        segmentT = Mathf.Clamp01(segmentT);
        
        // Calculate corresponding point on claw finger if available
        Vector3 clawPos = Vector3.zero;
        if (clawIndexPath != null)
        {
            int clawJointCount = clawIndexPath.GetJointCount();
            int clampedSeg = Mathf.Clamp(segmentIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawIndexPath.GetJoint(clampedSeg),
                clawIndexPath.GetJoint(clampedSeg + 1),
                segmentT
            );
        }
        
        // Convert segment index to motor ID (5-8)
        // segment 0 → motor 8, segment 1 → motor 7, segment 2 → motor 6, segment 3 → motor 5
        int motorID = 8 - segmentIndex;
        
        // Clamp to valid range (5-8)
        motorID = Mathf.Clamp(motorID, 5, 8);
        
        // Update index projection state
        indexSegmentIndex = segmentIndex;
        indexProjectionPosition = closestPointOnRightFinger;
        indexClawPosition = clawPos;
        indexClawProjectionPosition = clawPos;
        
        // Update debug spheres (only if enabled)
        if (showDebugSpheres)
        {
            if (indexRightFingerProjectionSphere != null)
            {
                indexRightFingerProjectionSphere.gameObject.SetActive(true);
                indexRightFingerProjectionSphere.position = closestPointOnRightFinger;
            }
            if (indexClawProjectionSphere != null && clawIndexPath != null)
            {
                indexClawProjectionSphere.gameObject.SetActive(true);
                indexClawProjectionSphere.position = clawPos;
            }
        }
        else
        {
            // Hide spheres when debug is disabled
            if (indexRightFingerProjectionSphere != null)
                indexRightFingerProjectionSphere.gameObject.SetActive(false);
            if (indexClawProjectionSphere != null)
                indexClawProjectionSphere.gameObject.SetActive(false);
        }
        
        // Check if motor changed
        if (motorID != indexProjectionMotorID)
        {
            // Check switch cooldown (if switching from another motor)
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
            {
                return; // Ignore during cooldown
            }
            
            // If currently active motor is a collider-based motor (1-4), force release it
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            // Clear middle projection if it was active
            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12)
            {
                middleProjectionMotorID = 0;
            }
            
            indexProjectionMotorID = motorID;
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPointOnRightFinger;
            lastSwitchTime = Time.time;
        }
        else
        {
            // Same motor - just update position
            activeTouchPosition = closestPointOnRightFinger;
        }
    }
    
    /// <summary>
    /// Updates middle finger motor selection based on projection
    /// </summary>
    private void UpdateMiddleFingerProjection()
    {
        // Check if middle finger is being touched
        bool isMiddleTouched = triggerRightMiddleTip != null && triggerRightMiddleTip.isRightMiddleTipTouched;
        
        if (!isMiddleTouched || leftHandPoint == null || rightMiddlePath == null)
        {
            // Middle finger not touched - clear middle projection state
            if (middleProjectionMotorID != 0)
            {
                // If middle projection was active, release it
                if (activeTouchedMotorID == middleProjectionMotorID)
                {
                    activeTouchedMotorID = 0;
                    activeTouchPosition = Vector3.zero;
                }
                middleProjectionMotorID = 0;
                middleSegmentIndex = -1;
            }
            
            // Hide debug spheres
            if (middleRightFingerProjectionSphere != null)
                middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null)
                middleClawProjectionSphere.gameObject.SetActive(false);
            
            return;
        }
        
        // Calculate projection on right hand middle finger
        int segmentIndex;
        float segmentT;
        Vector3 closestPointOnRightFinger;
        
        FingerMath.ClosestPointOnFinger(
            leftHandPoint.position,
            rightMiddlePath,
            out segmentIndex,
            out segmentT,
            out closestPointOnRightFinger
        );
        
        // Clamp values
        segmentT = Mathf.Clamp01(segmentT);
        
        // Calculate corresponding point on claw finger if available
        Vector3 clawPos = Vector3.zero;
        if (clawMiddlePath != null)
        {
            int clawJointCount = clawMiddlePath.GetJointCount();
            int clampedSeg = Mathf.Clamp(segmentIndex, 0, clawJointCount - 2);
            clawPos = Vector3.Lerp(
                clawMiddlePath.GetJoint(clampedSeg),
                clawMiddlePath.GetJoint(clampedSeg + 1),
                segmentT
            );
        }
        
        // Convert segment index to motor ID (9-12)
        // segment 0 → motor 12, segment 1 → motor 11, segment 2 → motor 10, segment 3 → motor 9
        int motorID = 12 - segmentIndex;
        
        // Clamp to valid range (9-12)
        motorID = Mathf.Clamp(motorID, 9, 12);
        
        // Update middle projection state
        middleSegmentIndex = segmentIndex;
        middleProjectionPosition = closestPointOnRightFinger;
        middleClawPosition = clawPos;
        middleClawProjectionPosition = clawPos;
        
        // Update debug spheres (only if enabled)
        if (showDebugSpheres)
        {
            if (middleRightFingerProjectionSphere != null)
            {
                middleRightFingerProjectionSphere.gameObject.SetActive(true);
                middleRightFingerProjectionSphere.position = closestPointOnRightFinger;
            }
            if (middleClawProjectionSphere != null && clawMiddlePath != null)
            {
                middleClawProjectionSphere.gameObject.SetActive(true);
                middleClawProjectionSphere.position = clawPos;
            }
        }
        else
        {
            // Hide spheres when debug is disabled
            if (middleRightFingerProjectionSphere != null)
                middleRightFingerProjectionSphere.gameObject.SetActive(false);
            if (middleClawProjectionSphere != null)
                middleClawProjectionSphere.gameObject.SetActive(false);
        }
        
        // Check if motor changed
        if (motorID != middleProjectionMotorID)
        {
            // Check switch cooldown (if switching from another motor)
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && activeTouchedMotorID != motorID && timeSinceLastSwitch < switchCooldown)
            {
                return; // Ignore during cooldown
            }
            
            // If currently active motor is a collider-based motor (1-4), force release it
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            // Clear index projection if it was active
            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8)
            {
                indexProjectionMotorID = 0;
            }
            
            middleProjectionMotorID = motorID;
            activeTouchedMotorID = motorID;
            activeTouchPosition = closestPointOnRightFinger;
            lastSwitchTime = Time.time;
        }
        else
        {
            // Same motor - just update position
            activeTouchPosition = closestPointOnRightFinger;
        }
    }

    // Called by MotorTriggerDetector when a motor is touched (only for motors 1-4 Thumb)
    internal void OnMotorTouched(int motorID, Vector3 position)
    {
        // Only handle motors 1-4 via collider (Thumb only)
        if (motorID < 1 || motorID > 4)
            return;
            
        // Only allow one motor at a time
        if (activeTouchedMotorID != motorID)
        {
            // Check switch cooldown
            float timeSinceLastSwitch = Time.time - lastSwitchTime;
            if (activeTouchedMotorID != 0 && timeSinceLastSwitch < switchCooldown)
            {
                return; // Ignore this touch during cooldown
            }
            
            // Force release the previous motor's detector (if it's a collider-based motor 1-4)
            if (activeTouchedMotorID >= 1 && activeTouchedMotorID <= 4)
            {
                int prevIndex = activeTouchedMotorID - 1;
                if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
                {
                    triggerDetectors[prevIndex].ForceRelease();
                }
            }
            
            // Clear index projection if it was active
            if (activeTouchedMotorID >= 5 && activeTouchedMotorID <= 8)
            {
                indexProjectionMotorID = 0;
            }
            
            // Clear middle projection if it was active
            if (activeTouchedMotorID >= 9 && activeTouchedMotorID <= 12)
            {
                middleProjectionMotorID = 0;
            }
            
            activeTouchedMotorID = motorID;
            activeTouchPosition = position;
            lastSwitchTime = Time.time;
        }
        else
        {
            // Update position for the currently active motor
            activeTouchPosition = position;
        }
    }

    // Called by MotorTriggerDetector when a motor is released (only for motors 1-4 Thumb)
    internal void OnMotorReleased(int motorID)
    {
        if (activeTouchedMotorID == motorID && motorID >= 1 && motorID <= 4)
        {
            activeTouchedMotorID = 0;
            activeTouchPosition = Vector3.zero;
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
    
    public int GetIndexSegmentIndex()
    {
        return indexSegmentIndex;
    }
    
    public Vector3 GetIndexClawProjectionPosition()
    {
        return indexClawPosition;
    }
    
    public int GetMiddleSegmentIndex()
    {
        return middleSegmentIndex;
    }
    
    public Vector3 GetMiddleClawProjectionPosition()
    {
        return middleClawPosition;
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
