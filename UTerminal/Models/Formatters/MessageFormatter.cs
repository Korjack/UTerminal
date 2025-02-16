using System;
using System.Text;
using System.Threading;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Serial;

namespace UTerminal.Models.Formatters;

/// <summary>
/// Handles formatting of serial message data with various encoding options
/// </summary>
public class MessageFormatter
{
    private static readonly ThreadLocal<char[]> SharedBuffer = new(() => new char[4096]);

    public MessageFormatter()
    {
        
    }

    /// <summary>
    /// Formats an array of messages according to the specified encoding
    /// </summary>
    /// <param name="messages">Array of messages to format</param>
    /// <param name="count">Number of messages to process</param>
    /// <param name="format">Encoding format to use</param>
    /// <returns>Formatted string containing all messages</returns>
    public string FormatMessages(ISerialMessage[] messages, EncodingBytes format)
    {
        var builder = new StringBuilder(messages.Length * 64);
        var timeBuffer = new char[14];

        foreach (var msg in messages)
        {
            TimeFormatter.FormatTime(msg.Timestamp, timeBuffer);
            builder.Append(timeBuffer)
                .Append(' ')
                .Append(FormatData(msg.Data, format))
                .AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>
    /// Formats a single message according to the specified encoding
    /// </summary>
    /// <param name="data">Raw byte data to format</param>
    /// <param name="format">Encoding format to use</param>
    /// <returns>Formatted string representation of the data</returns>
    public string FormatData(byte[] data, EncodingBytes format)
    {
        return format switch
        {
            EncodingBytes.ASCII => StringFromBufferOptimized(data, Encoding.ASCII),
            EncodingBytes.HEX => HexFromBufferOptimized(data),
            EncodingBytes.UTF8 => StringFromBufferOptimized(data, Encoding.UTF8),
            _ => throw new ArgumentException($"Unsupported format: {format}")
        };
    }

    private static string StringFromBufferOptimized(byte[] data, Encoding encoding)
    {
        var charCount = encoding.GetCharCount(data);
        var buffer = SharedBuffer.Value;
        if (charCount > buffer!.Length)
        {
            buffer = new char[charCount];
            SharedBuffer.Value = buffer;
        }

        encoding.GetChars(data, 0, data.Length, buffer, 0);
        return new string(buffer, 0, charCount);
    }

    private static string HexFromBufferOptimized(byte[] data)
    {
        if (data.Length == 0) return string.Empty;

        var buffer = new char[data.Length * 3 - 1];
        for (int i = 0; i < data.Length; i++)
        {
            var value = data[i];
            var pos = i * 3;
            buffer[pos] = GetHexChar(value >> 4);
            buffer[pos + 1] = GetHexChar(value & 0xF);
            if (i < data.Length - 1)
                buffer[pos + 2] = ' ';
        }

        return new string(buffer);
    }

    private static char GetHexChar(int value)
    {
        return (char)(value < 10 ? '0' + value : 'A' + (value - 10));
    }
}