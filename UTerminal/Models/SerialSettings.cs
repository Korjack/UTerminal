using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using ReactiveUI;

namespace UTerminal.Models;

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

/// <summary>
/// Serial Read Type
/// </summary>
public enum ReadMode
{
    NewLine,            // 줄바꿈 기준
    STX_ETX,            // STX/ETX (0x02/0x03) 기준
    Custom
}

public class SerialSettings : ReactiveObject
{
    #region ComPort Settings

    public string PortPath { get; set; } = string.Empty;
    public ObservableCollection<OptionRadioItem> RadioComPortItems { get; } =
    [
        new() { Text = "1", Value = 1, IsEnabled = true, IsSelected = true },
        new() { Text = "2", Value = 2, IsEnabled = false },
        new() { Text = "3", Value = 3, IsEnabled = false },
        new() { Text = "4", Value = 4, IsEnabled = false },
        new() { Text = "5", Value = 5, IsEnabled = false },
        new() { Text = "6", Value = 6, IsEnabled = false },
        new() { Text = "7", Value = 7, IsEnabled = false },
        new() { Text = "8", Value = 8, IsEnabled = false },
        new() { Text = "9", Value = 9, IsEnabled = false },
        new() { Text = "10", Value = 10, IsEnabled = false }
    ];

    #endregion
    
    #region Fields

    private BaudRateType _baudRate = BaudRateType.Baud9600;
    private ParityType _parity = ParityType.ParityNone;
    private DataBitsType _dataBits = DataBitsType.Bits8;
    private StopBitsType _stopBits = StopBitsType.Bits1;

    private byte _customSTX = 0x00;
    private byte _customETX = 0x00;

    #endregion
    
    #region Properties
    
    public BaudRateType BaudRate
    {
        get => _baudRate;
        set => this.RaiseAndSetIfChanged(ref _baudRate, value);
    }
    
    public ParityType Parity
    {
        get => _parity;
        set => this.RaiseAndSetIfChanged(ref _parity, value);
    }
    
    public DataBitsType DataBits
    {
        get => _dataBits;
        set => this.RaiseAndSetIfChanged(ref _dataBits, value);
    }
    
    public StopBitsType StopBits
    {
        get => _stopBits; 
        set => this.RaiseAndSetIfChanged(ref _stopBits, value);
    }

    public byte CustomSTX
    {
        get => _customSTX;
        set => this.RaiseAndSetIfChanged(ref _customSTX, value);
    }
    public byte CustomETX
    {
        get => _customETX;
        set => this.RaiseAndSetIfChanged(ref _customETX, value);
    }
    
    #endregion
}


public sealed class OptionRadioItem : INotifyPropertyChanged
{
    private string? _text;
    private int _value;
    private bool _isSelected;
    private bool _isEnabled;
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public string? Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }
    }

    public int Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
    }

    
    public override string ToString()
    {
        return $"Text: {Text} / Value: {Value} / IsSelected: {IsSelected} / IsEnabled: {IsEnabled}";
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}