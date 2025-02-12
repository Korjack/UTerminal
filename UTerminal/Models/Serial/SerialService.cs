using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UTerminal.Models.Interfaces;

namespace UTerminal.Models;

public class SerialService : ISerialService
{
    private readonly ISerialPort _serialPort;
    private readonly SerialDataParser _parser;
    
    public event EventHandler<ISerialMessage>? MsgReceived;

    private readonly CancellationTokenSource _msgReadToken = new();
    
    public SerialService(SerialConnectionConfiguration connectionConfig, SerialRuntimeConfiguration runtimeConfig)
    {
        _serialPort = new SerialPortAdapter(connectionConfig, runtimeConfig);
        _parser = new SerialDataParser();

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
    
    public bool IsConnected => _serialPort.IsConnected;
}