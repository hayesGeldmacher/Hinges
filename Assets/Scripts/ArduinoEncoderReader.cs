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

    private volatile int pendingValue = 0;
    public int EncoderValue { get; private set; }
    public event Action<int> OnEncoderChanged;

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
            catch (Exception) {  }
        }
    }

    void ParseLine(string line)
    {
        if (!line.StartsWith("ENC")) return;

        string[] parts = line.Split(' ');
        if (parts.Length != 2) return;

        if (int.TryParse(parts[1], out int value))
        {
            pendingValue = value;
        }
    }

    private void Update()
    {
        if (EncoderValue != pendingValue)
        {
            EncoderValue = pendingValue;
            OnEncoderChanged?.Invoke(EncoderValue);
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
