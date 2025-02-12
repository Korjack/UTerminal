namespace UTerminal.Models.Interfaces;

public interface IBufferManager
{
    public void Add(ISerialMessage message);
    public string GetCurrentString(SerialConstants.EncodingBytes format);
    public void Clear();
    public int Count { get; }
    public int Capacity { get; }
}