using System;
using System.Text;
using System.Threading;
using UTerminal.Models.Interfaces;

namespace UTerminal.Models.Formatters;

/// <summary>
/// Handles formatting of serial message data with various encoding options
/// </summary>
public class MessageFormatter
{
    private static readonly ThreadLocal<char[]> SharedBuffer = new(() => new char[4096]);
    private readonly TimeFormatter _timeFormatter;

    public MessageFormatter(TimeFormatter timeFormatter)
    {
        _timeFormatter = timeFormatter ?? throw new ArgumentNullException(nameof(timeFormatter));
    }

    /// <summary>
    /// Formats an array of messages according to the specified encoding
    /// </summary>
    /// <param name="messages">Array of messages to format</param>
    /// <param name="count">Number of messages to process</param>
    /// <param name="format">Encoding format to use</param>
    /// <returns>Formatted string containing all messages</returns>
    public string FormatMessages(ISerialMessage[] messages, int count, SerialConstants.EncodingBytes format)
    {
        var builder = new StringBuilder(count * 64);
        var timeBuffer = new char[14];

        for (var i = 0; i < count; i++)
        {
            _timeFormatter.FormatTime(messages[i].Timestamp, timeBuffer);
            builder.Append(timeBuffer)
                .Append(' ')
                .Append(FormatData(messages[i].Data, format))
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
    public string FormatData(byte[] data, SerialConstants.EncodingBytes format)
    {
        return format switch
        {
            SerialConstants.EncodingBytes.ASCII => StringFromBufferOptimized(data, Encoding.ASCII),
            SerialConstants.EncodingBytes.HEX => HexFromBufferOptimized(data),
            SerialConstants.EncodingBytes.UTF8 => StringFromBufferOptimized(data, Encoding.UTF8),
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