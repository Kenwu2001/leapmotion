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
    
    // 當前選中的索引（初始為 IndexAngle4Center: row 3, col 1）
    private int currentRow = 3;
    private int currentCol = 1;
    
    // 陣列大小
    private const int ROWS = 4;
    private const int COLS = 3;
    
    // 保存原始顏色和當前選中的物體
    private Dictionary<Renderer, Color> originalColors = new Dictionary<Renderer, Color>();
    private Renderer currentSelectedRenderer;

    public Color purpleColor = new Color(0.5f, 0f, 0.5f);
    private float rotationSpeed = 10f;
    
    // 储存每个关节的初始旋转
    private Quaternion[,] initialRotations = new Quaternion[4, 3];
    
    // 储存每个关节的当前角度
    private float[,] currentRotations = new float[4, 3];
        
    // Start is called before the first frame update
    void Start()
    {
        // 初始化陣列
        motorArray = new Transform[ROWS, COLS] {
            {ThumbAngle1Center, IndexAngle1Center, MiddleAngle1Center},
            {ThumbAngle2Center, IndexAngle2Center, MiddleAngle2Center},
            {ThumbAngle3Center, IndexAngle3Center, MiddleAngle3Center},
            {ThumbAngle4Center, IndexAngle4Center, MiddleAngle4Center}
        };
        
        // 初始化 Renderer 陣列
        rendererArray = new Renderer[ROWS, COLS] {
            {thumbJoint1Renderer, indexJoint1Renderer, middleJoint1Renderer},
            {thumbJoint2Renderer, indexJoint2Renderer, middleJoint2Renderer},
            {thumbJointˇRenderer, indexJoint3Renderer, middleJoint3Renderer},
            {thumbJoint4Renderer, indexJoint4Renderer, middleJoint4Renderer}
        };
        
        // 保存所有物體的原始顏色
        SaveOriginalColors();
        
        // 初始化初始旋转
        InitializeRotations();
        
        // 初始位置設置為 IndexAngle4Center
        UpdateSelection();
    }
    
    void InitializeRotations()
    {
        // 保存每个关节的初始旋转
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
        // 恢復上一個選中物體的顏色
        if (currentSelectedRenderer != null && originalColors.ContainsKey(currentSelectedRenderer))
        {
            currentSelectedRenderer.material.color = originalColors[currentSelectedRenderer];
        }
        
        // 設置新選中的物體
        currentSelectedRenderer = rendererArray[currentRow, currentCol];
        if (currentSelectedRenderer != null)
        {
            currentSelectedRenderer.material.color = Color.green;
            Transform selectedTransform = motorArray[currentRow, currentCol];
            Debug.Log($"選中: Row {currentRow}, Col {currentCol} - {selectedTransform.name}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // WASD 導航控制
        HandleNavigation();
        
        // QE 旋轉控制
        HandleRotation();
        
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
        
        // W - 上（往下一行，循環）
        if (Input.GetKeyDown(KeyCode.W))
        {
            currentRow = (currentRow + 1) % ROWS;
            moved = true;
        }
        // S - 下（往上一行，循環）
        else if (Input.GetKeyDown(KeyCode.S))
        {
            currentRow = (currentRow - 1 + ROWS) % ROWS;
            moved = true;
        }
        // A - 左（往右邊列，循環）
        else if (Input.GetKeyDown(KeyCode.A))
        {
            currentCol = (currentCol + 1) % COLS;
            moved = true;
        }
        // D - 右（往左邊列，循環）
        else if (Input.GetKeyDown(KeyCode.D))
        {
            currentCol = (currentCol - 1 + COLS) % COLS;
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
        
        // Q 键 - 减少角度
        if (Input.GetKey(KeyCode.Q))
        {
            currentRotations[currentRow, currentCol] -= rotationDelta;
            rotationChanged = true;
        }
        
        // E 键 - 增加角度
        if (Input.GetKey(KeyCode.E))
        {
            currentRotations[currentRow, currentCol] += rotationDelta;
            rotationChanged = true;
        }
        
        if (rotationChanged)
        {
            // 限制角度范围为 -60 到 60 度
            currentRotations[currentRow, currentCol] = 
                Mathf.Clamp(currentRotations[currentRow, currentCol], -60f, 60f);
            
            // 应用旋转：初始旋转 * 当前角度变化
            Quaternion initialRotation = initialRotations[currentRow, currentCol];
            float currentAngle = currentRotations[currentRow, currentCol];
            Quaternion deltaRotation;
            
            // 根据 row 决定旋转轴
            if (currentRow == 0) // Row 0: ThumbAngle1, IndexAngle1, MiddleAngle1 - Y 轴
            {
                deltaRotation = Quaternion.Euler(0f, currentAngle, 0f);
            }
            else if (currentRow == 1) // Row 1: ThumbAngle2, IndexAngle2, MiddleAngle2 - Z 轴
            {
                deltaRotation = Quaternion.Euler(0f, 0f, currentAngle);
            }
            else // Row 2 & 3: Angle3, Angle4 - X 轴
            {
                deltaRotation = Quaternion.Euler(currentAngle, 0f, 0f);
            }
            
            currentTransform.localRotation = initialRotation * deltaRotation;
            
            // Debug 输出
            string axisName = currentRow == 0 ? "Y" : (currentRow == 1 ? "Z" : "X");
            // Debug.Log($"Row {currentRow}, Col {currentCol} - {axisName} 轴: {currentAngle:F2}°");
        }
    }
}
