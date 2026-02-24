using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System;
using System.Collections;

[System.Serializable]
public class DataToSend
{
    public float a;
    public float b;
    public float c;
}

public class TcpSender : MonoBehaviour
{
    TcpClient client;
    NetworkStream stream;

    string serverIP = "192.168.200.117"; // robot IP address
    int port = 5005;

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
            var dataObj = new DataToSend
            {
                a = 12.23f,
                b = 35.64f,
                c = 94.23f
            };

            string json = JsonUtility.ToJson(dataObj) + "\n";

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);

            Debug.Log("Sent: " + json);

            yield return new WaitForSeconds(1f);
        }
    }

    void OnApplicationQuit()
    {
        stream?.Close();
        client?.Close();
    }
}