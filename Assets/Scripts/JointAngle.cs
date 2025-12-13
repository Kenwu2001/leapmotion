using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// i want the same funtionality when the right middle finger is touched (triggerRightThumbTip), just as the same way you do with right index finger

public class JointAngle : MonoBehaviour
{
    public Dictionary<string, Transform> joints = new Dictionary<string, Transform>();
    public float thumbAngle0, thumbAngle1, thumbLRAngle;
    public float indexAngle0, indexAngle1, indexAngle2, indexLRAngle;
    public float middleAngle0, middleAngle1, middleAngle2, middleLRAngle;

    private Vector3 palmNormal;
    private Vector3 thumbPlaneNormal;

    public float indexMiddleDistance;

    public float thumbPalmAngle;
    public float wristThumbAngle;

    // Public properties to expose plane state
    public bool isPlaneActive { get; private set; } = false;
    public string activeFinger { get; private set; } = "None"; // "Index", "Middle", "Thumb", or "None"
    public float isClockWise;

    // Reference for twisting
    public TriggerRightIndexTip triggerRightIndexTip;
    public TriggerRightMiddleTip triggerRightMiddleTip;
    public TriggerRightThumbTip triggerRightThumbTip;

    public Vector3 indexTipPos;
    public Vector3 thumbTipPos;
    public Vector3 projectedIndexTip;
    public Vector3 projectedThumbTip;

    private LineRenderer lineRenderer;
    private GameObject debugPlane;

    private Vector3 previousIndexTip;
    private Vector3 previousThumbTip;
    private bool hasPreviousFrame = false;
    private float lastRotationDirection = 1f; // Default to clockwise
    private float noRotationTimer = 0f; // Timer for no rotation detection

    private Queue<float> rotationHistory = new Queue<float>();
    private const int ROTATION_HISTORY_SIZE = 10; // Look at last 10 frames
    private float rotationChangeTimer = 0f;
    private const float ROTATION_CHANGE_COOLDOWN = 0.3f; // Don't change direction more than once per 0.3 seconds
    private float cumulativeRotation = 0f; // Track total rotation
    private const float MIN_ROTATION_THRESHOLD = 0.02f; // Minimum rotation to consider

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

        if (triggerRightMiddleTip == null)
            triggerRightMiddleTip = FindObjectOfType<TriggerRightMiddleTip>();

        if (triggerRightThumbTip == null)
            triggerRightThumbTip = FindObjectOfType<TriggerRightThumbTip>();

        // Create LineRenderer
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.005f;
        lineRenderer.endWidth = 0.005f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.positionCount = 2;

        // Create debug plane
        debugPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        debugPlane.transform.localScale = new Vector3(0.05f, 1f, 0.05f);
        debugPlane.name = "Index1_DebugPlane";

        // Make it semi-transparent and double-sided
        Renderer planeRenderer = debugPlane.GetComponent<Renderer>();
        Material planeMaterial = new Material(Shader.Find("Standard"));
        planeMaterial.color = new Color(0f, 1f, 0f, 0.3f);
        planeMaterial.SetFloat("_Mode", 3); // Transparent mode
        planeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        planeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        planeMaterial.SetInt("_ZWrite", 0);
        planeMaterial.SetInt("_Cull", 0); // Disable backface culling (0 = Off, shows both sides)
        planeMaterial.DisableKeyword("_ALPHATEST_ON");
        planeMaterial.EnableKeyword("_ALPHABLEND_ON");
        planeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        planeMaterial.renderQueue = 3000;
        planeRenderer.material = planeMaterial;
    }

    void Update()
    {
        UpdatePalmNormal();

        UpdateThumbPlane(); // Uncomment this line

        thumbPalmAngle = UpdateThumbPalmAngle();
        wristThumbAngle = GetWristThumbAngle();

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

        // Determine which finger to use based on touch detection
        bool useIndexFinger = false;
        bool useMiddleFinger = false;
        bool useThumbFinger = false;
        string activeJoint = "Index1";
        Dictionary<string, Vector3> touchedPoints = null;

        // Check TriggerRightIndexTip first
        if (triggerRightIndexTip != null)
        {
            Dictionary<string, Vector3> indexTouchPoints = triggerRightIndexTip.GetAllTouchedPoints();
            if (indexTouchPoints.ContainsKey("L_IndexTip") && indexTouchPoints.ContainsKey("L_ThumbTip"))
            {
                useIndexFinger = true;
                touchedPoints = indexTouchPoints;
                activeJoint = "Index1";
            }
        }

        // Check TriggerRightMiddleTip if index isn't active
        if (!useIndexFinger && triggerRightMiddleTip != null)
        {
            Dictionary<string, Vector3> middleTouchPoints = triggerRightMiddleTip.GetAllTouchedPoints();
            if (middleTouchPoints.ContainsKey("L_IndexTip") && middleTouchPoints.ContainsKey("L_ThumbTip"))
            {
                useMiddleFinger = true;
                touchedPoints = middleTouchPoints;
                activeJoint = "Middle1";
            }
        }

        // Check TriggerRightThumbTip if index and middle aren't active
        if (!useIndexFinger && !useMiddleFinger && triggerRightThumbTip != null)
        {
            Dictionary<string, Vector3> thumbTouchPoints = triggerRightThumbTip.GetAllTouchedPoints();
            if (thumbTouchPoints.ContainsKey("L_IndexTip") && thumbTouchPoints.ContainsKey("L_ThumbTip"))
            {
                useThumbFinger = true;
                touchedPoints = thumbTouchPoints;
                activeJoint = "Thumb1";
            }
        }

        // Process touched points and update visualization
        if ((useIndexFinger || useMiddleFinger || useThumbFinger) && touchedPoints != null)
        {
            if (touchedPoints.ContainsKey("L_IndexTip") && touchedPoints.ContainsKey("L_ThumbTip"))
            {
                indexTipPos = touchedPoints["L_IndexTip"];
                thumbTipPos = touchedPoints["L_ThumbTip"];

                // Project positions onto the debug plane
                if (debugPlane != null && joints.ContainsKey(activeJoint))
                {
                    Transform activeFingerJoint = joints[activeJoint];
                    Vector3 planeNormal = activeFingerJoint.right;

                    // UPDATE PLANE POSITION FIRST, BEFORE PROJECTION
                    debugPlane.transform.position = activeFingerJoint.position + activeFingerJoint.right * -0.22f;
                    debugPlane.transform.rotation = Quaternion.LookRotation(activeFingerJoint.up, activeFingerJoint.right);

                    // NOW get the updated plane position for projection
                    Vector3 planePoint = debugPlane.transform.position;

                    projectedIndexTip = ProjectPointOnPlane(indexTipPos, planePoint, planeNormal);
                    projectedThumbTip = ProjectPointOnPlane(thumbTipPos, planePoint, planeNormal);

                    lineRenderer.SetPosition(0, projectedIndexTip);
                    lineRenderer.SetPosition(1, projectedThumbTip);

                    // Debug.Log($"Active Joint: {activeJoint}, ProjectedIndex: {projectedIndexTip}, ProjectedThumb: {projectedThumbTip}");

                    // Calculate rotation direction compared to previous frame
                    if (hasPreviousFrame)
                    {
                        float newRotation = GetRotationDirection(
                            previousIndexTip, previousThumbTip,
                            projectedIndexTip, projectedThumbTip,
                            activeJoint
                        );
                        
                        // Add to rotation history
                        rotationHistory.Enqueue(newRotation);
                        if (rotationHistory.Count > ROTATION_HISTORY_SIZE)
                            rotationHistory.Dequeue();
                        
                        // Calculate weighted average (recent frames have more weight)
                        float weightedSum = 0f;
                        float weightTotal = 0f;
                        int index = 0;
                        foreach (float rot in rotationHistory)
                        {
                            float weight = (index + 1) / (float)rotationHistory.Count; // More recent = higher weight
                            weightedSum += rot * weight;
                            weightTotal += weight;
                            index++;
                        }
                        
                        float averageRotation = weightTotal > 0 ? weightedSum / weightTotal : 0f;
                        
                        // Track cumulative rotation magnitude
                        if (newRotation != 0f)
                        {
                            cumulativeRotation += Mathf.Abs(Vector3.Angle(
                                (previousThumbTip - previousIndexTip).normalized,
                                (projectedThumbTip - projectedIndexTip).normalized
                            ));
                        }
                        
                        // Only update if we have clear consensus AND enough rotation AND cooldown expired
                        rotationChangeTimer += Time.deltaTime;
                        
                        if (Mathf.Abs(averageRotation) > 0.5f && // Clear direction (> 50% consensus)
                            cumulativeRotation > MIN_ROTATION_THRESHOLD && // Minimum rotation threshold
                            rotationChangeTimer >= ROTATION_CHANGE_COOLDOWN) // Cooldown expired
                        {
                            float newDirection = averageRotation > 0 ? 1f : -1f;
                            
                            // Only change if different from current
                            if (newDirection != isClockWise)
                            {
                                isClockWise = newDirection;
                                lastRotationDirection = newDirection;
                                rotationChangeTimer = 0f; // Reset cooldown
                                cumulativeRotation = 0f; // Reset cumulative rotation
                            }
                            
                            noRotationTimer = 0f;
                        }
                        else if (cumulativeRotation < MIN_ROTATION_THRESHOLD)
                        {
                            // No meaningful rotation detected
                            noRotationTimer += Time.deltaTime;
                            
                            if (noRotationTimer >= 1f)
                            {
                                isClockWise = 0f;
                                cumulativeRotation = 0f;
                            }
                            else
                            {
                                isClockWise = lastRotationDirection;
                            }
                        }
                    }
                    else
                    {
                        isClockWise = lastRotationDirection;
                        noRotationTimer = 0f;
                        cumulativeRotation = 0f;
                        rotationHistory.Clear();
                    }

                    // Store current frame for next comparison
                    previousIndexTip = projectedIndexTip;
                    previousThumbTip = projectedThumbTip;
                    hasPreviousFrame = true;

                    // Show the plane
                    debugPlane.SetActive(true);
                    isPlaneActive = true;
                    activeFinger = useIndexFinger ? "Index" : (useMiddleFinger ? "Middle" : "Thumb");
                }
                else
                {
                    lineRenderer.SetPosition(0, indexTipPos);
                    lineRenderer.SetPosition(1, thumbTipPos);
                }

                lineRenderer.enabled = true;

                if (hasPreviousFrame && isClockWise != 0)
                {
                    // Debug.Log($"Rotation on {activeJoint}: {(isClockWise > 0 ? "CLOCKWISE" : "COUNTERCLOCKWISE")} ({isClockWise:F3})");
                }
            }
        }
        else
        {
            // No touch detected - hide everything
            lineRenderer.enabled = false;
            debugPlane.SetActive(false); // HIDE THE PLANE
            isPlaneActive = false;
            activeFinger = "None";
            hasPreviousFrame = false;
            lastRotationDirection = 1f;
            noRotationTimer = 0f;
            cumulativeRotation = 0f;
            rotationHistory.Clear();
            rotationChangeTimer = 0f;
        }

        // Debug.Log("indexTipPos: " + indexTipPos.ToString("F4") + ", thumbTipPos: " + thumbTipPos.ToString("F4"));
    }

    // calculate the angles between thumbPlaneNormal and palmNormal
    float UpdateThumbPalmAngle()
    {
        return Vector3.Angle(thumbPlaneNormal, palmNormal);
        // Debug.Log("Thumb-Palm Plane Angle: " + angle.ToString("F2") + " degrees");
    }

    // Calculate the angle between R_Wrist's red vector and R_thumb_a's red vector
    float GetWristThumbAngle()
    {
        if (!joints.ContainsKey("Wrist") || !joints.ContainsKey("Thumb0"))
            return 0f;

        Vector3 wristRight = joints["Wrist"].right;
        Vector3 thumbRight = joints["Thumb0"].right;

        // Vector3.Angle always returns the smaller angle (0-180 degrees)
        float angle = Vector3.Angle(wristRight, thumbRight);
        return angle;
    }

    // compute thumb plane normal from (Wrist, R_thumb_a, R_index_Proximal) points.
    void UpdateThumbPlane()
    {
        Vector3 p0 = joints["Wrist"].position;
        Vector3 p1 = joints["Thumb0"].position;
        Vector3 p2 = joints["Index0"].position;

        Vector3 v1 = (p1 - p0).normalized;
        Vector3 v2 = (p2 - p0).normalized;

        thumbPlaneNormal = Vector3.Cross(v1, v2).normalized;
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

    // Calculate rotation direction from previous frame to current frame
    // Returns 1 for clockwise, -1 for counterclockwise, 0 for no clear rotation
    float GetRotationDirection(Vector3 prevPoint1, Vector3 prevPoint2, Vector3 currPoint1, Vector3 currPoint2, string jointName = "Index1")
    {
        if (!joints.ContainsKey(jointName))
            return 0f;

        Transform joint = joints[jointName];
        Vector3 planeNormal = joint.right; // The plane normal (red axis)

        // Get the line vectors for previous and current frames
        Vector3 prevLine = (prevPoint2 - prevPoint1).normalized;
        Vector3 currLine = (currPoint2 - currPoint1).normalized;

        // Check if lines are too similar (no meaningful rotation)
        float similarity = Vector3.Dot(prevLine, currLine);
        if (similarity > 0.9999f) // Almost identical
            return 0f;

        // Calculate the cross product to determine rotation direction
        // Cross product of (previous → current) gives rotation axis
        Vector3 rotationAxis = Vector3.Cross(prevLine, currLine);

        // Project rotation axis onto plane normal to get signed rotation
        float rotationSign = Vector3.Dot(rotationAxis, planeNormal);

        // Return only 1 or -1 based on sign
        if (rotationSign > 0.001f)
            return 1f;  // Clockwise
        else if (rotationSign < -0.001f)
            return -1f; // Counterclockwise
        else
            return 0f;  // No clear rotation
    }

    // Public method to safely get joint transform
    public Transform GetJoint(string jointName)
    {
        if (joints != null && joints.ContainsKey(jointName))
            return joints[jointName];
        return null;
    }
}