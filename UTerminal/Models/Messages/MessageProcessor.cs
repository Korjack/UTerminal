using System;
using UTerminal.Models.Interfaces;

namespace UTerminal.Models;

public class MessageProcessor : IMessageProcessor
{
    private readonly IBufferManager _bufferManager;
    private SerialConstants.EncodingBytes _currentFormat = SerialConstants.EncodingBytes.ASCII;

    public event EventHandler<string>? BufferUpdated;

    public int BufferCapacity => _bufferManager.Capacity;

    public MessageProcessor(int capacity)
    {
        _bufferManager = new CircularBufferManager(capacity);
    }
    
    public void ProcessMessage(ISerialMessage message)
    {
        _bufferManager.Add(message);
        OnBufferUpdated();
    }

    public string FormatBuffer()
    {
        return _bufferManager.GetCurrentString(_currentFormat);
    }

    public void ChangeFormat(SerialConstants.EncodingBytes format)
    {
        if (_currentFormat == format) return;
        
        _currentFormat = format;
        OnBufferUpdated();
    }
    
    private void OnBufferUpdated()
    {
        BufferUpdated?.Invoke(this, FormatBuffer());
    }
}