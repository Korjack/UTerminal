using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ReactiveUI;
using UTerminal.Models.Messages;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Messages.Types;
using UTerminal.Models.Serial.Interfaces;

namespace UTerminal.Models.Serial;

public class SerialPortAdapter : ISerialPort
{
    // Basic Serial
    private readonly SerialPort _port;
    private SerialConnectionConfiguration _connectionConfig;
    private SerialRuntimeConfiguration _runtimeConfig;
    
    // Serial Data Handle
    private readonly Channel<ISerialMessage> _msgChannel;
    private readonly List<byte> _bufferList = [];
    
    private CancellationTokenSource _serialToken = null!;
    private bool _canBufferAdd;

    public ChannelReader<ISerialMessage> GetReadChannel() => _msgChannel.Reader;
    public bool IsConnected => _port?.IsOpen ?? false;

    public SerialPortAdapter(SerialConnectionConfiguration connectionConfig, SerialRuntimeConfiguration runtimeConfig)
    {
        _port = new SerialPort();
        _connectionConfig = connectionConfig;
        _runtimeConfig = runtimeConfig;
        
        // Init Channel
        _msgChannel = Channel.CreateUnbounded<ISerialMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        
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
    /// <returns>true if successfully opened</returns>
    public bool Open()
    {
        if (IsConnected) return false;

        try
        {
            _port.Open();
            
            // Clear Buffer before read data
            _port.DiscardInBuffer();
            
            _serialToken = new CancellationTokenSource();
            Task.Run(async () => await StartReading(_serialToken.Token));
            
            return true;
        }
        catch (Exception e)
        {
            // TODO: Send why disconnected
        }

        return false;
    }

    /// <summary>
    /// Cloase serial port
    /// </summary>
    /// <returns>true if successfully closed</returns>
    public bool Close()
    {
        if(!IsConnected) return false;
        
        _serialToken.Cancel();
        _port.Close();
        
        return true;
    }

    
    /// <summary>
    /// Writes serial data asynchronously. 
    /// </summary>
    /// <param name="data"><see cref="byte"/>[] - serial data</param>
    /// <returns>true if successfully write</returns>
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
            // TODO: Send why cant write
        }

        return false;
    }

    /// <summary>
    /// Manually read serial data using async. The read serial data is updated on the channel.
    /// </summary>
    /// <param name="token">Token to stop loop</param>
    public async Task StartReading(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                int bufferSize = _port.BytesToRead;

                if (bufferSize > 0)
                {
                    byte[] buffer = new byte[bufferSize];
                    await _port.BaseStream.ReadExactlyAsync(buffer, 0, bufferSize, token);

                    switch (_runtimeConfig.ReadMode)
                    {
                        case ReadModeType.NewLine:
                            await ProcessDataNewLine(buffer, token);
                            break;
                        case ReadModeType.StxEtx:
                            await ProcessDataStxEtx(buffer, token);
                            break;
                        case ReadModeType.Custom:
                            await ProcessDataStxEtx(buffer, token, _runtimeConfig.CustomStx, _runtimeConfig.CustomEtx);
                            break;
                    }
                }
            }
        }
        catch (OperationCanceledException e)
        {
            // 작업이 취소된 경우의 처리
        }
        catch (Exception e)
        {
            // 예외 처리
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
            if (currentByte == SerialConstants.ControlCharacters.NEWLINE)
            {
                if (_bufferList.Count > 0 && _bufferList[^1] == SerialConstants.ControlCharacters.CARRIAGE_RETURN)
                {
                    _bufferList.RemoveAt(_bufferList.Count - 1);
                }

                byte[] lineBytes = GetBufferFromList();
                            
                // 채널에 메시지 쓰기
                await _msgChannel.Writer.WriteAsync(new SerialMessage
                {
                    Data = lineBytes,
                    DataSize = lineBytes.Length,
                    Timestamp = DateTime.Now,
                    Type = MessageType.Received
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
    /// <param name="stx">(<see cref="byte"/>)If need customize STX</param>
    /// <param name="etx">(<see cref="byte"/>)If need customize ETX</param>
    private async Task ProcessDataStxEtx(byte[] buffer, CancellationToken token, byte stx = 0x02, byte etx = 0x03)
    {
        foreach (var currentByte in buffer)
        {
            // Buffer contain STX and ETX.
            if (currentByte == stx)
            {
                _canBufferAdd = true;
            }
            else if(currentByte == etx && _bufferList.Count >= _runtimeConfig.PacketSize - 1)
            {
                _canBufferAdd = false;
                _bufferList.Add(currentByte);
                
                byte[] data = GetBufferFromList();
                
                // Write message on channel
                await _msgChannel.Writer.WriteAsync(new SerialMessage
                {
                    Data = data,
                    DataSize = data.Length,
                    Timestamp = DateTime.Now,
                    Type = MessageType.Received
                }, token);
            }

            // Buffer adding
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
}