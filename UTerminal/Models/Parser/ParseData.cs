using ReactiveUI;

namespace UTerminal.Models.Parser;

/// <summary>
/// 파싱 데이터 클래스 (UI 바인딩 지원)
/// </summary>
public sealed class ParseData : ReactiveObject
{
    private object? _parsedValue;
    private string _displayValue = "";

    public ParseDataType ParseDataType { get; }
    public int Size { get; }
    public string Name { get; set; }
    public int Length { get; } = 1;

    /// <summary>
    /// 파싱된 값
    /// </summary>
    public object? ParsedValue
    {
        get => _parsedValue;
        set
        {
            if (_parsedValue == value) return;
            
            this.RaiseAndSetIfChanged(ref _parsedValue, value);
            UpdateDisplayValue();
        }
    }

    /// <summary>
    /// UI에 표시할 값
    /// </summary>
    public string DisplayValue
    {
        get => _displayValue;
        private set => this.RaiseAndSetIfChanged(ref _displayValue, value);
    }

    public ParseData(ParseDataType parseDataType, string name = "", int length = 1)
    {
        ParseDataType = parseDataType;
        Length = length;
        
        Name = string.IsNullOrEmpty(name) ? parseDataType.ToString() : name;
        Size = CalculateSize();
    }

    /// <summary>
    /// 데이터 타입의 크기를 반환
    /// </summary>
    private int CalculateSize()
    {
        return ParseDataType switch
        {
            ParseDataType.STX => 1,
            ParseDataType.ETX => 1,
            ParseDataType.Int8 => 1,
            ParseDataType.UInt8 => 1,
            ParseDataType.Byte => Length,
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
            ParseDataType.Byte => ParsedValue.ToString() ?? "-",
            ParseDataType.Float => $"{ParsedValue:F2}",
            ParseDataType.Double => $"{ParsedValue:F4}",
            _ => ParsedValue.ToString() ?? "-"
        };
    }
}