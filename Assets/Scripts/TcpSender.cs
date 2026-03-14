using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections;

public enum RotationType
{
    Euler,
    Quaternion,
    RotationMatrix
}

[System.Serializable]
public class DataToSend
{
    public float pos_x;
    public float pos_y;
    public float pos_z;

    // rotation_type: "euler", "quaternion", "rotation_matrix"
    public string rotation_type;

    // Euler (degrees)
    public float rot_x;
    public float rot_y;
    public float rot_z;

    // Quaternion
    public float quat_x;
    public float quat_y;
    public float quat_z;
    public float quat_w;

    // Rotation Matrix (row-major 3x3)
    public float r00, r01, r02;
    public float r10, r11, r12;
    public float r20, r21, r22;

    public bool new_session;
}

public class TcpSender : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;

    public GameObject r_wrist; // Drag r_wrist into Inspector
    public bool isSending = false; // Toggle in Inspector to start/stop sending

    string serverIP = "192.168.200.117"; // robot IP address
    int port = 5005;

    [Header("=== Rotation Mode ===")]
    [Tooltip("Choose which rotation representation to send")]
    public RotationType rotationMode = RotationType.Euler;

    [Header("=== Safety Settings ===")]
    [Tooltip("Maximum allowed position change per frame (meters). If exceeded, the frame is discarded.")]
    public float maxDeltaPosition = 0.03f;

    [Tooltip("Maximum allowed rotation change per frame (degrees). If exceeded, the frame is discarded.")]
    public float maxDeltaRotation = 10f; // 10 degrees per frame

    [Tooltip("Maximum allowed total offset from origin (meters). Acts as a workspace boundary.")]
    public float maxPositionFromOrigin = 0.5f; // 50cm workspace radius

    [Tooltip("Number of consecutive anomalies before auto-stopping")]
    public int maxConsecutiveAnomalies = 3;

    [Header("=== Debug ===")]
    public bool showDebugValues = false;

    // Debug fields (drawn by Custom Editor)
    [HideInInspector] public float debug_pos_x;
    [HideInInspector] public float debug_pos_y;
    [HideInInspector] public float debug_pos_z;
    [HideInInspector] public float debug_euler_x;
    [HideInInspector] public float debug_euler_y;
    [HideInInspector] public float debug_euler_z;
    [HideInInspector] public float debug_quat_x;
    [HideInInspector] public float debug_quat_y;
    [HideInInspector] public float debug_quat_z;
    [HideInInspector] public float debug_quat_w;
    [HideInInspector] public Vector3 debug_matrix_row0;
    [HideInInspector] public Vector3 debug_matrix_row1;
    [HideInInspector] public Vector3 debug_matrix_row2;

    private bool waseSendingLastFrame = false;
    private Vector3 originPosition;
    private Vector3 originRotation;
    private Quaternion originQuaternion;

    // Safety tracking
    private Vector3 lastRawPosition;
    private Vector3 lastRawRotation;
    private int consecutiveAnomalyCount = 0;
    private bool hasPreviousFrame = false;

    void Start()
    {
        ConnectToServer();
        StartCoroutine(SendLoop());
    }

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIP, port);
            stream = client.GetStream();
            Debug.Log("Connected to server");
        }
        catch (Exception e)
        {
            Debug.LogError("Connection error: " + e.Message);
        }
    }

    IEnumerator SendLoop()
    {
        while (true)
        {
            if (isSending && r_wrist != null)
            {
                // Turn on toggle, record the current position and rotation as origin
                if (!waseSendingLastFrame)
                {
                    originPosition = r_wrist.transform.position;
                    originRotation = r_wrist.transform.eulerAngles;
                    originQuaternion = r_wrist.transform.rotation;
                    lastRawPosition = r_wrist.transform.position;
                    lastRawRotation = r_wrist.transform.eulerAngles;
                    consecutiveAnomalyCount = 0;
                    hasPreviousFrame = false;
                    Debug.Log("Origin recorded - Pos: " + originPosition + " Rot: " + originRotation);
                }

                Vector3 currentRawPos = r_wrist.transform.position;
                Vector3 currentRawRot = r_wrist.transform.eulerAngles;

                // --- Safety Check 1: Frame-to-frame delta limit ---
                bool isAnomaly = false;
                if (hasPreviousFrame)
                {
                    float posDelta = Vector3.Distance(currentRawPos, lastRawPosition);
                    float rotDeltaX = Mathf.Abs(Mathf.DeltaAngle(lastRawRotation.x, currentRawRot.x));
                    float rotDeltaY = Mathf.Abs(Mathf.DeltaAngle(lastRawRotation.y, currentRawRot.y));
                    float rotDeltaZ = Mathf.Abs(Mathf.DeltaAngle(lastRawRotation.z, currentRawRot.z));
                    float maxRotDelta = Mathf.Max(rotDeltaX, Mathf.Max(rotDeltaY, rotDeltaZ));

                    if (posDelta > maxDeltaPosition)
                    {
                        Debug.LogWarning($"[SAFETY] Position jump detected: {posDelta:F4}m > {maxDeltaPosition}m threshold. Frame discarded.");
                        isAnomaly = true;
                    }
                    if (maxRotDelta > maxDeltaRotation)
                    {
                        Debug.LogWarning($"[SAFETY] Rotation jump detected: {maxRotDelta:F1}° > {maxDeltaRotation}° threshold. Frame discarded.");
                        isAnomaly = true;
                    }
                }

                if (isAnomaly)
                {
                    consecutiveAnomalyCount++;
                    Debug.LogWarning($"[SAFETY] Consecutive anomalies: {consecutiveAnomalyCount}/{maxConsecutiveAnomalies}");

                    // Auto-stop if too many consecutive anomalies
                    if (consecutiveAnomalyCount >= maxConsecutiveAnomalies)
                    {
                        isSending = false;
                        Debug.LogError("[SAFETY] Too many consecutive tracking anomalies! Auto-stopped sending to protect robot arm.");
                    }

                    // Don't update lastRaw — keep waiting for tracking to return to a sane position
                    waseSendingLastFrame = isSending;
                    yield return new WaitForSeconds(0.02f);
                    continue;
                }

                // Tracking is normal, reset anomaly counter
                consecutiveAnomalyCount = 0;
                lastRawPosition = currentRawPos;
                lastRawRotation = currentRawRot;
                hasPreviousFrame = true;

                // Calculate offset from origin
                Vector3 pos = currentRawPos - originPosition;
                Vector3 euler = new Vector3(
                    Mathf.DeltaAngle(originRotation.x, currentRawRot.x),
                    Mathf.DeltaAngle(originRotation.y, currentRawRot.y),
                    Mathf.DeltaAngle(originRotation.z, currentRawRot.z)
                );

                // Relative quaternion: from origin to current
                Quaternion currentQuat = r_wrist.transform.rotation;
                Quaternion relativeQuat = Quaternion.Inverse(originQuaternion) * currentQuat;

                // Rotation matrix from relative quaternion
                Matrix4x4 rotMatrix = Matrix4x4.Rotate(relativeQuat);

                // --- Safety Check 2: Workspace boundary ---
                if (pos.magnitude > maxPositionFromOrigin)
                {
                    Debug.LogWarning($"[SAFETY] Position {pos.magnitude:F3}m exceeds workspace radius {maxPositionFromOrigin}m. Clamping.");
                    pos = pos.normalized * maxPositionFromOrigin;
                }

                // Update debug variables
                debug_pos_x = pos.x;
                debug_pos_y = pos.y;
                debug_pos_z = pos.z;
                debug_euler_x = euler.x;
                debug_euler_y = euler.y;
                debug_euler_z = euler.z;
                debug_quat_x = relativeQuat.x;
                debug_quat_y = relativeQuat.y;
                debug_quat_z = relativeQuat.z;
                debug_quat_w = relativeQuat.w;
                debug_matrix_row0 = new Vector3(rotMatrix.m00, rotMatrix.m01, rotMatrix.m02);
                debug_matrix_row1 = new Vector3(rotMatrix.m10, rotMatrix.m11, rotMatrix.m12);
                debug_matrix_row2 = new Vector3(rotMatrix.m20, rotMatrix.m21, rotMatrix.m22);

                // Build data object
                var dataObj = new DataToSend
                {
                    pos_x = pos.x,
                    pos_y = pos.y,
                    pos_z = pos.z,
                    new_session = !waseSendingLastFrame
                };

                switch (rotationMode)
                {
                    case RotationType.Euler:
                        dataObj.rotation_type = "euler";
                        dataObj.rot_x = euler.x;
                        dataObj.rot_y = euler.y;
                        dataObj.rot_z = euler.z;
                        break;

                    case RotationType.Quaternion:
                        dataObj.rotation_type = "quaternion";
                        dataObj.quat_x = relativeQuat.x;
                        dataObj.quat_y = relativeQuat.y;
                        dataObj.quat_z = relativeQuat.z;
                        dataObj.quat_w = relativeQuat.w;
                        break;

                    case RotationType.RotationMatrix:
                        dataObj.rotation_type = "rotation_matrix";
                        dataObj.r00 = rotMatrix.m00; dataObj.r01 = rotMatrix.m01; dataObj.r02 = rotMatrix.m02;
                        dataObj.r10 = rotMatrix.m10; dataObj.r11 = rotMatrix.m11; dataObj.r12 = rotMatrix.m12;
                        dataObj.r20 = rotMatrix.m20; dataObj.r21 = rotMatrix.m21; dataObj.r22 = rotMatrix.m22;
                        break;
                }

                string json = JsonUtility.ToJson(dataObj) + "\n";

                if (stream != null)
                {
                    try
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(json);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Send error: " + e.Message);
                    }
                }

                Debug.Log("Sent: " + json);
            }

            waseSendingLastFrame = isSending;
            yield return new WaitForSeconds(0.02f);
        }
    }

    void OnApplicationQuit()
    {
        stream?.Close();
        client?.Close();
    }
}