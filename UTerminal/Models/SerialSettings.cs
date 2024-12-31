using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using ReactiveUI;

namespace UTerminal.Models;

/// <summary>
/// Baudrate 기본 설정값
/// </summary>
public enum BaudRateType
{
    [Description("600")] Baud600 = 600,
    [Description("1200")] Baud1200 = 1200,
    [Description("2400")] Baud2400 = 2400,
    [Description("4800")] Baud4800 = 4800,
    [Description("9600")] Baud9600 = 9600,
    [Description("14400")] Baud14400 = 14400,
    [Description("19200")] Baud19200 = 19200,
    [Description("28800")] Baud28800 = 28800,
    [Description("38400")] Baud38400 = 38400,
    [Description("56000")] Baud56000 = 56000,
    [Description("57600")] Baud57600 = 57600,
    [Description("115200")] Baud115200 = 115200,
    [Description("128000")] Baud128000 = 128000,
    [Description("256000")] Baud256000 = 256000
}

/// <summary>
/// Parity 기본 설정값
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
/// DataBits 기본 설정
/// </summary>
public enum DataBitsType
{
    [Description("5")] Bits5 = 5,
    [Description("6")] Bits6 = 6,
    [Description("7")] Bits7 = 7,
    [Description("8")] Bits8 = 8
}

public enum StopBitsType
{
    [Description("1")] Bits1 = StopBits.One,
    [Description("1.5")] Bits1_5 = StopBits.OnePointFive,
    [Description("2")] Bits2 = StopBits.Two
}


public class SerialSettings : ReactiveObject
{
    public string PortPath { get; set; } = string.Empty;

    private BaudRateType _baudRate = BaudRateType.Baud9600;
    public BaudRateType BaudRate
    {
        get => _baudRate;
        set => this.RaiseAndSetIfChanged(ref _baudRate, value);
    }

    private ParityType _parity = ParityType.ParityNone;
    public ParityType Parity
    {
        get => _parity;
        set => this.RaiseAndSetIfChanged(ref _parity, value);
    }

    private DataBitsType _dataBits = DataBitsType.Bits8;
    public DataBitsType DataBits
    {
        get => _dataBits;
        set => this.RaiseAndSetIfChanged(ref _dataBits, value);
    }

    private StopBitsType _stopBits = StopBitsType.Bits1;
    public StopBitsType StopBits
    {
        get => _stopBits; 
        set => this.RaiseAndSetIfChanged(ref _stopBits, value);
    }
    

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
}

public class OptionRadioItem : INotifyPropertyChanged
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
    
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}