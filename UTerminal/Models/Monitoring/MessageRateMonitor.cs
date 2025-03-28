using System.Diagnostics;
using UTerminal.Models.Monitoring.Interfaces;
using UTerminal.Models.Utils.Logger;

namespace UTerminal.Models.Monitoring;

public class MessageRateMonitor : IMessageRateMonitor
{
    private readonly Stopwatch _stopwatch = new();
    private readonly object _lock = new();
    
    private readonly SystemLogger _systemLogger = SystemLogger.Instance;

    private int _messageCount;

    private double _currentRate;
    public double CurrentRate
    {
        get
        {
            lock (_lock)
            {
                return _currentRate;
            }
        }
        
    }

    public MessageRateMonitor()
    {
        _stopwatch.Start();
        _systemLogger.LogInfo("Message Rate Monitor Initialized");
    }
    
    public void RegisterMessage()
    {
        lock (_lock)
        {
            _messageCount++;

            if (_stopwatch.ElapsedMilliseconds >= 1000)
            {
                _currentRate = _messageCount * 1000.0 / _stopwatch.ElapsedMilliseconds;
                _messageCount = 0;
                _stopwatch.Restart();
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _messageCount = 0;
            _currentRate = 0;
            _stopwatch.Restart();
        }
    }
}