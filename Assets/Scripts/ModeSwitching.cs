using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModeSwitching : MonoBehaviour
{
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

    public Renderer baseRenderer;

    private Color originalColor;
    public Color lightRedColor = new Color(1f, 0.5f, 0.5f, 1f); // Light red (temporary touch)
    public Color darkRedColor = Color.red; // Dark red (confirmed selection)
    
    [Header("Selection Timing")]
    [Tooltip("How many seconds to confirm selection (turn dark red)")]
    public float confirmationTime = 0.5f; // Confirm after exceeding this time
    
    public bool modeSelect = true;
    public bool motorSelected = false;
    public bool modeManipulate = false;
    
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
        originalColor = thumbJoint1Renderer.material.color;
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
        if (modeSelect && SelectMotorCollider != null)
        {
            int currentMotorID = SelectMotorCollider.currentTouchedMotorID;

            // Motor switched
            if (currentMotorID != lastTouchedMotorID)
            {
                if (currentMotorID != 0)
                {
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
                        HandleFingertipFirstConfirmation(currentMotorID);
                    }
                    else
                    {
                        confirmedMotorID = currentMotorID; // Original logic
                    }

                    UpdateMotorColors();
                }
            }
        }

        // Transition: Select → Manipulate (distance increases)
        // Only confirmed motor (dark red) can enter Manipulate mode
        // Fingertip priority mode: Must be in MotorConfirmed phase
        bool canEnterManipulate = modeSelect && motorSelected && confirmedMotorID != 0;
        if (useFingertipFirst)
        {
            canEnterManipulate = canEnterManipulate && currentPhase == SelectionPhase.MotorConfirmed;
        }

        if (canEnterManipulate)
        {
            float distance = jointAngle.GetLIndexToIndex2Distance();

            if (distance > 0.16f)
            {
                modeSelect = false;
                motorSelected = false;
                modeManipulate = true;
                hasEnteredCloseRange = false; // Reset when entering manipulate mode
                hasSetManipulateColors = false; // Reset color flag
            }
        }

        // Mode Manipulate: Track state and exit conditions
        if (modeManipulate)
        {
            // baseRenderer.material.color = Color.green; // Indicate Manipulate mode

            float distance = jointAngle.GetLIndexToIndex2Distance();

            // Set colors only once when entering manipulate mode
            if (!hasSetManipulateColors)
            {
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

                // Reset fingertip priority mode state
                if (useFingertipFirst)
                {
                    currentPhase = SelectionPhase.SelectingFingertip;
                    confirmedFingertipID = 0;
                    SelectMotorCollider.ResetFingertipConfirmation();
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
                    confirmedMotorID = motorID; // Temporarily record as the confirmed motor
                    
                    // Notify SelectMotorCollider that a fingertip has been confirmed
                    SelectMotorCollider.OnFingertipConfirmed(motorID);
                    
                    // Enter phase 2
                    currentPhase = SelectionPhase.SelectingMotor;
                    
                    // Update gray display (other motors of this finger are now selectable)
                    UpdateGrayColors();
                    
                    Debug.Log($"[ModeSwitching] Fingertip motor {motorID} confirmed. Entering motor selection phase.");
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
                        
                        // Notify SelectMotorCollider that the fingertip switched
                        SelectMotorCollider.OnFingertipConfirmed(motorID);
                        
                        // Return to phase 2 (selecting motors of that finger)
                        currentPhase = SelectionPhase.SelectingMotor;
                        
                        // Update gray display
                        UpdateGrayColors();
                        
                        Debug.Log($"[ModeSwitching] Switched to new fingertip motor {motorID}!");
                    }
                    else
                    {
                        // Still the same fingertip
                        confirmedMotorID = motorID;
                        currentPhase = SelectionPhase.MotorConfirmed;
                        Debug.Log($"[ModeSwitching] Maintaining fingertip motor {motorID}");
                    }
                }
                else
                {
                    // Selecting another motor of the confirmed finger
                    confirmedMotorID = motorID;
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
                    break;
            }
        }
    }
}
