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

    [Header("Manipulation Retargeting Colliders")]
    [Tooltip("Collider used during manipulate mode for thumb retargeting (Motor 1-4 / 13)")]
    public Collider thumbManipulateRetargetingCollider;

    [Tooltip("Collider used during manipulate mode for index retargeting (Motor 5-8 / 14)")]
    public Collider indexManipulateRetargetingCollider;

    [Tooltip("Collider used during manipulate mode for middle retargeting (Motor 9-12 / 15)")]
    public Collider middleManipulateRetargetingCollider;

    [Header("Debug Info")]
    public bool isThumbColliderActive = false;
    public bool isIndexColliderActive = false;
    public bool isMiddleColliderActive = false;
    public bool isThumbRetargetingColliderActive = false;
    public bool isIndexRetargetingColliderActive = false;
    public bool isMiddleRetargetingColliderActive = false;

    [Header("Debug Logging")]
    [Tooltip("Enable verbose collider state logs while debugging manipulate retargeting")]
    public bool enableVerboseLogs = true;

    [Tooltip("Minimum seconds between automatic state logs")]
    public float verboseLogInterval = 0.2f;

    private int lastMotorID = -1;
    private float _lastVerboseLogTime = -999f;

    private void Update()
    {
        if (modeSwitching == null || selectMotorCollider == null)
            return;

        // Only manage colliders when in manipulate mode
        if (modeSwitching.modeManipulate)
        {
            int motorID = modeSwitching.confirmedMotorID; // Updated to use confirmedMotorID (deep red indicates confirmed selection)

            if (enableVerboseLogs && Time.time - _lastVerboseLogTime >= verboseLogInterval)
            {
                _lastVerboseLogTime = Time.time;
                LogColliderSnapshot("ManipulateLoop");
            }

            // Determine the desired retargeting state based on current motorID
            bool thumbActive   = (motorID >= 1 && motorID <= 4) || motorID == 13;
            bool indexActive   = (motorID >= 5 && motorID <= 8) || motorID == 14;
            bool middleActive  = (motorID >= 9 && motorID <= 12) || motorID == 15;

            // Enforce correct state EVERY FRAME to override any external re-enabling
            if (motorID == 0)
            {
                // No motor touched: all selection colliders on, all retargeting colliders off
                SetSelectionColliders(true, true, true);
            }
            else
            {
                SetManipulationRetargetingColliders(thumbActive, indexActive, middleActive);
            }

            // Log AfterMotorSwitch only when motorID changes
            if (motorID != lastMotorID)
            {
                Debug.Log($"Current motorID: {motorID}");
                lastMotorID = motorID;

                if (enableVerboseLogs)
                {
                    LogColliderSnapshot("AfterMotorSwitch");
                }
            }
        }
        else
        {
            // Not in manipulate mode: selection colliders on, manipulation retargeting colliders off
            if (lastMotorID != -1) // Only log once when switching out of manipulate mode
            {
                Debug.Log("Not in manipulate mode, enabling all colliders");
                lastMotorID = -1;
            }
            SetSelectionColliders(true, true, true);

            if (enableVerboseLogs && Time.time - _lastVerboseLogTime >= verboseLogInterval)
            {
                _lastVerboseLogTime = Time.time;
                LogColliderSnapshot("SelectModeLoop");
            }
        }

        // Update debug info
        UpdateDebugInfo();
    }

    private void EnableCollider(Collider collider, bool enabled)
    {
        if (collider != null)
        {
            // Log the current state for debugging
            // Debug.Log($"Setting collider {collider.name} to {(enabled ? "enabled" : "disabled")}");

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

    private void SetSelectionColliders(bool thumbEnabled, bool indexEnabled, bool middleEnabled)
    {
        EnableCollider(thumbTipCollider, thumbEnabled);
        EnableCollider(indexTipCollider, indexEnabled);
        EnableCollider(middleTipCollider, middleEnabled);

        EnableCollider(thumbManipulateRetargetingCollider, false);
        EnableCollider(indexManipulateRetargetingCollider, false);
        EnableCollider(middleManipulateRetargetingCollider, false);

        if (enableVerboseLogs)
        {
            LogColliderSnapshot($"SetSelectionColliders(t:{thumbEnabled}, i:{indexEnabled}, m:{middleEnabled})");
        }
    }

    private void SetManipulationRetargetingColliders(bool thumbEnabled, bool indexEnabled, bool middleEnabled)
    {
        // Turn off the selection colliders so only the manipulate retargeting colliders stay active.
        EnableCollider(thumbTipCollider, thumbEnabled);
        EnableCollider(indexTipCollider, indexEnabled);
        EnableCollider(middleTipCollider, middleEnabled);

        EnableCollider(thumbManipulateRetargetingCollider, thumbEnabled);
        EnableCollider(indexManipulateRetargetingCollider, indexEnabled);
        EnableCollider(middleManipulateRetargetingCollider, middleEnabled);

        if (enableVerboseLogs)
        {
            LogColliderSnapshot($"SetManipulateRetargeting(t:{thumbEnabled}, i:{indexEnabled}, m:{middleEnabled})");
        }
    }

    private void UpdateDebugInfo()
    {
        isThumbColliderActive = thumbTipCollider != null && thumbTipCollider.enabled;
        isIndexColliderActive = indexTipCollider != null && indexTipCollider.enabled;
        isMiddleColliderActive = middleTipCollider != null && middleTipCollider.enabled;

        isThumbRetargetingColliderActive = thumbManipulateRetargetingCollider != null && thumbManipulateRetargetingCollider.enabled;
        isIndexRetargetingColliderActive = indexManipulateRetargetingCollider != null && indexManipulateRetargetingCollider.enabled;
        isMiddleRetargetingColliderActive = middleManipulateRetargetingCollider != null && middleManipulateRetargetingCollider.enabled;
    }

    private void LogColliderSnapshot(string source)
    {
        string mode = modeSwitching != null ? $"select={modeSwitching.modeSelect}, manipulate={modeSwitching.modeManipulate}" : "modeSwitching=null";
        int confirmed = modeSwitching != null ? modeSwitching.confirmedMotorID : -999;

        Debug.Log(
            "[ManipulationColliderManager] " + source +
            " | " + mode +
            $" | confirmedMotorID={confirmed}" +
            " | Sel(T/I/M)=" + ColliderState(thumbTipCollider) + "/" + ColliderState(indexTipCollider) + "/" + ColliderState(middleTipCollider) +
            " | Retarget(T/I/M)=" + ColliderState(thumbManipulateRetargetingCollider) + "/" + ColliderState(indexManipulateRetargetingCollider) + "/" + ColliderState(middleManipulateRetargetingCollider)
        );
    }

    private string ColliderState(Collider c)
    {
        if (c == null)
            return "null";

        bool goActive = c.gameObject.activeInHierarchy;
        return c.enabled ? (goActive ? "ON" : "ON(goOff)") : (goActive ? "off" : "off(goOff)");
    }
}