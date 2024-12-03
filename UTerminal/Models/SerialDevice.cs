using System;
using System.IO.Ports;
using System.Linq;

namespace UTerminal.Models;

public class SerialDevice
{
    private SerialPort? _serialPort;
    private readonly SerialSettings _settings;
    public event EventHandler<SerialMessage>? MessageReceived;

    public SerialDevice(SerialSettings settings)
    {
        _settings = settings;
    }

    public bool IsConnected => _serialPort?.IsOpen ?? false;

    public bool Connect()
    {
        try
        {
            _serialPort = new SerialPort(
                _settings.PortName,
                _settings.BaudRate,
                _settings.Parity,
                _settings.DataBits,
                _settings.StopBits
            );

            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();
            return true;
        }
        catch (Exception e)
        {
            OnMessageReceived(new SerialMessage 
            { 
                Data = e.Message,
                Timestamp = DateTime.Now,
                Type = SerialMessage.MessageType.Error
            });
            return false;
        }
    }

    public void Disconnect()
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.DataReceived -= SerialPort_DataReceived;
            _serialPort.Close();
        }
    }
    
    public bool SendData(string data)
    {
        if (_serialPort?.IsOpen != true) return false;

        try
        {
            _serialPort.WriteLine(data);
            OnMessageReceived(new SerialMessage 
            { 
                Data = data,
                Timestamp = DateTime.Now,
                Type = SerialMessage.MessageType.Sent
            });
            return true;
        }
        catch (Exception ex)
        {
            OnMessageReceived(new SerialMessage 
            { 
                Data = ex.Message,
                Timestamp = DateTime.Now,
                Type = SerialMessage.MessageType.Error
            });
            return false;
        }
    }


    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null) return;

        try
        {
            string data = _serialPort.ReadLine();
            OnMessageReceived(new SerialMessage 
            { 
                Data = data,
                Timestamp = DateTime.Now,
                Type = SerialMessage.MessageType.Received
            });
        }
        catch (Exception ex)
        {
            OnMessageReceived(new SerialMessage 
            { 
                Data = ex.Message,
                Timestamp = DateTime.Now,
                Type = SerialMessage.MessageType.Error
            });
        }
    }

    public void GetPortNames()
    {
        string[] portList = SerialPort.GetPortNames()
            .Select(port => port.Replace("/dev/", ""))
            .ToArray();
        
        for (var i = 0; i < 10 ; i++)
        {
            var radioItem = _settings.RadioComPortItems[i];
            if (portList.Length > i)
            {
                radioItem.Text = portList[i];
                radioItem.IsEnabled = true;
            }
            else
            {
                radioItem.Text = (i + 1).ToString();
                radioItem.IsEnabled = false;
            }
        }
    }
    
    protected virtual void OnMessageReceived(SerialMessage message)
    {
        MessageReceived?.Invoke(this, message);
    }

    public void Dispose()
    {
        Disconnect();
        _serialPort?.Dispose();
    }
}