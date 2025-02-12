using System;
using System.Globalization;
using System.Text;
using UTerminal.Models.Interfaces;

namespace UTerminal.Models;

public enum MessageType
{
    Received,
    Sent,
    Error
}

public class SerialMessage : ISerialMessage
{
    public byte[] Data { get; set; } = [];
    public int DataSize { get; set; } = 0;
    public string ErrorText { get; set; } = String.Empty;
    public DateTime Timestamp { get; set; }
    public MessageType Type { get; set; }

    public override string ToString()
    {
        return
            $"[{Timestamp.ToString(CultureInfo.CurrentCulture)}][Type: {Type}][Size: {DataSize}]  {BitConverter.ToString(Data).Replace("-", " ")}";
    }

    public string ToString(SerialConstants.EncodingBytes encoding, bool showDate = true)
    {
        var header =
            $"{(showDate ? $"[{Timestamp.ToString(CultureInfo.CurrentCulture)}]" : string.Empty)}[Type: {Type}][Size: {DataSize}]  ";
        return encoding switch
        {
            SerialConstants.EncodingBytes.ASCII => header + Encoding.ASCII.GetString(Data),
            SerialConstants.EncodingBytes.HEX => header + BitConverter.ToString(Data).Replace("-", " "),
            SerialConstants.EncodingBytes.UTF8 => header + Encoding.UTF8.GetString(Data),
            _ => throw new ArgumentException("Unsupported format")
        };
    }
}