using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DynamicData.Binding;
using ReactiveUI;
using UTerminal.Models.Interfaces;

namespace UTerminal.Models;

public class SerialPortAdapter : ISerialPort
{
    private readonly SerialPort _port;
    private SerialConnectionConfiguration _connectionConfig;
    private SerialRuntimeConfiguration _runtimeConfig;
    
    private readonly Channel<ISerialMessage> _msgChannel;
    public ChannelReader<ISerialMessage> GetReadChannel() => _msgChannel.Reader;
    private CancellationTokenSource _serialToken;
    
    private readonly List<byte> _bufferList;
    private bool _canBufferAdd;

    public bool IsConnected => _port?.IsOpen ?? false;

    public SerialPortAdapter(SerialConnectionConfiguration connectionConfig, SerialRuntimeConfiguration runtimeConfig)
    {
        _port = new SerialPort();
        _connectionConfig = connectionConfig;
        _runtimeConfig = runtimeConfig;
        _msgChannel = Channel.CreateUnbounded<ISerialMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });

        _bufferList = new List<byte>();
        
        UpdatePortConfig();

        _connectionConfig.WhenAnyValue(
            x => x.PortName,
            x => x.BaudRate,
            x => x.Parity,
            x => x.DataBits,
            x => x.StopBits)
            .Subscribe(_ => UpdatePortConfig());

        // _runtimeConfig.WhenAnyValue(
        //     x => x.ReadMode,
        //     x => x.CustomStx,
        //     x => x.CustomEtx,
        //     x => x.PacketSize)
        //     .Subscribe(_ => UpdateRuntimeConfig());
    }
    

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
    
    public bool Open()
    {
        if (IsConnected) return false;

        try
        {
            _port.Open();
            
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

    public bool Close()
    {
        if(!IsConnected) return false;
        
        _serialToken.Cancel();
        _port.Close();
        
        return true;
    }

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