using System;
using UTerminal.Models.Messages.Types;

namespace UTerminal.Models.Messages.Interfaces;

public interface ISerialMessage
{
    /// <summary>
    /// <see cref="byte"/>[] - Serial Data
    /// </summary>
    public byte[] Data { get; set; }
    
    /// <summary>
    /// <see cref="int"/> - Serial Data Size
    /// </summary>
    public int DataSize { get; set; }
    
    /// <summary>
    /// <see cref="DateTime"/> - Time at which the serial data was received.
    /// </summary>
    public DateTime Timestamp { get; set; }
    
    /// <summary>
    /// Message Type
    /// </summary>
    /// <remarks><see cref="MessageType"/></remarks>
    public MessageType Type { get; set; }
}