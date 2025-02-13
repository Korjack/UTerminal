using System;
using System.Threading.Tasks;
using UTerminal.Models.Messages.Interfaces;

namespace UTerminal.Models.Serial.Interfaces;

public interface ISerialService
{
    /// <summary>
    /// Event handler for serial message passing
    /// </summary>
    public event EventHandler<ISerialMessage> MsgReceived;
    
    /// <summary>
    /// The state of the connector of the parent object
    /// </summary>
    public bool IsConnected { get; }

    /// <summary>
    /// Serial connect
    /// </summary>
    /// <returns><see cref="bool"/> - If success, true. If not, false</returns>
    public bool Connect();

    /// <summary>
    /// Serial disconnect
    /// </summary>
    /// <returns><see cref="bool"/> - If success, true. If not, false</returns>
    public bool Disconnect();

    /// <summary>
    /// Writes serial data asynchronously.
    /// </summary>
    /// <param name="data"><see cref="string"/> - $ Hex string or ASCII-based string data</param>
    /// <returns>true if write is successful</returns>
    public Task<bool> WriteAsync(string data);
}