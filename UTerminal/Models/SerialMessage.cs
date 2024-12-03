using System;

namespace UTerminal.Models;

public class SerialMessage
{
    public string Data { get; set; } = String.Empty;
    public DateTime Timestamp { get; set; }
    public MessageType Type { get; set; }

    public enum MessageType
    {
        Received,
        Sent,
        Error
    }
}