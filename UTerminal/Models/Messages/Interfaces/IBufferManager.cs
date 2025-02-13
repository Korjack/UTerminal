using UTerminal.Models.Serial;

namespace UTerminal.Models.Messages.Interfaces;

public interface IBufferManager<T>
{
    /// <summary>
    /// Add an item to buffer
    /// </summary>
    /// <param name="item">The item to add to buffer</param>
    public void Add(T item);
    
    /// <summary>
    /// Creates and returns a copy of the current buffer contents.
    /// </summary>
    /// <returns>An array of <see cref="T"/> containing a copy of the current buffer contents</returns>
    public T[] GetSnapshot();
    
    /// <summary>
    /// Clear buffer
    /// </summary>
    public void Clear();
    
    public int Count { get; }
    public int Capacity { get; }
}