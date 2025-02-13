using System;
using System.Globalization;
using System.Text;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Messages.Types;
using UTerminal.Models.Serial;

namespace UTerminal.Models.Messages;

public class SerialMessage : ISerialMessage
{
    public byte[] Data { get; set; } = [];
    public int DataSize { get; set; } = 0;
    
    public string ErrorText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MessageType Type { get; set; }

    public override string ToString()
    {
        return
            $"[{Timestamp.ToString(CultureInfo.CurrentCulture)}][Type: {Type}][Size: {DataSize}]  {BitConverter.ToString(Data).Replace("-", " ")}";
    }

    public string ToString(EncodingBytes encoding, bool showDate = true)
    {
        var header =
            $"{(showDate ? $"[{Timestamp.ToString(CultureInfo.CurrentCulture)}]" : string.Empty)}[Type: {Type}][Size: {DataSize}]  ";
        return encoding switch
        {
            EncodingBytes.ASCII => header + Encoding.ASCII.GetString(Data),
            EncodingBytes.HEX => header + BitConverter.ToString(Data).Replace("-", " "),
            EncodingBytes.UTF8 => header + Encoding.UTF8.GetString(Data),
            _ => throw new ArgumentException("Unsupported format")
        };
    }
}