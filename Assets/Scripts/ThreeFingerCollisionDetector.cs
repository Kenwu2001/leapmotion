using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreeFingerCollisionDetector : MonoBehaviour
{
    [Header("6 Joint Colliders (2 per finger)")]
    public GameObject[] jointColliders = new GameObject[6];

    [Header("Finger Grouping (0=Thumb, 1=Index, 2=Middle)")]
    [Tooltip("為每個 collider 指定所屬手指：0=拇指, 1=食指, 2=中指")]
    public int[] fingerGroups = new int[6] { 0, 0, 1, 1, 2, 2 };

    [Header("Collision Setup")]
    public float colliderRadius = 0.03f; // 3cm sphere collider radius
    public bool isColliding { get; private set; } = false;

    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color collisionColor = Color.red;

    [Header("Control Reference")]
    public ClawModuleController clawController;

    // Track colliding pairs
    private HashSet<string> currentCollisions = new HashSet<string>();
    private Dictionary<GameObject, int> colliderIndexMap = new Dictionary<GameObject, int>();
    private Dictionary<GameObject, Renderer> colliderRenderers = new Dictionary<GameObject, Renderer>();
    
    // Store original motor states
    private bool wasColliding = false;
    private bool originalMappingState;
    private float storedThumbRotationY, storedThumbRotationZ;
    private float storedIndexRotationY, storedIndexRotationZ;
    private float storedMiddleRotationY, storedMiddleRotationZ;

    void Start()
    {
        // Auto-find ClawModuleController if not assigned
        if (clawController == null)
            clawController = FindObjectOfType<ClawModuleController>();

        // Setup colliders
        SetupColliders();
    }

    void SetupColliders()
    {
        for (int i = 0; i < jointColliders.Length; i++)
        {
            if (jointColliders[i] == null)
            {
                Debug.LogError($"❌ Joint Collider {i} is not assigned!");
                continue;
            }

            // Add or get SphereCollider
            SphereCollider sphereCol = jointColliders[i].GetComponent<SphereCollider>();
            if (sphereCol == null)
            {
                sphereCol = jointColliders[i].AddComponent<SphereCollider>();
            }
            sphereCol.radius = colliderRadius;
            sphereCol.isTrigger = true;

            // Add or get Rigidbody (kinematic)
            Rigidbody rb = jointColliders[i].GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = jointColliders[i].AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;

            // Add FingerJointCollider component
            FingerJointCollider fjc = jointColliders[i].GetComponent<FingerJointCollider>();
            if (fjc == null)
            {
                fjc = jointColliders[i].AddComponent<FingerJointCollider>();
            }
            fjc.fingerIndex = fingerGroups[i];
            fjc.jointIndex = i;
            fjc.detector = this;

            // Store index mapping
            colliderIndexMap[jointColliders[i]] = i;

            // Store renderer for visual feedback
            Renderer renderer = jointColliders[i].GetComponent<Renderer>();
            if (renderer != null)
            {
                colliderRenderers[jointColliders[i]] = renderer;
                renderer.material.color = normalColor;
            }
        }

        Debug.Log($"✅ Setup {jointColliders.Length} joint colliders with trigger detection");
    }

    void Update()
    {
        // Update collision state
        bool hasCollision = currentCollisions.Count > 0;
        isColliding = hasCollision;

        // Update visual feedback
        UpdateVisuals();

        if (clawController == null)
            return;

        // Handle collision state changes
        if (isColliding && !wasColliding)
        {
            StopMotors();
            wasColliding = true;
            Debug.Log("🛑 Collision START - Motors stopped");
        }
        else if (!isColliding && wasColliding)
        {
            ResumeMotors();
            wasColliding = false;
            Debug.Log("✅ Collision END - Motors resumed");
        }
        else if (isColliding)
        {
            KeepMotorsStopped();
        }
    }

    public void OnJointCollisionEnter(int index1, int index2)
    {
        // Skip if same finger
        if (fingerGroups[index1] == fingerGroups[index2])
            return;

        string key = GetCollisionKey(index1, index2);
        currentCollisions.Add(key);

        Debug.Log($"⚠️ Collision: Joint{index1} (Finger{fingerGroups[index1]}) <-> Joint{index2} (Finger{fingerGroups[index2]})");
    }

    public void OnJointCollisionExit(int index1, int index2)
    {
        string key = GetCollisionKey(index1, index2);
        currentCollisions.Remove(key);

        Debug.Log($"✅ Separation: Joint{index1} <-> Joint{index2}");
    }

    string GetCollisionKey(int i, int j)
    {
        // Always use smaller index first for consistency
        return i < j ? $"{i}-{j}" : $"{j}-{i}";
    }

    void UpdateVisuals()
    {
        // Reset all to normal
        foreach (var kvp in colliderRenderers)
        {
            kvp.Value.material.color = normalColor;
        }

        // Highlight colliding joints
        foreach (string collision in currentCollisions)
        {
            string[] indices = collision.Split('-');
            int i = int.Parse(indices[0]);
            int j = int.Parse(indices[1]);

            if (colliderRenderers.ContainsKey(jointColliders[i]))
                colliderRenderers[jointColliders[i]].material.color = collisionColor;
            
            if (colliderRenderers.ContainsKey(jointColliders[j]))
                colliderRenderers[jointColliders[j]].material.color = collisionColor;
        }
    }

    void StopMotors()
    {
        originalMappingState = clawController.isMapping;

        storedThumbRotationY = clawController.currentThumbRotationY;
        storedThumbRotationZ = clawController.currentThumbRotationZ;
        storedIndexRotationY = clawController.currentIndexRotationY;
        storedIndexRotationZ = clawController.currentIndexRotationZ;
        storedMiddleRotationY = clawController.currentMiddleRotationY;
        storedMiddleRotationZ = clawController.currentMiddleRotationZ;

        clawController.isMapping = false;
    }

    void KeepMotorsStopped()
    {
        clawController.isMapping = false;
        clawController.currentThumbRotationY = storedThumbRotationY;
        clawController.currentThumbRotationZ = storedThumbRotationZ;
        clawController.currentIndexRotationY = storedIndexRotationY;
        clawController.currentIndexRotationZ = storedIndexRotationZ;
        clawController.currentMiddleRotationY = storedMiddleRotationY;
        clawController.currentMiddleRotationZ = storedMiddleRotationZ;
    }

    void ResumeMotors()
    {
        clawController.isMapping = originalMappingState;
    }

    void OnDrawGizmos()
    {
        if (jointColliders == null) return;

        for (int i = 0; i < jointColliders.Length; i++)
        {
            if (jointColliders[i] != null)
            {
                Gizmos.color = isColliding ? Color.red : Color.cyan;
                Gizmos.DrawWireSphere(jointColliders[i].transform.position, colliderRadius);
            }
        }
    }
}

// Helper component for individual colliders
public class FingerJointCollider : MonoBehaviour
{
    public int fingerIndex; // 0=Thumb, 1=Index, 2=Middle
    public int jointIndex;  // 0-5
    public ThreeFingerCollisionDetector detector;

    void OnTriggerEnter(Collider other)
    {
        FingerJointCollider otherJoint = other.GetComponent<FingerJointCollider>();
        if (otherJoint != null && otherJoint.detector == detector)
        {
            detector.OnJointCollisionEnter(jointIndex, otherJoint.jointIndex);
        }
    }

    void OnTriggerExit(Collider other)
    {
        FingerJointCollider otherJoint = other.GetComponent<FingerJointCollider>();
        if (otherJoint != null && otherJoint.detector == detector)
        {
            detector.OnJointCollisionExit(jointIndex, otherJoint.jointIndex);
        }
    }
}
