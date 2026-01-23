using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class PaxiniValue : MonoBehaviour
{
    public string host = "127.0.0.1";
    public int port = 50007;

    private TcpClient client;
    private Thread thread;
    private volatile bool running;

    private string latestLine = "";
    private bool hasNewData = false;

    [Serializable]
    private class Payload
    {
        public float Fz_thumb;
        public float Fz_index;
        public float Fz_middle;
        public float Ft_thumb;
        public float Ft_index;
        public float Ft_middle;
        public double t;
    }

    void Start()
    {
        running = true;
        thread = new Thread(ReceiveLoop);
        thread.IsBackground = true;
        thread.Start();
    }

    void Update()
    {
        if (hasNewData)
        {
            hasNewData = false;

            Debug.Log($"[Unity] Raw JSON: {latestLine}");

            var payload = JsonUtility.FromJson<Payload>(latestLine);

            Debug.Log($"[Unity] Fz_thumb={payload.Fz_thumb:F3}, Fz_index={payload.Fz_index:F3}, Fz_middle={payload.Fz_middle:F3}, " +
                      $"Ft_thumb={payload.Ft_thumb:F3}, Ft_index={payload.Ft_index:F3}, Ft_middle={payload.Ft_middle:F3}, t={payload.t}");
        }
    }

    void ReceiveLoop()
    {
        while (running)
        {
            try
            {
                Debug.Log("[Unity] Connecting to Python...");
                client = new TcpClient(host, port);

                using var stream = client.GetStream();
                using var reader = new StreamReader(stream);

                Debug.Log("[Unity] Connected!");

                while (running && client.Connected)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;

                    latestLine = line;
                    hasNewData = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Unity] Connect failed, retrying... " + e.Message);
                Thread.Sleep(1000);
            }
        }
    }

    void OnDestroy()
    {
        running = false;
        try { client?.Close(); } catch { }
        try { thread?.Join(200); } catch { }
    }
}