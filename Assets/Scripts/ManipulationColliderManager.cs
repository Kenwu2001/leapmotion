using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManipulationColliderManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to ModeSwitching to check modeManipulate")]
    public ModeSwitching modeSwitching;
    
    [Tooltip("Reference to SelectMotorCollider to get currentTouchedMotorID")]
    public SelectMotorCollider selectMotorCollider;

    [Header("Paxini Colliders")]
    [Tooltip("Collider with TriggerRightThumbTip (Motor 1-4)")]
    public Collider thumbTipCollider;
    
    [Tooltip("Collider with TriggerRightIndexTip (Motor 5-8)")]
    public Collider indexTipCollider;
    
    [Tooltip("Collider with TriggerRightMiddleTip (Motor 9-12)")]
    public Collider middleTipCollider;

    [Header("Debug Info")]
    public bool isThumbColliderActive = false;
    public bool isIndexColliderActive = false;
    public bool isMiddleColliderActive = false;

    private int lastMotorID = -1;

    private void Update()
    {
        if (modeSwitching == null || selectMotorCollider == null)
            return;

        // Only manage colliders when in manipulate mode
        if (modeSwitching.modeManipulate)
        {
            int motorID = modeSwitching.confirmedMotorID; // Updated to use confirmedMotorID (deep red indicates confirmed selection)

            // Only update if motorID has changed
            if (motorID != lastMotorID)
            {
                Debug.Log($"Current motorID: {motorID}");
                lastMotorID = motorID;

                if (motorID >= 1 && motorID <= 4)
                {
                    // Motor 1-4: Only thumb collider active
                    Debug.Log("Activating thumb collider");
                    EnableCollider(thumbTipCollider, true);
                    EnableCollider(indexTipCollider, false);
                    EnableCollider(middleTipCollider, false);
                }
                else if (motorID >= 5 && motorID <= 8)
                {
                    // Motor 5-8: Only index collider active
                    Debug.Log("Activating index collider");
                    EnableCollider(thumbTipCollider, false);
                    EnableCollider(indexTipCollider, true);
                    EnableCollider(middleTipCollider, false);
                }
                else if (motorID >= 9 && motorID <= 12)
                {
                    // Motor 9-12: Only middle collider active
                    Debug.Log("Activating middle collider");
                    EnableCollider(thumbTipCollider, false);
                    EnableCollider(indexTipCollider, false);
                    EnableCollider(middleTipCollider, true);
                }
                else if (motorID == 0)
                {
                    // No motor touched: Enable all colliders
                    Debug.Log("No motor touched, enabling all colliders");
                    EnableCollider(thumbTipCollider, true);
                    EnableCollider(indexTipCollider, true);
                    EnableCollider(middleTipCollider, true);
                }
            }
        }
        else
        {
            // Not in manipulate mode: Enable all colliders
            if (lastMotorID != -1) // Only log once when switching out of manipulate mode
            {
                Debug.Log("Not in manipulate mode, enabling all colliders");
                lastMotorID = -1;
            }
            EnableCollider(thumbTipCollider, true);
            EnableCollider(indexTipCollider, true);
            EnableCollider(middleTipCollider, true);
        }

        // Update debug info
        UpdateDebugInfo();
    }

    private void EnableCollider(Collider collider, bool enabled)
    {
        if (collider != null)
        {
            // Log the current state for debugging
            Debug.Log($"Setting collider {collider.name} to {(enabled ? "enabled" : "disabled")}");

            // If disabling, clear touch records first
            if (!enabled)
            {
                TriggerRightThumbTip thumbTrigger = collider.GetComponent<TriggerRightThumbTip>();
                if (thumbTrigger != null)
                {
                    thumbTrigger.ClearTouches();
                }

                TriggerRightIndexTip indexTrigger = collider.GetComponent<TriggerRightIndexTip>();
                if (indexTrigger != null)
                {
                    indexTrigger.ClearTouches();
                }

                TriggerRightMiddleTip middleTrigger = collider.GetComponent<TriggerRightMiddleTip>();
                if (middleTrigger != null)
                {
                    middleTrigger.ClearTouches();
                }
            }

            collider.enabled = enabled;
        }
    }

    private void UpdateDebugInfo()
    {
        isThumbColliderActive = thumbTipCollider != null && thumbTipCollider.enabled;
        isIndexColliderActive = indexTipCollider != null && indexTipCollider.enabled;
        isMiddleColliderActive = middleTipCollider != null && middleTipCollider.enabled;
    }
}
