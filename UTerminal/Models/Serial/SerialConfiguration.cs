using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using ReactiveUI;

namespace UTerminal.Models;

public class SerialConnectionConfiguration : ReactiveObject
{
    public static class Defaults
    {
        public static int BaudRate => 9600;
        public static ParityType Parity => ParityType.ParityNone;
        public static DataBitsType DataBits => DataBitsType.Bits8;
        public static StopBitsType StopBits => StopBitsType.Bits1;
    }

    private string _portName = "COM1";
    public string PortName
    {
        get => _portName;
        set => this.RaiseAndSetIfChanged(ref _portName, value);
    }
    
    private BaudRateType _baudRate = Defaults.BaudRate;
    public BaudRateType BaudRate
    {
        get => _baudRate;
        set
        {
            if (value <= 0)
                throw new ArgumentException("Baud rate must be greater than 0");
            this.RaiseAndSetIfChanged(ref _baudRate, value);
        }
    }
    
    private ParityType _parity = Defaults.Parity;
    public ParityType Parity
    {
        get => _parity;
        set => this.RaiseAndSetIfChanged(ref _parity, value);
    }
    
    private DataBitsType _dataBits = Defaults.DataBits;
    public DataBitsType DataBits
    {
        get => _dataBits;
        set => this.RaiseAndSetIfChanged(ref _dataBits, value);
    }
    
    private StopBitsType _stopBits = Defaults.StopBits;
    public StopBitsType StopBits
    {
        get => _stopBits;
        set => this.RaiseAndSetIfChanged(ref _stopBits, value);
    }
}

public class SerialRuntimeConfiguration : ReactiveObject
{
    private ReadModeType _readMode = ReadModeType.NewLine;
    public ReadModeType ReadMode
    {
        get => _readMode;
        set => this.RaiseAndSetIfChanged(ref _readMode, value);
    }

    private byte _customStx;
    public byte CustomStx
    {
        get => _customStx;
        set => this.RaiseAndSetIfChanged(ref _customStx, value);
    }

    private byte _customEtx;
    public byte CustomEtx
    {
        get => _customEtx;
        set => this.RaiseAndSetIfChanged(ref _customEtx, value);
    }

    private int _packetSize;

    public int PacketSize
    {
        get => _packetSize;
        set => this.RaiseAndSetIfChanged(ref _packetSize, value);
    }
    public string PacketSizeText
    {
        get => _packetSize.ToString();
        set
        {
            if (int.TryParse(value, out int number))
            {
                this.RaiseAndSetIfChanged(ref _packetSize, number);
            }
        }
    }
}


/// <summary>
/// Baudrate Default Enum
/// </summary>
public class BaudRateType
{
    private readonly int _value;

    #region Static Fields

    public static implicit operator int(BaudRateType baudRateType) => baudRateType._value;
    public static implicit operator BaudRateType(int baudRate) => new(baudRate);
    public static IEnumerable<BaudRateType> StandardBaudRates
    {
        get
        {
            yield return Baud600;
            yield return Baud1200;
            yield return Baud2400;
            yield return Baud4800;
            yield return Baud9600;
            yield return Baud14400;
            yield return Baud19200;
            yield return Baud28800;
            yield return Baud38400;
            yield return Baud56000;
            yield return Baud57600;
            yield return Baud115200;
            yield return Baud128000;
            yield return Baud256000;
        }
    }

    #endregion

    #region Standard Baudrate

    public static readonly BaudRateType Baud600 = new (600);
    public static readonly BaudRateType Baud1200 = new (1200);
    public static readonly BaudRateType Baud2400 = new (2400);
    public static readonly BaudRateType Baud4800 = new (4800);
    public static readonly BaudRateType Baud9600 = new (9600);
    public static readonly BaudRateType Baud14400 = new (14400);
    public static readonly BaudRateType Baud19200 = new (19200);
    public static readonly BaudRateType Baud28800 = new (28800);
    public static readonly BaudRateType Baud38400 = new (38400);
    public static readonly BaudRateType Baud56000 = new (56000);
    public static readonly BaudRateType Baud57600 = new (57600);
    public static readonly BaudRateType Baud115200 = new (115200);
    public static readonly BaudRateType Baud128000 = new (128000);
    public static readonly BaudRateType Baud256000 = new (256000);

    #endregion
    
    public BaudRateType(int baudRate)
    {
        if (baudRate <= 0)
            throw new ArgumentException("Baud rate must be greater than 0", nameof(baudRate));

        _value = baudRate;
    }
    
    public override string ToString() => _value.ToString();
    public string DisplayValue => _value.ToString();

    #region Operators

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        
        return _value == ((BaudRateType)obj)._value;
    }
    public static bool operator ==(BaudRateType? left, BaudRateType? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(BaudRateType left, BaudRateType right)
    {
        return !(left == right);
    }

    #endregion
}

/// <summary>
/// Parity Default Enum
/// </summary>
public enum ParityType
{
    [Description("none")] ParityNone = Parity.None,
    [Description("odd")] ParityOdd = Parity.Odd,
    [Description("even")] ParityEven = Parity.Even,
    [Description("mark")] ParityMark = Parity.Mark,
    [Description("space")] ParitySpace = Parity.Space
}

/// <summary>
/// DataBits Default Enum
/// </summary>
public enum DataBitsType
{
    [Description("5")] Bits5 = 5,
    [Description("6")] Bits6 = 6,
    [Description("7")] Bits7 = 7,
    [Description("8")] Bits8 = 8
}

/// <summary>
/// StopBits Default Enum
/// </summary>
public enum StopBitsType
{
    [Description("1")] Bits1 = StopBits.One,
    [Description("1.5")] Bits1Point5 = StopBits.OnePointFive,
    [Description("2")] Bits2 = StopBits.Two
}

/// <summary>
/// Serial Encoding Types
/// </summary>
public enum EncodingBytes
{
    [Description("ASCII")] ASCII = 0,
    [Description("HEX")] HEX = 1,
    [Description("UTF8")] UTF8 = 2
}

public enum ReadModeType
{
    NewLine,
    StxEtx,
    Custom
}