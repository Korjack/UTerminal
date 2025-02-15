using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Monitoring;
using UTerminal.Models.Serial.Interfaces;
using UTerminal.Models.Utils;

namespace UTerminal.Models.Serial;

public class SerialService : ReactiveObject, ISerialService
{
    // Basic serial
    private readonly ISerialPort _serialPort;
    private readonly SerialDataParser _parser;
    private readonly MessageRateMonitor _rateMonitor;

    private readonly SerialConnectionConfiguration _connectionConfig;
    
    private readonly SystemLogger _systemLogger = SystemLogger.Instance;
    
    // Serial message receive event handler
    public event EventHandler<ISerialMessage>? MsgReceived;
    private readonly CancellationTokenSource _msgReadToken = new();
    
    public bool IsConnected => _serialPort.IsConnected;
    public double MessageRate => _rateMonitor.CurrentRate;      // message hz
    
    public SerialService(SerialConnectionConfiguration connectionConfig, SerialRuntimeConfiguration runtimeConfig)
    {
        _connectionConfig = connectionConfig;
        
        _serialPort = new SerialPortAdapter(connectionConfig, runtimeConfig);
        _parser = new SerialDataParser();
        _rateMonitor = new MessageRateMonitor();

        // When message received, Invoke Handler
        Task.Run(async () => await OnMessageReceived());
        
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
        
        return _serialPort.Open();
    }
    
    public bool Disconnect()
    {
        _systemLogger.LogInfo("Disconnect Serial");
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

    /// <summary>
    /// Monitors if there are any serial messages to read.
    /// </summary>
    private async Task OnMessageReceived()
    {
        var reader = _serialPort.GetReadChannel();
        var token = _msgReadToken.Token;
        
        if(reader == null) return;

        try
        {
            _systemLogger.LogInfo("Message Received Start");
            while (!_msgReadToken.IsCancellationRequested)
            {
                var message = await reader.ReadAsync(token);
                _rateMonitor.RegisterMessage();
                await RaiseEventAsync(message);
            }
        }
        catch (OperationCanceledException e)
        {
            _systemLogger.LogSystemError(e);
            _systemLogger.LogInfo("Message Received Canceled");
        }
    }
    
    /// <summary>
    /// Invoke a function connected to the EventHandler.
    /// </summary>
    /// <param name="message"><see cref="ISerialMessage"/></param>
    private Task RaiseEventAsync(ISerialMessage message)
    {
        var handler = MsgReceived;
        if (handler == null) return Task.CompletedTask;

        var delegates = handler.GetInvocationList()
            .Cast<EventHandler<ISerialMessage>>();

        foreach (var d in delegates)
        {
            // 각 핸들러를 비동기로 실행하지만 대기하지 않음
            Task.Run(() => d(this, message));
        }
        
        return Task.CompletedTask;
    }
}