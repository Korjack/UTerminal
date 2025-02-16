using System;
using UTerminal.Models.Formatters;
using UTerminal.Models.Messages.Interfaces;

namespace UTerminal.Models.Messages;

public class CircularBufferManager : IBufferManager<ISerialMessage>
{
    private readonly ISerialMessage[] _messageBuffer;
    private readonly object _lock = new();
    
    private int _count;
    private int _head;
    private int _tail;
    
    public CircularBufferManager(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));

        Capacity = capacity;
        
        _messageBuffer = new ISerialMessage[capacity];

        _count = 0;
        _head = 0;
        _tail = 0;
    }
    
    public void Add(ISerialMessage message)
    {
        lock (_lock)
        {
            if (_count == Capacity)
            {
                _head = (_head + 1) % Capacity;
                _count--;
            }

            _messageBuffer[_tail] = message;
            _tail = (_tail + 1) % Capacity;
            _count++;
        }
    }

    public ISerialMessage[] GetSnapshot()
    {
        if (_count == 0) return [];

        lock (_lock)
        {
            // Create Buffer
            var snapshot = new ISerialMessage[_count];

            // Check that the current buffer size does not exceed its capacity
            if (_head + _count <= Capacity)
            {
                _messageBuffer.AsSpan(_head, _count).CopyTo(snapshot);
            }
            else
            {
                var part = Capacity - _head;
                _messageBuffer.AsSpan(_head, part).CopyTo(snapshot);
                _messageBuffer.AsSpan(0, _count - part).CopyTo(snapshot.AsSpan(part));
            }
            
            return snapshot;
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            Array.Clear(_messageBuffer, 0, _messageBuffer.Length);
            _head = 0;
            _tail = 0;
            _count = 0;
        }
    }

    public int Count
    {
        get
        {
            lock (_lock) return _count;
        }
    }

    public int Capacity { get; }
}