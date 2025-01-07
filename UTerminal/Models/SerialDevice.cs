using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace UTerminal.Models;

public class SerialDevice : IDisposable
{
    #region Fields

    private SerialPort? _serialPort;
    private readonly SerialSettings _settings;

    #region Constants

    private const int NEWLINE_CHAR = 0x0A;
    private const int CARRIAGE_RETURN = 0x0D;
    private const int STX = 0x02;
    private const int ETX = 0x03;

    #endregion
    
    #region Data Process

    public event EventHandler<SerialMessage>? MessageReceived;      // 이벤트 연결 핸들러
    private ReadMode _currentMode = ReadMode.NewLine;
    private readonly List<byte> _bufferList = [];                   // 수신된 누적 바이트 버퍼 리스트
    
    // 메시치 처리 채널
    private readonly Channel<SerialMessage> _messageChannel = Channel.CreateUnbounded<SerialMessage>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });

    private bool _canBufferAdd = false;

    #endregion
    
    #region Tokens

    private CancellationTokenSource _serialTokenSource = new ();
    private CancellationTokenSource _channelTokenSource = new();

    #endregion
    
    #endregion

    #region Properties

    public SerialSettings SerialSettings => _settings;
    public bool IsConnected => _serialPort?.IsOpen ?? false;        // 시리얼 연결 여부
    public string[] SerialPortList { get; private set; } = [];      // 시리얼 연결 가능 목록
    
    public ReadMode CurrentMode
    {
        get => _currentMode;
        set
        {
            _bufferList.Clear();        // If Readmode changed, must be clear bufferlist
            _currentMode = value;
        }
    }

    #endregion
    
    #region Initialize & Dipose

    public SerialDevice()
    {
        _settings = new SerialSettings();
    }
    
    public SerialDevice(SerialSettings settings)
    {
        _settings = settings;
    }
    
    public void Dispose()
    {
        Disconnect();
        _serialPort?.Dispose();
    }

    #endregion
    
    #region SerialConnect

    /// <summary>
    /// Connect Serial
    /// </summary>
    /// <returns>Return <see cref="bool"/> type connect status</returns>
    public bool Connect()
    {
        if (IsConnected) return false;
        
        try
        {
            _serialPort = new SerialPort(
                _settings.PortPath,
                (int)_settings.BaudRate,
                (Parity)_settings.Parity,
                (int)_settings.DataBits,
                (StopBits)_settings.StopBits
            );

            if (_serialTokenSource.IsCancellationRequested) _serialTokenSource = new CancellationTokenSource();
            if (_channelTokenSource.IsCancellationRequested) _channelTokenSource = new CancellationTokenSource();
            
            _serialPort.Open();

            Task.Run(async () => await ProcessMessageAsync(_channelTokenSource, _serialTokenSource.Token));
            Task.Run(async () => await SerialPort_DataReceivedAsync(_serialTokenSource.Token));
            
            return true;
        }
        catch (Exception e)
        {
            _ = OnMessageReceived(new SerialMessage 
            { 
                ErrorText = e.Message,
                Timestamp = DateTime.Now,
                Type = SerialMessage.MessageType.Error
            });
            return false;
        }
    }

    /// <summary>
    /// Disconnect Serial
    /// </summary>
    public void Disconnect()
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Close();
            _serialTokenSource.Cancel();
        }
    }

    #endregion

    #region Serial Data Process

    /// <summary>
    /// Process serial data asynchronously.
    /// </summary>
    private async Task SerialPort_DataReceivedAsync(CancellationToken token)
    {
        if(_serialPort == null) return;     // Init 여부 확인
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                int bufferSize = _serialPort.BytesToRead;
                if (bufferSize > 0)
                {
                    byte[] buffer = new byte[bufferSize];
                    await _serialPort.BaseStream.ReadExactlyAsync(buffer, 0, bufferSize, token);

                    switch (_currentMode)
                    {
                        case ReadMode.NewLine:
                            await ProcessDataNewLine(buffer, token);
                            break;
                        case ReadMode.STX_ETX:
                            await ProcessDataStxEtx(buffer, token);
                            break;
                        case ReadMode.Custom:
                            throw new NotImplementedException("Not support custom yet");
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 작업이 취소된 경우의 처리
        }
        catch (Exception ex)
        {
            // 예외 처리
            Debug.WriteLine($"Error reading from serial port: {ex.Message}");
        }
    }


    /// <summary>
    /// Function to process buffer at each newline 
    /// </summary>
    /// <param name="buffer"><see cref="byte"/>[] Buffer data</param>
    /// <param name="token"><see cref="CancellationToken"/> When serial canceled, cancel it</param>
    private async Task ProcessDataNewLine(byte[] buffer, CancellationToken token)
    {
        // 읽은 데이터를 처리합니다
        foreach (var currentByte in buffer)
        {
            // 줄바꿈 여부 확인
            if (currentByte == NEWLINE_CHAR)
            {
                if (_bufferList.Count > 0 && _bufferList[^1] == CARRIAGE_RETURN)
                {
                    _bufferList.RemoveAt(_bufferList.Count - 1);
                }

                byte[] lineBytes = GetBufferFromList();
                            
                // 채널에 메시지 쓰기
                await _messageChannel.Writer.WriteAsync(new SerialMessage
                {
                    Data = lineBytes,
                    DataSize = lineBytes.Length,
                    Timestamp = DateTime.Now,
                    Type = SerialMessage.MessageType.Received
                }, token);
            }
            else
            {
                _bufferList.Add(currentByte);
            }
        }
    }

    
    /// <summary>
    /// Function to process buffer at each [STX ... ETX]
    /// </summary>
    /// <param name="buffer"><see cref="byte"/>[] Buffer data</param>
    /// <param name="token"><see cref="CancellationToken"/> When serial canceled, cancel it</param>
    private async Task ProcessDataStxEtx(byte[] buffer, CancellationToken token)
    {
        foreach (var currentByte in buffer)
        {
            switch (currentByte)
            {
                // Buffer contain STX and ETX.
                case STX:
                    _canBufferAdd = true;
                    break;
                case ETX:
                {
                    _canBufferAdd = false;
                    _bufferList.Add(currentByte);
                
                    byte[] data = GetBufferFromList();
                
                    // 채널에 메시지 쓰기
                    await _messageChannel.Writer.WriteAsync(new SerialMessage
                    {
                        Data = data,
                        DataSize = data.Length,
                        Timestamp = DateTime.Now,
                        Type = SerialMessage.MessageType.Received
                    }, token);
                    break;
                }
            }

            if (_canBufferAdd)
            {
                _bufferList.Add(currentByte);
            }
        }
    }

    
    /// <summary>
    /// Return <see cref="byte"/>[] from <see cref="List{Byte}"/> and Clear List
    /// </summary>
    /// <returns><see cref="byte"/>[]</returns>
    private byte[] GetBufferFromList()
    {
        byte[] bytes = new byte[_bufferList.Count];
        CollectionsMarshal.AsSpan(_bufferList).CopyTo(bytes);
                
        _bufferList.Clear();

        return bytes;
    }
    
    /// <summary>
    /// Asynchronously processes message data periodically when connecting to a serial connection.
    /// </summary>
    private async Task ProcessMessageAsync(CancellationTokenSource channelTokenSource, CancellationToken serialToken)
    {
        var queueToken = channelTokenSource.Token;
        var reader = _messageChannel.Reader;

        try 
        {
            while (!queueToken.IsCancellationRequested)
            {
                var message = await reader.ReadAsync(queueToken);
                await OnMessageReceived(message);

                if (serialToken.IsCancellationRequested && reader.Count == 0)
                {
                    await channelTokenSource.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // 정상적인 종료 처리
        }
    }
    
    /// <summary>
    /// Calls a function attached to an event asynchronously.
    /// </summary>
    /// <param name="message"><see cref="SerialMessage"/></param>
    private Task OnMessageReceived(SerialMessage message)
    {
        var handler = MessageReceived;
        if (handler != null)
        {
            var delegates = handler.GetInvocationList()
                .Cast<EventHandler<SerialMessage>>();

            // Fire and forget 방식으로 실행
            foreach (var d in delegates)
            {
                _ = Task.Run(() => d(this, message));
            }
        }

        return Task.CompletedTask;
    }
    
    #endregion
    
    #region Public Method
    
    /// <summary>
    /// Update the serial path in Settings.
    /// </summary>
    public void GetPortPaths()
    {
        SerialPortList = SerialPort.GetPortNames();
        string[] portList = SerialPortList.Select(port => port.Replace("/dev/", "")).ToArray();
        
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
            radioItem.Value = i;
        }
    }

    /// <summary>
    /// Set ComPort path
    /// </summary>
    /// <param name="port"><see cref="string"/> port name</param>
    public void SetPortPath(string port)
    {
        _settings.PortPath = port;
    }

    public async Task<bool> WriteAsync(byte[] data)
    {
        if(_serialPort == null) return false;
        
        await _serialPort.BaseStream.WriteAsync(data, _serialTokenSource.Token);

        return true;
    }
    
    #endregion
    
}