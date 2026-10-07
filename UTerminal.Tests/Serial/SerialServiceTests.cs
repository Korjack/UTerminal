using System.Collections.Concurrent;
using System.Text;
using UTerminal.Models.Messages.Interfaces;
using UTerminal.Models.Serial;

namespace UTerminal.Tests.Serial;

public class SerialServiceTests
{
    private static (SerialService service, FakeSerialPort port) Connected()
    {
        var port = new FakeSerialPort();
        var service = new SerialService(port, new SerialConnectionConfiguration(), new SerialRuntimeConfiguration());
        service.Connect();
        return (service, port);
    }

    [Fact(Skip = "A2 수정 전")]
    public void MsgReceived_PreservesPacketArrivalOrder()
    {
        // Arrange
        const int count = 2000;
        var (service, port) = Connected();
        var received = new ConcurrentQueue<string>();
        using var done = new CountdownEvent(count);
        service.MsgReceived += (_, msg) =>
        {
            received.Enqueue(Encoding.ASCII.GetString(msg.Data));
            done.Signal();
        };

        // Act
        for (var i = 0; i < count; i++)
        {
            port.Emit(Encoding.ASCII.GetBytes($"{i}\n"));
        }
        Assert.True(done.Wait(TimeSpan.FromSeconds(10)));

        // Assert
        Assert.Equal(Enumerable.Range(0, count).Select(i => i.ToString()), received);
    }

    [Theory]
    [InlineData("A\r\n", "A")]
    [InlineData("A\n", "A")]
    public void NewLineMode_SplitsOnLfAndDropsPrecedingCr(string input, string expected)
    {
        var (service, port) = Connected();
        ISerialMessage? message = null;
        using var done = new ManualResetEventSlim();
        service.MsgReceived += (_, msg) => { message = msg; done.Set(); };

        port.Emit(Encoding.ASCII.GetBytes(input));

        Assert.True(done.Wait(TimeSpan.FromSeconds(5)));
        Assert.Equal(expected, Encoding.ASCII.GetString(message!.Data));
    }
}
