using System;
using System.Text;
using System.Threading;

namespace UTerminal.Models;

public class SerialStringManager
{
    private readonly SerialMessage[] _messageBuffer;
    private readonly StringBuilder _stringBuilder;
    private EncodingBytes _currentFormat;
    
    private readonly int _capacity;
    private int _head;
    private int _tail;
    private int _count;
    
    private static readonly char[] TimeFormatBuffer = new char[14]; // [HH:mm:ss.fff]
    private static readonly ThreadLocal<char[]> SharedBuffer = new(() => new char[4096]); // 문자열 변환을 위한 재사용 가능한 버퍼

    public SerialStringManager(int capacity, EncodingBytes initialFormat = EncodingBytes.ASCII)
    {
        if (capacity <= 0) throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));

        _capacity = capacity;
        _head = 0;
        _tail = 0;
        _count = 0;
        
        _messageBuffer = new SerialMessage[capacity];
        _stringBuilder = new StringBuilder(capacity * 64);
        _currentFormat = initialFormat;
    }

    /// <summary>
    /// Add Serail Message
    /// </summary>
    /// <param name="message"><see cref="SerialMessage"/> message</param>
    public void Add(SerialMessage message)
    {
        if (_count == _capacity)
        {
            _head = (_head + 1) % _capacity;
            _count--;
        }

        _messageBuffer[_tail] = message;
        _tail = (_tail + 1) % _capacity;
        _count++;
    }

    /// <summary>
    /// Chnage Format
    /// </summary>
    /// <remarks>
    /// Format List on the following type: <see cref="EncodingBytes"/>
    /// <list type="bullet">
    ///     <item><see cref="EncodingBytes.ASCII"/></item>
    ///     <item><see cref="EncodingBytes.HEX"/></item>
    /// </list>
    /// </remarks>
    /// <param name="newFormat"></param>
    public void ChangeFormat(EncodingBytes newFormat)
    {
        _currentFormat = newFormat;
    }
    
    /// <summary>
    /// Returns a string created from <see cref="_messageBuffer"/>
    /// </summary>
    /// <returns><see cref="string"/> buffers</returns>
    public string GetCurrentString()
    {
        if (_count == 0) return string.Empty;

        _stringBuilder.Clear();
        
        int index = _head;
        for (int i = 0; i < _count; i++)
        {
            FormatTimeToBuffer(_messageBuffer[index].Timestamp, TimeFormatBuffer);
            _stringBuilder.Append(TimeFormatBuffer)
                         .Append(' ')
                         .Append(FormatData(_messageBuffer[index].Data, _currentFormat))
                         .AppendLine();
            
            index = (index + 1) % _capacity;
        }

        return _stringBuilder.ToString();
    }

    /// <summary>
    /// Convert Receive Time to byte Buffer.
    /// </summary>
    /// <param name="time"><see cref="DateTime"/> type</param>
    /// <param name="buffer"><see cref="char"/> array buffer</param>
    private static void FormatTimeToBuffer(DateTime time, char[] buffer)
    {
        buffer[0] = '[';
        WriteDigits(time.Hour, buffer, 1);
        buffer[3] = ':';
        WriteDigits(time.Minute, buffer, 4);
        buffer[6] = ':';
        WriteDigits(time.Second, buffer, 7);
        buffer[9] = '.';
        
        int ms = time.Millisecond;
        buffer[10] = (char)('0' + ms / 100);
        buffer[11] = (char)('0' + (ms / 10) % 10);
        buffer[12] = (char)('0' + ms % 10);
        buffer[13] = ']';
    }

    private static void WriteDigits(int value, char[] buffer, int startIndex)
    {
        buffer[startIndex] = (char)('0' + (value / 10));
        buffer[startIndex + 1] = (char)('0' + (value % 10));
    }

    
    /// <summary>
    /// Convert byte to string by format
    /// </summary>
    /// <param name="data"><see cref="byte"/> array data</param>
    /// <param name="format"><see cref="EncodingBytes"/> format type</param>
    /// <returns>Converted <see cref="string"/> data</returns>
    /// <exception cref="ArgumentException">if unsupported format type, Throw exception</exception>
    private string FormatData(byte[] data, EncodingBytes format)
    {
        return format switch
        {
            EncodingBytes.ASCII => StringFromBufferOptimized(data, Encoding.ASCII),
            EncodingBytes.HEX => HexFromBufferOptimized(data),
            EncodingBytes.UTF8 => StringFromBufferOptimized(data, Encoding.UTF8),
            _ => throw new ArgumentException("Unsupported format")
        };
    }

    /// <summary>
    /// Convert Byte to ASCII
    /// </summary>
    /// <param name="data"><see cref="byte"/> array data</param>
    /// <param name="encoding"><see cref="Encoding"/> type</param>
    /// <returns>Converted <see cref="string"/> data</returns>
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

    /// <summary>
    /// Convert Byte to HEX
    /// </summary>
    /// <param name="data"><see cref="byte"/> array data</param>
    /// <returns>Converted Hex <see cref="string"/> data</returns>
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

    /// <summary>
    /// Convert int to Hex
    /// </summary>
    /// <param name="value"><see cref="int"/> type value</param>
    /// <returns><see cref="char"/> data</returns>
    private static char GetHexChar(int value)
    {
        return (char)(value < 10 ? '0' + value : 'A' + (value - 10));
    }

    public int Count => _count;
    public int Capacity => _capacity;
    public bool IsFull => _count >= _capacity;
}