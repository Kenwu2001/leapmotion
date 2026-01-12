using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeltaUserStudy : MonoBehaviour
{
    public JointAngle jointAngle;

    public Transform ThumbAngle1Center;
    public Transform ThumbAngle2Center;
    public Transform ThumbAngle3Center;
    public Transform ThumbAngle4Center;
    public Renderer thumbJoint1Renderer;
    public Renderer thumbJoint2Renderer;
    public Renderer thumbJointˇRenderer;
    public Renderer thumbJoint4Renderer;

    public Transform IndexAngle1Center;
    public Transform IndexAngle2Center;
    public Transform IndexAngle3Center;
    public Transform IndexAngle4Center;
    public Renderer indexJoint1Renderer;
    public Renderer indexJoint2Renderer;
    public Renderer indexJoint3Renderer;
    public Renderer indexJoint4Renderer;

    public Transform MiddleAngle1Center;
    public Transform MiddleAngle2Center;
    public Transform MiddleAngle3Center;
    public Transform MiddleAngle4Center;
    public Renderer middleJoint1Renderer;
    public Renderer middleJoint2Renderer;
    public Renderer middleJoint3Renderer;
    public Renderer middleJoint4Renderer;

    Transform[,] motorArray;
    Renderer[,] rendererArray;
    
    // Currently selected index (initial: IndexAngle4Center: row 3, col 1)
    private int currentRow = 3;
    private int currentCol = 1;
    
    // Array size
    private const int ROWS = 4;
    private const int COLS = 3;
    
    // Save original colors and currently selected object
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private Renderer currentSelectedRenderer;

    public Color purpleColor = new Color(0.5f, 0f, 0.5f);
    private float rotationSpeed = 20f;
    
    // Store initial rotation of each joint
    private Quaternion[,] initialRotations = new Quaternion[4, 3];
    
    // Store current angle of each joint
    private float[,] currentRotations = new float[4, 3];
        
    // Start is called before the first frame update
    void Start()
    {
        // Initialize array
        motorArray = new Transform[ROWS, COLS] {
            {ThumbAngle1Center, IndexAngle1Center, MiddleAngle1Center},
            {ThumbAngle2Center, IndexAngle2Center, MiddleAngle2Center},
            {ThumbAngle3Center, IndexAngle3Center, MiddleAngle3Center},
            {ThumbAngle4Center, IndexAngle4Center, MiddleAngle4Center}
        };
        
        // Initialize Renderer array
        rendererArray = new Renderer[ROWS, COLS] {
            {thumbJoint1Renderer, indexJoint1Renderer, middleJoint1Renderer},
            {thumbJoint2Renderer, indexJoint2Renderer, middleJoint2Renderer},
            {thumbJointˇRenderer, indexJoint3Renderer, middleJoint3Renderer},
            {thumbJoint4Renderer, indexJoint4Renderer, middleJoint4Renderer}
        };
        
        // Save original colors of all objects
        SaveOriginalColors();
        
        // Initialize initial rotations
        InitializeRotations();
        
        // Set initial position to IndexAngle4Center
        UpdateSelection();
    }
    
    void InitializeRotations()
    {
        // Save initial rotation of each joint
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                Transform t = motorArray[row, col];
                if (t != null)
                {
                    initialRotations[row, col] = t.localRotation;
                    currentRotations[row, col] = 0f;
                }
            }
        }
    }
    
    void SaveOriginalColors()
    {
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                Renderer renderer = rendererArray[row, col];
                if (renderer != null && renderer.material != null)
                {
                    originalColors[renderer] = renderer.material.color;
                }
            }
        }
    }
    
    void UpdateSelection()
    {
        // Restore color of previously selected object
        if (currentSelectedRenderer != null && originalColors.ContainsKey(currentSelectedRenderer))
        {
            currentSelectedRenderer.material.color = originalColors[currentSelectedRenderer];
        }
        
        // Set new selected object
        currentSelectedRenderer = rendererArray[currentRow, currentCol];
        if (currentSelectedRenderer != null)
        {
            // If the selected is one of the IndexAngle1-4, use red, else green
            Transform selectedTransform = motorArray[currentRow, currentCol];
            if (selectedTransform == IndexAngle1Center || selectedTransform == IndexAngle2Center || selectedTransform == IndexAngle3Center || selectedTransform == IndexAngle4Center)
                currentSelectedRenderer.material.color = Color.red;
            else
                currentSelectedRenderer.material.color = Color.green;
            // Debug.Log($"Selected: Row {currentRow}, Col {currentCol} - {selectedTransform.name}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // WASD navigation control
        HandleNavigation();
        
        // QE rotation control
        HandleRotation();
        
        // R key - Set all Angle3 joints (row 2) to 89 degrees
        if (Input.GetKeyDown(KeyCode.J))
        {
            SetRow2To89Degrees();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            ResetAll();
        }
        
        // if (ThumbAngle3Center != null)
        //     ThumbAngle3Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f);

        // if (ThumbAngle4Center != null)
        //     ThumbAngle4Center.localRotation = Quaternion.Euler(jointAngle.thumbAngle1 + 10, 0f, 0f);

        // if (IndexAngle3Center != null)
        //     IndexAngle3Center.localRotation = Quaternion.Euler(jointAngle.indexAngle1, 0f, 0f);

        // if (IndexAngle4Center != null)
        //     IndexAngle4Center.localRotation = Quaternion.Euler(jointAngle.indexAngle2, 0f, 0f);

        // if (MiddleAngle3Center != null)
        //     MiddleAngle3Center.localRotation = Quaternion.Euler(jointAngle.middleAngle1, 0f, 0f);

        // if (MiddleAngle4Center != null)
        //     MiddleAngle4Center.localRotation = Quaternion.Euler(jointAngle.middleAngle2, 0f, 0f);
    }
    
    void HandleNavigation()
    {
        bool moved = false;
        
        // W - Up (go down one row, wrapping)
        if (Input.GetKeyDown(KeyCode.W))
        {
            currentRow = (currentRow + 1) % ROWS;
            moved = true;
        }
        // S - Down (go up one row, wrapping)
        else if (Input.GetKeyDown(KeyCode.S))
        {
            currentRow = (currentRow - 1 + ROWS) % ROWS;
            moved = true;
        }
        // A - Cycle: Index -> Thumb -> Middle -> Index
        else if (Input.GetKeyDown(KeyCode.A))
        {
            currentCol = (currentCol - 1 + COLS) % COLS;
            moved = true;
        }
        // D - Cycle: Index -> Middle -> Thumb -> Index
        else if (Input.GetKeyDown(KeyCode.D))
        {
            currentCol = (currentCol + 1) % COLS;
            moved = true;
        }
        
        if (moved)
        {
            UpdateSelection();
        }
    }
    
    void HandleRotation()
    {
        Transform currentTransform = motorArray[currentRow, currentCol];
        if (currentTransform == null) return;
        
        float rotationDelta = rotationSpeed * Time.deltaTime;
        bool rotationChanged = false;
        
        // Q key - decrease angle
        if (Input.GetKey(KeyCode.Q))
        {
            currentRotations[currentRow, currentCol] -= rotationDelta;
            rotationChanged = true;
        }
        
        // E key - increase angle
        if (Input.GetKey(KeyCode.E))
        {
            currentRotations[currentRow, currentCol] += rotationDelta;
            rotationChanged = true;
        }
        
        if (rotationChanged)
        {
            // Limit angle range to -60 to 60 degrees
            currentRotations[currentRow, currentCol] = 
                Mathf.Clamp(currentRotations[currentRow, currentCol], -89f, 89f);
            
            // Apply rotation: initial rotation * current angle change
            Quaternion initialRotation = initialRotations[currentRow, currentCol];
            float currentAngle = currentRotations[currentRow, currentCol];
            Quaternion deltaRotation;
            
            // Determine rotation axis based on row
            if (currentRow == 0) // Row 0: ThumbAngle1, IndexAngle1, MiddleAngle1 - Y axis
            {
                deltaRotation = Quaternion.Euler(0f, currentAngle, 0f);
            }
            else if (currentRow == 1) // Row 1: ThumbAngle2, IndexAngle2, MiddleAngle2 - Z axis
            {
                deltaRotation = Quaternion.Euler(0f, 0f, currentAngle);
            }
            else // Row 2 & 3: Angle3, Angle4 - X axis
            {
                deltaRotation = Quaternion.Euler(currentAngle, 0f, 0f);
            }
            
            currentTransform.localRotation = initialRotation * deltaRotation;
            
            // Debug output
            string axisName = currentRow == 0 ? "Y" : (currentRow == 1 ? "Z" : "X");
            // Debug.Log($"Row {currentRow}, Col {currentCol} - {axisName} axis: {currentAngle:F2}°");
        }
    }
    
    void SetRow2To89Degrees()
    {
        // Set all joints in row 2 (ThumbAngle3Center, IndexAngle3Center, MiddleAngle3Center) to 89 degrees
        int targetRow = 2;
        float targetAngle = 75f;
        
        for (int col = 0; col < COLS; col++)
        {
            Transform t = motorArray[targetRow, col];
            if (t != null)
            {
                currentRotations[targetRow, col] = targetAngle;
                Quaternion initialRotation = initialRotations[targetRow, col];
                Quaternion deltaRotation = Quaternion.Euler(targetAngle, 0f, 0f); // Row 2 uses X axis
                t.localRotation = initialRotation * deltaRotation;
            }
        }
    }

    void ResetAll()
    {
        // Reset all joints to initial rotations
        for (int row = 0; row < ROWS; row++)
        {
            for (int col = 0; col < COLS; col++)
            {
                Transform t = motorArray[row, col];
                if (t != null)
                {
                    currentRotations[row, col] = 0f;
                    t.localRotation = initialRotations[row, col];
                }
            }
        }
    }
}
