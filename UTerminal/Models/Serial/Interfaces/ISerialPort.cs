using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DynamicData;

namespace UTerminal.Models.Interfaces;

public interface ISerialPort
{
    public bool IsConnected { get; }
    public ChannelReader<ISerialMessage>? GetReadChannel() => null;
    
    public bool Open();

    public bool Close();

    public Task<bool> WriteAsync(byte[] data);

    public Task StartReading(CancellationToken token);
}