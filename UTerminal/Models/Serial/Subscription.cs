using System;

namespace UTerminal.Models.Serial;

public class Subscription : IDisposable
{
    private readonly Action _unsubscribe;
    private bool _disposed;
    
    public Subscription(Action unsubscribe)
    {
        _unsubscribe = unsubscribe ?? throw new ArgumentNullException(nameof(unsubscribe));
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _unsubscribe();
        _disposed = true;
    }
}