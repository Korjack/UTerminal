using System;

namespace UTerminal.Models.Interfaces;

public interface ISerialMessage
{
    public byte[] Data { get; set; }
    public int DataSize { get; set; }
    public DateTime Timestamp { get; set; }
    public MessageType Type { get; set; }
}