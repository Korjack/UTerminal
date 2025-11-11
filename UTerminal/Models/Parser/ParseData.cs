using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UTerminal.Models.Parser;

/// <summary>
/// 파싱 데이터 클래스 (UI 바인딩 지원)
/// </summary>
public class ParseData : INotifyPropertyChanged
{
    private object? _parsedValue;
    private string _displayValue = "";

    public ParseDataType ParseDataType { get; }
    public string Name { get; set; }
    public int Length { get; set; } = 1;

    /// <summary>
    /// 파싱된 값
    /// </summary>
    public object? ParsedValue
    {
        get => _parsedValue;
        set
        {
            if (_parsedValue != value)
            {
                _parsedValue = value;
                OnPropertyChanged();
                UpdateDisplayValue();
            }
        }
    }

    /// <summary>
    /// UI에 표시할 값
    /// </summary>
    public string DisplayValue
    {
        get => _displayValue;
        private set
        {
            if (_displayValue != value)
            {
                _displayValue = value;
                OnPropertyChanged();
            }
        }
    }

    public ParseData(ParseDataType parseDataType, string name = "")
    {
        ParseDataType = parseDataType;
        Name = string.IsNullOrEmpty(name) ? parseDataType.ToString() : name;
    }

    /// <summary>
    /// 데이터 타입의 크기를 반환
    /// </summary>
    public int GetSize()
    {
        return ParseDataType switch
        {
            ParseDataType.STX => 1,
            ParseDataType.ETX => 1,
            ParseDataType.Int8 => 1,
            ParseDataType.UInt8 => 1,
            ParseDataType.Byte => Length,
            ParseDataType.Hex => Length,
            ParseDataType.Int16 => 2,
            ParseDataType.UInt16 => 2,
            ParseDataType.Int32 => 4,
            ParseDataType.UInt32 => 4,
            ParseDataType.Float => 4,
            ParseDataType.Double => 8,
            ParseDataType.String => Length,
            _ => 0
        };
    }

    /// <summary>
    /// 파싱된 값을 UI 표시용 문자열로 변환
    /// </summary>
    private void UpdateDisplayValue()
    {
        if (ParsedValue == null)
        {
            DisplayValue = "-";
            return;
        }

        DisplayValue = ParseDataType switch
        {
            ParseDataType.STX => $"0x{ParsedValue:X2}",
            ParseDataType.ETX => $"0x{ParsedValue:X2}",
            ParseDataType.Hex => BitConverter.ToString((byte[])ParsedValue).Replace("-", " "),
            ParseDataType.Byte => BitConverter.ToString((byte[])ParsedValue).Replace("-", " "),
            ParseDataType.Float => $"{ParsedValue:F2}",
            ParseDataType.Double => $"{ParsedValue:F4}",
            _ => ParsedValue.ToString() ?? "-"
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}