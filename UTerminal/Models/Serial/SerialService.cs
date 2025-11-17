using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ReactiveUI;
using UTerminal.Models.Messages;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Monitoring;
using UTerminal.Models.Serial.Interfaces;
using UTerminal.Models.Utils.Logger;

namespace UTerminal.Models.Serial;

public class SerialService : ReactiveObject, ISerialService
{
    // Basic serial
    private readonly ISerialPort _serialPort;
    private readonly SerialDataParser _parser;
    private readonly MessageRateMonitor _rateMonitor;
    private readonly SerialRuntimeConfiguration _runtimeConfig;

    private readonly SerialConnectionConfiguration _connectionConfig;
    private readonly SystemLogger _systemLogger = SystemLogger.Instance;
    
    // Serial message receive event handler
    public event EventHandler<ISerialMessage>? MsgReceived;
    
    private IDisposable? _rawDataSubscription;
    private readonly List<byte> _bufferList = [];
    private bool _canBufferAdd;
    
    public bool IsConnected => _serialPort.IsConnected;
    public double MessageRate => _rateMonitor.CurrentRate;
    
    public SerialService(ISerialPort serialPort,
                        SerialConnectionConfiguration connectionConfig, 
                        SerialRuntimeConfiguration runtimeConfig)
    {
        _serialPort = serialPort;
        _connectionConfig = connectionConfig;
        _runtimeConfig = runtimeConfig;
        
        _parser = new SerialDataParser();
        _rateMonitor = new MessageRateMonitor();
        
        _systemLogger.LogInfo("Initialized Serial Service");
    }
    
    public bool Connect()
    {
        _systemLogger.LogInfo($"Serial Port Connect at\n" +
                              $"\t Port: {_connectionConfig.PortName}\n" +
                              $"\t BaudRate: {_connectionConfig.BaudRate}\n" +
                              $"\t Parity: {_connectionConfig.Parity}\n" +
                              $"\t DataBits: {_connectionConfig.DataBits}\n" +
                              $"\t StopBits: {_connectionConfig.StopBits}\n\n");
        
        var result = _serialPort.Open();
        
        if (result)
        {
            _rawDataSubscription = _serialPort.SubscribeRawData(OnRawDataReceived);
        }
        
        return result;
    }
    
    public bool Disconnect()
    {
        _systemLogger.LogInfo("Disconnect Serial");
        
        _rawDataSubscription?.Dispose();
        _rawDataSubscription = null;
        
        _bufferList.Clear();
        _canBufferAdd = false;
        
        return _serialPort.Close();
    }
    
    public async Task<bool> WriteAsync(string data)
    {
        if (!_serialPort.IsConnected) return false;
        
        byte[] parseData = _parser.ParseToBytes(data);
        var result = await _serialPort.WriteAsync(parseData);

        _systemLogger.LogInfo($"Serial Write Status: {result}" +
                              $"\t String Data: {data}\n" +
                              $"\t Parsed Data: {BitConverter.ToString(parseData)}\n\n");
        return result;
    }

    private void OnRawDataReceived(SerialMessage message)
    {
        // ReadMode에 따라 처리
        switch (_runtimeConfig.ReadMode)
        {
            case ReadModeType.NewLine:
                ProcessDataNewLine(message);
                break;
            case ReadModeType.StxEtx:
                ProcessDataStxEtx(message);
                break;
            case ReadModeType.Custom:
                ProcessDataStxEtx(message, _runtimeConfig.CustomStx, _runtimeConfig.CustomEtx);
                break;
        }
        _rateMonitor.RegisterMessage();
    }
    
    private void ProcessDataNewLine(SerialMessage message)
    {
        var buffer = message.Data;
        var list = _bufferList;
        
        foreach (var currentByte in buffer)
        {
            if (currentByte == SerialConstants.ControlCharacters.NEWLINE)
            {
                if (list.Count > 0 && list[^1] == SerialConstants.ControlCharacters.CARRIAGE_RETURN)
                {
                    list.RemoveAt(list.Count - 1);
                }

                var newBuffer = GetBufferFromList();
                var newMessage = new SerialMessage()
                {
                    Data = newBuffer,
                    DataSize = newBuffer.Length,
                    Type = message.Type,
                    Timestamp = message.Timestamp
                };
                
                RaiseMessageReceived(newMessage);
            }
            else
            {
                list.Add(currentByte);
            }
        }
    }

    private void ProcessDataStxEtx(
        SerialMessage message,
        byte stx = SerialConstants.ControlCharacters.STX,
        byte etx = SerialConstants.ControlCharacters.ETX
    )
    {
        var buffer = message.Data;

        var list = _bufferList;
        int requiredSize = _runtimeConfig.PacketSize;

        for (int i = 0; i < buffer.Length; i++)
        {
            byte currentByte = buffer[i];

            if (currentByte == stx)
            {
                _canBufferAdd = true;
                list.Add(currentByte);
                continue;
            }
            
            if(!_canBufferAdd) continue;
            
            list.Add(currentByte);

            if (currentByte == etx && list.Count >= requiredSize)
            {
                _canBufferAdd = false;

                var newBuffer = GetBufferFromList();
                var newMessage = new SerialMessage()
                {
                    Data = newBuffer,
                    DataSize = newBuffer.Length,
                    Type = message.Type,
                    Timestamp = message.Timestamp
                };
                
                RaiseMessageReceived(newMessage);
            }
        }
    }
    
    private byte[] GetBufferFromList()
    {
        byte[] bytes = new byte[_bufferList.Count];
        CollectionsMarshal.AsSpan(_bufferList).CopyTo(bytes);
        _bufferList.Clear();
        return bytes;
    }
    
    private void RaiseMessageReceived(ISerialMessage message)
    {
        var handler = MsgReceived;
        if (handler == null) return;

        var delegates = handler.GetInvocationList()
            .Cast<EventHandler<ISerialMessage>>();

        foreach (var d in delegates)
        {
            // 비동기 실행으로 처리 스레드 블로킹 방지
            Task.Run(() => d(this, message));
        }
    }
}