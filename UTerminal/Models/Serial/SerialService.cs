using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Serial.Interfaces;

namespace UTerminal.Models.Serial;

public class SerialService : ISerialService
{
    // Basic serial
    private readonly ISerialPort _serialPort;
    private readonly SerialDataParser _parser;
    
    // Serial message receive event handler
    public event EventHandler<ISerialMessage>? MsgReceived;
    private readonly CancellationTokenSource _msgReadToken = new();
    
    public bool IsConnected => _serialPort.IsConnected;
    
    public SerialService(SerialConnectionConfiguration connectionConfig, SerialRuntimeConfiguration runtimeConfig)
    {
        _serialPort = new SerialPortAdapter(connectionConfig, runtimeConfig);
        _parser = new SerialDataParser();

        // When message received, Invoke Handler
        Task.Run(async () => await OnMessageReceived());
    }
    
    public bool Connect()
    {
        return _serialPort.Open();
    }
    
    public bool Disconnect()
    {
        return _serialPort.Close();
    }
    
    public async Task<bool> WriteAsync(string data)
    {
        if (!_serialPort.IsConnected) return false;
        
        byte[] parseData = _parser.ParseToBytes(data);
        await _serialPort.WriteAsync(parseData);

        return true;
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
            while (!_msgReadToken.IsCancellationRequested)
            {
                var message = await reader.ReadAsync(token);
                await RaiseEventAsync(message);
            }
        }
        catch (OperationCanceledException e)
        {
            // TODO: Message received event normaly canceled
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