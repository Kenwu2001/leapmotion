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

    public bool isThumbPaxiniZero = false;
    public bool isThumbTouchSnapped = false;
    public bool isIndexPaxiniZero = false;
    public bool isIndexTouchSnapped = false;
    public bool isMiddlePaxiniZero = false;
    public bool isMiddleTouchSnapped = false;

    // Store initial joint angle values when snap occurs
    private float initialThumb0Angle;
    private float initialThumb1Angle;
    private float initialIndex0Angle;
    private float initialIndex1Angle;
    private float initialIndex2Angle;
    private float initialMiddle0Angle;
    private float initialMiddle1Angle;
    private float initialMiddle2Angle;

    // Scripts
    public JointAngle jointAngle;

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

            // Debug.Log($"[Unity] Raw JSON: {latestLine}");

            var payload = JsonUtility.FromJson<Payload>(latestLine);

            // Debug.Log($"[Unity] Fz_thumb={payload.Fz_thumb:F3}, Fz_index={payload.Fz_index:F3}, Fz_middle={payload.Fz_middle:F3}, " +
            //           $"Ft_thumb={payload.Ft_thumb:F3}, Ft_index={payload.Ft_index:F3}, Ft_middle={payload.Ft_middle:F3}, t={payload.t}");

            if (payload.Fz_thumb == 0)
            {
                isThumbPaxiniZero = true;
                isThumbTouchSnapped = false;
            }

            if (payload.Fz_index == 0)
            {
                isIndexPaxiniZero = true;
                isIndexTouchSnapped = false;
            }

            if (payload.Fz_middle == 0)
            {
                isMiddlePaxiniZero = true;
                isMiddleTouchSnapped = false;
            }

            if (payload.Fz_thumb > 0.4f && isThumbPaxiniZero)
            {
                if (!isThumbTouchSnapped)
                {
                    isThumbTouchSnapped = true;
                    isThumbPaxiniZero = false;
                    // Record initial joint angle values
                    Transform thumb0 = jointAngle.GetJoint("Thumb0");
                    Transform thumb1 = jointAngle.GetJoint("Thumb1");
                    if (thumb0 != null && thumb1 != null)
                    {
                        initialThumb0Angle = thumb0.localEulerAngles.z;
                        initialThumb1Angle = thumb1.localEulerAngles.z;
                        initialThumb0Angle = initialThumb0Angle < 100f ? initialThumb0Angle + 360f : initialThumb0Angle;
                        initialThumb1Angle = initialThumb1Angle < 100f ? initialThumb1Angle + 360f : initialThumb1Angle;
                    }
                    // lock every thumb gripper motor
                    // ...
                }
            }

            // if thumb is snapped, continuously check if it should be cancelled
            if (isThumbTouchSnapped)
            {
                // if thumb joint angle moves too much, cancel the snap
                if (cancleTouchSnap("Thumb0", "Thumb1", initialThumb0Angle, initialThumb1Angle))
                {
                    isThumbTouchSnapped = false;
                }
            }

            if (payload.Fz_index > 0.4f && isIndexPaxiniZero)
            {
                if (!isIndexTouchSnapped)
                {
                    isIndexTouchSnapped = true;
                    isIndexPaxiniZero = false;
                    // Record initial joint angle values
                    Transform index0 = jointAngle.GetJoint("Index0");
                    Transform index1 = jointAngle.GetJoint("Index1");
                    Transform index2 = jointAngle.GetJoint("Index2");
                    if (index0 != null && index1 != null && index2 != null)
                    {
                        initialIndex0Angle = index0.localEulerAngles.z;
                        initialIndex1Angle = index1.localEulerAngles.z;
                        initialIndex2Angle = index2.localEulerAngles.z;
                        initialIndex0Angle = initialIndex0Angle < 100f ? initialIndex0Angle + 360f : initialIndex0Angle;
                        initialIndex1Angle = initialIndex1Angle < 100f ? initialIndex1Angle + 360f : initialIndex1Angle;
                        initialIndex2Angle = initialIndex2Angle < 100f ? initialIndex2Angle + 360f : initialIndex2Angle;
                    }
                }
            }

            // if index is snapped, continuously check if it should be cancelled
            if (isIndexTouchSnapped)
            {
                if (cancleTouchSnap("Index0", "Index1", initialIndex0Angle, initialIndex1Angle, "Index2", initialIndex2Angle))
                {
                    isIndexTouchSnapped = false;
                }
            }

            if (payload.Fz_middle > 0.4f && isMiddlePaxiniZero)
            {
                if (!isMiddleTouchSnapped)
                {
                    isMiddleTouchSnapped = true;
                    isMiddlePaxiniZero = false;
                    // Record initial joint angle values
                    Transform middle0 = jointAngle.GetJoint("Middle0");
                    Transform middle1 = jointAngle.GetJoint("Middle1");
                    Transform middle2 = jointAngle.GetJoint("Middle2");
                    if (middle0 != null && middle1 != null && middle2 != null)
                    {
                        initialMiddle0Angle = middle0.localEulerAngles.z;
                        initialMiddle1Angle = middle1.localEulerAngles.z;
                        initialMiddle2Angle = middle2.localEulerAngles.z;
                        initialMiddle0Angle = initialMiddle0Angle < 100f ? initialMiddle0Angle + 360f : initialMiddle0Angle;
                        initialMiddle1Angle = initialMiddle1Angle < 100f ? initialMiddle1Angle + 360f : initialMiddle1Angle;
                        initialMiddle2Angle = initialMiddle2Angle < 100f ? initialMiddle2Angle + 360f : initialMiddle2Angle;
                    }
                }
            }

            // if middle is snapped, continuously check if it should be cancelled
            if (isMiddleTouchSnapped)
            {
                Debug.Log("Checking cancleMiddleTouchSnap...");
                if (cancleTouchSnap("Middle0", "Middle1", initialMiddle0Angle, initialMiddle1Angle, "Middle2", initialMiddle2Angle))
                {
                    isMiddleTouchSnapped = false;
                }
            }
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

    bool cancleTouchSnap(string joint0Name, string joint1Name, float initial0Angle, float initial1Angle, 
                         string joint2Name = null, float initial2Angle = 0f)
    {
        if (jointAngle == null)
            return false;

        Debug.Log("111111111111111111111111111111111111111");

        Transform joint0 = jointAngle.GetJoint(joint0Name);
        Transform joint1 = jointAngle.GetJoint(joint1Name);
        
        if (joint0 == null || joint1 == null)
            return false;

        Debug.Log("22222222222222222222222222222222222222222");
        

        float accumulatedJoint0 = 0f;
        float accumulatedJoint1 = 0f;
        float accumulatedJoint2 = 0f;

        // Get current angles
        float current0 = joint0.localEulerAngles.z;
        float current1 = joint1.localEulerAngles.z;

        current0 = current0 < 100f ? current0 + 360f : current0;
        current1 = current1 < 100f ? current1 + 360f : current1;

        accumulatedJoint0 = initial0Angle - current0;
        accumulatedJoint1 = initial1Angle - current1;

        if (joint2Name != null)
        {
            Transform joint2 = jointAngle.GetJoint(joint2Name);
            if (joint2 == null)
                return false;

            Debug.Log("33333333333333333333333333333333333333333333");
            
                
            float current2 = joint2.localEulerAngles.z;
            current2 = current2 < 100f ? current2 + 360f : current2;
            accumulatedJoint2 = initial2Angle - current2;
        }

        float threshold = 15f;
        if (joint2Name != null)
        {
            Debug.Log("accumulatedJoint0, accumulatedJoint1, accumulatedJoint2: " + accumulatedJoint0 + ", " + accumulatedJoint1 + ", " + accumulatedJoint2);

            return (accumulatedJoint0 + accumulatedJoint1 + accumulatedJoint2 > threshold) || 
            (accumulatedJoint0 + accumulatedJoint1 + accumulatedJoint2 < -threshold);
        }
        else
        {
            return (accumulatedJoint0 + accumulatedJoint1 > threshold) || (accumulatedJoint0 + accumulatedJoint1 < -threshold);
        }
    }

    // bool cancleThumbTouchSnap()
    // {
    //     if (initialThumb0 == null || initialThumb1 == null || jointAngle == null)
    //         return false;

    //     Transform currentThumb0 = jointAngle.GetJoint("Thumb0");
    //     Transform currentThumb1 = jointAngle.GetJoint("Thumb1");

    //     if (currentThumb0 == null || currentThumb1 == null)
    //         return false;

    //     // Compare positions - if moved too much, cancel the snap
    //     float distance0 = Vector3.Distance(initialThumb0.position, currentThumb0.position);
    //     float distance1 = Vector3.Distance(initialThumb1.position, currentThumb1.position);

    //     // Compare rotations - if rotated too much, cancel the snap
    //     float angle0 = Quaternion.Angle(initialThumb0.rotation, currentThumb0.rotation);
    //     float angle1 = Quaternion.Angle(initialThumb1.rotation, currentThumb1.rotation);

    //     // Threshold values - adjust as needed
    //     float positionThreshold = 0.01f; // 1cm
    //     float angleThreshold = 10f; // 10 degrees

    //     return distance0 > positionThreshold || distance1 > positionThreshold ||
    //            angle0 > angleThreshold || angle1 > angleThreshold;
    // }


    // bool cancleIndexTouchSnap()
    // {
    //     if (initialIndex0 == null || initialIndex1 == null || jointAngle == null)
    //         return false;

    //     Transform currentIndex0 = jointAngle.GetJoint("Index0");
    //     Transform currentIndex1 = jointAngle.GetJoint("Index1");

    //     if (currentIndex0 == null || currentIndex1 == null)
    //         return false;

    //     float distance0 = Vector3.Distance(initialIndex0.position, currentIndex0.position);
    //     float distance1 = Vector3.Distance(initialIndex1.position, currentIndex1.position);

    //     float angle0 = Quaternion.Angle(initialIndex0.rotation, currentIndex0.rotation);
    //     float angle1 = Quaternion.Angle(initialIndex1.rotation, currentIndex1.rotation);

    //     float positionThreshold = 0.01f;
    //     float angleThreshold = 10f;

    //     return distance0 > positionThreshold || distance1 > positionThreshold ||
    //            angle0 > angleThreshold || angle1 > angleThreshold;
    // }

    // bool cancleMiddleTouchSnap()
    // {
    //     if (initialMiddle0 == null || initialMiddle1 == null || jointAngle == null)
    //         return false;

    //     Transform currentMiddle0 = jointAngle.GetJoint("Middle0");
    //     Transform currentMiddle1 = jointAngle.GetJoint("Middle1");

    //     if (currentMiddle0 == null || currentMiddle1 == null)
    //         return false;

    //     float distance0 = Vector3.Distance(initialMiddle0.position, currentMiddle0.position);
    //     float distance1 = Vector3.Distance(initialMiddle1.position, currentMiddle1.position);

    //     float angle0 = Quaternion.Angle(initialMiddle0.rotation, currentMiddle0.rotation);
    //     float angle1 = Quaternion.Angle(initialMiddle1.rotation, currentMiddle1.rotation);

    //     float positionThreshold = 0.01f;
    //     float angleThreshold = 10f;

    //     return distance0 > positionThreshold || distance1 > positionThreshold ||
    //            angle0 > angleThreshold || angle1 > angleThreshold;
    // }

    void OnDestroy()
    {
        running = false;
        try { client?.Close(); } catch { }
        try { thread?.Join(200); } catch { }
    }
}