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

    private bool waseSendingLastFrame = false;
    private Vector3 originPosition;
    private Vector3 originRotation;

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
                // turn on toggle, record the current position and rotation as origin
                if (!waseSendingLastFrame)
                {
                    originPosition = r_wrist.transform.position;
                    originRotation = r_wrist.transform.eulerAngles;
                    Debug.Log("Origin recorded - Pos: " + originPosition + " Rot: " + originRotation);
                }

                Vector3 pos = r_wrist.transform.position - originPosition;
                Vector3 rot = new Vector3(
                    Mathf.DeltaAngle(originRotation.x, r_wrist.transform.eulerAngles.x),
                    Mathf.DeltaAngle(originRotation.y, r_wrist.transform.eulerAngles.y),
                    Mathf.DeltaAngle(originRotation.z, r_wrist.transform.eulerAngles.z)
                );

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