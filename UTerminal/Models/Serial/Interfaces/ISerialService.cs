using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UTerminal.Models.Interfaces;

public interface ISerialService
{
    public event EventHandler<ISerialMessage> MsgReceived;
    public bool IsConnected { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool Connect();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public bool Disconnect();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Task<bool> WriteAsync(string data);
}