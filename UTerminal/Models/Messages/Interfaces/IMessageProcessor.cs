using System;
using UTerminal.Models.Serial;

namespace UTerminal.Models.Messages.Interfaces;

public interface IMessageProcessor<T>
{
    /// <summary>
    /// Event raised when message buffer is updated
    /// </summary>
    public event EventHandler<string> BufferUpdated;
    
    /// <summary>
    /// Process received serial message
    /// </summary>
    /// <param name="item">The item to process</param>
    void ProcessMessage(T item);
    
    /// <summary>
    /// Gives a buffer converted to a string.
    /// </summary>
    /// <returns>Formatted string representation of buffer content</returns>
    string GetFormatBuffer();

    /// <summary>
    /// Invoke a function that is attached to an event handler.
    /// </summary>
    void OnBufferUpdated();
}