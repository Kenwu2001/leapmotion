using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class SerialManager : MonoBehaviour
{

    [SpaceAttribute(10)]
    [HeaderAttribute("                       ------- Port Settings ------- ")]
    [SpaceAttribute(10)]
    public string Port = "COM3";
    public int BaudRate = 115200;

    public SerialPort serialPort;

    void Awake()
    {
        serialInitialize();
    }

    private void serialInitialize()
    {
        try
        {
            serialPort = new SerialPort(Port, BaudRate);
            serialPort.ReadTimeout = 20;
            serialPort.WriteTimeout = 20;
            serialPort.NewLine = "\n";
            serialPort.Open();
            Debug.Log("SerialManager connected: " + Port + " @ " + BaudRate);
        }
        catch (Exception ex)
        {
            string[] ports = SerialPort.GetPortNames();
            string availablePorts = (ports != null && ports.Length > 0) ? string.Join(", ", ports) : "(none)";
            Debug.LogError("SerialManager open failed on " + Port + " @ " + BaudRate + ". Available ports: " + availablePorts + ". Error: " + ex.Message);
        }
    }

    private void OnDestroy()
    {
        try
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("SerialManager close warning: " + ex.Message);
        }
    }
}