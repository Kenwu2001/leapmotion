using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections;

[System.Serializable]
public class DataToSend
{
    public float pos_x;
    public float pos_y;
    public float pos_z;
    public float rot_x;
    public float rot_y;
    public float rot_z;
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

    [Header("=== Safety Settings ===")]
    [Tooltip("Maximum allowed position change per frame (meters). If exceeded, the frame is discarded.")]
    public float maxDeltaPosition = 0.03f;

    [Tooltip("Maximum allowed rotation change per frame (degrees). If exceeded, the frame is discarded.")]
    public float maxDeltaRotation = 10f; // 10 degrees per frame

    [Tooltip("Maximum allowed total offset from origin (meters). Acts as a workspace boundary.")]
    public float maxPositionFromOrigin = 0.5f; // 50cm workspace radius

    [Tooltip("Number of consecutive anomalies before auto-stopping")]
    public int maxConsecutiveAnomalies = 3;

    private bool waseSendingLastFrame = false;
    private Vector3 originPosition;
    private Vector3 originRotation;

    // Safety tracking
    private Vector3 lastRawPosition;
    private Vector3 lastRawRotation;
    private int consecutiveAnomalyCount = 0;
    private bool hasPreviousFrame = false;

    void Start()
    {
        try
        {
            client = new TcpClient(serverIP, port);
            stream = client.GetStream();
            Debug.Log("Connected to server");

            StartCoroutine(SendLoop());
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
                Vector3 rot = new Vector3(
                    Mathf.DeltaAngle(originRotation.x, currentRawRot.x),
                    Mathf.DeltaAngle(originRotation.y, currentRawRot.y),
                    Mathf.DeltaAngle(originRotation.z, currentRawRot.z)
                );

                // --- Safety Check 2: Workspace boundary ---
                if (pos.magnitude > maxPositionFromOrigin)
                {
                    Debug.LogWarning($"[SAFETY] Position {pos.magnitude:F3}m exceeds workspace radius {maxPositionFromOrigin}m. Clamping.");
                    pos = pos.normalized * maxPositionFromOrigin;
                }

                var dataObj = new DataToSend
                {
                    pos_x = pos.x,
                    pos_y = pos.y,
                    pos_z = pos.z,
                    rot_x = rot.x,
                    rot_y = rot.y,
                    rot_z = rot.z,
                    new_session = !waseSendingLastFrame
                };

                string json = JsonUtility.ToJson(dataObj) + "\n";

                byte[] bytes = Encoding.UTF8.GetBytes(json);
                stream.Write(bytes, 0, bytes.Length);

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