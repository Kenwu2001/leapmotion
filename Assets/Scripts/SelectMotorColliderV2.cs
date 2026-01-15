using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectMotorColliderV2 : MonoBehaviour
{
    [Header("3 Finger Tip Colliders")]
    [Tooltip("Element 0: Thumb Tip Collider, Element 1: Index Tip Collider, Element 2: Middle Tip Collider")]
    public GameObject[] fingerTipColliders = new GameObject[3];
    
    [Header("Target Tag")]
    public string targetTag = "L_IndexTipSmall";
    
    [Header("Debug Info")]
    [Tooltip("Currently touched finger ID (0=none, 1=thumb, 2=index, 3=middle)")]
    public int currentTouchedFingerID = 0;
    
    [Tooltip("Touch count for current finger")]
    public int currentFingerTouchCount = 0;
    
    [Tooltip("Which motor cycle (0-3) for the current finger")]
    public int currentMotorCycle = 0;
    
    [Tooltip("Is any finger currently being touched?")]
    public bool isAnyFingerTouched = false;
    
    [Header("Individual Counters")]
    public int thumbCounter = 0;
    public int indexCounter = 0;
    public int middleCounter = 0;

    // Track which finger is currently active (1=thumb, 2=index, 3=middle, 0=none)
    private int activeFingerID = 0;
    private Vector3 activeTouchPosition = Vector3.zero;
    
    // Individual counters for each finger
    private int[] fingerCounters = new int[3]; // 0=thumb, 1=index, 2=middle
    
    // Child trigger components for each finger collider
    private FingerTriggerDetector[] triggerDetectors = new FingerTriggerDetector[3];

    private void Start()
    {
        // Setup trigger detectors for each finger collider
        for (int i = 0; i < fingerTipColliders.Length; i++)
        {
            if (fingerTipColliders[i] != null)
            {
                // Add or get the detector component
                FingerTriggerDetector detector = fingerTipColliders[i].GetComponent<FingerTriggerDetector>();
                if (detector == null)
                {
                    detector = fingerTipColliders[i].AddComponent<FingerTriggerDetector>();
                }
                
                // Initialize with fingerID 1-3 (i+1)
                detector.Initialize(i + 1, targetTag, this);
                triggerDetectors[i] = detector;
            }
            else
            {
                Debug.LogWarning($"Finger tip collider at index {i} is not assigned!");
            }
        }
    }

    private void Update()
    {
        // Update debug info
        currentTouchedFingerID = activeFingerID;
        
        if (activeFingerID > 0)
        {
            int fingerIndex = activeFingerID - 1;
            currentFingerTouchCount = fingerCounters[fingerIndex];
            // Motor cycle: 0-3 (every 4 touches = 1 cycle)
            currentMotorCycle = fingerCounters[fingerIndex] > 0 ? (fingerCounters[fingerIndex] - 1) / 4 : 0;
        }
        else
        {
            currentFingerTouchCount = 0;
            currentMotorCycle = 0;
        }
        
        isAnyFingerTouched = activeFingerID != 0;
        
        // Update individual counter display
        thumbCounter = fingerCounters[0];
        indexCounter = fingerCounters[1];
        middleCounter = fingerCounters[2];
    }

    // Called by FingerTriggerDetector when a finger is touched
    internal void OnFingerTouched(int fingerID, Vector3 position)
    {
        if (fingerID < 1 || fingerID > 3) return;
        
        int fingerIndex = fingerID - 1;
        
        // 如果切換到不同的手指
        if (activeFingerID != 0 && activeFingerID != fingerID)
        {
            // 強制釋放前一個手指的偵測器
            int prevIndex = activeFingerID - 1;
            if (prevIndex >= 0 && prevIndex < triggerDetectors.Length && triggerDetectors[prevIndex] != null)
            {
                Debug.Log($"手指 {GetFingerName(activeFingerID)} 自動釋放 (新觸碰: {GetFingerName(fingerID)})");
                triggerDetectors[prevIndex].ForceRelease();
            }
            
            // 重置所有手指的計數器
            for (int i = 0; i < fingerCounters.Length; i++)
            {
                fingerCounters[i] = 0;
            }
            
            // 設定新的活躍手指，並從1開始計數
            activeFingerID = fingerID;
            activeTouchPosition = position;
            fingerCounters[fingerIndex] = 1;
            
            Debug.Log($"手指 {GetFingerName(fingerID)} 觸碰！計數: {fingerCounters[fingerIndex]}");
        }
        // 如果是第一次觸碰（沒有活躍手指）
        else if (activeFingerID == 0)
        {
            activeFingerID = fingerID;
            activeTouchPosition = position;
            fingerCounters[fingerIndex] = 1;
            Debug.Log($"手指 {GetFingerName(fingerID)} 觸碰！計數: {fingerCounters[fingerIndex]}");
        }
        // 如果是同一個手指持續觸碰（OnTriggerStay）
        else if (activeFingerID == fingerID)
        {
            // 更新位置，但不增加計數（計數只在 OnTriggerEnter 時增加）
            activeTouchPosition = position;
        }
    }

    // Called by FingerTriggerDetector when a finger is released
    internal void OnFingerReleased(int fingerID)
    {
        if (activeFingerID == fingerID)
        {
            Debug.Log($"手指 {GetFingerName(fingerID)} 釋放！最終計數: {fingerCounters[fingerID - 1]}");
            activeFingerID = 0;
            activeTouchPosition = Vector3.zero;
        }
    }
    
    // Called by FingerTriggerDetector on re-enter (離開後再進入同一個collider)
    internal void OnFingerReEntered(int fingerID, Vector3 position)
    {
        if (fingerID < 1 || fingerID > 3) return;
        
        int fingerIndex = fingerID - 1;
        
        // 只有在這個手指是當前活躍手指時才累加
        if (activeFingerID == fingerID)
        {
            fingerCounters[fingerIndex]++;
            activeTouchPosition = position;
            Debug.Log($"手指 {GetFingerName(fingerID)} 再次進入！計數: {fingerCounters[fingerIndex]}");
        }
    }

    // Helper method to get finger name
    private string GetFingerName(int fingerID)
    {
        switch (fingerID)
        {
            case 1: return "拇指 (Thumb)";
            case 2: return "食指 (Index)";
            case 3: return "中指 (Middle)";
            default: return "未知";
        }
    }

    // ==================== Public Query Methods ====================
    
    public int GetTouchedFingerID()
    {
        return activeFingerID;
    }

    public bool IsTouched()
    {
        return activeFingerID != 0;
    }

    public bool IsFingerTouched(int fingerID)
    {
        return activeFingerID == fingerID;
    }

    public int GetFingerCounter(int fingerID)
    {
        if (fingerID >= 1 && fingerID <= 3)
        {
            return fingerCounters[fingerID - 1];
        }
        return 0;
    }

    public int GetThumbCounter()
    {
        return fingerCounters[0];
    }

    public int GetIndexCounter()
    {
        return fingerCounters[1];
    }

    public int GetMiddleCounter()
    {
        return fingerCounters[2];
    }

    public int GetMotorCycleForFinger(int fingerID)
    {
        if (fingerID >= 1 && fingerID <= 3 && fingerCounters[fingerID - 1] > 0)
        {
            return (fingerCounters[fingerID - 1] - 1) / 4;
        }
        return 0;
    }

    public Vector3 GetTouchPosition()
    {
        return activeTouchPosition;
    }

    public GameObject GetTouchedFingerGameObject()
    {
        if (activeFingerID >= 1 && activeFingerID <= fingerTipColliders.Length)
        {
            return fingerTipColliders[activeFingerID - 1];
        }
        return null;
    }

    // Reset all counters
    public void ResetAllCounters()
    {
        for (int i = 0; i < fingerCounters.Length; i++)
        {
            fingerCounters[i] = 0;
        }
        activeFingerID = 0;
        Debug.Log("所有手指計數器已重置！");
    }

    // Reset specific finger counter
    public void ResetFingerCounter(int fingerID)
    {
        if (fingerID >= 1 && fingerID <= 3)
        {
            fingerCounters[fingerID - 1] = 0;
            Debug.Log($"手指 {GetFingerName(fingerID)} 計數器已重置！");
        }
    }
}

// Helper component attached to each finger collider
internal class FingerTriggerDetector : MonoBehaviour
{
    private int fingerID;
    private string targetTag;
    private SelectMotorColliderV2 manager;
    private bool isActiveFingerDetector = false;
    private HashSet<Collider> touchedColliders = new HashSet<Collider>();
    private bool wasInside = false; // 追蹤是否曾經在內部

    public void Initialize(int id, string tag, SelectMotorColliderV2 managerRef)
    {
        fingerID = id;
        targetTag = tag;
        manager = managerRef;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            touchedColliders.Add(other);
            
            if (touchedColliders.Count == 1 && manager != null)
            {
                isActiveFingerDetector = true;
                
                // 如果之前離開過（wasInside為true），這是重新進入
                if (wasInside)
                {
                    manager.OnFingerReEntered(fingerID, other.transform.position);
                }
                else
                {
                    // 第一次進入
                    manager.OnFingerTouched(fingerID, other.transform.position);
                    wasInside = true;
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(targetTag) && touchedColliders.Count > 0 && isActiveFingerDetector && manager != null)
        {
            // OnTriggerStay 只更新位置，不增加計數
            manager.OnFingerTouched(fingerID, other.transform.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(targetTag))
        {
            touchedColliders.Remove(other);
            
            if (touchedColliders.Count == 0 && isActiveFingerDetector && manager != null)
            {
                isActiveFingerDetector = false;
                manager.OnFingerReleased(fingerID);
                // wasInside 保持為 true，這樣下次進入時會被視為重新進入
            }
        }
    }

    // Force this detector to stop being active (called when another finger takes over)
    public void ForceRelease()
    {
        if (isActiveFingerDetector)
        {
            isActiveFingerDetector = false;
            wasInside = false; // 重置狀態
            // Don't reset touchedColliders - the collider might still be physically touching
            // But we stop reporting to the manager
        }
    }
}