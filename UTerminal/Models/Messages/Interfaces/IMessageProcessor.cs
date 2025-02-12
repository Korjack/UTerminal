using System;

namespace UTerminal.Models.Interfaces;

public interface IMessageProcessor
{
    /// <summary>
    /// Process received serial message
    /// </summary>
    /// <param name="message">Raw message to process</param>
    void ProcessMessage(ISerialMessage message);
    
    /// <summary>
    /// Format current buffer content according to specified encoding
    /// </summary>
    /// <returns>Formatted string representation of buffer content</returns>
    string FormatBuffer();
    
    /// <summary>
    /// Change the message encoding format
    /// </summary>
    void ChangeFormat(SerialConstants.EncodingBytes format);
    
    /// <summary>
    /// Event raised when message buffer is updated
    /// </summary>
    event EventHandler<string> BufferUpdated;
}