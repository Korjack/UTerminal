using UTerminal.Models.Messages;
using UTerminal.Models.Serial.Interfaces;

namespace UTerminal.Tests.Serial;

/// <summary>
/// 하드웨어 없이 원시 데이터를 밀어 넣는 ISerialPort 대역.
/// </summary>
public sealed class FakeSerialPort : ISerialPort
{
    private Action<SerialMessage>? _handler;

    public bool IsConnected { get; private set; }

    public IDisposable SubscribeRawData(Action<SerialMessage> handler)
    {
        _handler = handler;
        return new UTerminal.Models.Serial.Subscription(() => _handler = null);
    }

    public bool Open() => IsConnected = true;

    public bool Close()
    {
        IsConnected = false;
        return true;
    }

    public Task<bool> WriteAsync(byte[] data) => Task.FromResult(IsConnected);

    public Task StartReading(CancellationToken token) => Task.CompletedTask;

    /// <summary>수신 루프가 청크 하나를 읽은 것처럼 구독자를 호출한다.</summary>
    public void Emit(params byte[] data) =>
        _handler?.Invoke(new SerialMessage { Data = data, DataSize = data.Length, Timestamp = DateTime.Now });
}
