using System;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using UTerminal.Models.Messages;
using UTerminal.Models.Messages.Types;
using UTerminal.Models.Serial.Interfaces;
using UTerminal.Models.Utils.Logger;

namespace UTerminal.Models.Serial;

public class SerialPortAdapter : ISerialPort
{
    // Basic Serial
    private readonly SerialPort _port;
    private SerialConnectionConfiguration _connectionConfig;
    
    // Raw Data Broadcasting
    private Action<SerialMessage>[] _rawDataSubscribers = [];
    private readonly ReaderWriterLockSlim _subscriberLock = new();
    private int _subscriberCount = 0;
    
    private readonly SystemLogger _systemLogger = SystemLogger.Instance;
    private CancellationTokenSource? _serialToken;

    public bool IsConnected => _port?.IsOpen ?? false;

    public SerialPortAdapter(SerialConnectionConfiguration connectionConfig)
    {
        _port = new SerialPort();
        _connectionConfig = connectionConfig;
        
        // Set default setting on init
        UpdatePortConfig();
        
        // When connection setting changed
        _connectionConfig.WhenAnyValue(
            x => x.PortName,
            x => x.BaudRate,
            x => x.Parity,
            x => x.DataBits,
            x => x.StopBits)
            .Subscribe(_ => UpdatePortConfig());
    }
    
    /// <summary>
    /// Subscribe to raw data stream
    /// </summary>
    public IDisposable SubscribeRawData(Action<SerialMessage> handler)
    {
        _subscriberLock.EnterWriteLock();
        try
        {
            // Array resize for performance
            var newArray = new Action<SerialMessage>[_subscriberCount + 1];
            Array.Copy(_rawDataSubscribers, newArray, _subscriberCount);
            newArray[_subscriberCount] = handler;
            _rawDataSubscribers = newArray;
            _subscriberCount++;
        }
        finally
        {
            _subscriberLock.ExitWriteLock();
        }
        
        return new Subscription(() => UnsubscribeRawData(handler));
    }
    
    private void UnsubscribeRawData(Action<SerialMessage> handler)
    {
        _subscriberLock.EnterWriteLock();
        try
        {
            var index = Array.IndexOf(_rawDataSubscribers, handler);
            if (index < 0) return;
            
            var newArray = new Action<SerialMessage>[_subscriberCount - 1];
            Array.Copy(_rawDataSubscribers, 0, newArray, 0, index);
            Array.Copy(_rawDataSubscribers, index + 1, newArray, index, _subscriberCount - index - 1);
            _rawDataSubscribers = newArray;
            _subscriberCount--;
        }
        finally
        {
            _subscriberLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Update port config from connection setting
    /// </summary>
    private void UpdatePortConfig()
    {
        if (!IsConnected)
        {
            _port.PortName = _connectionConfig.PortName;
            _port.BaudRate = _connectionConfig.BaudRate;
            _port.Parity = (Parity)_connectionConfig.Parity;
            _port.DataBits = (int)_connectionConfig.DataBits;
            _port.StopBits = (StopBits)_connectionConfig.StopBits;
        }
    }
    
    /// <summary>
    /// Open serial port
    /// </summary>
    public bool Open()
    {
        if (IsConnected) return false;

        try
        {
            _port.Open();
            _port.DiscardInBuffer();
            _port.DiscardOutBuffer();
            
            _serialToken = new CancellationTokenSource();
            Task.Run(async () => await StartReading(_serialToken.Token));
            
            return true;
        }
        catch (Exception e)
        {
            _systemLogger.LogSystemError(e);
        }

        return false;
    }

    /// <summary>
    /// Close serial port
    /// </summary>
    public bool Close()
    {
        if(!IsConnected) return false;
        
        _serialToken?.Cancel();
        _port.Close();
        
        return true;
    }
    
    /// <summary>
    /// Write serial data asynchronously
    /// </summary>
    public async Task<bool> WriteAsync(byte[] data)
    {
        if (!IsConnected) return false;

        try
        {
            await _port.BaseStream.WriteAsync(data);
            return true;
        }
        catch (Exception e)
        {
            _systemLogger.LogSystemError(e);
        }

        return false;
    }

    /// <summary>
    /// Read serial data and broadcast to all subscribers
    /// </summary>
    public async Task StartReading(CancellationToken token)
    {
        try
        {
            _systemLogger.LogInfo("Serial Read Ready.");
            while (!token.IsCancellationRequested)
            {
                int bufferSize = _port.BytesToRead;

                if (bufferSize > 0)
                {
                    byte[] buffer = new byte[bufferSize];
                    await _port.BaseStream.ReadExactlyAsync(buffer, 0, bufferSize, token);

                    var message = new SerialMessage()
                    {
                        Data = buffer,
                        DataSize = bufferSize,
                        Timestamp = DateTime.Now,
                        Type = MessageType.Received
                    };

                    // Broadcast raw data to all subscribers
                    BroadcastRawData(message);
                }
            }
        }
        catch (OperationCanceledException e)
        {
            _systemLogger.LogSystemError(e);
            _systemLogger.LogInfo("Serial Reading Stopped");
        }
        catch (Exception e)
        {
            _systemLogger.LogSystemError(e);
        }
    }
    
    /// <summary>
    /// Broadcast raw data to all subscribers with zero-copy optimization
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BroadcastRawData(SerialMessage message)
    {
        _subscriberLock.EnterReadLock();
        try
        {
            var count = _subscriberCount;
            var subscribers = _rawDataSubscribers;
            
            // 각 구독자에게 독립적인 복사본 전달
            for (int i = 0; i < count; i++)
            {
                subscribers[i](message);
            }
        }
        finally
        {
            _subscriberLock.ExitReadLock();
        }
    }
}