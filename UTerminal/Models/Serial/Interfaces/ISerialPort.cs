using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using UTerminal.Models.Messages.Interfaces;

namespace UTerminal.Models.Serial.Interfaces;

public interface ISerialPort
{
    /// <summary>
    /// Check connection status
    /// </summary>
    public bool IsConnected { get; }
    
    /// <summary>
    /// If necessary, you can configure the channel to receive serial data.
    /// </summary>
    /// <returns>null</returns>
    public ChannelReader<ISerialMessage>? GetReadChannel() => null;
    
    /// <summary>
    /// Serial open
    /// </summary>
    /// <returns><see cref="bool"/> - If success, true. If not, false</returns>
    public bool Open();

    /// <summary>
    /// Serial close
    /// </summary>
    /// <returns><see cref="bool"/> - If success, true. If not, false</returns>
    public bool Close();

    /// <summary>
    /// Write serial data
    /// </summary>
    /// <param name="data"><see cref="byte"/>[] - data</param>
    /// <returns>true or false by success</returns>
    public Task<bool> WriteAsync(byte[] data);

    /// <summary>
    /// A function to manually read serial data using asynchronous.
    /// </summary>
    /// <param name="token">A token to terminate when serial reception is finished.</param>
    public Task StartReading(CancellationToken token);
}