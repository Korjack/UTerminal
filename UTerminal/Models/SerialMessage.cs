using System;
using System.Globalization;

namespace UTerminal.Models;

public class SerialMessage
{
    public byte[] Data { get; set; } = [];
    public int DataSize { get; set; } = 0;
    public string Text { get; set; } = String.Empty;
    public DateTime Timestamp { get; set; }
    public MessageType Type { get; set; }

    public enum MessageType
    {
        Received,
        Sent,
        Error
    }

    public override string ToString()
    {
        return $"[{Timestamp.ToString(CultureInfo.CurrentCulture)}][{Type}] {Data}";
    }
}