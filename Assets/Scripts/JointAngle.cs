using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class JointAngle : MonoBehaviour
{
    public Dictionary<string, Transform> joints = new Dictionary<string, Transform>();
    public float thumbAngle0, thumbAngle1, thumbLRAngle;
    public float indexAngle0, indexAngle1, indexAngle2, indexLRAngle;
    public float middleAngle0, middleAngle1, middleAngle2, middleLRAngle;

    private Vector3 palmNormal;
    private Vector3 thumbPlaneNormal;

    public float indexMiddleDistance;
    public float isClockWise;

    // Reference to TriggerRightIndexTip
    public TriggerRightIndexTip triggerRightIndexTip;

    public Vector3 indexTipPos;
    public Vector3 thumbTipPos;

    private LineRenderer lineRenderer;


    // some scripts

    void Start()
    {
        // Thumb joints
        joints["Thumb0"] = GameObject.Find("R_thumb_a").transform;
        joints["Thumb1"] = GameObject.Find("R_thumb_b").transform;
        // joints["ThumbM"] = GameObject.Find("R_thumb_Proximal").transform;

        // Index joints
        joints["Index0"] = GameObject.Find("R_index_Proximal").transform;
        joints["Index1"] = GameObject.Find("R_index_b").transform;
        joints["Index2"] = GameObject.Find("R_index_c").transform;
        // joints["IndexM"] = GameObject.Find("R_index_meta").transform;

        // Middle joints
        joints["Middle0"] = GameObject.Find("R_middle_Proximal").transform;
        joints["Middle1"] = GameObject.Find("R_middle_b").transform;
        joints["Middle2"] = GameObject.Find("R_middle_c").transform;
        // joints["MiddleM"] = GameObject.Find("R_middle_meta").transform;

        // Points needed for forming the basic plane of the palm
        joints["Wrist"] = GameObject.Find("R_Wrist").transform;
        joints["PalmIndex"] = GameObject.Find("R_index_Proximal").transform;
        joints["PalmRing"] = GameObject.Find("R_ring_Proximal").transform;

        thumbAngle0 = 0f;
        thumbAngle1 = 0f;
        indexAngle0 = 0f;
        indexAngle1 = 0f;
        indexAngle2 = 0f;
        middleAngle0 = 0f;
        middleAngle1 = 0f;
        middleAngle2 = 0f;
        thumbLRAngle = 0f;
        indexLRAngle = 0f;
        middleLRAngle = 0f;

        indexMiddleDistance = 0f;

        // Optionally auto-find if not assigned in Inspector
        if (triggerRightIndexTip == null)
            triggerRightIndexTip = FindObjectOfType<TriggerRightIndexTip>();

        // Create LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.positionCount = 2;
    }

    void Update()
    {
        UpdatePalmNormal();

        // UpdateThumbPlane();

        // thumbAngle0 = GetThumbAngle("Thumb0");
        thumbAngle0 = joints["Thumb0"].localEulerAngles.z < 100 ? 0 : 360 - joints["Thumb0"].localEulerAngles.z;
        thumbAngle1 = GetJointAngle("Thumb1", "Thumb0");
        // thumbLRAngle = GetRotateAngle("ThumbM", "Thumb0", "Thumb1");

        indexAngle0 = GetJointPalmAngle("Index0");
        indexAngle1 = GetJointAngle("Index1", "Index0");
        indexAngle2 = GetJointAngle("Index2", "Index1");
        // indexLRAngle = GetRotateAngle("IndexM", "Index0", "Index1");

        middleAngle0 = GetJointPalmAngle("Middle0");
        middleAngle1 = GetJointAngle("Middle1", "Middle0");
        middleAngle2 = GetJointAngle("Middle2", "Middle1");
        // middleLRAngle = GetRotateAngle("MiddleM", "Middle0", "Middle1");
        indexMiddleDistance = GetProjectedDistanceOnPalm("Index1", "Middle1") * 100f;

        // Fetch touched points from TriggerRightIndexTip
        if (triggerRightIndexTip != null)
        {
            Dictionary<string, Vector3> touchedPoints = triggerRightIndexTip.GetAllTouchedPoints();
            // Example: access positions by tag
            if (touchedPoints.ContainsKey("L_IndexTip") && touchedPoints.ContainsKey("L_ThumbTip"))
            {
                indexTipPos = touchedPoints["L_IndexTip"];
                thumbTipPos = touchedPoints["L_ThumbTip"];
                // Use indexTipPos and thumbTipPos as needed
                // Debug.DrawLine(indexTipPos, thumbTipPos, Color.red);
                lineRenderer.SetPosition(0, indexTipPos);
                lineRenderer.SetPosition(1, thumbTipPos);
                lineRenderer.enabled = true;
            }
            else
            {
                lineRenderer.enabled = false;
            }
        }

        // OnDrawGizmos();

        Debug.Log("indexTipPos: " + indexTipPos.ToString("F4") + ", thumbTipPos: " + thumbTipPos.ToString("F4"));
    }

    // Compute the palm’s normal from (Wrist, PalmIndex, PalmRing) points.
    void UpdatePalmNormal()
    {
        Vector3 p0 = joints["Wrist"].position;
        Vector3 p1 = joints["PalmIndex"].position;
        Vector3 p2 = joints["PalmRing"].position;

        Vector3 v1 = (p1 - p0).normalized;
        Vector3 v2 = (p2 - p0).normalized;

        palmNormal = Vector3.Cross(v1, v2).normalized;
    }

    float GetJointPalmAngle(string targetJoint)
    {
        if (!joints.ContainsKey(targetJoint))
            return 0f;

        Vector3 jointForwardVector = joints[targetJoint].right;

        Vector3 projectedForward = Vector3.ProjectOnPlane(jointForwardVector, palmNormal).normalized;

        float angle = Vector3.Angle(jointForwardVector, projectedForward);
        return angle;
    }

    float GetJointAngle(string targetJoint, string parentJoint)
    {
        // Calculate the angle between the target joint and its parent joint
        if (!joints.ContainsKey(targetJoint) || !joints.ContainsKey(parentJoint))
            return 0f;

        Vector3 targetDirection = joints[targetJoint].right;
        Vector3 parentDirection = joints[parentJoint].right;

        float angle = Vector3.Angle(targetDirection, parentDirection);

        // Ensure the angle is always positive
        return Mathf.Abs(angle);
    }

    float GetRotateAngle(string basicPoint, string middlePoint, string targetPoint)
    {
        Vector3 basicVector = joints[middlePoint].position - joints[basicPoint].position;
        Vector3 targetVector = joints[targetPoint].position - joints[middlePoint].position;

        if (basicVector == Vector3.zero || targetVector == Vector3.zero)
            return 0f;

        // project onto the palm normal plane and calculate the angle
        Vector3 projectedBasic = Vector3.ProjectOnPlane(basicVector, palmNormal).normalized;
        Vector3 projectedTarget = Vector3.ProjectOnPlane(targetVector, palmNormal).normalized;
        float angle = Vector3.Angle(projectedBasic, projectedTarget);

        return angle;
    }

    // void UpdateThumbPlane()
    // {
    //     Vector3 p0 = joints["ThumbM"].position;
    //     Vector3 p1 = joints["Thumb0"].position;
    //     Vector3 p2 = joints["Index0"].position;

    //     Vector3 v1 = (p1 - p0).normalized;
    //     Vector3 v2 = (p2 - p0).normalized;

    //     thumbPlaneNormal = Vector3.Cross(v1, v2).normalized;
    // }

    // when calculating thumb angles, we need to use thumb plane
    float GetThumbAngle(string targetJoint)
    {
        if (!joints.ContainsKey(targetJoint))
            return 0f;

        Vector3 jointForwardVector = joints[targetJoint].right;

        Vector3 projectedForward = Vector3.ProjectOnPlane(jointForwardVector, thumbPlaneNormal).normalized;

        float angle = Vector3.Angle(jointForwardVector, projectedForward);
        return angle;
    }

    float GetProjectedDistanceOnPalm(string jointA, string jointB)
    {
        if (!joints.ContainsKey(jointA) || !joints.ContainsKey(jointB))
            return 0f;

        Vector3 pointA = joints[jointA].position;
        Vector3 pointB = joints[jointB].position;
        Vector3 palmOrigin = joints["Wrist"].position;

        Vector3 projectedA = ProjectPointOnPlane(pointA, palmOrigin, palmNormal);
        Vector3 projectedB = ProjectPointOnPlane(pointB, palmOrigin, palmNormal);

        return Vector3.Distance(projectedA, projectedB);
    }

    Vector3 ProjectPointOnPlane(Vector3 point, Vector3 planePoint, Vector3 planeNormal)
    {
        Vector3 toPoint = point - planePoint;
        float distance = Vector3.Dot(toPoint, planeNormal);
        return point - (distance * planeNormal);
    }

    // void OnDrawGizmos()
    // {
    //     if (indexTipPos != Vector3.zero && thumbTipPos != Vector3.zero)
    //     {
    //         Gizmos.color = Color.red;
    //         Gizmos.DrawLine(indexTipPos, thumbTipPos);
    //         Gizmos.DrawSphere(indexTipPos, 0.02f);   // slightly bigger so you can see them
    //         Gizmos.DrawSphere(thumbTipPos, 0.02f);
    //     }
    // }

    //TODO: To determine the coordinates of the collider where your left index finger and thumb touch, 
    // you need to calculate whether the line segment connecting the two points projects clockwise or 
    // counterclockwise onto the red X-axis of joints["Index1"].


}