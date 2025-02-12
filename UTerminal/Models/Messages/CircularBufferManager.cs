using System;
using UTerminal.Models.Formatters;
using UTerminal.Models.Interfaces;

namespace UTerminal.Models;

public class CircularBufferManager : IBufferManager
{
    private readonly ISerialMessage[] _messageBuffer;
    private readonly object _lock = new();

    private int _count;
    private int _head;
    private int _tail;

    private readonly TimeFormatter _timeFormatter;
    private readonly MessageFormatter _messageFormatter;


    public CircularBufferManager(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than 0", nameof(capacity));

        Capacity = capacity;
        _messageBuffer = new ISerialMessage[capacity];
        _timeFormatter = new TimeFormatter();
        _messageFormatter = new MessageFormatter(_timeFormatter);

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

    public string GetCurrentString(SerialConstants.EncodingBytes format)
    {
        if (_count == 0) return string.Empty;

        ISerialMessage[] snapshot;
        int snapshotCount;

        lock (_lock)
        {
            snapshot = new ISerialMessage[_count];
            snapshotCount = _count;

            var index = _head;
            for (var i = 0; i < _count; i++)
            {
                snapshot[i] = _messageBuffer[index];
                index = (index + 1) % Capacity;
            }
        }

        return _messageFormatter.FormatMessages(snapshot, snapshotCount, format);
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