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
    public Color redColor = Color.red;
    
    public bool modeSelect = true;
    public bool motorSelected = false;
    public bool modeManipulate = false;
    
    public int lastTouchedMotorID = 0;
    public int currentRedMotorID = 0; // Currently selected motor (displayed in red)
    private bool hasEnteredCloseRange = false; // Track if we've entered < 0.16f during manipulation
    
    void Start()
    {
        originalColor = thumbJoint1Renderer.material.color;
        modeSelect = true;
        motorSelected = false;
        modeManipulate = false;
        ResetAllColors();
    }

    void Update()
    {
        if (modeSelect && SelectMotorCollider != null)
        {
            int currentMotorID = SelectMotorCollider.currentTouchedMotorID;
            
            // Update renderer color based on touched motor ID
            if (currentMotorID != lastTouchedMotorID)
            {
                if (currentMotorID != 0)
                {
                    // A motor is touched - set it to red and mark as selected
                    SetMotorColor(currentMotorID, redColor);
                    currentRedMotorID = currentMotorID; // Record the red motor
                    if (!motorSelected)
                    {
                        motorSelected = true;
                    }
                }
                // Don't reset colors when motor is released - keep the last selected motor red
                
                lastTouchedMotorID = currentMotorID;
            }
        }
        
        // Transition: Select → Manipulate (distance increases)
        if (modeSelect && motorSelected)
        {
            if (jointAngle.GetLIndexToIndex2Distance() > 0.16f)
            {
                modeSelect = false;
                motorSelected = false;
                modeManipulate = true;
                hasEnteredCloseRange = false; // Reset when entering manipulate mode
            }
        }

        // Mode Manipulate: Track state and exit conditions
        if (modeManipulate)
        {
            baseRenderer.material.color = Color.green; // Indicate Manipulate mode
            
            float distance = jointAngle.GetLIndexToIndex2Distance();
            
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
                currentRedMotorID = 0; // Clear the red motor record
                lastTouchedMotorID = 0;
                hasEnteredCloseRange = false;
                baseRenderer.material.color = originalColor; // Reset base color
            }
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
}
