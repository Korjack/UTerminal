namespace UTerminal.Models.Monitoring.Interfaces;

public interface IMessageRateMonitor
{
    double CurrentRate { get; }

    void RegisterMessage();
    void Reset();
}