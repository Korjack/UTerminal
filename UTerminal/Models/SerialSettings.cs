using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;

namespace UTerminal.Models;

public class SerialSettings
{
    public string PortPath { get; set; } = string.Empty;

    public int BaudRate
    {
        get => _baudRate;
        set
        {
            foreach (var baudRateItem in RadioBaudRateItems)
            {
                baudRateItem.IsSelected = baudRateItem.Value == value;
            }
            _baudRate = value;
        }
    }
    private int _baudRate = 9600;

    public Parity Parity
    {
        get => _parity;
        set
        {
            foreach (var parityItem in RadioParityItems)
            {
                parityItem.IsSelected = (Parity)parityItem.Value == value;
            }

            _parity = value;
        }
    }
    private Parity _parity = Parity.None;

    public int DataBits
    {
        get => _dataBits;
        set
        {
            foreach (var dataBitsItem in RadioDataBitsItems)
            {
                dataBitsItem.IsSelected = dataBitsItem.Value == value;
            }
            _dataBits = value;
        }
    }
    private int _dataBits = 8;

    public StopBits StopBits
    {
        get => _stopBits;
        set
        {
            foreach (var stopBitsItem in RadioStopBitsItems)
            {
                stopBitsItem.IsSelected = (StopBits)stopBitsItem.Value == value;
            }
            _stopBits = value;
        }
    }
    private StopBits _stopBits = StopBits.One;
    

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
    
    public ObservableCollection<OptionRadioItem> RadioBaudRateItems { get; } =
    [
        new() { Text = "600", Value = 600 },
        new() { Text = "1200", Value = 1200 },
        new() { Text = "2400", Value = 2400 },
        new() { Text = "4800", Value = 4800 },
        new() { Text = "9600", Value = 9600, IsSelected = true },
        new() { Text = "14400", Value = 14400 },
        new() { Text = "19200", Value = 19200 },
        new() { Text = "28800", Value = 28800 },
        new() { Text = "38400", Value = 38400 },
        new() { Text = "56000", Value = 56000 },
        new() { Text = "57600", Value = 57600 },
        new() { Text = "115200", Value = 115200 },
        new() { Text = "128000", Value = 128000 },
        new() { Text = "256000", Value = 256000 }
    ];
    
    public ObservableCollection<OptionRadioItem> RadioParityItems { get; } =
    [
        new() { Text = "none", Value = (int)Parity.None, IsSelected = true},
        new() { Text = "odd", Value = (int)Parity.Odd},
        new() { Text = "even", Value = (int)Parity.Even },
        new() { Text = "mark", Value = (int)Parity.Mark },
        new() { Text = "space", Value = (int)Parity.Space },
    ];
    
    public ObservableCollection<OptionRadioItem> RadioDataBitsItems { get; } =
    [
        new() { Text = "5", Value = 5 },
        new() { Text = "6", Value = 6 },
        new() { Text = "7", Value = 7 },
        new() { Text = "8", Value = 8, IsSelected = true }
    ];
    
    public ObservableCollection<OptionRadioItem> RadioStopBitsItems { get; } =
    [
        new() { Text = "1", Value = (int)StopBits.One, IsSelected = true},
        new() { Text = "1.5", Value = (int)StopBits.OnePointFive},
        new() { Text = "2", Value = (int)StopBits.Two },
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