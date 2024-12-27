using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace UTerminal.Models;


public class SerialDevice : IDisposable
{
    
    #region 변수 목록

    #region 시리얼 포트 변수

    private SerialPort? _serialPort;
    private readonly SerialSettings _settings;
    
    public bool IsConnected => _serialPort?.IsOpen ?? false;        // 시리얼 연결 여부
    
    public string[] SerialPortList { get; private set; }            // 시리얼 연결 가능 목록

    #endregion

    #region 데이터 처리 변수
    
    public event EventHandler<SerialMessage>? MessageReceived;      // 이벤트 연결 핸들러
    private CancellationTokenSource _cancellationTokenSource;
    
    private readonly ConcurrentQueue<SerialMessage> _messages = new();      // 메시지 처리 큐
    private readonly List<byte> _bufferList = [];                        // 수신된 누적 바이트 버퍼 리스트
    
    
    /// <summary>
    /// 시리얼 읽기 모드 설정
    /// </summary>
    private readonly ReadMode _currentMode = ReadMode.NewLine;
    public enum ReadMode
    {
        NewLine,            // 줄바꿈 기준
        STX_ETX,            // STX/ETX (0x02/0x03) 기준
    }
    
    #endregion

    #endregion
    
    
    public SerialDevice(SerialSettings settings)
    {
        _settings = settings;
        _cancellationTokenSource = new CancellationTokenSource();
    }
    
    
    /// <summary>
    /// 시리얼을 연결합니다.
    /// </summary>
    /// <returns><see cref="bool"/> 연결 상태</returns>
    public bool Connect()
    {
        if (IsConnected) return false;
        
        try
        {
            _serialPort = new SerialPort(
                _settings.PortPath,
                _settings.BaudRate,
                _settings.Parity,
                _settings.DataBits,
                _settings.StopBits
            );

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();    // 토큰 초기화
            }
            
            _serialPort.Open();
            _ = ProcessMessageAsync(_cancellationTokenSource.Token);            // 큐 메시지 처리 비동기 함수
            _ = SerialPort_DataReceivedAsync(_cancellationTokenSource.Token);   // 시리얼 데이터 수신 처리 함수

            return true;
        }
        catch (Exception e)
        {
            _ = OnMessageReceived(new SerialMessage 
            { 
                Text = e.Message,
                Timestamp = DateTime.Now,
                Type = SerialMessage.MessageType.Error
            });
            return false;
        }
    }

    /// <summary>
    /// 시리얼 연결을 해제합니다.
    /// </summary>
    public void Disconnect()
    {
        if (_serialPort?.IsOpen == true)
        {
            _cancellationTokenSource.Cancel();
            _serialPort.Close();
        }
    }

    
    /// <summary>
    /// 실제 데이터를 받아서 처리하는 부분
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

                    // 읽은 데이터를 처리합니다
                    foreach (var currentByte in buffer)
                    {
                        // 줄바꿈 여부 확인
                        if (currentByte == 0x0A)
                        {
                            if (_bufferList.Count > 0 && _bufferList[^1] == 0x0D)
                            {
                                _bufferList.RemoveAt(_bufferList.Count - 1);
                            }
                    
                            byte[] lineBytes = _bufferList.ToArray();
                            _bufferList.Clear();
                    
                            // 큐에 메시지 데이터 담기
                            _messages.Enqueue(new SerialMessage
                            {
                                Data = lineBytes,
                                DataSize = lineBytes.Length,
                                Timestamp = DateTime.Now,
                                Type = SerialMessage.MessageType.Received
                            });
                        }
                        else
                        {
                            _bufferList.Add(currentByte);
                        }
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
    /// 설정에 있는 시리얼 경로 정보를 업데이트합니다.
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
    /// 포트 경로를 설정합니다.
    /// </summary>
    /// <param name="port">시리얼 포트 경로</param>
    public void SetPortPath(string port)
    {
        _settings.PortPath = port;
    }


    /// <summary>
    /// 시리얼 연결시 주기적으로 메시지 데이터를 비동기로 처리합니다.
    /// </summary>
    /// <param name="token">Token</param>
    private async Task ProcessMessageAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (_messages.TryDequeue(out var message))
            {
                await OnMessageReceived(message);
            }
            else
            {
                await Task.Delay(50, token);
            }
        }
    }
    
    /// <summary>
    /// 이벤트에 연결된 함수에 비동기로 호출합니다.
    /// </summary>
    /// <param name="message"><see cref="SerialMessage"/> 메시지</param>
    private async Task OnMessageReceived(SerialMessage message)
    {
        var handler = MessageReceived;
        if (handler != null)
        {
            var delegates = handler.GetInvocationList()
                .Cast<EventHandler<SerialMessage>>();

            var tasks = delegates.Select(d => Task.Run(() => d(this, message)));
            await Task.WhenAll(tasks);
        }
    }

    
    public void Dispose()
    {
        Disconnect();
        _cancellationTokenSource.Cancel();
        _serialPort?.Dispose();
    }
}