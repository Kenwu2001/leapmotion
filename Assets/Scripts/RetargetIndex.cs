using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RetargetIndex : MonoBehaviour
{
    public string[] targetTags = { "L_IndexTipRetarget", "L_ThumbTipRetarget" };
    public Transform handIndexTip;
    public Transform gripperIndexTip;
    public LeapAnchorOffset leapAnchorOffset;
    private int touchCount = 0;

    [Header("Collider Visualization")]
    [Tooltip("Show collider visualization in Scene and Game view")]
    public bool showColliderGizmo = true;
    
    [Tooltip("Color of the collider visualization (default: semi-transparent green)")]
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
    
    [Tooltip("Color when touching targets (default: semi-transparent yellow)")]
    public Color gizmoColorActive = new Color(1f, 1f, 0f, 0.5f);

    // Store touched points and their positions
    private Dictionary<string, Vector3> touchedPoints = new Dictionary<string, Vector3>();

    // Recorded positions when L_IndexTipRetarget first touches
    private Vector3 recordedHandIndexTipPosition;
    private Vector3 recordedGripperIndexTipPosition;
    private Vector3 recordedLeftThumbTipPosition;
    private Vector3 recordedLeftThumbTipLocalPosition; // Local position relative to this collider
    public bool hasRecordedPositions = false;

    private void Update()
    {
        // Continuously update gripperIndexTip position while retargeting is active
        if (hasRecordedPositions && gripperIndexTip != null)
        {
            recordedGripperIndexTipPosition = gripperIndexTip.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchCount++;
                // Add or update the touched point position
                touchedPoints[tag] = other.transform.position;

                // Record positions when L_ThumbTipRetarget enters for the first time
                if (tag == "L_ThumbTipRetarget" && !hasRecordedPositions)
                {
                    // Record the touch point position at the moment of contact
                    recordedLeftThumbTipPosition = other.transform.position;
                    // Record local position so it moves with the collider
                    recordedLeftThumbTipLocalPosition = transform.InverseTransformPoint(other.transform.position);
                    
                    if (handIndexTip != null)
                        recordedHandIndexTipPosition = handIndexTip.position;
                    if (gripperIndexTip != null)
                        recordedGripperIndexTipPosition = gripperIndexTip.position;
                    
                    hasRecordedPositions = true;
                    
                    // Start retargeting ONLY when first recording positions
                    if (leapAnchorOffset != null)
                    {
                        leapAnchorOffset.StartRetargeting();
                    }
                }

                break;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchCount = Mathf.Max(0, touchCount - 1);
                // Remove the touched point
                touchedPoints.Remove(tag);
                
                // Only reset and stop retargeting when no targets are touching anymore
                if (touchCount == 0)
                {
                    hasRecordedPositions = false;
                    
                    // Stop retargeting ONLY when all targets have exited
                    if (leapAnchorOffset != null)
                    {
                        leapAnchorOffset.StopRetargeting();
                    }
                }
                break;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Update position while staying in trigger
        foreach (string tag in targetTags)
        {
            if (other.CompareTag(tag))
            {
                touchedPoints[tag] = other.transform.position;
                break;
            }
        }
    }

    // Public method to get the position of a touched point by tag
    public bool TryGetTouchedPointPosition(string tag, out Vector3 position)
    {
        return touchedPoints.TryGetValue(tag, out position);
    }

    // Public method to get all currently touched points and their positions
    public Dictionary<string, Vector3> GetAllTouchedPoints()
    {
        return new Dictionary<string, Vector3>(touchedPoints);
    }

    // Public methods to get recorded positions
    public Vector3 GetRecordedHandIndexTipPosition()
    {
        return recordedHandIndexTipPosition;
    }

    public Vector3 GetRecordedGripperIndexTipPosition()
    {
        return recordedGripperIndexTipPosition;
    }

    public Vector3 GetRecordedLeftThumbTipPosition()
    {
        return recordedLeftThumbTipPosition;
    }

    // Get the touch point position that moves with the collider
    public Vector3 GetDynamicLeftThumbTipPosition()
    {
        if (!hasRecordedPositions)
            return new Vector3(-1f, -1f, -1f);
        
        // Convert local position back to world space
        return transform.TransformPoint(recordedLeftThumbTipLocalPosition);
    }

    public bool HasRecordedPositions()
    {
        return hasRecordedPositions;
    }

    // Get current gripper transform for real-time tracking
    public Transform GetGripperIndexTip()
    {
        return gripperIndexTip;
    }

    // Reset recorded positions
    public void ResetRecordedPositions()
    {
        hasRecordedPositions = false;
        recordedHandIndexTipPosition = Vector3.zero;
        recordedGripperIndexTipPosition = Vector3.zero;
        recordedLeftThumbTipPosition = Vector3.zero;
        recordedLeftThumbTipLocalPosition = Vector3.zero;
    }

    // Visualize the collider
    private void OnDrawGizmos()
    {
        if (!showColliderGizmo) return;

        // Set color based on whether targets are touching
        Gizmos.color = (touchCount > 0) ? gizmoColorActive : gizmoColor;

        // Draw based on collider type
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        if (col is BoxCollider)
        {
            BoxCollider box = (BoxCollider)col;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.DrawWireCube(box.center, box.size);
        }
        else if (col is SphereCollider)
        {
            SphereCollider sphere = (SphereCollider)col;
            Vector3 worldCenter = transform.TransformPoint(sphere.center);
            float worldRadius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            Gizmos.DrawSphere(worldCenter, worldRadius);
            Gizmos.DrawWireSphere(worldCenter, worldRadius);
        }
        else if (col is CapsuleCollider)
        {
            CapsuleCollider capsule = (CapsuleCollider)col;
            Vector3 worldCenter = transform.TransformPoint(capsule.center);
            float worldRadius = capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
            float worldHeight = capsule.height * transform.lossyScale.y;
            
            // Draw approximate capsule using spheres at top and bottom
            Vector3 offset = Vector3.up * (worldHeight / 2f - worldRadius);
            Gizmos.DrawWireSphere(worldCenter + offset, worldRadius);
            Gizmos.DrawWireSphere(worldCenter - offset, worldRadius);
        }
        else if (col is MeshCollider)
        {
            MeshCollider meshCol = (MeshCollider)col;
            if (meshCol.sharedMesh != null)
            {
                Gizmos.DrawWireMesh(meshCol.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
            }
        }

        // Reset matrix
        Gizmos.matrix = Matrix4x4.identity;
    }
}
