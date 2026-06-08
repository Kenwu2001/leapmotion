using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModeSwitching : MonoBehaviour
{
    private const int ThumbPaxiniMotorID = 13;
    private const int IndexPaxiniMotorID = 14;
    private const int MiddlePaxiniMotorID = 15;

    public JointAngle jointAngle;
    public SelectMotorCollider SelectMotorCollider;

    public Renderer thumbJoint1Renderer;
    public Renderer thumbJoint2Renderer;
    public Renderer thumbJoint3Renderer;
    public Renderer thumbJoint4Renderer;

    public Renderer indexJoint1Renderer;
    public Renderer indexJoint2Renderer;
    public Renderer indexJoint3Renderer;
    public Renderer indexJoint4Renderer;

    public Renderer middleJoint1Renderer;
    public Renderer middleJoint2Renderer;
    public Renderer middleJoint3Renderer;
    public Renderer middleJoint4Renderer;

    public Renderer thumbPaxiniRenderer;
    public Renderer indexPaxiniRenderer;
    public Renderer middlePaxiniRenderer;

    public Renderer baseRenderer;

    private Color originalColor;
    private Color thumbPaxiniOriginalColor;
    private Color indexPaxiniOriginalColor;
    private Color middlePaxiniOriginalColor;
    public Color lightRedColor = new Color(1f, 0.5f, 0.5f, 1f); // Light red (temporary touch)
    public Color darkRedColor = Color.red; // Dark red (confirmed selection)
    
    [Header("Selection Timing")]
    [Tooltip("How many seconds to confirm selection (turn dark red)")]
    public float confirmationTime = 0.25f; // Confirm after exceeding this time
    
    public bool modeSelect = true;
    public bool motorSelected = false;
    public bool modeManipulate = false;
    public float currentHandSeparationDistance = 0f;
    
    public int lastTouchedMotorID = 0;
    public int currentRedMotorID = 0; // Currently touched motor (displayed in light/dark red)
    
    [Header("Confirmed Selection")]
    [Tooltip("ID of the confirmed motor (dark red) - this is retained after leaving modeSelect")]
    public int confirmedMotorID = 0; // Confirmed motor (dark red)
    
    private float touchStartTime = 0f; // Time when touch started
    private bool isConfirmed = false; // Whether the currently touched motor is confirmed (dark red)
    
    private bool hasEnteredCloseRange = false; // Track if we've entered < 0.16f during manipulation
    private bool hasSetManipulateColors = false; // Track if we've set manipulate colors

    public Material yellowMaterial;
    
    [Header("=== New Feature: Fingertip Priority Selection ===")]
    [Tooltip("Enable fingertip priority mode: Fingertip motors (4, 8, 12) must be confirmed before selecting other motors")]
    public bool useFingertipFirst = false;
    
    [Tooltip("Current selection phase")]
    public SelectionPhase currentPhase = SelectionPhase.SelectingFingertip;
    
    [Tooltip("ID of the confirmed fingertip motor")]
    public int confirmedFingertipID = 0;
    
    public Color grayColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray (disabled/unselectable)
    
    public enum SelectionPhase
    {
        SelectingFingertip,   // Phase 1: Selecting fingertip (4, 8, 12)
        SelectingMotor,       // Phase 2: Selecting the motor of the finger
        MotorConfirmed        // Final motor confirmed
    }
    
    void Start()
    {
        if (SelectMotorCollider != null)
        {
            if (thumbPaxiniRenderer == null && SelectMotorCollider.triggerRightThumbTip != null)
                thumbPaxiniRenderer = SelectMotorCollider.triggerRightThumbTip.thumbPaxiniRenderer;
            if (indexPaxiniRenderer == null && SelectMotorCollider.triggerRightIndexTip != null)
                indexPaxiniRenderer = SelectMotorCollider.triggerRightIndexTip.indexPaxiniRenderer;
            if (middlePaxiniRenderer == null && SelectMotorCollider.triggerRightMiddleTip != null)
                middlePaxiniRenderer = SelectMotorCollider.triggerRightMiddleTip.middlePaxiniRenderer;
        }

        originalColor = thumbJoint1Renderer.material.color;
        thumbPaxiniOriginalColor = thumbPaxiniRenderer != null ? thumbPaxiniRenderer.material.color : originalColor;
        indexPaxiniOriginalColor = indexPaxiniRenderer != null ? indexPaxiniRenderer.material.color : originalColor;
        middlePaxiniOriginalColor = middlePaxiniRenderer != null ? middlePaxiniRenderer.material.color : originalColor;
        modeSelect = true;
        motorSelected = false;
        modeManipulate = false;
        
        // Initialize fingertip priority feature
        if (useFingertipFirst)
        {
            currentPhase = SelectionPhase.SelectingFingertip;
            confirmedFingertipID = 0;
        }
        
        ResetAllColors();

        // If fingertip priority mode is enabled, initially gray out non-fingertip motors
        if (useFingertipFirst)
        {
            UpdateGrayColors();
        }
    }

    void Update()
    {
        if (jointAngle != null)
        {
            currentHandSeparationDistance = jointAngle.GetLIndexToIndex2Distance();
        }

        if (modeSelect && SelectMotorCollider != null)
        {
            int currentMotorID = SelectMotorCollider.currentTouchedMotorID;

            // Motor switched
            if (currentMotorID != lastTouchedMotorID)
            {
                if (currentMotorID != 0)
                {
                    // Guard: if ANY Paxini (13/14/15) is confirmed (yellow), do NOT turn it off
                    // yet — yellow must stay until the new motor is confirmed after hold time,
                    // regardless of whether the hover is same-finger or cross-finger.
                    bool confirmedIsPaxini =
                        confirmedMotorID == ThumbPaxiniMotorID ||
                        confirmedMotorID == IndexPaxiniMotorID ||
                        confirmedMotorID == MiddlePaxiniMotorID;
                    if (!confirmedIsPaxini)
                    {
                        SelectMotorCollider.ForcePaxiniOffForMotor(currentMotorID);
                    }

                    // New motor touched
                    touchStartTime = Time.time; // Record the start touch time
                    isConfirmed = false; // New touch not yet confirmed
                    currentRedMotorID = currentMotorID;

                    // Update color display
                    UpdateMotorColors();

                    if (!motorSelected)
                    {
                        motorSelected = true;
                    }
                }
                else
                {
                    // Left all motors - only keep the confirmed selection in dark red
                    currentRedMotorID = 0;
                    UpdateMotorColors();
                }

                lastTouchedMotorID = currentMotorID;
            }
            else if (currentMotorID != 0)
            {
                // Continuously touching the same motor - check if confirmation time exceeded
                if (!isConfirmed && (Time.time - touchStartTime) >= confirmationTime)
                {
                    // Exceeded confirmation time, turn dark red (confirmed selection)
                    isConfirmed = true;

                    // Handle confirmation logic based on whether fingertip priority mode is enabled
                    if (useFingertipFirst)
                    {
                        // If a Paxini was confirmed (yellow), clear it now that a new motor is confirmed.
                        if (ShouldClearConfirmedPaxiniForNewMotor(currentMotorID))
                            SelectMotorCollider.ForcePaxiniOffForMotor(confirmedMotorID);
                        HandleFingertipFirstConfirmation(currentMotorID);
                    }
                    else
                    {
                        // If a Paxini was confirmed (yellow), clear it now that a new motor is confirmed.
                        if (ShouldClearConfirmedPaxiniForNewMotor(currentMotorID))
                            SelectMotorCollider.ForcePaxiniOffForMotor(confirmedMotorID);
                        confirmedMotorID = currentMotorID; // Original logic
                        SelectMotorCollider.ForcePaxiniOffForMotor(currentMotorID);
                    }

                    UpdateMotorColors();
                }
            }
        }

        bool isConfirmedPaxiniMotor = IsPaxiniMotor(confirmedMotorID);

        // For Paxini pseudo motors (13/14/15): never enter manipulate mode.
        // When hands separate, return to base select state but keep freeze (yellow) handled by TriggerRight*Tip.
        if (modeSelect && motorSelected && isConfirmedPaxiniMotor)
        {
            float distance = currentHandSeparationDistance;
            if (distance > 0.16f)
            {
                ReturnToBaseSelectStateAfterPaxini();
            }
        }

        // Transition: Select → Manipulate (distance increases)
        // Only confirmed motor (dark red) can enter Manipulate mode
        // Fingertip priority mode: Must be in MotorConfirmed phase
        bool canEnterManipulate = modeSelect && motorSelected && confirmedMotorID != 0;
        if (isConfirmedPaxiniMotor)
        {
            canEnterManipulate = false;
        }
        if (useFingertipFirst)
        {
            canEnterManipulate = canEnterManipulate && currentPhase == SelectionPhase.MotorConfirmed;
        }
        if (SelectMotorCollider != null && SelectMotorCollider.suppressManipulateTransition)
        {
            canEnterManipulate = false;
        }

        if (canEnterManipulate)
        {
            float distance = currentHandSeparationDistance;

            if (distance > 0.16f)
            {
                modeSelect = false;
                motorSelected = false;
                modeManipulate = true;
                hasEnteredCloseRange = false; // Reset when entering manipulate mode
                hasSetManipulateColors = false; // Reset color flag
                
                // Hide all debug visuals (spheres + LineRenderers) when entering manipulate mode
                if (SelectMotorCollider != null)
                {
                    SelectMotorCollider.HideAllDebugVisuals();
                }
            }
        }

        // Mode Manipulate: Track state and exit conditions
        if (modeManipulate)
        {
            // baseRenderer.material.color = Color.green; // Indicate Manipulate mode

            float distance = currentHandSeparationDistance;

            // Set colors only once when entering manipulate mode
            if (!hasSetManipulateColors)
            {
                // Reset all motors to original color first (no gray in manipulation mode)
                ResetAllColors();
                
                if (confirmedMotorID == 1)
                {
                    thumbJoint1Renderer.material.color = Color.red; // Keep red
                    thumbJoint2Renderer.material = yellowMaterial;
                    thumbJoint3Renderer.material = yellowMaterial;
                    thumbJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 2)
                {
                    thumbJoint2Renderer.material.color = Color.red; // Keep red
                    thumbJoint1Renderer.material = yellowMaterial;
                    thumbJoint3Renderer.material = yellowMaterial;
                    thumbJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 3)
                {
                    thumbJoint3Renderer.material.color = Color.red; // Keep red
                    thumbJoint1Renderer.material = yellowMaterial;
                    thumbJoint2Renderer.material = yellowMaterial;
                    thumbJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 4)
                {
                    thumbJoint4Renderer.material.color = Color.red; // Keep red
                    thumbJoint1Renderer.material = yellowMaterial;
                    thumbJoint2Renderer.material = yellowMaterial;
                    thumbJoint3Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 5)
                {
                    indexJoint1Renderer.material.color = Color.red; // Keep red
                    indexJoint2Renderer.material = yellowMaterial;
                    indexJoint3Renderer.material = yellowMaterial;
                    indexJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 6)
                {
                    indexJoint2Renderer.material.color = Color.red; // Keep red
                    indexJoint1Renderer.material = yellowMaterial;
                    indexJoint3Renderer.material = yellowMaterial;
                    indexJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 7)
                {
                    indexJoint3Renderer.material.color = Color.red; // Keep red
                    indexJoint1Renderer.material = yellowMaterial;
                    indexJoint2Renderer.material = yellowMaterial;
                    indexJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 8)
                {
                    indexJoint4Renderer.material.color = Color.red; // Keep red
                    indexJoint1Renderer.material = yellowMaterial;
                    indexJoint2Renderer.material = yellowMaterial;
                    indexJoint3Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 9)
                {
                    middleJoint1Renderer.material.color = Color.red; // Keep red
                    middleJoint2Renderer.material = yellowMaterial;
                    middleJoint3Renderer.material = yellowMaterial;
                    middleJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 10)
                {
                    middleJoint2Renderer.material.color = Color.red; // Keep red
                    middleJoint1Renderer.material = yellowMaterial;
                    middleJoint3Renderer.material = yellowMaterial;
                    middleJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 11)
                {
                    middleJoint3Renderer.material.color = Color.red; // Keep red
                    middleJoint1Renderer.material = yellowMaterial;
                    middleJoint2Renderer.material = yellowMaterial;
                    middleJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == 12)
                {
                    middleJoint4Renderer.material.color = Color.red; // Keep red
                    middleJoint1Renderer.material = yellowMaterial;
                    middleJoint2Renderer.material = yellowMaterial;
                    middleJoint3Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == ThumbPaxiniMotorID)
                {
                    if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = Color.red;
                    thumbJoint1Renderer.material = yellowMaterial;
                    thumbJoint2Renderer.material = yellowMaterial;
                    thumbJoint3Renderer.material = yellowMaterial;
                    thumbJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == IndexPaxiniMotorID)
                {
                    if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = Color.red;
                    indexJoint1Renderer.material = yellowMaterial;
                    indexJoint2Renderer.material = yellowMaterial;
                    indexJoint3Renderer.material = yellowMaterial;
                    indexJoint4Renderer.material = yellowMaterial;
                }
                else if (confirmedMotorID == MiddlePaxiniMotorID)
                {
                    if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = Color.red;
                    middleJoint1Renderer.material = yellowMaterial;
                    middleJoint2Renderer.material = yellowMaterial;
                    middleJoint3Renderer.material = yellowMaterial;
                    middleJoint4Renderer.material = yellowMaterial;
                }
                hasSetManipulateColors = true;
            }

            // Track if we've entered close range (< 0.16f) for manipulation
            if (distance < 0.16f)
            {
                hasEnteredCloseRange = true;
            }

            // Only exit if we've performed manipulation (entered close range) 
            // and then moved back out (> 0.16f)
            if (hasEnteredCloseRange && distance > 0.16f)
            {
                modeSelect = true;
                motorSelected = false;
                modeManipulate = false;
                ResetAllColors();
                currentRedMotorID = 0; // Clear the current touch record
                confirmedMotorID = 0; // Clear confirmed selection
                lastTouchedMotorID = 0;
                isConfirmed = false;
                touchStartTime = 0f;
                hasEnteredCloseRange = false;
                hasSetManipulateColors = false; // Reset color flag
                // baseRenderer.material.color = originalColor; // Reset base color

                // Restore debug visuals based on current toggle states
                if (SelectMotorCollider != null)
                {
                    SelectMotorCollider.RestoreDebugVisuals();
                }

                // Reset fingertip priority mode state
                if (useFingertipFirst)
                {
                    currentPhase = SelectionPhase.SelectingFingertip;
                    confirmedFingertipID = 0;
                    SelectMotorCollider.ResetFingertipConfirmation();
                    SelectMotorCollider.ReleaseFrozenLine();
                    UpdateGrayColors(); // Gray out non-fingertip motors again
                }
            }
        }
    }
    
    /// <summary>
    /// Update motor colors:
    /// - The confirmed motor (`confirmedMotorID`) is shown in dark red
    /// - The currently touched but unconfirmed motor is shown in light red
    /// </summary>
    private void UpdateMotorColors()
    {
        // Reset all colors first
        if (useFingertipFirst)
        {
            // Fingertip-first mode: set gray base
            UpdateGrayColors();
        }
        else
        {
            ResetAllColors();
        }

        // If there is a confirmed motor, show it in dark red
        if (confirmedMotorID != 0)
        {
            SetMotorColorDirect(confirmedMotorID, darkRedColor);
        }
        
        // If the currently touched motor is not the confirmed one, show it in light red
        if (currentRedMotorID != 0 && currentRedMotorID != confirmedMotorID)
        {
            if (isConfirmed)
            {
                // If the current touch is confirmed, use dark red
                SetMotorColorDirect(currentRedMotorID, darkRedColor);
            }
            else
            {
                // Not yet confirmed, use light red
                SetMotorColorDirect(currentRedMotorID, lightRedColor);
            }
        }
    }

    private bool IsSameFingerGroup(int motorA, int motorB)
    {
        return GetFingerGroupIndex(motorA) == GetFingerGroupIndex(motorB);
    }

    private bool IsPaxiniMotor(int motorID)
    {
        return motorID == ThumbPaxiniMotorID ||
               motorID == IndexPaxiniMotorID ||
               motorID == MiddlePaxiniMotorID;
    }

    private bool ShouldClearConfirmedPaxiniForNewMotor(int newMotorID)
    {
        if (!IsPaxiniMotor(confirmedMotorID)) return false;
        if (newMotorID == 0 || newMotorID == confirmedMotorID) return false;

        // Keep Paxini ON across different fingers (13/14/15 can be yellow at the same time).
        // Only clear when confirming another motor in the same finger group.
        return IsSameFingerGroup(confirmedMotorID, newMotorID);
    }

    private void ReturnToBaseSelectStateAfterPaxini()
    {
        modeSelect = true;
        modeManipulate = false;
        motorSelected = false;
        currentRedMotorID = 0;
        confirmedMotorID = 0;
        lastTouchedMotorID = 0;
        isConfirmed = false;
        touchStartTime = 0f;
        hasEnteredCloseRange = false;
        hasSetManipulateColors = false;

        if (useFingertipFirst)
        {
            currentPhase = SelectionPhase.SelectingFingertip;
            confirmedFingertipID = 0;

            if (SelectMotorCollider != null)
            {
                SelectMotorCollider.ResetFingertipConfirmation();
                SelectMotorCollider.ReleaseFrozenLine();
            }

            // Return only 1-12 motors to base selectable colors.
            // Paxini color is managed by TriggerRight*Tip (freeze yellow can stay ON).
            thumbJoint1Renderer.material.color = grayColor;
            thumbJoint2Renderer.material.color = grayColor;
            thumbJoint3Renderer.material.color = grayColor;
            thumbJoint4Renderer.material.color = originalColor;

            indexJoint1Renderer.material.color = grayColor;
            indexJoint2Renderer.material.color = grayColor;
            indexJoint3Renderer.material.color = grayColor;
            indexJoint4Renderer.material.color = originalColor;

            middleJoint1Renderer.material.color = grayColor;
            middleJoint2Renderer.material.color = grayColor;
            middleJoint3Renderer.material.color = grayColor;
            middleJoint4Renderer.material.color = originalColor;
        }
        else
        {
            thumbJoint1Renderer.material.color = originalColor;
            thumbJoint2Renderer.material.color = originalColor;
            thumbJoint3Renderer.material.color = originalColor;
            thumbJoint4Renderer.material.color = originalColor;

            indexJoint1Renderer.material.color = originalColor;
            indexJoint2Renderer.material.color = originalColor;
            indexJoint3Renderer.material.color = originalColor;
            indexJoint4Renderer.material.color = originalColor;

            middleJoint1Renderer.material.color = originalColor;
            middleJoint2Renderer.material.color = originalColor;
            middleJoint3Renderer.material.color = originalColor;
            middleJoint4Renderer.material.color = originalColor;
        }
    }

    private int GetFingerGroupIndex(int motorID)
    {
        if (motorID >= 1 && motorID <= 4) return 0;
        if (motorID == ThumbPaxiniMotorID) return 0;

        if (motorID >= 5 && motorID <= 8) return 1;
        if (motorID == IndexPaxiniMotorID) return 1;

        if (motorID >= 9 && motorID <= 12) return 2;
        if (motorID == MiddlePaxiniMotorID) return 2;

        return -1;
    }

    /// <summary>
    /// Clears the confirmed motor selection for a given finger (identified by motor range)
    /// and refreshes motor colors. Called by SelectMotorCollider when entering the freeze zone,
    /// so the previously dark-red motor reverts to its original color (Problem 2 fix).
    /// </summary>
    public void ClearConfirmedMotorForFinger(int minMotor, int maxMotor)
    {
        if (!modeSelect) return;
        if (confirmedMotorID >= minMotor && confirmedMotorID <= maxMotor)
        {
            confirmedMotorID = 0;
            motorSelected = false;
            currentRedMotorID = 0;
            isConfirmed = false;
            UpdateMotorColors();
        }
    }

    /// <summary>
    /// Set a single motor's color directly (do not reset other colors)
    /// </summary>
    private void SetMotorColorDirect(int motorID, Color color)
    {
        switch (motorID)
        {
            case 1: thumbJoint1Renderer.material.color = color; break;
            case 2: thumbJoint2Renderer.material.color = color; break;
            case 3: thumbJoint3Renderer.material.color = color; break;
            case 4: thumbJoint4Renderer.material.color = color; break;
            case 5: indexJoint1Renderer.material.color = color; break;
            case 6: indexJoint2Renderer.material.color = color; break;
            case 7: indexJoint3Renderer.material.color = color; break;
            case 8: indexJoint4Renderer.material.color = color; break;
            case 9: middleJoint1Renderer.material.color = color; break;
            case 10: middleJoint2Renderer.material.color = color; break;
            case 11: middleJoint3Renderer.material.color = color; break;
            case 12: middleJoint4Renderer.material.color = color; break;
            case ThumbPaxiniMotorID: if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = color; break;
            case IndexPaxiniMotorID: if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = color; break;
            case MiddlePaxiniMotorID: if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = color; break;
        }
    }
    
    private void SetMotorColor(int motorID, Color color)
    {
        // Reset all colors first
        ResetAllColors();
                
        // Set the specific motor's color
        switch (motorID)
        {
            case 1:
                thumbJoint1Renderer.material.color = color;
                break;
            case 2:
                thumbJoint2Renderer.material.color = color;
                break;
            case 3:
                thumbJoint3Renderer.material.color = color;
                break;
            case 4:
                thumbJoint4Renderer.material.color = color;
                break;
            case 5:
                indexJoint1Renderer.material.color = color;
                break;
            case 6:
                indexJoint2Renderer.material.color = color;
                break;
            case 7:
                indexJoint3Renderer.material.color = color;
                break;
            case 8:
                indexJoint4Renderer.material.color = color;
                break;
            case 9:
                middleJoint1Renderer.material.color = color;
                break;
            case 10:
                middleJoint2Renderer.material.color = color;
                break;
            case 11:
                middleJoint3Renderer.material.color = color;
                break;
            case 12:
                middleJoint4Renderer.material.color = color;
                break;
            case ThumbPaxiniMotorID:
                if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = color;
                break;
            case IndexPaxiniMotorID:
                if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = color;
                break;
            case MiddlePaxiniMotorID:
                if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = color;
                break;
            default:
                break;
        }
    }
    
    private void ResetAllColors()
    {
        thumbJoint1Renderer.material.color = originalColor;
        thumbJoint2Renderer.material.color = originalColor;
        thumbJoint3Renderer.material.color = originalColor;
        thumbJoint4Renderer.material.color = originalColor;
        
        indexJoint1Renderer.material.color = originalColor;
        indexJoint2Renderer.material.color = originalColor;
        indexJoint3Renderer.material.color = originalColor;
        indexJoint4Renderer.material.color = originalColor;
        
        middleJoint1Renderer.material.color = originalColor;
        middleJoint2Renderer.material.color = originalColor;
        middleJoint3Renderer.material.color = originalColor;
        middleJoint4Renderer.material.color = originalColor;

        if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
        if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
        if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
    }

    public Color GetPaxiniDisplayColor(int motorID, Color fallbackOriginalColor)
    {
        // Paxini renderers (13/14/15): always return original color.
        // Yellow is handled by the showFreezeColor path in TriggerRight*Tip (bypasses this function).
        // Gray/red must never affect Paxini — they are not part of the 1-12 motor selection system.
        switch (motorID)
        {
            case ThumbPaxiniMotorID:
                return thumbPaxiniRenderer != null ? thumbPaxiniOriginalColor : fallbackOriginalColor;
            case IndexPaxiniMotorID:
                return indexPaxiniRenderer != null ? indexPaxiniOriginalColor : fallbackOriginalColor;
            case MiddlePaxiniMotorID:
                return middlePaxiniRenderer != null ? middlePaxiniOriginalColor : fallbackOriginalColor;
        }

        // Motors 1-12: apply gray/red logic as normal
        Color baseColor = fallbackOriginalColor;

        if (useFingertipFirst)
        {
            if (currentPhase == SelectionPhase.SelectingFingertip)
            {
                baseColor = grayColor;
            }
            else
            {
                switch (confirmedFingertipID)
                {
                    case 4:
                        baseColor = (motorID >= 1 && motorID <= 4) ? fallbackOriginalColor : grayColor;
                        break;
                    case 8:
                        baseColor = (motorID >= 5 && motorID <= 8) ? fallbackOriginalColor : grayColor;
                        break;
                    case 12:
                        baseColor = (motorID >= 9 && motorID <= 12) ? fallbackOriginalColor : grayColor;
                        break;
                    default:
                        baseColor = grayColor;
                        break;
                }
            }
        }

        if (confirmedMotorID == motorID)
            return darkRedColor;

        if (currentRedMotorID == motorID && currentRedMotorID != confirmedMotorID)
            return isConfirmed ? darkRedColor : lightRedColor;

        return baseColor;
    }
    
    /// <summary>
    /// Handle motor confirmation logic for fingertip-first mode
    /// </summary>
    private void HandleFingertipFirstConfirmation(int motorID)
    {
        switch (currentPhase)
        {
            case SelectionPhase.SelectingFingertip:
                // Phase 1: only accept fingertip motors (4, 8, 12)
                if (motorID == 4 || motorID == 8 || motorID == 12)
                {
                    confirmedFingertipID = motorID;
                    confirmedMotorID = motorID; // Record as the confirmed motor
                    
                    // Notify SelectMotorCollider that a fingertip has been confirmed
                    SelectMotorCollider.OnFingertipConfirmed(motorID);
                    
                    // Capture frozen line for this finger
                    SelectMotorCollider.CaptureFrozenLine(motorID);
                    
                    // Go directly to MotorConfirmed so that the user can
                    // separate hands immediately to enter manipulation mode.
                    // If they later touch a different motor on the same finger,
                    // the MotorConfirmed case below will handle the switch.
                    currentPhase = SelectionPhase.MotorConfirmed;
                    
                    // Update gray display (other motors of this finger are now selectable)
                    UpdateGrayColors();
                    
                    // Debug.Log($"[ModeSwitching] Fingertip motor {motorID} confirmed. Ready for manipulation (or select another motor).");
                }
                break;
                
            case SelectionPhase.SelectingMotor:
            case SelectionPhase.MotorConfirmed:
                // Check whether a new fingertip was selected (switch finger/change mind)
                if (motorID == 4 || motorID == 8 || motorID == 12)
                {
                    if (motorID != confirmedFingertipID)
                    {
                        // Switch to the new finger
                        confirmedFingertipID = motorID;
                        confirmedMotorID = motorID;
                        SelectMotorCollider.ForcePaxiniOffForMotor(motorID);
                        
                        // Notify SelectMotorCollider that the fingertip switched
                        SelectMotorCollider.OnFingertipConfirmed(motorID);
                        
                        // Capture frozen line for the new finger
                        SelectMotorCollider.CaptureFrozenLine(motorID);

                        // New fingertip is already confirmed, allow immediate manipulation.
                        currentPhase = SelectionPhase.MotorConfirmed;
                        
                        // Update gray display
                        UpdateGrayColors();
                        
                        Debug.Log($"[ModeSwitching] Switched to new fingertip motor {motorID}. Ready for manipulation.");
                    }
                    else
                    {
                        // Still the same fingertip
                        confirmedMotorID = motorID;
                        SelectMotorCollider.ForcePaxiniOffForMotor(motorID);
                        currentPhase = SelectionPhase.MotorConfirmed;
                        Debug.Log($"[ModeSwitching] Maintaining fingertip motor {motorID}");
                    }
                }
                else
                {
                    // Selecting another motor of the confirmed finger
                    confirmedMotorID = motorID;
                    SelectMotorCollider.ForcePaxiniOffForMotor(motorID);
                    currentPhase = SelectionPhase.MotorConfirmed;
                    Debug.Log($"[ModeSwitching] Motor {motorID} confirmed. Can enter manipulate mode.");
                }
                break;
        }
    }
    
    /// <summary>
    /// Update gray display (unselectable motors)
    /// </summary>
    private void UpdateGrayColors()
    {
        if (!useFingertipFirst)
        {
            ResetAllColors();
            return;
        }
        
        if (currentPhase == SelectionPhase.SelectingFingertip)
        {
            // Phase 1: only fingertips (4, 8, 12) are selectable, others gray out
            thumbJoint1Renderer.material.color = grayColor;
            thumbJoint2Renderer.material.color = grayColor;
            thumbJoint3Renderer.material.color = grayColor;
            thumbJoint4Renderer.material.color = originalColor; // Motor 4 - fingertip selectable
            
            indexJoint1Renderer.material.color = grayColor;
            indexJoint2Renderer.material.color = grayColor;
            indexJoint3Renderer.material.color = grayColor;
            indexJoint4Renderer.material.color = originalColor; // Motor 8 - fingertip selectable
            
            middleJoint1Renderer.material.color = grayColor;
            middleJoint2Renderer.material.color = grayColor;
            middleJoint3Renderer.material.color = grayColor;
            middleJoint4Renderer.material.color = originalColor; // Motor 12 - fingertip selectable

            if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
            if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
            if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
        }
        else if (currentPhase == SelectionPhase.SelectingMotor || currentPhase == SelectionPhase.MotorConfirmed)
        {
            // Phase 2/3: motors of the confirmed finger are selectable, other fingers' fingertips remain selectable
            switch (confirmedFingertipID)
            {
                case 4: // Thumb
                    thumbJoint1Renderer.material.color = originalColor;
                    thumbJoint2Renderer.material.color = originalColor;
                    thumbJoint3Renderer.material.color = originalColor;
                    thumbJoint4Renderer.material.color = originalColor;
                    
                    indexJoint1Renderer.material.color = grayColor;
                    indexJoint2Renderer.material.color = grayColor;
                    indexJoint3Renderer.material.color = grayColor;
                    indexJoint4Renderer.material.color = originalColor; // Index fingertip remains selectable
                    
                    middleJoint1Renderer.material.color = grayColor;
                    middleJoint2Renderer.material.color = grayColor;
                    middleJoint3Renderer.material.color = grayColor;
                    middleJoint4Renderer.material.color = originalColor; // Middle fingertip remains selectable
                    if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
                    if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
                    if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
                    break;
                    
                case 8: // Index finger
                    thumbJoint1Renderer.material.color = grayColor;
                    thumbJoint2Renderer.material.color = grayColor;
                    thumbJoint3Renderer.material.color = grayColor;
                    thumbJoint4Renderer.material.color = originalColor; // Thumb fingertip remains selectable
                    
                    indexJoint1Renderer.material.color = originalColor;
                    indexJoint2Renderer.material.color = originalColor;
                    indexJoint3Renderer.material.color = originalColor;
                    indexJoint4Renderer.material.color = originalColor;
                    
                    middleJoint1Renderer.material.color = grayColor;
                    middleJoint2Renderer.material.color = grayColor;
                    middleJoint3Renderer.material.color = grayColor;
                    middleJoint4Renderer.material.color = originalColor; // Middle fingertip remains selectable
                    if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
                    if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
                    if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
                    break;
                    
                case 12: // Middle finger
                    thumbJoint1Renderer.material.color = grayColor;
                    thumbJoint2Renderer.material.color = grayColor;
                    thumbJoint3Renderer.material.color = grayColor;
                    thumbJoint4Renderer.material.color = originalColor; // Thumb fingertip remains selectable
                    
                    indexJoint1Renderer.material.color = grayColor;
                    indexJoint2Renderer.material.color = grayColor;
                    indexJoint3Renderer.material.color = grayColor;
                    indexJoint4Renderer.material.color = originalColor; // Index fingertip remains selectable
                    
                    middleJoint1Renderer.material.color = originalColor;
                    middleJoint2Renderer.material.color = originalColor;
                    middleJoint3Renderer.material.color = originalColor;
                    middleJoint4Renderer.material.color = originalColor;
                    if (thumbPaxiniRenderer != null) thumbPaxiniRenderer.material.color = thumbPaxiniOriginalColor;
                    if (indexPaxiniRenderer != null) indexPaxiniRenderer.material.color = indexPaxiniOriginalColor;
                    if (middlePaxiniRenderer != null) middlePaxiniRenderer.material.color = middlePaxiniOriginalColor;
                    break;
            }
        }
    }
}
