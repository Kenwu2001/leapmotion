using UnityEngine;

/// <summary>
/// Manages the enabling/disabling of 12 motor colliders.
/// When modeSelect=true, enable the 4 colliders of the corresponding finger based on which right-hand renderer the ball is moving on.
/// Motor ID 1-4 = Thumb1-4 (element[0-3])
/// Motor ID 5-8 = Index1-4 (element[4-7])
/// Motor ID 9-12 = Middle1-4 (element[8-11])
/// </summary>
public class FingerColliderManager : MonoBehaviour
{
    [Header("Mode Control")]
    public ModeSwitching modeSwitching;
    
    [Header("Right Hand Touch Detectors")]
    public TriggerRightThumbTip triggerRightThumbTip;
    public TriggerRightIndexTip triggerRightIndexTip;
    public TriggerRightMiddleTip triggerRightMiddleTip;
    
    [Header("12 Motor Colliders")]
    [Tooltip("12 motor colliders: [0-3]=Thumb1-4, [4-7]=Index1-4, [8-11]=Middle1-4")]
    public Collider[] motorColliders = new Collider[12];
    
    [Header("Debug Info")]
    public int currentActiveFinger = -1; // 0=Thumb, 1=Index, 2=Middle, -1=None
    public string debugInfo = "";

    private int lastActiveFinger = -1;

    void Update()
    {
        if (modeSwitching == null)
        {
            debugInfo = "ModeSwitching is null!";
            return;
        }

        // When modeSelect=false, enable all colliders
        if (!modeSwitching.modeSelect)
        {
            EnableAllColliders();
            currentActiveFinger = -1;
            lastActiveFinger = -1;
            debugInfo = "modeSelect=false: All colliders enabled";
            return;
        }

        // When modeSelect=true, manage colliders based on which right-hand renderer the ball is moving on
        int activeFinger = DetectActiveFingerFromBall();
        
        // If the state has not changed, no need to repeat operations
        if (activeFinger == lastActiveFinger)
            return;
        
        lastActiveFinger = activeFinger;
        currentActiveFinger = activeFinger;

        if (activeFinger == -1)
        {
            // No ball is moving on any renderer, disable all colliders
            DisableAllColliders();
            debugInfo = "modeSelect=true, No ball on renderer: All colliders disabled";
        }
        else
        {
            // Enable only the 4 colliders of the active finger, disable others
            UpdateColliderStates(activeFinger);
            
            string fingerName = GetFingerName(activeFinger);
            debugInfo = $"modeSelect=true, Ball on {fingerName} renderer: Only {fingerName} colliders enabled";
        }
    }

    /// <summary>
    /// Detects which right-hand renderer the ball is moving on.
    /// Returns: 0=Thumb, 1=Index, 2=Middle, -1=None
    /// </summary>
    int DetectActiveFingerFromBall()
    {
        // Check thumb renderer
        if (triggerRightThumbTip != null && triggerRightThumbTip.isRightThumbTipTouched)
        {
            return 0; // Thumb
        }
        
        // Check index renderer
        if (triggerRightIndexTip != null && triggerRightIndexTip.isRightIndexTipTouched)
        {
            return 1; // Index
        }
        
        // Check middle renderer
        if (triggerRightMiddleTip != null && triggerRightMiddleTip.isRightMiddleTipTouched)
        {
            return 2; // Middle
        }
        
        // No ball is moving on any renderer
        return -1;
    }

    /// <summary>
    /// Updates collider states: enable only the 4 colliders of the specified finger
    /// </summary>
    void UpdateColliderStates(int activeFingerIndex)
    {
        for (int i = 0; i < motorColliders.Length; i++)
        {
            if (motorColliders[i] == null) continue;
            
            // Determine which finger this collider belongs to
            int colliderFingerIndex = i / 4; // 0-3→0, 4-7→1, 8-11→2
            
            // Enable only the colliders of the active finger
            bool shouldEnable = (colliderFingerIndex == activeFingerIndex);
            motorColliders[i].enabled = shouldEnable;
        }
    }

    /// <summary>
    /// Enable all colliders
    /// </summary>
    void EnableAllColliders()
    {
        foreach (var collider in motorColliders)
        {
            if (collider != null)
                collider.enabled = true;
        }
    }

    /// <summary>
    /// Disable all colliders
    /// </summary>
    void DisableAllColliders()
    {
        foreach (var collider in motorColliders)
        {
            if (collider != null)
                collider.enabled = false;
        }
    }

    /// <summary>
    /// Get the finger name for debugging
    /// </summary>
    string GetFingerName(int fingerIndex)
    {
        switch (fingerIndex)
        {
            case 0: return "Thumb";
            case 1: return "Index";
            case 2: return "Middle";
            default: return "Unknown";
        }
    }

    // Visualize the current state in the Inspector
    void OnValidate()
    {
        if (motorColliders.Length != 12)
        {
            Debug.LogWarning("[FingerColliderManager] The motorColliders array should have 12 elements!");
        }
    }
}
