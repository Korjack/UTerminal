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
