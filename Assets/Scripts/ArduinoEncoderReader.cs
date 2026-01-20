using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;


public class ArduinoEncoderReader : MonoBehaviour
{
    public string portName = "COM4";
    public int baudRate = 9600;

    SerialPort serialPort;
    Thread readThread;
    bool running = false;

    private volatile int encoderValue = 0;

    private void Start()
    {
        serialPort = new SerialPort(portName, baudRate);

        try
        {
            serialPort.Open();
            running = true;

            readThread = new Thread(ReadSerial);
            readThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    void ReadSerial()
    {
        while (running)
        {
            try
            {
                string line = serialPort.ReadLine();
                ParseLine(line);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }

    void ParseLine(string line)
    {
        if (!line.StartsWith("ENC")) return;

        string[] parts = line.Split(' ');
        if (parts.Length != 2) return;

        if (int.TryParse(parts[1], out int value))
        {
            encoderValue = value;
            Debug.Log("Encoder: " + encoderValue);
        }
    }

    private void OnDestroy()
    {
        running = false;

        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }

        if (readThread != null && readThread.IsAlive)
        {
            readThread.Join();
        }
    }
}
