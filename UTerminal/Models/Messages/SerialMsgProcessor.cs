using System;
using UTerminal.Models.Formatters;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Serial;
using UTerminal.Models.Utils.Logger;

namespace UTerminal.Models.Messages;

public class SerialMsgProcessor : IMessageProcessor<ISerialMessage>
{
    private readonly IBufferManager<ISerialMessage> _bufferManager;
    private readonly MessageFormatter _messageFormatter;

    private readonly SystemLogger _systemLogger = SystemLogger.Instance;
    private readonly SerialMsgLogger _msgLogManager = SerialMsgLogger.Instance;
    
    private EncodingBytes _currentFormat = EncodingBytes.ASCII;
    
    public event EventHandler<string>? BufferUpdated;
    public int BufferCapacity => _bufferManager.Capacity;

    public SerialMsgProcessor(int capacity)
    {
        _bufferManager = new CircularBufferManager(capacity);
        _messageFormatter = new MessageFormatter();
        
        _systemLogger.LogInfo("Message Processor Initialized");
    }
    
    public void ProcessMessage(ISerialMessage message)
    {
        _bufferManager.Add(message);

        // If serial logging started 
        if (_msgLogManager.IsStartLogging)
        {
            _msgLogManager.Info(((SerialMessage)message).ToString(_currentFormat, false));
        }
        
        OnBufferUpdated();
    }

    public string GetFormatBuffer()
    {
        var messages = _bufferManager.GetSnapshot();
        return _messageFormatter.FormatMessages(messages, _currentFormat);
    }
    
    public void OnBufferUpdated()
    {
        BufferUpdated?.Invoke(this, GetFormatBuffer());
    }

    /// <summary>
    /// Change message format
    /// </summary>
    /// <param name="format">type of <see cref="EncodingBytes"/></param>
    /// <remarks>
    /// <see cref="EncodingBytes"/> - A list of types that can be encoded.
    /// <list type="bullet">
    /// <item>ASCII</item>
    /// <item>HEX</item>
    /// <item>UTF-8</item>
    /// </list>
    /// </remarks>
    public void ChangeFormat(EncodingBytes format)
    {
        if (_currentFormat == format) return;
        
        _systemLogger.LogConfigurationChange("Message Decode Format", _currentFormat.ToString(), format.ToString());
        _currentFormat = format;
        OnBufferUpdated();
    }
}