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
            
            // 馬達切換了
            if (currentMotorID != lastTouchedMotorID)
            {
                if (currentMotorID != 0)
                {
                    // 新馬達被觸碰
                    touchStartTime = Time.time; // 記錄開始觸碰時間
                    isConfirmed = false; // 新觸碰還未確認
                    currentRedMotorID = currentMotorID;
                    
                    // 更新顏色顯示
                    UpdateMotorColors();
                    
                    if (!motorSelected)
                    {
                        motorSelected = true;
                    }
                }
                else
                {
                    // 離開所有馬達 - 只保留深紅色的確認選擇
                    currentRedMotorID = 0;
                    UpdateMotorColors();
                }
                
                lastTouchedMotorID = currentMotorID;
            }
            else if (currentMotorID != 0)
            {
                // 持續觸碰同一個馬達 - 檢查是否超過確認時間
                if (!isConfirmed && (Time.time - touchStartTime) >= confirmationTime)
                {
                    // 超過確認時間，變成深紅色（確認選擇）
                    isConfirmed = true;
                    confirmedMotorID = currentMotorID; // 記錄確認選擇的馬達
                    UpdateMotorColors();
                }
            }
        }
        
        // Transition: Select → Manipulate (distance increases)
        // 只有確認選擇了馬達（深紅色）才能進入 Manipulate 模式
        if (modeSelect && motorSelected && confirmedMotorID != 0)
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
            }
        }
    }
    
    /// <summary>
    /// 更新馬達顏色：
    /// - 確認選擇的馬達（confirmedMotorID）顯示深紅色
    /// - 當前觸碰但未確認的馬達顯示淺紅色
    /// </summary>
    private void UpdateMotorColors()
    {
        // 先重置所有顏色
        ResetAllColors();
        
        // 如果有確認選擇的馬達，顯示深紅色
        if (confirmedMotorID != 0)
        {
            SetMotorColorDirect(confirmedMotorID, darkRedColor);
        }
        
        // 如果當前觸碰的馬達不是確認選擇的馬達，顯示淺紅色
        if (currentRedMotorID != 0 && currentRedMotorID != confirmedMotorID)
        {
            if (isConfirmed)
            {
                // 如果當前觸碰的已經確認了，用深紅色
                SetMotorColorDirect(currentRedMotorID, darkRedColor);
            }
            else
            {
                // 還沒確認，用淺紅色
                SetMotorColorDirect(currentRedMotorID, lightRedColor);
            }
        }
    }
    
    /// <summary>
    /// 直接設定單個馬達的顏色（不重置其他顏色）
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
}
