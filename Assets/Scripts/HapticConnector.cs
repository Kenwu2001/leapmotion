using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class WristAngleUdpSender : MonoBehaviour
{
    public Transform R_wrist;

    public string pythonIp = "127.0.0.1";
    public int pythonPort = 5005;

    UdpClient client;

    void Start()
    {
        client = new UdpClient();
    }

    void Update()
    {
        float angle = WristAngleUtil.GetWristAngleDeg(R_wrist);
        int direction = AngleToDirection(angle);

        // Debug.Log($"Wrist Angle: {angle:F1}°, Direction: {direction}");

        // Send angle (Python calculates direction) or directly send direction
        string msg = $"W,{angle:F1}";
        // string msg = $"D,{direction}";

        byte[] data = Encoding.UTF8.GetBytes(msg);
        client.Send(data, data.Length, pythonIp, pythonPort);
    }

    static int AngleToDirection(float angle)
    {
        if (angle >= 330f || angle <= 45f) return 1;
        if (angle <= 135f) return 2;
        if (angle <= 275f) return 3;
        return 4;
    }

    void OnApplicationQuit()
    {
        client.Close();
    }
}